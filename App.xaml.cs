using PsPrintNotifier.TrayApp.Common.CrossThreadingHelpers;
using System.Windows;
using System.Windows.Threading;

namespace PsPrintNotifier.TrayApp
{
	public partial class App : System.Windows.Application
	{
		private static Mutex? _appMutex;

		public static Dispatcher? UiDispatcher { get; private set; }

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			const string mutexName = "PsPrintNotifier.TrayApp.Singleton";
			bool createdNew;

			_appMutex = new Mutex(true, mutexName, out createdNew);

			if (!createdNew)
			{
				MessageBoxHelper.ShowWarning("O aplicativo já está em execução", "Aviso");
				Shutdown();

				return;
			}

			UiDispatcher = Dispatcher.CurrentDispatcher;
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			var startup = new StartUp();
		}


	}
}
