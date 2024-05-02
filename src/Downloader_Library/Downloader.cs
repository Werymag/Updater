using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace Updater
{
    public class Downloader
    {
        /// <summary>
        /// Event when file downloaded, copied or failed (null)   
        /// </summary>
        public event Action<bool?, string, int, int>? FileDownload;

        public event Func<bool>? IsAgreeToKillProcess;

        /// <summary>
        /// Path to exe program file
        /// </summary>
        public readonly string ProgramPath;
        /// <summary>
        /// Program Name
        /// </summary>
        public readonly string ProgramName;

        /// <summary>
        /// Special directory for storage version
        /// </summary>
        public readonly string VersionsDirectory;
        /// <summary>
        /// Directory for download
        /// </summary>
        public readonly string DownloadDirectory;
        /// <summary>
        /// Program directory
        /// </summary>
        public readonly string ProgramDirectory;

        /// <summary>
        /// Current version backup
        /// </summary>
        public readonly string BackupOldVersionDirectory;

        /// <summary>
        /// Current version exe file
        /// </summary>
        public readonly Version CurrentVersion;

        /// <summary>
        /// Server address
        /// </summary>
        public readonly string Url;

        /// <summary>
        /// How many old version storage
        /// </summary>
        private const int _versionHistory = 4;

        /// <summary>
        /// Directory where last version is downloading 
        /// </summary>
        private string LastVersionDirectory = "";

        public Downloader(string programPath, string url)
        {
            var appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            ProgramPath = programPath;
            ProgramName = Path.GetFileNameWithoutExtension(programPath);
            VersionsDirectory = $"{appDataDirectory}\\{ProgramName}\\Versions\\";
            DownloadDirectory = $"{VersionsDirectory}\\Download\\";
            ProgramDirectory = Path.GetDirectoryName(programPath) ?? throw new Exception("Wrong path to .exe file");
            string? version = FileVersionInfo.GetVersionInfo(programPath).FileVersion
                ?? throw new Exception("Could not determine the application version");
            CurrentVersion = new Version(version);
            BackupOldVersionDirectory = $"{VersionsDirectory}\\{CurrentVersion}\\";
            Url = url;
        }


        /// <summary>
        /// Update program to new version
        /// </summary>
        public async Task<(bool IsSuccess, string Message)> DownloadLastVersionAsync()
        {
            try
            {
                var lastVersion = await GetLastVersionAsync();
                if (lastVersion is null || lastVersion <= CurrentVersion) return (false, "Update does not need");

                LastVersionDirectory = PrepareDirectories(lastVersion);
                await DownloadNewVersionAsync(lastVersion);
                return (true, "Download success");

                //await KillAllProcess();
                //var result = MoveNewVersionToProgramDirectory(lastVersionDirectory);
                //return result;
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        /// <summary>
        /// Update program to new version
        /// </summary>
        public async Task<(bool IsSuccess, string Message)> CopyFileToSourceAsync(bool? isKillAllProcess = null)
        {
            try
            {
                await KillAllProcess(isKillAllProcess);
                return MoveNewVersionToProgramDirectory(LastVersionDirectory);         
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        /// <summary>
        /// Check is program need update
        /// </summary>   
        /// <returns>Is Update Need</returns>
        public async Task<bool?> IsUpdateNeedAsync()
        {
            string? version = FileVersionInfo.GetVersionInfo(ProgramPath).FileVersion;
            if (version is null) return null;
            var currentVersion = new Version(version);
            var lastVersion = await GetLastVersionAsync();

            return lastVersion != null ? (lastVersion > currentVersion) : null;
        }

        /// <summary>
        /// Check working process
        /// </summary>
        /// <returns></returns>
        public bool IsProgramHaveWorkingProcess()
        {
            return GetWorkingProgramProcess().Count() > 0;
        }

        /// <summary>
        /// Kill all process have the same name
        /// </summary>
        private async Task KillAllProcess(bool? isAgreeToKillProcess = null)
        {
            List<Process> programProcesses;

            // 10 attempts to kill process
            await Task.Run(() =>
            {
                int counter = 0;
                do
                {
                    programProcesses = GetWorkingProgramProcess();

                    if (programProcesses.Count > 0)
                    {
                        isAgreeToKillProcess ??= IsAgreeToKillProcess?.Invoke();
                        if (isAgreeToKillProcess == true)
                        {
                            foreach (Process process in programProcesses)
                            { process.Kill(); }
                        }
                        if (isAgreeToKillProcess == false) throw new Exception("Need close programs before update");
                    }
                    Task.Delay(500);
                    programProcesses = GetWorkingProgramProcess();
                    counter++;
                } while (programProcesses.Count > 0 && counter < 10);

                if (programProcesses.Count > 0) throw new Exception("Can't close the programs");
            });
        }

        private List<Process> GetWorkingProgramProcess()
        {
            return Process.GetProcesses()
                           .Where(p => p.ProcessName?.ToLower() == ProgramName?.ToLower())
                           .ToList();
        }


        /// <summary>
        /// Uploading a new version to AppData/Roaming/{program} and then updating the program files in the program directory
        /// </summary>
        /// <param name="lastVersion">Last available version</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>

        public async Task DownloadNewVersionAsync(Version lastVersion)
        {
            var fileList = await GetFilesListWithHashAsync(lastVersion)
                ?? throw new Exception("FileList does not downloaded");
            for (int i = 0; i < fileList.Count; i++)
            {
                VersionInfo? file = fileList[i];
                var oldVersionFilePath = $"{ProgramDirectory}\\{file.FileName}";
                var downloadFilePath = $"{DownloadDirectory}\\{file.FileName}";
                if (File.Exists(oldVersionFilePath))
                {
                    // If file has same hash - copy it
                    var md5Hash = await CreateMd5HashStringForFileAsync(oldVersionFilePath);
                    if (md5Hash == file.Md5Hash)
                    {
                        var directoryName = Path.GetDirectoryName(downloadFilePath);
                        if (directoryName is not null) Directory.CreateDirectory(directoryName);
                        File.Copy(oldVersionFilePath, downloadFilePath);
                        FileDownload?.Invoke(false, file.FileName, i + 1, fileList.Count);
                        continue;
                    }
                }

                bool isDownloadSucceed = await DownloadFileFromUrlAsync(lastVersion, file.FileName);

                FileDownload?.Invoke(isDownloadSucceed ? true : null, file.FileName, i + 1, fileList.Count);
            }

            // Copy system uninstall files
            var uninstallFiles = Directory.GetFiles(ProgramDirectory).Where(p => p.Contains("unins", StringComparison.InvariantCultureIgnoreCase));
            foreach (var uninstallFile in uninstallFiles)
            {
                File.Copy(uninstallFile, $"{DownloadDirectory}\\{Path.GetFileName(uninstallFile)}");
            }
        }


        /// <summary>
        /// Clear and create directories
        /// </summary>
        /// <param name="lastAvailableVersion"></param>
        /// <returns></returns>
        private string PrepareDirectories(Version lastAvailableVersion)
        {
            if (!Path.Exists(VersionsDirectory)) Directory.CreateDirectory(VersionsDirectory);

            // Early downloaded versions
            var earlyDownloadedVersions = Directory.GetDirectories(VersionsDirectory)
                .Where(s => Version.TryParse(Path.GetFileName(s), out _))
                .Select(s => new Version(Path.GetFileName(s)))
                .Order().ToList();

            // If the current version already exists, but is not installed, delete and download again to avoid problems
            if (earlyDownloadedVersions.Contains(lastAvailableVersion)) Directory.Delete($"{VersionsDirectory}\\{lastAvailableVersion}", true);

            // Delete last versions directory
            for (int i = 0; i < earlyDownloadedVersions.Count - (_versionHistory + 1); i++)
                Directory.Delete($"{VersionsDirectory}\\{earlyDownloadedVersions[i]}", true);

            var downloadDirectory = $"{VersionsDirectory}\\Download\\";

            if (Directory.Exists(downloadDirectory)) Directory.Delete(downloadDirectory, true);
            Directory.CreateDirectory(downloadDirectory);

            // Backup current version if does not exist         
            if (!Path.Exists(BackupOldVersionDirectory))
            {
                CopyAllDataToDirectory(ProgramDirectory, BackupOldVersionDirectory, true);
            }


            var lastVersionDirectory = $"{VersionsDirectory}\\{lastAvailableVersion}\\";
            return lastVersionDirectory;
        }

        /// <summary>
        /// Move downloaded files to program directory
        /// </summary>
        /// <param name="lastVersionDirectory">Directory with last version files</param>
        public (bool IsSuccess, string Message) MoveNewVersionToProgramDirectory(string lastVersionDirectory)
        {
            Directory.Move(DownloadDirectory, lastVersionDirectory);
            try
            {
                ClearDirectory(ProgramDirectory);
            }
            catch (Exception e)
            {
                // If can't clear old version directory return old data
                CopyAllDataToDirectory(BackupOldVersionDirectory, ProgramDirectory, true);
                return (false, e.Message);
            }

            CopyAllDataToDirectory(lastVersionDirectory, ProgramDirectory, true);
            return (true, "Ok");
        }

        /// <summary>
        /// Get last version 
        /// </summary> 
        /// <returns>Version or null if program does not exist</returns>
        private async Task<Version?> GetLastVersionAsync()
        {
            var httpClient = new HttpClient();
            using HttpRequestMessage request =
                new(HttpMethod.Get, $"{Url}/Version/GetActualVersion?program={ProgramName}");
            var response = await httpClient.SendAsync(request);
            var version = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode ? new Version(version) : null;
        }

        /// <summary>
        /// Get files list with md5 hash
        /// </summary>>
        /// <param name="lastVersion">Program version</param>
        /// <returns>File list with hash</returns>
        private async Task<AllFilesVersionInfo?> GetFilesListWithHashAsync(Version lastVersion)
        {
            var httpClient = new HttpClient();
            using HttpRequestMessage request =
                new(HttpMethod.Get, $"{Url}/Version/GetFilesListWithHash?program={ProgramName}&version={lastVersion}");
            var response = await httpClient.SendAsync(request);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AllFilesVersionInfo>() : null;
        }

        /// <summary>
        /// Download file from server
        /// </summary>
        /// <param name="lastVersion">Version</param>
        /// <param name="filePath">Relative path to file</param>
        /// <returns>Is success</returns>
        private async Task<bool> DownloadFileFromUrlAsync(Version lastVersion, string filePath)
        {
            var httpClient = new HttpClient();

            using var request =
                new HttpRequestMessage(HttpMethod.Get, $"{Url}/Version/GetFile");

            request.Content = JsonContent.Create(new DownloadFileInfo(ProgramName, lastVersion.ToString(), filePath));

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Ошибка скачивания файла");
                return false;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var directory = Path.GetDirectoryName(filePath);
            if (directory is not null && directory != "\\")
                Directory.CreateDirectory(DownloadDirectory + directory);

            // Save file
            var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(DownloadDirectory + filePath);
            await Task.Run(() =>
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            });
            return true;
        }

        /// <summary>
        /// Get Md5 hash for file as string
        /// </summary>
        /// <param name="filePath">path to file</param>
        /// <returns>Md5 hash string</returns>
        private async Task<string> CreateMd5HashStringForFileAsync(string filePath)
        {
            using var md5 = MD5.Create();
            await using var stream = File.OpenRead(filePath);
            var hash = await md5.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Full directory copy 
        /// </summary>
        /// <param name="sourceDirectory">From</param>
        /// <param name="destinationDirectory">To</param>
        private void CopyAllDataToDirectory(string sourceDirectory, string destinationDirectory, bool recursive = true)
        {

            var directory = new DirectoryInfo(sourceDirectory);

            if (!directory.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {directory.FullName}");

            DirectoryInfo[] directories = directory.GetDirectories();

            Directory.CreateDirectory(destinationDirectory);

            foreach (FileInfo file in directory.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDirectory, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDirectories in directories)
                {
                    string newDestinationDirectory = Path.Combine(destinationDirectory, subDirectories.Name);
                    CopyAllDataToDirectory(subDirectories.FullName, newDestinationDirectory, true);
                }
            }
        }

        /// <summary>
        /// Clear directory
        /// </summary>
        private void ClearDirectory(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);

            if (!directoryInfo.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {directoryInfo.FullName}");

            directoryInfo.GetFiles().ToList().ForEach(file => file.Delete());
            directoryInfo.GetDirectories().ToList().ForEach(directory => directory.Delete(true));
        }
    }

    /// <summary>
    /// Structure of a request to download a file from a server
    /// </summary>
    internal record class DownloadFileInfo(string program, string Version, string FilePath);

    /// <summary>
    /// Files list with Md5 hash
    /// </summary>
    internal class AllFilesVersionInfo : List<VersionInfo> { }

    /// <summary>
    /// Version information about file
    /// </summary>
    internal record class VersionInfo(string FileName, string Md5Hash);
}
