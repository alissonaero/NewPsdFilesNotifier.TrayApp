#nullable disable

///TODO: Criar um drop down para selecionar a frequência de verificação dos arquivos na pasta monitorada.
///TODO: Transformar a aplicação para que se possa monitorar mais de uma pasta e mais de um tipo de arquivo.

using NewPsdFilesNotifier.TrayApp.Common;
using NewPsdFilesNotifier.TrayApp.Models;
using NewPsdFilesNotifier.TrayApp.Services;
using NewPsdFilesNotifier.TrayApp.Forms;
using System.Diagnostics;
using System.IO;
using System.Text;
using ST = System.Timers;
using SW = System.Windows;

namespace NewPsdFilesNotifier.TrayApp
{
	public class StartUp : IDisposable
	{
		NotifyIcon notifyIcon;
		string ProcessedFilesFolder;
		bool isReadyToUse = false;
		FileSystemWatcher watcher;
		string pathToWatch;
		ST.Timer clientStatusTimer;
		HashSet<string> checkedOutFiles = [];
		bool dirIsReady = false;
		DateTime lastCheckedTime = DateTime.MinValue;
		ToolStripMenuItem printNowStripe;
		bool userNotified = false;
		AppSettings settings;
		readonly WhatsAppNotifier msgSender;
		DateTime lastNotificationTime = DateTime.MinValue;

		public StartUp()
		{
			settings = LoadAndEnsureSettings();

			msgSender = new WhatsAppNotifier(settings);

			SetAppConfigs();
		}

		private void SetAppConfigs()
		{

			if (settings == null) return;

			pathToWatch = settings.FolderPath;

			if (!IsDirectoryReady(pathToWatch))
			{
				ShowTrayNotification("Unidade não pronta", "Aguardando Google Drive estar disponível...", 4000);

				return;
			}

			ProcessedFilesFolder = Path.Combine(pathToWatch, "PROCESSADOS");

			try
			{
				if (!Directory.Exists(ProcessedFilesFolder))
					Directory.CreateDirectory(ProcessedFilesFolder);
			}
			catch (FileNotFoundException)
			{
				ShowTrayNotification("Unidade não encontrada", "Aguardando Google Drive estar disponível...", 4000);
				return;
			}
			catch (UnauthorizedAccessException) //Para drivers virtuais como Google Drive, OneDrive, etc., alguns diretórios não permitem a criação de subpastas.
			{
				MessageBoxHelper.ShowWarning(
					"Não foi possível criar a pasta PROCESSADOS. Verifique as permissões de escrita na pasta monitorada.",
					"Erro de Configuração");

				if (settings == null) return;
			}

			// Criação do ícone da bandeja
			notifyIcon = new NotifyIcon
			{
				Visible = true,
				Text = "Monitor de Impressão PSD",
				BalloonTipTitle = "Serviço de Impressão",
				BalloonTipText = "Rodando em background."
			};

			using (var stream = new MemoryStream(NewPsdFilesNotifier.TrayApp.Properties.Resources.PsdIcon))
			{
				notifyIcon.Icon = new Icon(stream);
			}

			// Menu de contexto
			var contextMenu = new ContextMenuStrip();

			var header = new ToolStripMenuItem("Print Manager v1.0.3.3") { Enabled = false };
			contextMenu.Items.Add(header);

			contextMenu.Items.Add(new ToolStripSeparator());

			printNowStripe = new ToolStripMenuItem("Imprimir Agora", null, (s, e) => PrintFiles());
			contextMenu.Items.Add(printNowStripe);
			contextMenu.Items.Add("Timer Status", null, (s, e) => CheckStatus());

			contextMenu.Items.Add(new ToolStripSeparator());

			contextMenu.Items.Add("Configurações", null, (s, e) => OpenConfigForm());
			contextMenu.Items.Add("Sair", null, (s, e) => Shutdown());

			notifyIcon.ContextMenuStrip = contextMenu;

			clientStatusTimer = new ST.Timer(600_000); // a cada 10 minutos
			clientStatusTimer.Elapsed += (s, e) => HasFiles();
			clientStatusTimer.AutoReset = true;

			try
			{
				MonitorsSwitchOn();

				ShowTrayNotification("Monitor de Impressão PSD", "Serviço ON LINE", 4000);

				msgSender.SendWhatsupMessage($"Monitor de Impressão PSD iniciado com sucesso às {DateTime.Now.ToShortTimeString()}.");

				isReadyToUse = true;
			}
			catch (Exception ex)
			{
				ShowTrayNotification("Falha", $"Erro ao iniciar o monitoramento: {ex.Message}", 5000);
			}

			//Faz a primeira varredura para verificar se já existem arquivos na pasta
			if (HasFiles() && !clientStatusTimer.Enabled)
			{
				clientStatusTimer.Start();
			}
		}

