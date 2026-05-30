namespace EcommerceApp.ViewModels
{
    /// <summary>
    /// Shipping and payment context held in session until Stripe payment succeeds.
    /// No database order is created until checkout completes.
    /// </summary>
    public class CheckoutSessionData
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public decimal OrderTotal { get; set; }
        public string PaymentIntentId { get; set; } = string.Empty;
    }
}
