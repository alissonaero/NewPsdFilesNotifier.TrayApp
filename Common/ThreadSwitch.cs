#nullable enable

using NewPsdFilesNotifier.TrayApp.Common;
using NewPsdFilesNotifier.TrayApp;
using System.Windows;
using SW = System.Windows;

namespace NewPsdFilesNotifier.TrayApp.Common
{
	internal static class MessageBoxHelper
	{
		private static void ShowMessage(string message, string title, MessageBoxImage image)
		{
			if (App.UiDispatcher == null)
				throw new InvalidOperationException("UI Dispatcher is not set.");

			if (!App.UiDispatcher.CheckAccess())
			{
				App.UiDispatcher.Invoke(() => SW.MessageBox.Show(message, title, MessageBoxButton.OK, image));
				return;
			}

			SW.MessageBox.Show(message, title, MessageBoxButton.OK, image);
		}

		public static void ShowError(string message, string title = "Error")
			=> ShowMessage(message, title, MessageBoxImage.Error);

		public static void ShowInfo(string message, string title = "Info")
			=> ShowMessage(message, title, MessageBoxImage.Information);

		public static void ShowWarning(string message, string title = "Warning")
			=> ShowMessage(message, title, MessageBoxImage.Warning);
	}

	internal static class FormInvoker
	{
		public static void ShowForm(Window form, bool modal = false)
		{
			ArgumentNullException.ThrowIfNull(form);

			if (App.UiDispatcher == null)
				throw new InvalidOperationException("UI Dispatcher is not set.");

			App.UiDispatcher.Invoke(() =>
			{
				if (modal)
					form.ShowDialog();  
				else
					form.Show();       
			});
		}
	}

}