		private AppSettings LoadAndEnsureSettings()
		{
			settings = SettingsManager.Load();

			if (string.IsNullOrWhiteSpace(settings.FolderPath))
			{
				MessageBoxHelper.ShowWarning(
					"Pasta monitorada não definida. Por favor, configure a pasta nas configurações.",
					"Configuração Necessária");

				OpenConfigForm();

				settings = SettingsManager.Load();

				if (string.IsNullOrWhiteSpace(settings.FolderPath))
				{
					MessageBoxHelper.ShowWarning(
						"Pasta monitorada ainda não definida. O aplicativo será encerrado.",
						"Configuração Necessária");

					SW.Application.Current.Shutdown();
					return null;
				}
			}

			return settings;
		}

		private void OpenConfigForm()
		{
			FormInvoker.ShowForm(new ConfigurationForm(), true);
			
			LoadAndEnsureSettings();
		}

		private void OnFileManaged(object sender, FileSystemEventArgs e)
		{
			// Só notifica a cada 10 minutos para evitar "spam", caso o usuário vá adicionando arquivos a conta gotas.
			if (lastNotificationTime.Date != DateTime.MinValue && (lastNotificationTime.AddMinutes(10) > DateTime.Now))  return; 

			if (!clientStatusTimer.Enabled) 
			{
				ShowTrayNotification("Novo(s) PSD detectado(s)", "🖨️ Novo(s) arquivo(s) encontrado(s) para impressão.", 4000);
				msgSender.SendWhatsupMessage("Novos PSD detectados");
			}
			else
			{
				ShowTrayNotification("Arquivos disponíveis", "🖨️ Há arquivos na pasta prontos para impressão.", 4000);
			}

			//Se o timer já rodou o encontrou arquivos chamando HasFiles() e notificou o usuário, precisamos reiniciar o mecanismo			 
			if (userNotified) userNotified = false;

			if (!clientStatusTimer.Enabled) clientStatusTimer.Start();

			lastNotificationTime = DateTime.Now;
		}

		private bool HasFiles()
		{
			if (!dirIsReady) //é false por default 
			{
				// Verifica se o diretório está pronto para ser monitorado
				dirIsReady = IsDirectoryReady(pathToWatch);

				//Se não estiver pronto, notifica
				if (!dirIsReady)
				{
					ShowTrayNotification("Carregando...", "Pasta a ser monitorada não está disponível", 4000);

					msgSender.SendWhatsupMessage("Pasta a ser monitorada não está disponível...");

					//Iniciamos o timer para verificar novamente em 25 minutos
					if (!clientStatusTimer.Enabled)
					{
						clientStatusTimer.Start();
					}

					return false;
				}
			}

			//Ignora arquivos já processados
			var files = Directory.GetFiles(pathToWatch, "*.psd")
							.Where(f => !checkedOutFiles.Contains(f))
							.Select(f => Path.GetFileNameWithoutExtension(f))
							.ToArray();


			bool has = files.Length > 0;

			if (has)
			{
				if (!userNotified)
				{
					StringBuilder message = new("Os sequintes arquivos estão na pasta para impressão:\n\n");

					foreach (var file in files)
					{
						message.AppendLine(file);
					}

					userNotified = true; // Marca que o usuário já foi notificado sobre os arquivos na pasta
					msgSender.SendWhatsupMessage(message.ToString(), 2000);
				}

				printNowStripe.Text = $"Imprimir agora ({files.Length} arquivos)";

				ShowTrayNotification("Arquivos PSD", $"{files.Length} arquivo(s) encontrado(s) para impressão.", 4000);

				lastCheckedTime = DateTime.Now;
			}
			else
			{
				printNowStripe.Text = "Imprimir Agora";
				MonitorsSwitchOn();
			}

			return has;
		}

		private void MonitorsSwitchOn()
		{
			if (watcher != null)
			{
				watcher.EnableRaisingEvents = false;
				watcher.Dispose();
			}

			watcher = new FileSystemWatcher(pathToWatch, "*.psd")
			{
				IncludeSubdirectories = false,
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite
			};

			watcher.Created += OnFileManaged;
			watcher.Changed += OnFileManaged;
			watcher.EnableRaisingEvents = true;

			//Reset timer, so, in case it's stuck, it will be restored to the initial state.
			if (clientStatusTimer.Enabled)
				clientStatusTimer.Stop();

			clientStatusTimer.Start();

			lastCheckedTime = DateTime.Now;
		}

