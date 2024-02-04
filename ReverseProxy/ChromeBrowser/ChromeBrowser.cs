using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ReverseProxy.ChromeBrowser;

// https://github.com/BaristaLabs/chrome-dev-tools-generator/blob/master/src/BaristaLabs.ChromeDevTools.Core/Chrome.cs

public class ChromeBrowser : IDisposable
{

    private readonly Process _process;
    private readonly DirectoryInfo _userDirectory;
    private readonly int _remoteDebuggingPort;

    public ChromeBrowser(Process chromeProcess, DirectoryInfo userDirectory, int remoteDebuggingPort)
    {
        Console.WriteLine(_userDirectory);
        _process = chromeProcess ?? throw new ArgumentNullException(nameof(chromeProcess));
        _userDirectory = userDirectory ?? throw new ArgumentNullException(nameof(userDirectory));
        _remoteDebuggingPort = remoteDebuggingPort;
    }

    public void Dispose()
    {
        if (_process != null)
        {
            if (!_process.HasExited)
            {
                _process.WaitForExit(5000);
                _process.Kill();
            }

            _process.Dispose();

        }

        if (_userDirectory != null)
        {
            _userDirectory.Delete(true);
        }
    }

    public static ChromeBrowser? Open(int remoteDebuggingPort = 9222)
    {
        string path = Path.GetRandomFileName();
        var directoryInfo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), path));
        var remoteDebuggingArg = $"--remote-debugging-port={remoteDebuggingPort}";
        var userDirectoryArg = $"--user-data-dir=\"{directoryInfo.FullName}\"";
        var chromeProcessArgs = $"{remoteDebuggingArg} {userDirectoryArg} --bwsi --no-first-run";

        Process? chromeProcess;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string programFiles = RuntimeInformation.OSArchitecture == Architecture.X86
                ? "Program Files (x86)"
                : "Program Files";
            var processInfo = new ProcessStartInfo($"C:\\{programFiles}\\Google\\Chrome\\Application\\chrome.exe", chromeProcessArgs) { CreateNoWindow = true };
            chromeProcess = Process.Start(processInfo);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            chromeProcess = Process.Start("google-chrome", chromeProcessArgs);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            chromeProcess = Process.Start("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", chromeProcessArgs);
        }
        else
        {
            throw new InvalidOperationException("Unknown or unsupported platform.");
        }

        if (chromeProcess != null)
        {
            return new ChromeBrowser(chromeProcess, directoryInfo, remoteDebuggingPort);
        }

        return null;
    }


}