using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;

namespace Launcher
{
	// Token: 0x02000007 RID: 7
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			
			// Add global exception handler
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			this.DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = e.ExceptionObject as Exception;
			MessageBox.Show("Fatal error: " + (ex?.Message ?? "Unknown error"), "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show("Error: " + e.Exception.Message + "\n\nStack trace:\n" + e.Exception.StackTrace, 
				"Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
			e.Handled = true;
		}
	}
}