		private void PrintFiles()
		{
			if (!isReadyToUse)
			{
				SW.MessageBox.Show("A aplicação ainda não está pronto para imprimir. Aguarde a inicialização.");
				return;
			}

			if (string.IsNullOrEmpty(pathToWatch) || !Directory.Exists(pathToWatch))
			{
				SW.MessageBox.Show("Pasta monitorada não definida ou não existe.");
				return;
			}

			try
			{
				var files = Directory.GetFiles(pathToWatch, "*.psd")
							.Where(f => !checkedOutFiles.Contains(f))
							.ToArray();

				if (files.Length == 0)
				{
					SW.MessageBox.Show("Nenhum arquivo .PSD novo para imprimir.", "Aviso", SW.MessageBoxButton.OK, SW.MessageBoxImage.Warning);
					return;
				}

				string psPath = settings.PhotoShopFolderPath;

				if (string.IsNullOrEmpty(psPath) || !File.Exists(psPath))
				{
					MessageBoxHelper.ShowWarning("Caminho do Photoshop não configurado ou inválido.", "Configuração Necessária");
					return;
				}

				// Adiciona os arquivos na lista de "em processamento" 	 
				foreach (var arquivo in files)
				{
					checkedOutFiles.Add(arquivo);
					Process.Start(psPath, $"\"{arquivo}\"");
				}

				printNowStripe.Text = "Imprimir Agora";

				ShowTrayNotification("Impressão", "Arquivos PSD enviados para o Photoshop.", 4000);

				Task.Run(() => PhotoShopClosingWhatcher(files));
			}
			catch (Exception ex)
			{
				SW.MessageBox.Show($"Erro ao abrir arquivos: {ex.Message}", "Falha", SW.MessageBoxButton.OK, SW.MessageBoxImage.Error);
			}
			finally
			{
				MonitorsSwitchOn(); // Reboot the monitoring engine after printing
			}
		}

		private void PhotoShopClosingWhatcher(string[] files)
		{
			try
			{
				// Aguarda o fechamento do Photoshop
				while (Process.GetProcessesByName("Photoshop").Length > 0)
				{
					Thread.Sleep(2000);
				}

				App.UiDispatcher?.Invoke(() =>
					notifyIcon.ShowBalloonTip(3000, "Impressão concluída", "Movendo arquivos para PROCESSADOS...", ToolTipIcon.Info)
				);

				foreach (var file in files)
				{
					string fileName = Path.GetFileName(file);
					string destination = Path.Combine(ProcessedFilesFolder, fileName);

					// Se o arquivo já existir, gera um nome único
					int counter = 1;
					string baseName = Path.GetFileNameWithoutExtension(fileName);
					string extension = Path.GetExtension(fileName);

					while (File.Exists(destination))
					{
						string newName = $"{baseName} ({counter++}){extension}";
						destination = Path.Combine(ProcessedFilesFolder, newName);
					}

					File.Move(file, destination);

					checkedOutFiles.Remove(file);
				}

				App.UiDispatcher?.Invoke(() =>
					notifyIcon.ShowBalloonTip(3000, "Impressão concluída", "Arquivos movidos para \"processados\"", ToolTipIcon.Info)
				);
			}
			catch (Exception ex)
			{
				App.UiDispatcher?.Invoke(() =>
					SW.MessageBox.Show($"Erro ao mover arquivos: {ex.Message}", "Falha", SW.MessageBoxButton.OK, SW.MessageBoxImage.Error)
				);
			}
		}

		void ShowTrayNotification(string title, string message, int timeout)
		{
			notifyIcon.BalloonTipTitle = title;
			notifyIcon.BalloonTipText = message;
			notifyIcon.ShowBalloonTip(timeout);
		}

		private static bool IsDirectoryReady(string path)
		{
			try
			{
				// Verifica se o diretório existe
				if (!Directory.Exists(path)) return false;

				// Tenta obter os arquivos (sem necessariamente usar o resultado)
				Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
				return true;
			}
			catch (IOException ex)
			{
				// Pode estar carregando ainda, especialmente em unidades virtuais
				App.UiDispatcher?.Invoke(() =>
					SW.MessageBox.Show($"Erro ao ler arquivos: {ex.Message}", "Falha", SW.MessageBoxButton.OK, SW.MessageBoxImage.Error)
				);
				return false;
			}
			catch (UnauthorizedAccessException ex)
			{
				App.UiDispatcher?.Invoke(() =>
					SW.MessageBox.Show($"Erro ao acessar diretório: {ex.Message}", "Falha", SW.MessageBoxButton.OK, SW.MessageBoxImage.Error)
				);
				return false;
			}
		}

		private void CheckStatus()
		{
			App.UiDispatcher?.Invoke(() =>
					SW.MessageBox.Show($"O Timer foi executado pela última vez às {lastCheckedTime.ToShortTimeString()}", "Timer Status", SW.MessageBoxButton.OK, SW.MessageBoxImage.Information)
				);
		}

		private void Shutdown()
		{

			this.Dispose();

		}

		public void Dispose()
		{


			if (watcher != null)
			{
				watcher.EnableRaisingEvents = false;
			}

			watcher?.Dispose();

			notifyIcon.Visible = false;
			notifyIcon.Dispose();
			SW.Application.Current.Shutdown();
		}
	}
}
