using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;
using Launcher.Properties;
using Microsoft.Win32;

namespace Launcher
{
	// Token: 0x02000008 RID: 8
	public partial class MainWindow : Window
	{
		// Token: 0x0600003D RID: 61
		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(string className, string windowTitle);

		// Token: 0x0600003E RID: 62
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ShowWindow(IntPtr hWnd, MainWindow.ShowWindowEnum flags);

		// Token: 0x0600003F RID: 63
		[DllImport("user32.dll")]
		private static extern int SetForegroundWindow(IntPtr hwnd);

		// Token: 0x06000040 RID: 64
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowPlacement(IntPtr hWnd, ref MainWindow.Windowplacement lpwndpl);

		// Token: 0x06000041 RID: 65 RVA: 0x00002E18 File Offset: 0x00001018
		private void BringWindowToFront()
		{
			IntPtr intPtr = MainWindow.FindWindow(null, "Launcher");
			MainWindow.Windowplacement windowplacement = default(MainWindow.Windowplacement);
			MainWindow.GetWindowPlacement(intPtr, ref windowplacement);
			if (windowplacement.showCmd == 2)
			{
				MainWindow.ShowWindow(intPtr, MainWindow.ShowWindowEnum.Restore);
			}
			MainWindow.ShowWindow(intPtr, MainWindow.ShowWindowEnum.Show);
			MainWindow.ShowWindow(intPtr, MainWindow.ShowWindowEnum.Restore);
			MainWindow.SetForegroundWindow(intPtr);
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002E6C File Offset: 0x0000106C
		public MainWindow()
		{
			this.InitializeComponent();
			int.Parse(DateTime.Now.ToString("dd"));
			this.ni.Icon = Launcher.Properties.Resources.ico;
			this.ni.DoubleClick += delegate(object sender, EventArgs args)
			{
				base.Show();
				base.WindowState = WindowState.Normal;
				this.ni.Visible = false;
			};
			base.MouseDown += new MouseButtonEventHandler(this.MainBgr_MouseDown);
		base.PreviewMouseDown += this.MainBgr_PreviewMouseDown;
		base.PreviewMouseUp += this.MainBgr_PreviewMouseUp;
		this.CheckRegistryPath();
		bool flag;
		MainWindow.mutex = new Mutex(false, "JTLAUNCHER1.2", out flag);
		if (!flag)
			{
				System.Windows.MessageBox.Show("Launcher already in use!", "Launcher");
				base.Close();
			}
			this.backgroundWorker1.WorkerSupportsCancellation = true;
			this.backgroundWorker2.WorkerSupportsCancellation = true;
			this.backgroundWorker3.WorkerSupportsCancellation = true;
			this.backgroundWorker1.DoWork += this.backgroundWorker1_DoWork;
			this.backgroundWorker1.RunWorkerCompleted += this.backgroundWorker1_RunWorkerCompleted;
			this.backgroundWorker2.DoWork += this.backgroundWorker2_DoWork;
			this.backgroundWorker2.RunWorkerCompleted += this.backgroundWorker2_RunWorkerCompleted;
			this.backgroundWorker3.DoWork += this.backgroundWorker3_DoWork;
			this.backgroundWorker3.RunWorkerCompleted += this.backgroundWorker3_RunWorkerCompleted;
			MainWindow._FormInstance = this;
			this.Progressbar1.Width = 745.0;
			this.Progressbar2.Width = 745.0;
			this.Configs_ = Config.GetConfigs();
			this.Configs_.LoadLocalConfig("Data/Local/Launcher.bmd", "28755");
			if (!string.IsNullOrEmpty(this.Configs_.RSSLink))
			{
				this.SiteAdress = this.Configs_.RSSLink;
				if (!this.SiteAdress.EndsWith("/"))
				{
					this.SiteAdress += "/";
				}
			}
			this._oa = new DoubleAnimation();
			this._oa.From = new double?(0.0);
			this._oa.To = new double?((double)1);
			this._oa.Duration = new Duration(TimeSpan.FromMilliseconds(800.0));
			base.BeginAnimation(UIElement.OpacityProperty, this._oa);
			this.timerTime.Tick += this.timer1_Tick;
			this.timerTime.Interval = 100;
			this.timerTime.Start();
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003120 File Offset: 0x00001320
		private void CheckRegistryPath()
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Webzen\\Pegasus\\Config", true);
			if (registryKey == null)
			{
				registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Pegasus\\Config");
			}
			registryKey.Close();
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003158 File Offset: 0x00001358
		public bool testSite(string url)
		{
			Uri requestUri = new Uri(url);
			try
			{
				HttpWebResponse httpWebResponse = (HttpWebResponse)((HttpWebRequest)WebRequest.Create(requestUri)).GetResponse();
			}
			catch (WebException)
			{
			}
			return true;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003198 File Offset: 0x00001398
		private void timer1_Tick(object sender, EventArgs e)
		{
			DateTime dateTime = DateTime.UtcNow.AddHours((double)this.Configs_.TimeZone);
		}

		// Token: 0x06000046 RID: 70 RVA: 0x000031BF File Offset: 0x000013BF
		private static BitmapImage GetImage(string imageUri)
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.UriSource = new Uri("pack://siteoforigin:,,,/" + imageUri, UriKind.RelativeOrAbsolute);
			bitmapImage.EndInit();
			return bitmapImage;
		}

		// Token: 0x06000078 RID: 120
		private void ReloadBackgroundImage(string imagePath)
		{
			if (!base.Dispatcher.CheckAccess())
			{
				base.Dispatcher.Invoke(new Action<string>(this.ReloadBackgroundImage), new object[]
				{
					imagePath
				});
				return;
			}
			try
			{
				// Normalize the path - remove leading slash if present
				string normalizedPath = imagePath.TrimStart('/', '\\');
				
				// Determine the URI based on file location
				string fullPath = Path.Combine(this.ExecutablePath, normalizedPath);
				string actualPath = null;
				
				// Check if file exists
				if (File.Exists(fullPath))
				{
					actualPath = fullPath;
				}
				else if (File.Exists(normalizedPath))
				{
					actualPath = Path.GetFullPath(normalizedPath);
				}
				else
				{
					// File doesn't exist, use default background
					actualPath = Path.Combine(this.ExecutablePath, "Resources", "background.png");
					if (!File.Exists(actualPath))
					{
						// Fallback to pack URI for embedded resource
						actualPath = null;
					}
				}
				
				// Clear the old image source first to force release
				if (this.BackgroundImageBrush != null)
				{
					if (this.BackgroundImageBrush.ImageSource != null)
					{
						// Dispose old image if possible
						BitmapImage oldImage = this.BackgroundImageBrush.ImageSource as BitmapImage;
						if (oldImage != null && oldImage.StreamSource != null)
						{
							try
							{
								oldImage.StreamSource.Close();
							}
							catch { }
						}
					}
					this.BackgroundImageBrush.ImageSource = null;
				}
				
				// Force garbage collection to clear image cache
				GC.Collect();
				GC.WaitForPendingFinalizers();
				
				// Create a new BitmapImage with cache bypass
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				
				string uriString;
				if (actualPath != null)
				{
					// For local files, use file URI with timestamp to bypass cache
					uriString = new Uri(actualPath).ToString() + "?t=" + DateTime.Now.Ticks;
				}
				else
				{
					// For resource files, use pack URI with timestamp
					uriString = "pack://siteoforigin:,,,/Resources/background.png?t=" + DateTime.Now.Ticks;
				}
				
				bitmapImage.UriSource = new Uri(uriString, UriKind.RelativeOrAbsolute);
				bitmapImage.CacheOption = BitmapCacheOption.None;
				bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
				bitmapImage.EndInit();
				
				// Update the ImageBrush
				if (this.BackgroundImageBrush != null)
				{
					this.BackgroundImageBrush.ImageSource = bitmapImage;
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show("Error reloading background image: " + ex.Message);
			}
		}

		// Token: 0x06000047 RID: 71 RVA: 0x000031EC File Offset: 0x000013EC
		public static string XOR_EncryptDecrypt(string str)
		{
			char[] array = str.ToCharArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (char)((uint)array[i] ^ MainWindow.key);
			}
			return new string(array);
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00003221 File Offset: 0x00001421
		private void MainBgr_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				this.m_IsPressed = true;
				return;
			}
			this.m_IsPressed = false;
		}

		// Token: 0x06000049 RID: 73 RVA: 0x0000323A File Offset: 0x0000143A
		private void MainBgr_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			this.m_IsPressed = false;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003243 File Offset: 0x00001443
		private void MainBgr_MouseDown(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (this.m_IsPressed)
			{
				base.DragMove();
			}
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00003254 File Offset: 0x00001454
		private void button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			this._oa.From = new double?((double)1);
			this._oa.To = new double?(0.0);
			this._oa.Completed += delegate(object send, EventArgs ea)
			{
				Environment.Exit(0);
			};
			this._oa.Duration = new Duration(TimeSpan.FromMilliseconds(800.0));
			base.BeginAnimation(UIElement.OpacityProperty, this._oa);
		}

		// Token: 0x0600004C RID: 76 RVA: 0x000025C2 File Offset: 0x000007C2
		private void OnFadeComplete(object sender, EventArgs e)
		{
			base.Close();
		}

		// Token: 0x0600004D RID: 77 RVA: 0x000032E8 File Offset: 0x000014E8
		private void SetWinMode()
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Webzen\\Pegasus\\Config", true);
			if (!this.IsMinimized)
			{
				registryKey.SetValue("WindowMode", "1", RegistryValueKind.DWord);
				this.IsMinimized = true;
				return;
			}
			registryKey.SetValue("WindowMode", "0", RegistryValueKind.DWord);
			this.IsMinimized = false;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x0000333F File Offset: 0x0000153F
		private void WinMode_Click(object sender, RoutedEventArgs e)
		{
			this.SetWinMode();
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003347 File Offset: 0x00001547
		private void OptionBtn_Click(object sender, RoutedEventArgs e)
		{
			new Options
			{
				Owner = this,
				ShowInTaskbar = false
			}.Show();
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00003361 File Offset: 0x00001561
		private void minimaze_btn_Click(object sender, RoutedEventArgs e)
		{
			this.Minimaze();
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00003369 File Offset: 0x00001569
		private void Minimaze()
		{
			base.WindowState = WindowState.Minimized;
			if (WindowState.Minimized == base.WindowState)
			{
				this.ni.Visible = true;
				base.Hide();
				this.ni.ShowBalloonTip(1000, "MuEldryn", "Launcher minimizado!", ToolTipIcon.Info);
			}
		}

		// Token: 0x06000052 RID: 82 RVA: 0x000033A8 File Offset: 0x000015A8
		private void StartGameBtn_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				WebClient webClient = new WebClient();
				try
				{
					webClient.DownloadFileCompleted += this.MiniUpdDownloadFileCompleted;
					webClient.DownloadFileAsync(new Uri(this.SiteAdress + "MiniUpdate/update.info"), "update.info", "update.info");
					this.flag = 1;
				}
				catch (Exception ex)
				{
					System.Windows.MessageBox.Show(ex.Message);
				}
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x06000053 RID: 83 RVA: 0x0000342C File Offset: 0x0000162C
		private void CheckUpdateButton_Click(object sender, EventArgs e)
		{
			try
			{
				WebClient webClient = new WebClient();
				webClient.DownloadFileCompleted += this.FullUpdDownloadFileCompleted;
				webClient.DownloadFileAsync(new Uri(this.SiteAdress + "FullUpdate/client.info"), "client.info", "client.info");
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x06000054 RID: 84 RVA: 0x0000348C File Offset: 0x0000168C
		public void SetProgBar(int value)
		{
			value = Math.Max(0, Math.Min(100, value));
			this.Progressbar1.Width = 350.0 * (double)value / 100.0;
			this.Progress1Text.Text = value.ToString() + "%";
		}

		// Token: 0x06000055 RID: 85 RVA: 0x000034E8 File Offset: 0x000016E8
		public void SetProgBar2(int value)
		{
			value = Math.Max(0, Math.Min(100, value));
			this.Progressbar2.Width = 350.0 * (double)value / 100.0;
			this.Progress2Text.Text = value.ToString() + "%";
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00002C3E File Offset: 0x00000E3E
		public void setPicturebox4(string s)
		{
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00002C3E File Offset: 0x00000E3E
		public void setPicturebox7(string s)
		{
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00003544 File Offset: 0x00001744
		private void CheckPort(string Ip, int Port, System.Windows.Controls.Label LB)
		{
			if (!new TcpClient().BeginConnect(Ip, Port, null, null).AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1.0)))
			{
				LB.Foreground = System.Windows.Media.Brushes.Red;
				LB.Content = "OFFLINE";
				return;
			}
			LB.Foreground = System.Windows.Media.Brushes.RoyalBlue;
			LB.Content = "ONLINE";
		}

		// Token: 0x06000059 RID: 89 RVA: 0x000035A8 File Offset: 0x000017A8
		private void setTextLabelInvoke(string s, int type)
		{
			if (!base.Dispatcher.CheckAccess())
			{
				switch (type)
				{
				case 2:
					base.Dispatcher.Invoke(new Action<string>(this.setPicturebox4), new object[]
					{
						s
					});
					return;
				case 3:
					base.Dispatcher.Invoke(new Action<string>(this.setPicturebox7), new object[]
					{
						s
					});
					return;
				case 4:
					base.Dispatcher.Invoke(new Action<int>(this.SetProgBar), new object[]
					{
						int.Parse(s)
					});
					return;
				case 5:
					base.Dispatcher.Invoke(new Action<int>(this.SetProgBar2), new object[]
					{
						int.Parse(s)
					});
					return;
				default:
					return;
				}
			}
			else
			{
				switch (type)
				{
				case 2:
					this.setPicturebox4(s);
					return;
				case 3:
					this.setPicturebox7(s);
					return;
				case 4:
					this.SetProgBar(int.Parse(s));
					return;
				case 5:
					this.SetProgBar2(int.Parse(s));
					return;
				default:
					return;
				}
			}
		}

		// Token: 0x0600005A RID: 90 RVA: 0x000036C0 File Offset: 0x000018C0
		private void FullUpdDownloadFileCompleted(object sender, AsyncCompletedEventArgs evA)
		{
			try
			{
				if (evA.Error != null)
				{
					File.Delete((string)evA.UserState);
				}
				else if (!this.backgroundWorker1.IsBusy)
				{
					this.backgroundWorker1.RunWorkerAsync();
				}
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00003714 File Offset: 0x00001914
		private void MiniUpdDownloadFileCompleted(object sender, AsyncCompletedEventArgs evA)
		{
			if (evA.Error != null)
			{
				File.Delete((string)evA.UserState);
				return;
			}
			if (!this.backgroundWorker3.IsBusy)
			{
				this.backgroundWorker3.RunWorkerAsync();
			}
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00003748 File Offset: 0x00001948
		private void _DownloadCheckSumFileCompleted(object sender, AsyncCompletedEventArgs evA)
		{
			try
			{
				this.progbytes += this.newbytes;
				this.newbytes = 0.0;
				this.bytesold = 0.0;
				if (evA.Error != null)
				{
					string path = (string)evA.UserState;
					this.setTextLabelInvoke("An error occurred while loading...", 1);
					File.Delete(path);
				}
				this._busy.Set();
			}
			catch (Exception)
			{
				System.Windows.MessageBox.Show("Failed to download! [" + (string)evA.UserState + "]");
			}
		}

		// Token: 0x0600005D RID: 93 RVA: 0x000037EC File Offset: 0x000019EC
		private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				if (File.Exists("client.info"))
				{
					BinaryReader binaryReader = new BinaryReader(new FileStream("client.info", FileMode.Open));
					this.iUpdFileCnt = Convert.ToInt32(MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString()));
					int num = 0;
					while (binaryReader.PeekChar() > 0)
					{
						if (this.backgroundWorker1.CancellationPending)
						{
							e.Cancel = true;
							return;
						}
						if (num <= this.iUpdFileCnt)
						{
							this.setTextLabelInvoke((num * 100 / this.iUpdFileCnt).ToString() ?? "", 3);
							num++;
						}
						string text = MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString());
						string b = MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString());
						text.Split(new char[]
						{
							'\\'
						});
						new FileInfo(text).Directory.Create();
						this.setTextLabelInvoke("Checking [" + text + "]...", 1);
						try
						{
							Crc32 crc = new Crc32();
							string text2 = string.Empty;
							if (File.Exists(text))
							{
								byte[] buffer = File.ReadAllBytes(text);
								foreach (byte b2 in crc.ComputeHash(buffer))
								{
									text2 += b2.ToString("x2").ToUpper();
								}
							}
							else
							{
								text2 = null;
							}
							if ((text2 == null || text2 != b) && text2 != b)
							{
								WebClient webClient = new WebClient();
								webClient.DownloadFileAsync(new Uri(this.SiteAdress + "FullUpdate/" + text), text, text);
								webClient.DownloadFileCompleted += this._DownloadCheckSumFileCompleted;
								this.setTextLabelInvoke("Loading: [" + text + "]", 1);
								this._busy.WaitOne();
								this._busy.Reset();
							}
						}
						catch (Exception ex)
						{
							System.Windows.MessageBox.Show(ex.Message);
							Environment.Exit(0);
						}
					}
					binaryReader.Close();
					this.setTextLabelInvoke("true", 2);
					this.setTextLabelInvoke("100", 3);
					this.setTextLabelInvoke("Check is over, you can start the game!", 1);
				}
				else
				{
					this.setTextLabelInvoke("true", 2);
					this.setTextLabelInvoke("An error occurred while updating...", 1);
				}
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00003A50 File Offset: 0x00001C50
		private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				if (File.Exists("update.info"))
				{
					BinaryReader binaryReader = new BinaryReader(new FileStream("update.info", FileMode.Open));
					this.iUpdFileCnt = Convert.ToInt32(MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString()));
					Convert.ToInt32(MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString()));
					int num = 0;
					while (binaryReader.PeekChar() > 0)
					{
						if (this.backgroundWorker1.CancellationPending)
						{
							e.Cancel = true;
							return;
						}
						if (num <= this.iUpdFileCnt)
						{
							this.setTextLabelInvoke((num * 100 / this.iUpdFileCnt).ToString() ?? "", 3);
							num++;
						}
						string text = MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString());
						string b = MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString());
						Convert.ToInt32(MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString()));
						text.Split(new char[]
						{
							'\\'
						});
						new FileInfo(text).Directory.Create();
						this.setTextLabelInvoke("Checking [" + text + "]...", 1);
						try
						{
							Crc32 crc = new Crc32();
							string text2 = string.Empty;
							if (File.Exists(text))
							{
								byte[] buffer = File.ReadAllBytes(text);
								foreach (byte b2 in crc.ComputeHash(buffer))
								{
									text2 += b2.ToString("x2").ToUpper();
								}
							}
							else
							{
								text2 = null;
							}
							if ((text2 == null || text2 != b) && text2 != b)
							{
								WebClient webClient = new WebClient();
								string text3 = text;
								if (text == "Launcher.exe")
								{
									text3 = "NewLauncher.exe";
								}
								webClient.DownloadProgressChanged += this.client_DownloadProgressChanged;
								webClient.DownloadFileAsync(new Uri(this.SiteAdress + "MiniUpdate/" + text), text3, text3);
								webClient.DownloadFileCompleted += this._DownloadCheckSumFileCompleted;
								this.setTextLabelInvoke("Loading: [" + text + "]", 1);
								this._busy.WaitOne();
								this._busy.Reset();
							}
						}
						catch (Exception ex)
						{
							System.Windows.MessageBox.Show(ex.Message);
							Environment.Exit(0);
						}
					}
					binaryReader.Close();
					this.setTextLabelInvoke("true", 2);
					this.setTextLabelInvoke("100", 3);
					this.setTextLabelInvoke("Check is over, you can start the game!", 1);
				}
				else
				{
					this.setTextLabelInvoke("true", 2);
					this.setTextLabelInvoke("An error occurred while updating...", 1);
				}
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00003D08 File Offset: 0x00001F08
		private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			double num = double.Parse(e.BytesReceived.ToString());
			double num2 = double.Parse(e.TotalBytesToReceive.ToString());
			double d = num / num2 * 100.0;
			this.setTextLabelInvoke(int.Parse(Math.Truncate(d).ToString()).ToString(), 5);
			this.setTextLabelInvoke(int.Parse(Math.Truncate(d).ToString()).ToString(), 6);
			double totalFileSize = this.TotalFileSize;
			double d2 = (num + this.progbytes) / totalFileSize * 100.0;
			double num3 = num;
			num -= this.bytesold;
			this.bytesold = num3;
			this.newbytes += num;
			this.setTextLabelInvoke(int.Parse(Math.Truncate(d2).ToString()).ToString(), 4);
			this.setTextLabelInvoke(int.Parse(Math.Truncate(d2).ToString()).ToString(), 7);
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00003E20 File Offset: 0x00002020
		private void client_DownloadProgressChanged2(object sender, DownloadProgressChangedEventArgs e)
		{
			double num = double.Parse(e.BytesReceived.ToString());
			double num2 = double.Parse(e.TotalBytesToReceive.ToString());
			double d = num / num2 * 100.0;
			this.setTextLabelInvoke(int.Parse(Math.Truncate(d).ToString()).ToString(), 5);
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00003E84 File Offset: 0x00002084
		private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				this.SetCurrentFileText("Update finished!");
				this.SelfUpdate();
				string path = this.ExecutablePath + "\\client.info";
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			string path2 = this.ExecutablePath + "\\update.info";
			if (File.Exists(path2))
			{
				File.Delete(path2);
			}
			if (this.flag == 1)
			{
				this.StartGameClient();
			}
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00003F64 File Offset: 0x00002164
		private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				this.SetCurrentFileText("Update process finished!");
				this.SelfUpdate();
				string path = this.ExecutablePath + "\\client.info";
				if (File.Exists(path))
				{
					File.Delete(path);
				}
				string path2 = this.ExecutablePath + "\\update.info";
				if (File.Exists(path2))
				{
					File.Delete(path2);
				}
				if (this.flag == 1)
				{
					this.StartGameClient();
				}
			}
			catch (Exception)
			{
			}
		}

		/// <summary>
		/// Inicia o Main apontando IP/porta do Launcher.bmd (sem rebuild do client).
		/// </summary>
		private void StartGameClient()
		{
			try
			{
				if (!this.IsLauncherStarted)
				{
					MainWindow.mutex = new Mutex(false, "Launcher", out this.IsLauncherStarted);
				}
				string exe = Path.Combine(System.Windows.Forms.Application.StartupPath, this.Configs_.StartFile);
				string args = string.Format("connect /u{0} /p{1}", this.Configs_.ServerIP, this.Configs_.CSPort);
				this.WriteClientConfigIni();
				Process.Start(new ProcessStartInfo
				{
					FileName = exe,
					Arguments = args,
					WorkingDirectory = System.Windows.Forms.Application.StartupPath,
					UseShellExecute = true
				});
				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, "MuEldryn");
			}
		}

