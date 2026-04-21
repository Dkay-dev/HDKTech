namespace HDKTech.Models.Momo
{
    public class MomoExecuteResponseModel
    {
        public string OrderId { get; set; }
        public string Amount { get; set; }
        public string FullName { get; set; }
        public string OrderInfo { get; set; }
        /// <summary>
        /// Chữ ký HMAC-SHA256 từ MoMo trả về trong query string
        /// </summary>
        public string Signature { get; set; }    // ✅ THÊM MỚI — dùng để ValidateSignature
    }
}