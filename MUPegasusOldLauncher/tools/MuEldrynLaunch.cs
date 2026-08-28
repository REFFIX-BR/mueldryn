using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

/// <summary>
/// Ponte entre o Launcher Pegasus (legado) e o Main MuEldryn.
/// Lê Data/Local/Launcher.bmd e inicia:
///   Main.exe connect /uIP /pPORTA
/// Também espelha IP/porta no config.ini.
/// </summary>
class MuEldrynLaunch
{
    const string EncryptKey = "28755";
    const string Iv = "tu89geji340t89u2";

    static string Decrypt(string cipherText, string passPhrase)
    {
        byte[] iv = Encoding.ASCII.GetBytes(Iv);
        byte[] data = Convert.FromBase64String(cipherText.Trim());
        byte[] key = new PasswordDeriveBytes(passPhrase, null).GetBytes(32);
        var t = new RijndaelManaged { Mode = CipherMode.CBC }.CreateDecryptor(key, iv);
        using (var ms = new MemoryStream(data))
        using (var cs = new CryptoStream(ms, t, CryptoStreamMode.Read))
        {
            byte[] buf = new byte[data.Length];
            int n = cs.Read(buf, 0, buf.Length);
            return Encoding.UTF8.GetString(buf, 0, n);
        }
    }

    static int Main(string[] args)
    {
        try
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string bmd = Path.Combine(dir, "Data", "Local", "Launcher.bmd");
            if (!File.Exists(bmd))
            {
                Console.WriteLine("Launcher.bmd não encontrado em Data\\Local\\");
                return 1;
            }

            string[] lines = Regex.Split(Decrypt(File.ReadAllText(bmd), EncryptKey), "\r\n");
            string serverIp = lines.Length > 1 ? lines[1].Trim() : "200.11.121.89";
            string csPort = lines.Length > 3 ? lines[3].Trim() : "44406";
            string mainExe = lines.Length > 4 ? lines[4].Trim() : "Main.exe";
            if (string.IsNullOrEmpty(mainExe)) mainExe = "Main.exe";

            WriteConfigIni(dir, serverIp, csPort);

            string mainPath = Path.Combine(dir, mainExe);
            if (!File.Exists(mainPath))
            {
                Console.WriteLine("Executável não encontrado: " + mainExe);
                return 2;
            }

            string argLine = string.Format("connect /u{0} /p{1}", serverIp, csPort);
            Process.Start(new ProcessStartInfo
            {
                FileName = mainPath,
                Arguments = argLine,
                WorkingDirectory = dir,
                UseShellExecute = true
            });
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return 99;
        }
    }

    static void WriteConfigIni(string dir, string ip, string port)
    {
        try
        {
            string ini = Path.Combine(dir, "config.ini");
            string content;
            if (File.Exists(ini))
            {
                content = File.ReadAllText(ini);
                if (content.IndexOf("[CONNECTION SETTINGS]", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    content = Regex.Replace(content, @"(?im)^ServerIP=.*$", "ServerIP=" + ip);
                    content = Regex.Replace(content, @"(?im)^ServerPort=.*$", "ServerPort=" + port);
                }
                else
                {
                    content += "\r\n[CONNECTION SETTINGS]\r\nServerIP=" + ip + "\r\nServerPort=" + port + "\r\n";
                }
            }
            else
            {
                content = "[CONNECTION SETTINGS]\r\nServerIP=" + ip + "\r\nServerPort=" + port + "\r\n";
            }
            File.WriteAllText(ini, content);
        }
        catch { }
    }
}
