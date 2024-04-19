﻿using System.Runtime.InteropServices;
using Updater;

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

    (bool isSuccess, string message) = await downloader.UpdateProgramAsync();

    Console.WriteLine();  
    if (isSuccess)
    {
        SetForegroundWindow(GetConsoleWindow());
        WriteColorMessage("Программа успешно обновлена", ConsoleColor.Green);
    }
    else
    {
        WriteColorMessage($"Ошибка обновления программы\n{message}", ConsoleColor.Red);
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
    Console.Write(((double)currentFile / filesCount).ToString("  00.0%   "));

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

[DllImport("kernel32.dll", ExactSpelling = true)]
static extern IntPtr GetConsoleWindow();

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool SetForegroundWindow(IntPtr hWnd);
