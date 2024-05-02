using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Updater;

namespace Updater_Windows
{
    public partial class ViewModel : INotifyPropertyChanged
    {
        public string MessageTop { get; set; } = "";
        public double Progress { get; set; } = 0;
        public bool IsProgramHaveRunningProcess { get; set; } = false;

        /// <summary>
        /// Show that now nothing downloading 
        /// </summary>
        public bool IsNotDownloading { get; set; } = false;

        private readonly Downloader Downloader = null!;
        /// <summary>
        /// Move dialog on top
        /// </summary>
        private readonly Action DialogOnTop;
        private readonly string ProgramPath = null!;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ViewModel(Action dialogOnTop)
        {
            DialogOnTop = dialogOnTop;
            var args = Environment.GetCommandLineArgs();
            if (args.Length < 3)  { MessageTop = "Переданы некорректные данные для обновления"; return; }
        

            ProgramPath = args[1];
            string url = args[2];
            if (!File.Exists(ProgramPath)) { MessageTop = "Переданы некорректные данные для обновления"; return; }

            Downloader = new Downloader(ProgramPath, url);
            Downloader.FileDownload += Downloader_FileDownload;
            // Downloader.IsAgreeToKillProcess += Downloader_IsAgreeToKillProcess;

            DownloadFiles();
        }

        /// <summary>
        /// Download files, if program does not have running process, run moving files, else - ask to kill process
        /// </summary>
        private async void DownloadFiles()
        {
            MessageTop = "Проверка актуальной версии!";
            var isNeed = await Downloader.IsUpdateNeedAsync();
            DialogOnTop();

            if (isNeed is null)
            {
                MessageTop = "Ошибка обновления!\nОшибка доступа к серверу!";
            }

            if (isNeed == true)
            {

                MessageTop = $"Загрузка файлов программы {Downloader.ProgramName}";
                IsNotDownloading = false;
                (bool isDownloadSuccess, string downloadMessage) = await Downloader.DownloadLastVersionAsync();
                IsNotDownloading = true;
                if (!isDownloadSuccess)
                { MessageTop = $"Ошибка загрузки программы {Downloader.ProgramName}\n{downloadMessage}"; }

                IsProgramHaveRunningProcess = Downloader.IsProgramHaveWorkingProcess();

                if (IsProgramHaveRunningProcess)
                {
                    MessageTop = $"Внимание, все запущенные программы\n{Downloader.ProgramName} будут закрыты!";
                    IsProgramHaveRunningProcess = true;
                }

                if (!IsProgramHaveRunningProcess)
                {
                    UpdateFiles();
                }
            }

            if (isNeed == false)
            {
                MessageTop = "Обновление не требуется!\nУстановлена актуальная версия!";
            }
        }

        public async void UpdateFiles()
        {
            (bool isCopySuccess, string copyMessage) = await Downloader.CopyFileToSourceAsync(true);
            if (!isCopySuccess)
            {
                MessageTop = $"Ошибка обновления программы {Downloader.ProgramName}!\n{copyMessage}\n" +
                    "Пожалуйста отключите антивирус или запустите программу с правами администратора.";
            }
            if (isCopySuccess)
            {
                MessageTop = "Программа успешно обновлена.\nЗапускается новая версия ...";
                DialogOnTop();
                StartProgram(ProgramPath);
                await Task.Delay(2000);
                Environment.Exit(0);
            }
        }
        [RelayCommand]
        public void OnApply()
        {
            if (IsProgramHaveRunningProcess) { IsProgramHaveRunningProcess = false; UpdateFiles(); }
            else { Environment.Exit(0); };
        }

        [RelayCommand]
        public void OnClose()
        {
            Environment.Exit(0);
        }



        private void Downloader_FileDownload(bool? isDownload, string fileName, int currentFile, int filesCount)
        {
            Progress = 100 * (double)currentFile / filesCount;
        }

        /// <summary>
        /// Run updated program
        /// </summary>
        private bool StartProgram(string pathToProgram)
        {
            Process iStartProcess = new();
            iStartProcess.StartInfo.FileName = pathToProgram;
            iStartProcess.StartInfo.UseShellExecute = true;
            if (Environment.OSVersion.Version.Major >= 6)
            {
                iStartProcess.StartInfo.Verb = "runas";
            }
            iStartProcess.Start();
            return true;
        }

    }
}
