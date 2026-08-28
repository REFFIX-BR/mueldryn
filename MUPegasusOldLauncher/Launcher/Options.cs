using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace Launcher
{
	// Token: 0x02000006 RID: 6
	public partial class Options : Window
	{
		// Token: 0x06000020 RID: 32 RVA: 0x0000245C File Offset: 0x0000065C
		public Options()
		{
			this.InitializeComponent();
			base.MouseDown += new MouseButtonEventHandler(this.MainBgr_MouseDown);
			base.PreviewMouseDown += this.MainBgr_PreviewMouseDown;
			base.PreviewMouseUp += this.MainBgr_PreviewMouseUp;
			this._oa = new DoubleAnimation();
			this._oa.From = new double?(0.0);
			this._oa.To = new double?((double)1);
			this._oa.Duration = new Duration(TimeSpan.FromMilliseconds(800.0));
			base.BeginAnimation(UIElement.OpacityProperty, this._oa);
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002510 File Offset: 0x00000710
		private void MainBgr_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				this.m_IsPressed = true;
				return;
			}
			this.m_IsPressed = false;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002529 File Offset: 0x00000729
		private void MainBgr_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			this.m_IsPressed = false;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002532 File Offset: 0x00000732
		private void MainBgr_MouseDown(object sender, MouseEventArgs e)
		{
			if (this.m_IsPressed)
			{
				base.DragMove();
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002544 File Offset: 0x00000744
		private void Close_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			this._oa.From = new double?((double)1);
			this._oa.To = new double?(0.0);
			this._oa.Completed += delegate(object send, EventArgs ea)
			{
				base.Close();
			};
			this._oa.Duration = new Duration(TimeSpan.FromMilliseconds(800.0));
			base.BeginAnimation(UIElement.OpacityProperty, this._oa);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x000025C2 File Offset: 0x000007C2
		private void Cansel_Click(object sender, RoutedEventArgs e)
		{
			base.Close();
		}

		// Token: 0x06000026 RID: 38 RVA: 0x000025CC File Offset: 0x000007CC
		private void LoadRegistryConfigs()
		{
			try
			{
				this.isLoading = true;
				RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Pegasus\\Config");
				if (registryKey != null)
				{
					TextBox accountBox = this.AccountBox;
					object value = registryKey.GetValue("ID");
					accountBox.Text = (((value != null) ? value.ToString() : null) ?? "");
					object value2 = registryKey.GetValue("LangSelection");
					string a = ((value2 != null) ? value2.ToString() : null) ?? "ENG";
					this.Language.Items.Clear();
					this.Language.Items.Add("VNE");
					this.Language.Items.Add("ENG");
					this.Language.SelectedItem = ((a == "VNE") ? "VNE" : "ENG");
					int num = 1;
					int.TryParse(registryKey.GetValue("SoundOnOFF", 1).ToString(), out num);
					this.Effects.IsChecked = new bool?(num == 1);
					int num2 = 1;
					int.TryParse(registryKey.GetValue("MusicOnOFF", 1).ToString(), out num2);
					this.Music.IsChecked = new bool?(num2 == 1);
					int num3 = 1;
					int.TryParse(registryKey.GetValue("WindowMode", 1).ToString(), out num3);
					this.WinMode.IsChecked = new bool?(num3 == 0);
					string[] array = new string[]
					{
						"1366x768",
						"1440x900",
						"1600x900",
						"1680x1050",
						"1920x1080"
					};
					this.Resolution.Items.Clear();
					foreach (string newItem in array)
					{
						this.Resolution.Items.Add(newItem);
					}
					Dictionary<int, int> dictionary = new Dictionary<int, int>
					{
						{
							0,
							0
						},
						{
							5,
							1
						},
						{
							6,
							2
						},
						{
							7,
							3
						},
						{
							8,
							4
						}
					};
					int key = 4;
					int.TryParse(registryKey.GetValue("Resolution", 4).ToString(), out key);
					this.Resolution.SelectedIndex = (dictionary.ContainsKey(key) ? dictionary[key] : 4);
					int num4 = 1;
					int num5 = 0;
					int.TryParse(registryKey.GetValue("Color16bit", 1).ToString(), out num4);
					int.TryParse(registryKey.GetValue("Color32bit", 0).ToString(), out num5);
					this.Color16bit.IsChecked = new bool?(num4 == 1);
					this.Color32bit.IsChecked = new bool?(num5 == 1);
					registryKey.Close();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading configs: " + ex.Message);
			}
			finally
			{
				this.isLoading = false;
			}
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000028EC File Offset: 0x00000AEC
		private void SaveRegistryConfigs()
		{
			try
			{
				RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Pegasus\\Config");
				if (registryKey != null)
				{
					registryKey.SetValue("ID", this.AccountBox.Text);
					RegistryKey registryKey2 = registryKey;
					string name = "LangSelection";
					object selectedItem = this.Language.SelectedItem;
					registryKey2.SetValue(name, ((selectedItem != null) ? selectedItem.ToString() : null) ?? "ENG");
					RegistryKey registryKey3 = registryKey;
					string name2 = "SoundOnOFF";
					bool? isChecked = this.Effects.IsChecked;
					bool flag = true;
					registryKey3.SetValue(name2, (isChecked.GetValueOrDefault() == flag & isChecked != null) ? 1 : 0);
					RegistryKey registryKey4 = registryKey;
					string name3 = "MusicOnOFF";
					isChecked = this.Music.IsChecked;
					flag = true;
					registryKey4.SetValue(name3, (isChecked.GetValueOrDefault() == flag & isChecked != null) ? 1 : 0);
					RegistryKey registryKey5 = registryKey;
					string name4 = "WindowMode";
					isChecked = this.WinMode.IsChecked;
					flag = true;
					registryKey5.SetValue(name4, (isChecked.GetValueOrDefault() == flag & isChecked != null) ? 0 : 1);
					Dictionary<int, int> dictionary = new Dictionary<int, int>
					{
						{
							0,
							0
						},
						{
							1,
							5
						},
						{
							2,
							6
						},
						{
							3,
							7
						},
						{
							4,
							8
						}
					};
					int key = this.Resolution.SelectedIndex;
					if (!dictionary.ContainsKey(key))
					{
						key = 4;
					}
					registryKey.SetValue("Resolution", dictionary[key]);
					RegistryKey registryKey6 = registryKey;
					string name5 = "Color16bit";
					isChecked = this.Color16bit.IsChecked;
					flag = true;
					registryKey6.SetValue(name5, (isChecked.GetValueOrDefault() == flag & isChecked != null) ? 1 : 0);
					RegistryKey registryKey7 = registryKey;
					string name6 = "Color32bit";
					isChecked = this.Color32bit.IsChecked;
					flag = true;
					registryKey7.SetValue(name6, (isChecked.GetValueOrDefault() == flag & isChecked != null) ? 1 : 0);
					registryKey.Close();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error saving configs: " + ex.Message);
			}
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002B08 File Offset: 0x00000D08
		private void Color16bit_Checked(object sender, RoutedEventArgs e)
		{
			if (this.isLoading)
			{
				return;
			}
			this.Color32bit.IsChecked = new bool?(false);
			this.SaveRegistryConfigs();
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002B2A File Offset: 0x00000D2A
		private void Color32bit_Checked(object sender, RoutedEventArgs e)
		{
			if (this.isLoading)
			{
				return;
			}
			this.Color16bit.IsChecked = new bool?(false);
			this.SaveRegistryConfigs();
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002B4C File Offset: 0x00000D4C
		private void AttachEvents()
		{
			this.Effects.Checked += delegate(object s, RoutedEventArgs e)
			{
				this.SaveRegistryConfigs();
			};
			this.Effects.Unchecked += delegate(object s, RoutedEventArgs e)
			{
				this.SaveRegistryConfigs();
			};
			this.Music.Checked += delegate(object s, RoutedEventArgs e)
			{
				this.SaveRegistryConfigs();
			};
			this.Music.Unchecked += delegate(object s, RoutedEventArgs e)
			{
				this.SaveRegistryConfigs();
			};
			this.WinMode.Checked += delegate(object s, RoutedEventArgs e)
			{
				this.SaveRegistryConfigs();
			};
			this.WinMode.Unchecked += delegate(object s, RoutedEventArgs e)
			{
				this.SaveRegistryConfigs();
			};
			this.Language.SelectionChanged += delegate(object s, SelectionChangedEventArgs e)
			{
				this.SaveRegistryConfigs();
			};
			this.Resolution.SelectionChanged += delegate(object s, SelectionChangedEventArgs e)
			{
				this.SaveRegistryConfigs();
			};
			this.AccountBox.LostFocus += delegate(object s, RoutedEventArgs e)
			{
				this.SaveRegistryConfigs();
			};
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002C28 File Offset: 0x00000E28
		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			this.LoadRegistryConfigs();
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002C30 File Offset: 0x00000E30
		private void Save_Click(object sender, RoutedEventArgs e)
		{
			this.SaveRegistryConfigs();
			base.Close();
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002C3E File Offset: 0x00000E3E
	private void Resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
	}

	// Token: 0x04000010 RID: 16
	private int Resolution_Value;

	// Token: 0x04000011 RID: 17
	private readonly DoubleAnimation _oa;

	// Token: 0x04000012 RID: 18
	private bool m_IsPressed;

	// Token: 0x04000013 RID: 19
	private bool isLoading;
	}
}
