namespace OmmoBackend.Helpers.Constants
{
    public static class StripeEvents
    {
        public const string CheckoutSessionCompleted =
            "checkout.session.completed";

        public const string InvoicePaid =
            "invoice.paid";

        public const string InvoicePaymentFailed =
            "invoice.payment_failed";

        public const string CustomerSubscriptionDeleted =
            "customer.subscription.deleted";
    }
}
