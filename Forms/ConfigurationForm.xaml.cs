#nullable disable

using PsPrintNotifier.TrayApp.Common;
using PsPrintNotifier.TrayApp.Common.CrossThreadingHelpers;
using PsPrintNotifier.TrayApp.Models;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SW = System.Windows;


namespace PsPrintNotifier.TrayApp.Forms
{
 
	public partial class ConfigurationForm : Window
	{
		private AppSettings _currentSettings;

		public ConfigurationForm()
		{
			InitializeComponent();

			LoadSettings();
		}

		private void LoadSettings()
		{
			_currentSettings = SettingsManager.Load();
			FolderPathTextBox.Text = _currentSettings.FolderPath ?? string.Empty;
			PhotoshopPathTextBox.Text = _currentSettings.PhotoShopFolderPath ?? string.Empty;
			ApiKeyTextBox.Text = _currentSettings.WhatsAppApiKey ?? string.Empty;

			string numero = _currentSettings.WhatsAppContact ?? string.Empty;

			if (numero[2..3].Equals("9"))
			{
				numero = numero.Insert(2, "9");
			}

			WhatsAppTextBox.Text = numero;
		}

		private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new System.Windows.Forms.FolderBrowserDialog();
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				FolderPathTextBox.Text = dialog.SelectedPath;
			}
		}



		private bool _isChangingText = false;

		private void WhatsAppTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_isChangingText) return;

			var textBox = sender as SW.Controls.TextBox;
			if (textBox == null) return;

			_isChangingText = true;

			int caretIndex = textBox.CaretIndex;

			string numbers = Regex.Replace(textBox.Text, @"\D", "");
			if (numbers.Length > 11)
				numbers = numbers.Substring(0, 11);

			string formatted = Helper.ApplyWhatsupMask(numbers);

			textBox.Text = formatted;

			// Adjust caret
			int nonDigitCount = Regex.Matches(formatted.Substring(0, caretIndex), @"\D").Count;
			caretIndex = numbers.Length + nonDigitCount;
			textBox.CaretIndex = Math.Min(caretIndex, textBox.Text.Length);

			_isChangingText = false;
		}

		private void WhatsAppTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			// Only allow digits
			e.Handled = !Regex.IsMatch(e.Text, @"\d");
		}


		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			string folderPath = FolderPathTextBox.Text.Trim();
			string whatsapp = WhatsAppTextBox.Text.Trim();
			string photoshopPath = PhotoshopPathTextBox.Text.Trim();
			string apiKey = ApiKeyTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
			{
				MessageBoxHelper.ShowWarning ("Informe um caminho de pasta válido.", "Erro");
				return;
			}

			if (!string.IsNullOrEmpty(whatsapp) && whatsapp.Length < 10)
			{
				MessageBoxHelper.ShowWarning("Número de WhatsApp parece incompleto.", "Erro" );
				return;
			}

			if (string.IsNullOrWhiteSpace(photoshopPath) || !File.Exists(photoshopPath))
			{
				MessageBoxHelper.ShowWarning("Caminho do Photoshop inválido ou inexistente.", "Erro");
				return;
			}

			if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.All(char.IsDigit))
			{
				MessageBoxHelper.ShowWarning("API Key do CallMeBot deve ser um número.", "Erro" );
				return;
			}

			_currentSettings = new AppSettings
			{
				FolderPath = folderPath,
				WhatsAppContact = Helper.ClearWhatsAppNumber(whatsapp),
				PhotoShopFolderPath  = photoshopPath,
				WhatsAppApiKey = apiKey
			};

			bool saved = SettingsManager.Save(_currentSettings);

			if (saved)
			{
				MessageBoxHelper.ShowInfo("Configuração salva com sucesso.", "Sucesso");
				this.Close();
			}
			else
			{
				MessageBoxHelper.ShowError("Houve uma falha ao salvar as configurações.", "Erro");
			}
		}

		private void SelectPhotoshopPath_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.OpenFileDialog
			{
				Title = "Selecione o executável do Photoshop",
				Filter = "Executáveis (*.exe)|*.exe",
				CheckFileExists = true,
				Multiselect = false
			};

			if (dialog.ShowDialog() == true)
			{
				PhotoshopPathTextBox.Text = dialog.FileName;
			}
		}

		private void WhatsAppTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			var textBox = sender as SW.Controls.TextBox;
			if (textBox == null) return;

			string numero = Regex.Replace(textBox.Text, @"\D", "");

			if (numero.Length == 10)
				textBox.Text = Regex.Replace(numero, @"(\d{2})(\d{4})(\d{4})", "($1) $2-$3");
			else if (numero.Length == 11)
				textBox.Text = Regex.Replace(numero, @"(\d{2})(\d{5})(\d{4})", "($1) $2-$3");
			// Se estiver incompleto, mantém como está
		}

		private void WhatsAppTextBox_Loaded(object sender, RoutedEventArgs e) => WhatsAppTextBox_LostFocus(sender, e);
		 
	}
}
