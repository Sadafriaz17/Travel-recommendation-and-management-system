namespace TourwebsiteFYP.Models
{
    /// <summary>
    /// DTO representing a row in the SafepayPaymentAttempt table.
    /// This is a plain C# class – NOT an EF entity – so the EDMX is never touched.
    /// </summary>
    public class SafepayAttemptDto
    {
        public int SafepayAttemptId { get; set; }
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public string TrackerToken { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public System.DateTime? CompletedAt { get; set; }
        public System.DateTime? WebhookReceivedAt { get; set; }
        public string RawWebhookPayload { get; set; }
    }
}
