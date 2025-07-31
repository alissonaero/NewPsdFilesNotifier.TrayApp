using PsPrintNotifier.TrayApp.Common.CrossThreadingHelpers;
using PsPrintNotifier.TrayApp.Models;
using System.Net.Http;

namespace PsPrintNotifier.TrayApp.Services
{
	internal static class NotificationService
	{
		public static class WhatsAppNotifier
		{
			public static async Task SendAsync(string phoneNumber, string message, string apiKey)
			{
			 
				AppSettings settings = SettingsManager.Load();

				if (String.IsNullOrEmpty(settings.WhatsAppApiKey)) return; ///TODO: Implementar um log 

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
}
