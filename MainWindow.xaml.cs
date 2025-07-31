using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Windows;

namespace PsPrintNotifier.TrayApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly NotifyIcon notifyIcon;

		private string observedFolder;

		private string processeFilesdFolder => Path.Combine(observedFolder, "PROCESSADOS");


		public MainWindow()
		{
			InitializeComponent();

			notifyIcon = new NotifyIcon
			{

				Visible = true,
				Text = "Monitor de Impressão PSD",
				BalloonTipTitle = "Serviço de Impressão",
				BalloonTipText = "Rodando em background."
			};

			using (var stream = new MemoryStream(Properties.Resources.PsdIcon)) // psd: nome do recurso
			{
				notifyIcon.Icon = new Icon(stream);
			}

			notifyIcon.BalloonTipClicked += (s, e) =>
			{
				var result = System.Windows.MessageBox.Show(
					"Deseja iniciar a impressão dos arquivos PSD agora?",
					"Confirmar Impressão",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question
				);

				if (result == MessageBoxResult.Yes)
				{
					BtnImprimir_Click(null, null);
				}
			};

			StartPipeListener();

			// Context menu
			var contextMenu = new ContextMenuStrip();
			contextMenu.Items.Add("Abrir painel", null, (s, e) => Show());
			contextMenu.Items.Add("Sair", null, (s, e) => Close());
			notifyIcon.ContextMenuStrip = contextMenu;	
		}


		private async void StartPipeListener()
		{
			await Task.Run(() =>
			{
				while (true)
				{
					using var server = new NamedPipeServerStream("PsPrintPipe", PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances);
					server.WaitForConnection();

					using var reader = new StreamReader(server);
					string? message = reader.ReadLine();

					if (!string.IsNullOrEmpty(message))
					{
						Dispatcher.Invoke(() =>
						{
							// Interpreta a mensagem baseada no prefixo
							if (message.StartsWith("CMD:PATH|"))
							{
								string path = message.Replace("CMD:PATH|", "").Trim();
								observedFolder = path;
							}
							else if (message.StartsWith("CMD:NOTIFY|"))
							{
								string texto = message.Replace("CMD:NOTIFY|", "").Trim();
								notifyIcon.BalloonTipTitle = "Notificação do Serviço";
								notifyIcon.BalloonTipText = texto;
								notifyIcon.ShowBalloonTip(4000);
							}
							else if (message.Contains("PING"))
							{
								notifyIcon.BalloonTipTitle = "Monitor de Arquivos para Impressão";
								notifyIcon.BalloonTipText = "Serviço ON LINE";
								notifyIcon.ShowBalloonTip(4000);
							}
							else
							{
								// Mensagem desconhecida
								notifyIcon.BalloonTipTitle = "Mensagem recebida";
								notifyIcon.BalloonTipText = message;
								notifyIcon.ShowBalloonTip(3000);
							}
						});
					}
				}
			});
		}


		private void BtnImprimir_Click(object? sender, RoutedEventArgs? e)
		{
			if (string.IsNullOrEmpty(observedFolder) || !Directory.Exists(observedFolder))
			{
				System.Windows.MessageBox.Show("Pasta monitorada não definida ou não existe.");
				return;
			}

			try
			{
				string[] arquivos = Directory.GetFiles(observedFolder, "*.psd");

				if (arquivos.Length == 0)
				{
					System.Windows.MessageBox.Show("Nenhum arquivo .PSD encontrado.");
					return;
				}

				string psPath = "C:\\Portables\\Photoshop 2022\\Adobe.Photoshop.2022.23.1.1.Portable\\App\\Photoshop\\photoshop.exe";

				if (!File.Exists(psPath))
				{
					System.Windows.MessageBox.Show("Caminho do Photoshop não encontrado. Verifique a instalação.");
					return;
				}

				foreach (var arquivo in arquivos)
				{
					Process.Start(psPath, $"\"{arquivo}\"");
				}

				// Dispara verificação quando Photoshop for fechado
				Task.Run(() => MonitorarFechamentoPhotoshop(arquivos));
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show($"Erro ao abrir arquivos: {ex.Message}");
			}
		}

		private void MonitorarFechamentoPhotoshop(string[] arquivos)
		{
			try
			{
				// Aguarda enquanto houver processo "Photoshop"
				while (Process.GetProcessesByName("Photoshop").Length > 0)
				{
					Thread.Sleep(2000);
				}

				Dispatcher.Invoke(() => notifyIcon.ShowBalloonTip(3000, "Impressão concluída", "Movendo arquivos para PROCESSADOS...", ToolTipIcon.Info));

				// Cria pasta se não existir
				Directory.CreateDirectory(processeFilesdFolder);

				foreach (var arquivo in arquivos)
				{
					string destino = Path.Combine(processeFilesdFolder, Path.GetFileName(arquivo));
					File.Move(arquivo, destino);
				}

				Dispatcher.Invoke(() =>
				{
					System.Windows.MessageBox.Show("Todos os arquivos foram processados e movidos.");
				});
			}
			catch (Exception ex)
			{
				Dispatcher.Invoke(() =>
				{
					System.Windows.MessageBox.Show($"Erro ao mover arquivos: {ex.Message}");
				});
			}
		}

	}
}