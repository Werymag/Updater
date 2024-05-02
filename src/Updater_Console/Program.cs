using System.Diagnostics;
using System.Runtime.InteropServices;
using Updater;
using static System.Net.Mime.MediaTypeNames;

if (args.Length < 2) return;

string programPath = args[0];
string url = args[1];
if (!File.Exists(programPath)) return;


Console.ForegroundColor = ConsoleColor.Blue;
Console.Clear();

var downloader = new Downloader(programPath, url);
downloader.FileDownload += Downloader_FileDownload;
downloader.IsAgreeToKillProcess += Downloader_IsAgreeToKillProcess;
var IsNeed = await downloader.IsUpdateNeedAsync();

if (IsNeed == true)
{
    SetForegroundWindow(GetConsoleWindow());
    Console.Clear();
    Console.CursorVisible = false;
    Console.Write("  00.0%   ");
    Console.Write(string.Join("", Enumerable.Range(0, 100).Select(i => "_")));

    (bool isDownloadSuccess, string downloadMessage) = await downloader.DownloadLastVersionAsync();
    (bool isCopySuccess, string copyMessage) = await downloader.CopyFileToSourceAsync();
  
    Console.WriteLine();
    if (!isDownloadSuccess)
    { WriteColorMessage($"Ошибка загрузки программы\n{downloadMessage}", ConsoleColor.Red); }

    if (!isCopySuccess)
    {
        WriteColorMessage($"Ошибка обновления программы\n{copyMessage}", ConsoleColor.Red);
        WriteColorMessage("Пожалуйста отключите антивирус или запустите программу с правами администратора для корректной установки обновления", ConsoleColor.Red);
    }

    if (isDownloadSuccess && isCopySuccess)
    {
        SetForegroundWindow(GetConsoleWindow());
        WriteColorMessage("Программа успешно обновлена", ConsoleColor.Green);
        StartProgram(programPath);
        await Task.Delay(1000);
        Environment.Exit(0);
    }

    Console.ReadLine();
}


/// Question about kill all process 
bool Downloader_IsAgreeToKillProcess()
{
    while (true)
    {
        SetForegroundWindow(GetConsoleWindow());
        Console.WriteLine();
        WriteColorMessage("   Внимание, все запущенные версии программы будут закрыты (Y/N)", ConsoleColor.DarkRed);
        var answer = Console.ReadKey().Key;
        if (answer == ConsoleKey.Y)
        {
            Console.WriteLine();
            return true;
        }
        if (answer == ConsoleKey.N) return false;
    }
}

/// Progress bar
void Downloader_FileDownload(bool? isDownload, string fileName, int currentFile, int filesCount)
{
    Console.SetCursorPosition(0, Console.CursorTop);
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write(((double)currentFile / filesCount).ToString("  00.0%   "));
    Console.ForegroundColor = isDownload == true ? ConsoleColor.Blue : ConsoleColor.Green;
    Console.SetCursorPosition(10 + 100 * currentFile / filesCount, Console.CursorTop);
    Console.Write("#");
}

/// Color message
void WriteColorMessage(string Message, ConsoleColor foregroundColor = ConsoleColor.White, ConsoleColor backgroundColor = ConsoleColor.Black)
{
    Console.BackgroundColor = backgroundColor;
    Console.ForegroundColor = foregroundColor;
    Console.WriteLine(Message);
}

/// Run updated program
bool StartProgram(string pathToProgram)
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

[DllImport("kernel32.dll", ExactSpelling = true)]
static extern IntPtr GetConsoleWindow();

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool SetForegroundWindow(IntPtr hWnd);
