using NewPsdFilesNotifier.TrayApp.Common;
using NewPsdFilesNotifier.TrayApp.Models;
using System.Net.Http;


namespace NewPsdFilesNotifier.TrayApp.Services
{

	internal class WhatsAppNotifier(AppSettings settings)
	{
		readonly AppSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings), "As configurações de usuário precisam ser informadas");

		public async void SendWhatsupMessage(string message, int delay = 0)
		{
			if (string.IsNullOrEmpty(message))
			{
				throw new ArgumentNullException(nameof(message), "Não é possível enviar mensagem sem conteúdo");
			}

			///TODO: Criar um tutorial de como criar a chave da API do WhatsApp no CallMeBot
			if (string.IsNullOrEmpty(_settings.WhatsAppApiKey))
			{
				return;
			}

			if (string.IsNullOrEmpty(_settings.WhatsAppContact))
			{
				MessageBoxHelper.ShowWarning("Contato do WhatsApp não configurado. Por favor, informe nas configurações se quiser ser notificado.", "Configuração Necessária");
				return;
			}

			///TODO: implementar o combo de paises
			await SendAsync(
			"55" + _settings.WhatsAppContact,
			message,
			_settings.WhatsAppApiKey);

			if (delay > 0)
			{
				await Task.Delay(delay); // Aguardar o tempo necessário para evitar sobrecarga de requisições	
			}
		}

		static async Task SendAsync(string phoneNumber, string message, string apiKey)
		{
#if DEBUG
			return;
#endif
			if (string.IsNullOrWhiteSpace(apiKey)) return; ///TODO: Implementar um log 

			try
			{
				using var client = new HttpClient();

				client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
				client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
				client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
				client.DefaultRequestHeaders.Add("Connection", "keep-alive");

				message = message.Replace(" ", "+").Trim();

				var url = $"https://api.callmebot.com/whatsapp.php?phone={phoneNumber}&text={message}&apikey={apiKey}";

				var response = await client.GetAsync(url);

				if (!response.IsSuccessStatusCode)
				{
					MessageBoxHelper.ShowWarning("Falha ao enviar mensagem via WhatsApp.", "Notificação");
				}
			}
			catch (Exception ex)
			{
				MessageBoxHelper.ShowError($"Erro ao enviar WhatsApp: {ex.Message}", "Falha de Notificação");
			}
		}
	}

}
