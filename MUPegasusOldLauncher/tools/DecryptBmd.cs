using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
class P {
  static string Decrypt(string cipherText, string passPhrase) {
    byte[] iv = Encoding.ASCII.GetBytes("tu89geji340t89u2");
    byte[] data = Convert.FromBase64String(cipherText);
    byte[] key = new PasswordDeriveBytes(passPhrase, null).GetBytes(32);
    var t = new RijndaelManaged { Mode = CipherMode.CBC }.CreateDecryptor(key, iv);
    using (var ms = new MemoryStream(data))
    using (var cs = new CryptoStream(ms, t, CryptoStreamMode.Read)) {
      byte[] buf = new byte[data.Length];
      int n = cs.Read(buf, 0, buf.Length);
      return Encoding.UTF8.GetString(buf, 0, n);
    }
  }
  static void Main() {
    string path = @"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\MUPegasusOldLauncher\Launcher\bin\Release\Data\Local\Launcher.bmd";
    Console.WriteLine(Decrypt(File.ReadAllText(path), "28755"));
  }
}
