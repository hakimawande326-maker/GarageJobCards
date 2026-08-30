using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace GarageJobCards.Infrastructure
{
    public static class SmsHelper
    {
        public static void SendSms(string toPhoneLocal, string message)
        {
            var tokenId = ConfigurationManager.AppSettings["BulkSmsTokenId"];
            var tokenSecret = ConfigurationManager.AppSettings["BulkSmsTokenSecret"];

            if (string.IsNullOrEmpty(tokenId) || string.IsNullOrEmpty(tokenSecret))
                throw new InvalidOperationException("BulkSMS settings are missing from Web.config.");

            var toE164 = ToE164SouthAfrica(toPhoneLocal);

            using (var client = new HttpClient())
            {
                var authBytes = Encoding.ASCII.GetBytes(tokenId + ":" + tokenSecret);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

                var json = "[{\"to\":\"" + toE164 + "\",\"body\":\"" + EscapeJson(message) + "\"}]";
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = client.PostAsync("https://api.bulksms.com/v1/messages", content).GetAwaiter().GetResult();
                var responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                    throw new Exception("BulkSMS failed (" + response.StatusCode + "): " + responseText);
            }
        }

        private static string ToE164SouthAfrica(string localNumber)
        {
            var digits = localNumber.Trim();
            if (digits.StartsWith("0"))
                digits = digits.Substring(1);
            return "+27" + digits;
        }

        private static string EscapeJson(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}