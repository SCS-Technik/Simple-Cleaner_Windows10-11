using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading.Tasks;

static class ProcessExtensions
{
    public static Task<bool> WaitForExitAsync(this Process process)
    {
        var tcs = new TaskCompletionSource<bool>();
        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) => tcs.TrySetResult(true);
        return tcs.Task;
    }
}

class Program
{
    static async Task Main()
    {
        try
        {
            if (IsWindows() && IsAdministrator())
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);

                ProcessStartInfo cmdPsi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                Process cmdProcess = new Process { StartInfo = cmdPsi };

                cmdProcess.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                cmdProcess.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

                cmdProcess.Start();

                cmdProcess.BeginOutputReadLine();
                cmdProcess.BeginErrorReadLine();

                ExecuteCmdCommand(cmdProcess, "wmic.exe /Namespace:\\\\root\\default Path SystemRestore Call CreateRestorePoint \"Wiederherstellungspunkt vor Programmstart\", 100, 7");

                ExecuteCmdCommands(cmdProcess);

                ExecuteCmdCommand(cmdProcess, "cmd /k");

                await cmdProcess.WaitForExitAsync();

                Console.WriteLine("Erfolgreich beendet. Drücken Sie Enter, um das Programm zu schließen.");
                Console.ReadLine(); // Warte auf Benutzereingabe, bevor das Programm beendet wird
                Environment.Exit(0); // Beendet das Programm
            }
            else
            {
                Console.WriteLine("Dieses Programm muss als Administrator unter Windows ausgeführt werden.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ein Fehler ist aufgetreten: {ex.Message}");
        }
    }

    static void ExecuteCmdCommands(Process cmdProcess)
    {
        string[] cmdCommands = {
            "ipconfig /flushdns",
            "ipconfig /renew",
        "nbtstat -R",
        "nbtstat -RR",
        "netsh int ip reset c:\\resetlog.txt",
        "netsh winsock reset",
        "del /q /s %temp%\\*.*",
        "del /q /s C:\\Windows\\Temp\\*.*",
        "del /q /s C:\\Windows\\Prefetch\\*.*",
        "echo y | winget upgrade --all",
        "dism /Online /Cleanup-Image /AnalyzeComponentStore",
        "%windir%\\System32\\dism.exe /Online /Cleanup-Image /StartComponentCleanup",
        "dism /Online /Cleanup-Image /ScanHealth",
        "dism /Online /Cleanup-Image /CheckHealth",
        "dism /Online /Cleanup-Image /RestoreHealth",
        //"sfc /scannow",
        "defrag C: /U /V",
        "wuauclt /detectnow && wuauclt /downloadnow && wuauclt /updatenow"
        };

        foreach (var cmdCommand in cmdCommands)
        {
            try
            {
                ExecuteCmdCommand(cmdProcess, cmdCommand);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ein Fehler ist aufgetreten beim Ausführen des Befehls '{cmdCommand}': {ex.Message}");
            }
        }

        cmdProcess.StandardInput.WriteLine("exit");
    }

    static void ExecuteCmdCommand(Process process, string command)
    {
        process.StandardInput.WriteLine(command);
        process.StandardInput.WriteLine("echo Command completed: " + command);
    }

    static bool IsWindows()
    {
        return System.Runtime.InteropServices.RuntimeInformation
            .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
    }

    static bool IsAdministrator()
    {
        if (!IsWindows()) return false;

        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);

        if (IsWindows())
        {
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        return false;
    }
}
