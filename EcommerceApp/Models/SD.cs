namespace EcommerceApp.Models
{
    // Static Details class to hold constant strings
    public static class SD
    {
        // Roles
        public const string RoleAdmin = "admin";
        public const string RoleUser = "user";

        // Low stock threshold for dashboard
        public const int LowStockThreshold = 5;

        public const string StatusPending = "Pending";
        public const string StatusApproved = "Approved";
        public const string StatusInProcess = "Processing";
        public const string StatusShipped = "Shipped";
        public const string StatusCancelled = "Cancelled";
        public const string StatusRefunded = "Refunded";

        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusDelayedPayment = "ApprovedForDelayedPayment";
        public const string PaymentStatusRejected = "Rejected";

        public static readonly string[] AllowedOrderStatuses =
        {
            StatusPending,
            StatusApproved,
            StatusInProcess,
            StatusShipped,
            StatusCancelled,
            StatusRefunded
        };
    }
}