		/// <summary>
		/// Espelha IP/porta no config.ini do client (fallback se args falharem).
		/// </summary>
		private void WriteClientConfigIni()
		{
			try
			{
				string ini = Path.Combine(System.Windows.Forms.Application.StartupPath, "config.ini");
				string content;
				if (File.Exists(ini))
				{
					content = File.ReadAllText(ini);
					if (content.IndexOf("[CONNECTION SETTINGS]", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						content = System.Text.RegularExpressions.Regex.Replace(content, @"(?im)^ServerIP=.*$", "ServerIP=" + this.Configs_.ServerIP);
						content = System.Text.RegularExpressions.Regex.Replace(content, @"(?im)^ServerPort=.*$", "ServerPort=" + this.Configs_.CSPort);
					}
					else
					{
						content += "\r\n[CONNECTION SETTINGS]\r\nServerIP=" + this.Configs_.ServerIP + "\r\nServerPort=" + this.Configs_.CSPort + "\r\n";
					}
				}
				else
				{
					content = "[CONNECTION SETTINGS]\r\nServerIP=" + this.Configs_.ServerIP + "\r\nServerPort=" + this.Configs_.CSPort + "\r\n";
				}
				File.WriteAllText(ini, content);
			}
			catch
			{
			}
		}

	// Token: 0x06000063 RID: 99 RVA: 0x00004044 File Offset: 0x00002244
	private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				if (File.Exists("update.info"))
				{
					using (FileStream fileStream = new FileStream("update.info", FileMode.Open))
					{
						using (BinaryReader binaryReader = new BinaryReader(fileStream))
						{
							this.iUpdFileCnt = Convert.ToInt32(MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString()));
							Convert.ToInt32(MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString()));
							List<string> list = new List<string>();
							while (binaryReader.PeekChar() > 0)
							{
								if (this.backgroundWorker3.CancellationPending)
								{
									e.Cancel = true;
									return;
								}
								string text = MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString());
								string b2 = MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString());
								double num = (double)Convert.ToInt32(MainWindow.XOR_EncryptDecrypt(binaryReader.ReadString()));
								new FileInfo(text).Directory.Create();
								this.SetCurrentFileText("Checking: " + text);
								Crc32 crc = new Crc32();
								string a = null;
								if (File.Exists(text))
								{
									byte[] buffer = File.ReadAllBytes(text);
									a = string.Concat(from b in crc.ComputeHash(buffer)
									select b.ToString("X2"));
								}
								if (a != b2)
								{
									list.Add(text);
									this.TotalFileSize += num;
									this.SetCurrentFileText("Need update: " + text);
								}
							}
							this.filesToDownload = list.Count;
							this.filesDownloaded = 0;
							foreach (string text2 in list)
							{
								WebClient webClient = new WebClient();
								webClient.DownloadFileCompleted += delegate(object s, AsyncCompletedEventArgs args)
								{
									this.filesDownloaded++;
									if (this.filesDownloaded == this.filesToDownload)
									{
										this.SetCurrentFileText("Check finished!");
									}
								};
								webClient.DownloadFileAsync(new Uri(this.SiteAdress + "MiniUpdate/" + text2), text2);
							}
							if (this.filesToDownload == 0)
							{
								this.SetCurrentFileText("Check finished!");
							}
						}
						goto IL_1F0;
					}
				}
				this.SetCurrentFileText("Update file not found!");
				IL_1F0:;
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message);
			}
		}

		// Token: 0x06000064 RID: 100 RVA: 0x000042B4 File Offset: 0x000024B4
		private void backgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				if (!this.backgroundWorker2.IsBusy)
				{
					this.backgroundWorker2.RunWorkerAsync();
				}
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x06000065 RID: 101 RVA: 0x000042F0 File Offset: 0x000024F0
		private void StartExec(string FileName)
		{
			try
			{
				Process.Start(this.ExecutablePath + "\\" + FileName);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("StartExec() {0}", ex.Message));
			}
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00004340 File Offset: 0x00002540
		private void RunUpdater()
		{
			if (this.CheckIsNewLauncherPresent(this.ExecutablePath + "\\NewLauncher.exe"))
			{
				new Process
				{
					StartInfo = 
					{
						Verb = "runas",
						FileName = this.ExecutablePath + "\\MuUpdater.exe"
					}
				}.Start();
				Environment.Exit(0);
			}
		}

		// Token: 0x06000067 RID: 103 RVA: 0x000043A4 File Offset: 0x000025A4
		private void FacebookClick(object sender, RoutedEventArgs e)
		{
			this.OpenSite("/register");
		}

		// Token: 0x06000068 RID: 104 RVA: 0x000043D4 File Offset: 0x000025D4
		private void DiscordClick(object sender, RoutedEventArgs e)
		{
			this.OpenSite("/info");
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00004404 File Offset: 0x00002604
		private void DiscordButton_Click(object sender, RoutedEventArgs e)
		{
			this.OpenSite("/");
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00004424 File Offset: 0x00002624
		private void ForumClick(object sender, RoutedEventArgs e)
		{
			this.OpenSite("/");
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00004454 File Offset: 0x00002654
		private void InstagramClick(object sender, RoutedEventArgs e)
		{
			this.OpenSite("/donate");
		}

		private void RankingsClick(object sender, RoutedEventArgs e)
		{
			this.OpenSite("/rankings");
		}

		private void OpenSite(string path)
		{
			try
			{
				// Site Morpheus (não confundir com a URL de update de arquivos)
				string baseUrl = "http://127.0.0.1:8090";
				if (string.IsNullOrEmpty(path)) path = "/";
				if (!path.StartsWith("/")) path = "/" + path;
				Process.Start(new ProcessStartInfo
				{
					FileName = baseUrl.TrimEnd('/') + path,
					UseShellExecute = true
				});
			}
			catch
			{
			}
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00004484 File Offset: 0x00002684
		private void SelfUpdate()
		{
			try
			{
				if (this.CheckIsNewLauncherPresent(this.ExecutablePath + "\\NewLauncher.exe"))
				{
					this.RunUpdater();
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("SelfUpdate() {0}", ex.Message));
			}
		}

		// Token: 0x0600006D RID: 109 RVA: 0x000044D4 File Offset: 0x000026D4
		private bool GetSelfUpdateState()
		{
			bool result;
			try
			{
				result = Settings.Default.IsSelfUpdatePresent;
			}
			catch (Exception)
			{
				result = false;
			}
			return result;
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00004504 File Offset: 0x00002704
		private void SetSelfUpdateState(bool State)
		{
			try
			{
				Settings.Default.IsSelfUpdatePresent = State;
				Settings.Default.Save();
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x0600006F RID: 111 RVA: 0x0000453C File Offset: 0x0000273C
		private bool CheckIsNewLauncherPresent(string FilePath)
		{
			bool result;
			try
			{
				if (File.Exists(FilePath))
				{
					this.SetSelfUpdateState(true);
					result = true;
				}
				else
				{
					this.SetSelfUpdateState(false);
					result = false;
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("CheckIsNewLauncherPresent() {0}", ex.Message));
				this.SetSelfUpdateState(false);
				result = false;
			}
			return result;
		}

		// Token: 0x06000070 RID: 112 RVA: 0x0000459C File Offset: 0x0000279C
		private string[,] getRssData(string channel)
		{
			string[,] result;
			try
			{
				Stream responseStream = WebRequest.Create(channel).GetResponse().GetResponseStream();
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(responseStream);
				XmlNodeList xmlNodeList = xmlDocument.SelectNodes("rss/channel/item");
				string[,] array = new string[100, 4];
				for (int i = 0; i < xmlNodeList.Count; i++)
				{
					XmlNode xmlNode = xmlNodeList.Item(i).SelectSingleNode("title");
					if (xmlNode != null)
					{
						array[i, 0] = xmlNode.InnerText;
					}
					else
					{
						array[i, 0] = "";
					}
					xmlNode = xmlNodeList.Item(i).SelectSingleNode("pubDate");
					if (xmlNode != null)
					{
						array[i, 1] = xmlNode.InnerText;
					}
					else
					{
						array[i, 1] = "";
					}
					xmlNode = xmlNodeList.Item(i).SelectSingleNode("link");
					if (xmlNode != null)
					{
						array[i, 2] = xmlNode.InnerText;
					}
					else
					{
						array[i, 2] = "";
					}
				}
				result = array;
			}
			catch (Exception)
			{
				result = new string[100, 4];
			}
			return result;
		}

		// Token: 0x06000071 RID: 113 RVA: 0x000046B4 File Offset: 0x000028B4
		private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)this.Language.Template.FindName("PART_EditableTextBox", this.Language);
			if (textBox != null)
			{
				ComboBoxItem comboBoxItem = this.Language.SelectedItem as ComboBoxItem;
				if (comboBoxItem != null)
				{
					object content = comboBoxItem.Content;
					string text = ((content != null) ? content.ToString() : null) ?? "ENG";
					textBox.Text = text;
					this.ApplyLanguage(text);
					this.SaveRegistryConfigs(text);
				}
			}
		}

		// Token: 0x06000072 RID: 114 RVA: 0x0000472A File Offset: 0x0000292A
		private void ApplyLanguage(string lang)
		{
			Console.WriteLine("Selected language: " + lang);
		}

		// Token: 0x06000073 RID: 115 RVA: 0x0000473C File Offset: 0x0000293C
		private void LoadRegistryConfigs()
		{
			try
			{
				using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Pegasus\\Config"))
				{
					if (registryKey != null)
					{
						object value = registryKey.GetValue("LangSelection");
						string text = ((value != null) ? value.ToString() : null) ?? "ENG";
						foreach (object obj in ((IEnumerable)this.Language.Items))
						{
							ComboBoxItem comboBoxItem = (ComboBoxItem)obj;
							if ((string)comboBoxItem.Content == text)
							{
								this.Language.SelectedItem = comboBoxItem;
								break;
							}
						}
						System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)this.Language.Template.FindName("PART_EditableTextBox", this.Language);
						if (textBox != null)
						{
							textBox.Text = text;
						}
						this.ApplyLanguage(text);
						
						// Check for background image path in registry
						object bgImageValue = registryKey.GetValue("BackgroundImage");
						if (bgImageValue != null)
						{
							string bgImagePath = bgImageValue.ToString();
							// Verify the file still exists, if not remove from registry
							string fullPath = Path.Combine(this.ExecutablePath, bgImagePath.TrimStart('/', '\\'));
							if (File.Exists(fullPath) || File.Exists(bgImagePath))
							{
								this.ReloadBackgroundImage(bgImagePath);
							}
							else
							{
								// Remove invalid path from registry
								registryKey.DeleteValue("BackgroundImage", false);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show("Error loading configs: " + ex.Message);
			}
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00004868 File Offset: 0x00002A68
		private void SaveRegistryConfigs(string lang)
		{
			try
			{
				using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Pegasus\\Config"))
				{
					if (registryKey != null)
					{
						registryKey.SetValue("LangSelection", lang);
					}
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show("Error saving configs: " + ex.Message);
			}
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000048DC File Offset: 0x00002ADC
		private void SetCurrentFileText(string text)
		{
			if (!base.Dispatcher.CheckAccess())
			{
				base.Dispatcher.Invoke(new Action<string>(this.SetCurrentFileText), new object[]
				{
					text
				});
				return;
			}
			this.CurrentFileText.Text = text;
		}

		// Token: 0x06000076 RID: 118 RVA: 0x0000491A File Offset: 0x00002B1A
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			this.SetProgBar(100);
			this.SetProgBar2(100);
			this.LoadRegistryConfigs();
			
			// Check for imagebk2.jpg and reload if it exists (this will override registry setting)
			string imagebk2Path = "imagebk2.jpg";
			string fullImagePath = Path.Combine(this.ExecutablePath, imagebk2Path);
			if (File.Exists(fullImagePath) || File.Exists(imagebk2Path))
			{
				this.ReloadBackgroundImage(imagebk2Path);
				// Save to registry for next time
				try
				{
					using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Pegasus\\Config"))
					{
						if (registryKey != null)
						{
							registryKey.SetValue("BackgroundImage", imagebk2Path);
						}
					}
				}
				catch { }
			}
			
			base.BeginAnimation(UIElement.OpacityProperty, this._oa);
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00002C3E File Offset: 0x00000E3E
		private void Image1_png_ImageFailed(object sender, ExceptionRoutedEventArgs e)
		{
		}

		// Token: 0x04000020 RID: 32
		private string[,] rssData;

		// Token: 0x04000021 RID: 33
		private bool IsMinimized;

		// Token: 0x04000022 RID: 34
		private string SiteAdress = "http://200.11.121.89/update/";

		// Token: 0x04000023 RID: 35
		private int iUpdFileCnt;

		// Token: 0x04000024 RID: 36
		private double TotalFileSize;

		// Token: 0x04000025 RID: 37
		private static uint key = 99U;

		// Token: 0x04000026 RID: 38
		public static MainWindow _FormInstance;

		// Token: 0x04000027 RID: 39
		private NotifyIcon ni = new NotifyIcon();

		// Token: 0x04000028 RID: 40
		private BackgroundWorker backgroundWorker1 = new BackgroundWorker();

		// Token: 0x04000029 RID: 41
		private BackgroundWorker backgroundWorker2 = new BackgroundWorker();

		// Token: 0x0400002A RID: 42
		private BackgroundWorker backgroundWorker3 = new BackgroundWorker();

		// Token: 0x0400002B RID: 43
		private ManualResetEvent _busy = new ManualResetEvent(false);

		// Token: 0x0400002C RID: 44
		private System.Windows.Forms.Timer timerTime = new System.Windows.Forms.Timer();

		// Token: 0x0400002D RID: 45
		private readonly DoubleAnimation _oa;

		// Token: 0x0400002E RID: 46
		private Config Configs_;

		// Token: 0x0400002F RID: 47
		private static Mutex mutex;

		// Token: 0x04000030 RID: 48
		private bool IsLauncherStarted;

		// Token: 0x04000031 RID: 49
		private bool m_IsPressed;

		// Token: 0x04000032 RID: 50
		private const double ProgressMaxWidth = 350.0;

		// Token: 0x04000033 RID: 51
		private double newbytes;

		// Token: 0x04000034 RID: 52
		private double bytesold;

		// Token: 0x04000035 RID: 53
		private double progbytes;

		// Token: 0x04000036 RID: 54
		private int flag;

		// Token: 0x04000037 RID: 55
		private int filesToDownload;

		// Token: 0x04000038 RID: 56
		private int filesDownloaded;

		// Token: 0x04000039 RID: 57
		private string ExecutablePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

		// Token: 0x0200000B RID: 11
		private enum ShowWindowEnum
		{
			// Token: 0x0400004E RID: 78
			Hide,
			// Token: 0x0400004F RID: 79
			ShowNormal,
			// Token: 0x04000050 RID: 80
			ShowMinimized,
			// Token: 0x04000051 RID: 81
			ShowMaximized,
			// Token: 0x04000052 RID: 82
			Maximize = 3,
			// Token: 0x04000053 RID: 83
			ShowNormalNoActivate,
			// Token: 0x04000054 RID: 84
			Show,
			// Token: 0x04000055 RID: 85
			Minimize,
			// Token: 0x04000056 RID: 86
			ShowMinNoActivate,
			// Token: 0x04000057 RID: 87
			ShowNoActivate,
			// Token: 0x04000058 RID: 88
			Restore,
			// Token: 0x04000059 RID: 89
			ShowDefault,
			// Token: 0x0400005A RID: 90
			ForceMinimized
		}

		// Token: 0x0200000C RID: 12
		private struct Windowplacement
		{
			// Token: 0x0400005B RID: 91
			public int length;

			// Token: 0x0400005C RID: 92
			public int flags;

			// Token: 0x0400005D RID: 93
			public int showCmd;

			// Token: 0x0400005E RID: 94
			public System.Drawing.Point ptMinPosition;

			// Token: 0x0400005F RID: 95
			public System.Drawing.Point ptMaxPosition;

			// Token: 0x04000060 RID: 96
			public Rectangle rcNormalPosition;
		}
	}
}
