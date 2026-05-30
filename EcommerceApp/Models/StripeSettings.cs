namespace EcommerceApp.Models
{
    public class StripeSettings
    {
        public const string SectionName = "Stripe";

        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>ISO currency code (e.g. usd, pkr). Use usd for Stripe test cards.</summary>
        public string Currency { get; set; } = "usd";

        /// <summary>
        /// Mode of Stripe usage: "Test" or "Live". Default is Test.
        /// </summary>
        public string Mode { get; set; } = "Test";

        public static long ToStripeAmount(decimal amount, string currency)
        {
            var zeroDecimalCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf",
                "ugx", "vnd", "vuv", "xaf", "xof", "xpf"
            };

            if (zeroDecimalCurrencies.Contains(currency))
                return (long)Math.Round(amount, MidpointRounding.AwayFromZero);

            return (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        }
    }
}
