namespace HDKTech.Models.Momo
{
    public class OrderInfoModel
    {
        public string OrderId { get; set; }
        public string FullName { get; set; }
        public long Amount { get; set; }
        public string OrderInfo { get; set; }

        /// <summary>
        /// PendingCheckout.Id gửi sang MoMo dưới dạng extraData.
        /// MoMo trả lại field này trong callback → dùng để look up PendingCheckout
        /// thay vì TempData (vốn có thể bị mất khi redirect).
        /// </summary>
        public string ExtraData { get; set; } = string.Empty;
    }
}
