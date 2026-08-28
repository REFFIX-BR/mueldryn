using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Gera Data/Local/Launcher.bmd criptografado para o MuEldryn.
/// Formato (linhas CRLF):
///   0 = URL base de update (com barra final)
///   1 = IP do Connect Server
///   2 = Porta GameServer (status)
///   3 = Porta Connect Server
///   4 = Executável do client
///   5 = Timezone (UTC offset, Brasil = -3)
/// </summary>
class MakeLauncherBmd
{
    const string Key = "28755";
    const string Iv = "tu89geji340t89u2";

    static string Encrypt(string plainText, string passPhrase)
    {
        byte[] iv = Encoding.UTF8.GetBytes(Iv);
        byte[] data = Encoding.UTF8.GetBytes(plainText);
        byte[] key = new PasswordDeriveBytes(passPhrase, null).GetBytes(32);
        var t = new RijndaelManaged { Mode = CipherMode.CBC }.CreateEncryptor(key, iv);
        using (var ms = new MemoryStream())
        {
            using (var cs = new CryptoStream(ms, t, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
            }
            return Convert.ToBase64String(ms.ToArray());
        }
    }

    static void Main(string[] args)
    {
        // Defaults MuEldryn (VPS)
        string updateUrl = "http://200.11.121.89/update/";
        string serverIp = "200.11.121.89";
        string gsPort = "55901";
        string csPort = "44406";
        string startFile = "MuEldrynLaunch.exe";
        string timezone = "-3";

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--url") updateUrl = args[++i];
            else if (args[i] == "--ip") serverIp = args[++i];
            else if (args[i] == "--gs") gsPort = args[++i];
            else if (args[i] == "--cs") csPort = args[++i];
            else if (args[i] == "--exe") startFile = args[++i];
            else if (args[i] == "--tz") timezone = args[++i];
        }

        if (!updateUrl.EndsWith("/")) updateUrl += "/";

        string plain = string.Join("\r\n", new[] {
            updateUrl, serverIp, gsPort, csPort, startFile, timezone, ""
        });

        string outDir = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Launcher\bin\Release\Data\Local"));
        // Prefer project Data folder next to tools
        string projectData = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            @"..\..\Data\Local"));

        // Resolve relative to this source tree
        string root = @"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\MUPegasusOldLauncher";
        string[] targets = new[] {
            Path.Combine(root, @"Launcher\bin\Release\Data\Local\Launcher.bmd"),
            Path.Combine(root, @"pack\Data\Local\Launcher.bmd"),
            Path.Combine(root, @"UpdateServer\client-seed\Data\Local\Launcher.bmd"),
        };

        string cipher = Encrypt(plain, Key);
        foreach (var t in targets)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(t));
            File.WriteAllText(t, cipher, Encoding.ASCII);
            Console.WriteLine("Wrote " + t);
        }
        Console.WriteLine("--- plain ---");
        Console.WriteLine(plain.Replace("\r\n", " | "));
    }
}
