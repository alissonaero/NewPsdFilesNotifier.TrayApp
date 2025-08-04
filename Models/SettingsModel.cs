#nullable disable

using NewPsdFilesNotifier.TrayApp.Common;
using Newtonsoft.Json;
using System.IO;
using System.Web;


namespace NewPsdFilesNotifier.TrayApp.Models
{
	public class AppSettings
	{
		public string FolderPath { get; set; } = string.Empty;
		public string WhatsAppContact { get; set; }

		public string WhatsAppApiKey { get; set; } = string.Empty;

		public string PhotoShopFolderPath { get; set; } = string.Empty;

	}

	public static class SettingsManager
	{
		private static readonly string SettingsFilePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"NewPsdFilesNotifier",
			"settings.json");

		private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Ignore,
			DefaultValueHandling = DefaultValueHandling.Populate
		};

		public static AppSettings Load()
		{
			try
			{
				if (File.Exists(SettingsFilePath))
				{
					var json = File.ReadAllText(SettingsFilePath);
					return JsonConvert.DeserializeObject<AppSettings>(json, SerializerSettings);
				}
			}
			catch (Exception ex)
			{

				MessageBoxHelper.ShowError($"Failed to load settings: {ex.Message}", "Error");
			}

			return new AppSettings();
		}

		public static bool Save(AppSettings settings)
		{
			try
			{
				var dir = Path.GetDirectoryName(SettingsFilePath);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				var json = JsonConvert.SerializeObject(settings, SerializerSettings);
				File.WriteAllText(SettingsFilePath, json);
				return true;
			}
			catch (Exception ex)
			{
				MessageBoxHelper.ShowError($"Failed to save settings: {ex.Message}", "Error");

				return false;
			}
		}
	}


}
