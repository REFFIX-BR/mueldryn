using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Launcher
{
	// Token: 0x02000005 RID: 5
	internal class SecureStringManager
	{
		// Token: 0x0600001D RID: 29 RVA: 0x00002344 File Offset: 0x00000544
		public static string Encrypt(string plainText, string passPhrase)
		{
			byte[] bytes = Encoding.UTF8.GetBytes("tu89geji340t89u2");
			byte[] bytes2 = Encoding.UTF8.GetBytes(plainText);
			byte[] bytes3 = new PasswordDeriveBytes(passPhrase, null).GetBytes(32);
			ICryptoTransform transform = new RijndaelManaged
			{
				Mode = CipherMode.CBC
			}.CreateEncryptor(bytes3, bytes);
			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
			cryptoStream.Write(bytes2, 0, bytes2.Length);
			cryptoStream.FlushFinalBlock();
			byte[] inArray = memoryStream.ToArray();
			memoryStream.Close();
			cryptoStream.Close();
			return Convert.ToBase64String(inArray);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000023D0 File Offset: 0x000005D0
		public static string Decrypt(string cipherText, string passPhrase)
		{
			byte[] bytes = Encoding.ASCII.GetBytes("tu89geji340t89u2");
			byte[] array = Convert.FromBase64String(cipherText);
			byte[] bytes2 = new PasswordDeriveBytes(passPhrase, null).GetBytes(32);
			ICryptoTransform transform = new RijndaelManaged
			{
				Mode = CipherMode.CBC
			}.CreateDecryptor(bytes2, bytes);
			MemoryStream memoryStream = new MemoryStream(array);
			CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read);
			byte[] array2 = new byte[array.Length];
			int count = cryptoStream.Read(array2, 0, array2.Length);
			memoryStream.Close();
			cryptoStream.Close();
			return Encoding.UTF8.GetString(array2, 0, count);
		}

		// Token: 0x0400000E RID: 14
		private const string initVector = "tu89geji340t89u2";

		// Token: 0x0400000F RID: 15
		private const int keysize = 256;
	}
}
