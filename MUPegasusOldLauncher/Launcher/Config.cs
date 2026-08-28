using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Launcher
{
	// Token: 0x02000003 RID: 3
	internal class Config
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600000D RID: 13 RVA: 0x00002207 File Offset: 0x00000407
		// (set) Token: 0x0600000E RID: 14 RVA: 0x0000220F File Offset: 0x0000040F
		public string StartFile
		{
			get
			{
				return this._StartFile;
			}
			set
			{
				this._StartFile = value;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600000F RID: 15 RVA: 0x00002218 File Offset: 0x00000418
		// (set) Token: 0x06000010 RID: 16 RVA: 0x00002220 File Offset: 0x00000420
		public string RSSLink
		{
			get
			{
				return this._RSSLink;
			}
			set
			{
				this._RSSLink = value;
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000011 RID: 17 RVA: 0x00002229 File Offset: 0x00000429
		// (set) Token: 0x06000012 RID: 18 RVA: 0x00002231 File Offset: 0x00000431
		public string ServerIP
		{
			get
			{
				return this._ServerIP;
			}
			set
			{
				this._ServerIP = value;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000013 RID: 19 RVA: 0x0000223A File Offset: 0x0000043A
		// (set) Token: 0x06000014 RID: 20 RVA: 0x00002242 File Offset: 0x00000442
		public string GSPort
		{
			get
			{
				return this._GSPort;
			}
			set
			{
				this._GSPort = value;
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000015 RID: 21 RVA: 0x0000224B File Offset: 0x0000044B
		// (set) Token: 0x06000016 RID: 22 RVA: 0x00002253 File Offset: 0x00000453
		public string CSPort
		{
			get
			{
				return this._CSPort;
			}
			set
			{
				this._CSPort = value;
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000017 RID: 23 RVA: 0x0000225C File Offset: 0x0000045C
		// (set) Token: 0x06000018 RID: 24 RVA: 0x00002264 File Offset: 0x00000464
		public int TimeZone
		{
			get
			{
				return this._TimeZone;
			}
			set
			{
				this._TimeZone = value;
			}
		}

		// Token: 0x06000019 RID: 25 RVA: 0x0000226D File Offset: 0x0000046D
		public static Config GetConfigs()
		{
			if (Config._Instance == null)
			{
				Config._Instance = new Config();
			}
			return Config._Instance;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002285 File Offset: 0x00000485
		protected Config()
		{
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002290 File Offset: 0x00000490
		public void LoadLocalConfig(string FileName, string EncryptKey)
		{
			try
			{
				if (!File.Exists(FileName))
				{
					MessageBox.Show("Config file not found! \nPlease reinstall client!", "Error");
					Environment.Exit(0);
				}
				string[] array = Regex.Split(SecureStringManager.Decrypt(File.ReadAllText(FileName), EncryptKey), "\r\n");
				Config._Instance.RSSLink = array[0];
				Config._Instance.ServerIP = array[1];
				Config._Instance.GSPort = array[2];
				Config._Instance.CSPort = array[3];
				Config._Instance.StartFile = array[4];
				Config._Instance.TimeZone = Convert.ToInt32(array[5]);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}

		// Token: 0x04000007 RID: 7
		private static Config _Instance;

		// Token: 0x04000008 RID: 8
		private string _StartFile;

		// Token: 0x04000009 RID: 9
		private string _RSSLink;

		// Token: 0x0400000A RID: 10
		private string _ServerIP;

		// Token: 0x0400000B RID: 11
		private string _GSPort;

		// Token: 0x0400000C RID: 12
		private string _CSPort;

		// Token: 0x0400000D RID: 13
		private int _TimeZone;
	}
}
