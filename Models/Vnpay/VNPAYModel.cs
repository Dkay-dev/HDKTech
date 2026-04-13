namespace HDKTech.Models.Vnpay
{
    public class VNPAYModel
    {
        public int Id { get; set; }
        public string OrderId { get; set; }
        public string PaymentMethod { get; set; }
        public string OrderDescription { get; set; }
        public string TransactionId { get; set; }
        public string PaymentId { get; set; }
        public bool Success { get; set; }
        public string VnPayResponseCode { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}