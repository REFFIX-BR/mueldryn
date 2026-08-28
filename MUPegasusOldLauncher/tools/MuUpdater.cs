using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

/// <summary>
/// Troca Launcher.exe por NewLauncher.exe (self-update do launcher).
/// </summary>
class MuUpdater
{
    [STAThread]
    static void Main()
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        string current = Path.Combine(dir, "Launcher.exe");
        string incoming = Path.Combine(dir, "NewLauncher.exe");
        string backup = Path.Combine(dir, "Launcher.bak.exe");

        if (!File.Exists(incoming))
        {
            MessageBox.Show("Nenhum NewLauncher.exe encontrado.", "MuEldryn Updater");
            return;
        }

        // Espera o launcher fechar
        for (int i = 0; i < 40; i++)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("Launcher"))
                {
                    try
                    {
                        string mod = null;
                        if (p.MainModule != null) mod = p.MainModule.FileName;
                        if (mod != null && Path.GetDirectoryName(mod) != null &&
                            Path.GetDirectoryName(mod).Equals(dir.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                            p.Kill();
                    }
                    catch { }
                }
            }
            catch { }
            Thread.Sleep(250);
            try
            {
                if (File.Exists(current))
                {
                    if (File.Exists(backup)) File.Delete(backup);
                    File.Move(current, backup);
                }
                File.Move(incoming, current);
                Process.Start(new ProcessStartInfo(current) { WorkingDirectory = dir });
                return;
            }
            catch
            {
                Thread.Sleep(250);
            }
        }
        MessageBox.Show("Falha ao aplicar update do launcher. Feche o Launcher e rode MuUpdater.exe de novo.", "MuEldryn Updater");
    }
}
