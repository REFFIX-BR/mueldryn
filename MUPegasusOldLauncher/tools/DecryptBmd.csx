using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

class Program {
  const string initVector = "tu89geji340t89u2";
  static string Decrypt(string cipherText, string passPhrase) {
    byte[] bytes = Encoding.ASCII.GetBytes(initVector);
    byte[] array = Convert.FromBase64String(cipherText);
    byte[] bytes2 = new PasswordDeriveBytes(passPhrase, null).GetBytes(32);
    ICryptoTransform transform = new RijndaelManaged { Mode = CipherMode.CBC }.CreateDecryptor(bytes2, bytes);
    using (var memoryStream = new MemoryStream(array))
    using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read)) {
      byte[] array2 = new byte[array.Length];
      int count = cryptoStream.Read(array2, 0, array2.Length);
      return Encoding.UTF8.GetString(array2, 0, count);
    }
  }
  static string Encrypt(string plainText, string passPhrase) {
    byte[] bytes = Encoding.UTF8.GetBytes(initVector);
    byte[] bytes2 = Encoding.UTF8.GetBytes(plainText);
    byte[] bytes3 = new PasswordDeriveBytes(passPhrase, null).GetBytes(32);
    ICryptoTransform transform = new RijndaelManaged { Mode = CipherMode.CBC }.CreateEncryptor(bytes3, bytes);
    using (var memoryStream = new MemoryStream()) {
      using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write)) {
        cryptoStream.Write(bytes2, 0, bytes2.Length);
        cryptoStream.FlushFinalBlock();
      }
      return Convert.ToBase64String(memoryStream.ToArray());
    }
  }
  static void Main() {
    string path = @"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\MUPegasusOldLauncher\Launcher\bin\Release\Data\Local\Launcher.bmd";
    string cipher = File.ReadAllText(path);
    Console.WriteLine("DECRYPTED:");
    Console.WriteLine(Decrypt(cipher, "28755"));
  }
}
