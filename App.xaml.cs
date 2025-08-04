using NewPsdFilesNotifier.TrayApp.Common;
using System.Windows;
using System.Windows.Threading;

namespace NewPsdFilesNotifier.TrayApp
{
	public partial class App : System.Windows.Application
	{
		private static Mutex? _appMutex;

		public static Dispatcher? UiDispatcher { get; private set; }

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			UiDispatcher = Dispatcher.CurrentDispatcher;

			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			const string mutexName = "NewPsdFilesNotifier.TrayApp";

			_appMutex = new Mutex(true, mutexName, out bool createdNew);

			if (!createdNew)
			{
				MessageBoxHelper.ShowWarning("O NewPsdFilesNotifier já está em execução", "NewPsdFilesNotifier.TrayApp");

				Shutdown();

				return;
			}

			_ = new StartUp();
		}


	}
}
