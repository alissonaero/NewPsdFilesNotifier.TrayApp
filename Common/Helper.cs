using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PsPrintNotifier.TrayApp.Common
{
	internal static class Helper
	{

		public static string ApplyWhatsupMask(string number)
		{
			number = Regex.Replace(number, @"\D", "");
			
			if (number.Length > 11)
				number = number.Substring(0, 11);

			return number.Length switch
			{
				<= 2 => $"({number}",
				<= 7 => $"({number[..2]}) {number[2..]}",
				<= 11 => $"({number[..2]}) {number[2..7]}-{number[7..]}",
				_ => number
			};
		}

		public static string ClearWhatsAppNumber(string text)
		{
			string onlynumbers = Regex.Replace(text, @"\D", "");

			// Remove o '9' extra, se presente (celulares com 9 dígitos no DDD 11+)
			if (onlynumbers.Length == 11 && onlynumbers[2] == '9')
				onlynumbers = onlynumbers.Remove(2, 1);

			return onlynumbers;
		}

	}

	
}
