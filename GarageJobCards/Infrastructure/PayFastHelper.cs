using System.Collections.Specialized;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace GarageJobCards.Infrastructure
{
    // Real PayFast integration (South African payment gateway), using their
    // sandbox by default so it works without a live merchant account.
    //
    // To go live with a real account: add these three keys to Web.config's
    // <appSettings> and they'll override the sandbox defaults automatically:
    //   PayFastMerchantId, PayFastMerchantKey, PayFastPassphrase
    // and change PayFastUrl below from the sandbox host to
    // "https://www.payfast.co.za/eng/process".
    public static class PayFastHelper
    {
        // PayFast's own published sandbox test credentials - safe to use
        // as-is for testing, no real merchant account needed.
        private const string SandboxMerchantId = "10000100";
        private const string SandboxMerchantKey = "46f0cd694581a";
        public const string PayFastUrl = "https://sandbox.payfast.co.za/eng/process";

        public static string MerchantId
        {
            get { return ConfigurationManager.AppSettings["PayFastMerchantId"] ?? SandboxMerchantId; }
        }

        public static string MerchantKey
        {
            get { return ConfigurationManager.AppSettings["PayFastMerchantKey"] ?? SandboxMerchantKey; }
        }

        private static string Passphrase
        {
            get { return ConfigurationManager.AppSettings["PayFastPassphrase"]; } // null in sandbox - PayFast doesn't require one for sandbox testing
        }

        // Builds the full set of fields to submit to PayFast, including a
        // valid signature, ready to render as hidden form fields that
        // auto-submit to PayFastUrl.
        public static NameValueCollection BuildPaymentFields(int purchaseId, decimal amount, string itemName, string buyerEmail, string returnUrl, string cancelUrl, string notifyUrl)
        {
     var fields = new NameValueCollection
            {
                { "merchant_id", MerchantId },
                { "merchant_key", MerchantKey },
                { "return_url", returnUrl },
                { "cancel_url", cancelUrl },
                { "notify_url", notifyUrl },
                { "email_address", buyerEmail ?? "" },
                { "m_payment_id", purchaseId.ToString() },
                { "amount", amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                { "item_name", itemName }
            };

            fields["signature"] = GenerateSignature(fields);
            return fields;
        }

        // PayFast's signature spec: concatenate all non-empty fields in the
        // order given (NOT alphabetical), URL-encode each value, join with
        // "&", append the passphrase if one is set, then MD5 hash the whole
        // string. The ITN callback must be verified the same way.
        public static string GenerateSignature(NameValueCollection fields)
        {
            var sb = new StringBuilder();
            foreach (string key in fields.Keys)
            {
                var value = fields[key];
                if (string.IsNullOrEmpty(value)) continue;
                if (sb.Length > 0) sb.Append("&");
                sb.Append(key).Append("=").Append(PayFastUrlEncode(value));
            }

            if (!string.IsNullOrEmpty(Passphrase))
                sb.Append("&passphrase=").Append(PayFastUrlEncode(Passphrase));

            using (var md5 = MD5.Create())
            {
                var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                var hex = new StringBuilder();
                foreach (var b in hashBytes) hex.Append(b.ToString("x2"));
                return hex.ToString();
            }
        }

        // PayFast specifically requires uppercase hex digits in percent-
        // escapes (e.g. %3A, not %3a) - .NET's HttpUtility.UrlEncode
        // produces lowercase by default, which silently breaks the
        // signature PayFast recalculates on their end.
        private static string PayFastUrlEncode(string value)
        {
            var encoded = HttpUtility.UrlEncode(value);
            var sb = new StringBuilder(encoded.Length);
            for (int i = 0; i < encoded.Length; i++)
            {
                if (encoded[i] == '%' && i + 2 < encoded.Length)
                {
                    sb.Append('%').Append(char.ToUpperInvariant(encoded[i + 1])).Append(char.ToUpperInvariant(encoded[i + 2]));
                    i += 2;
                }
                else
                {
                    sb.Append(encoded[i]);
                }
            }
            return sb.ToString();
        }
    }
}
