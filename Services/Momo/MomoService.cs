using HDKTech.Models.Momo;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace HDKTech.Services.Momo
{
    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        private readonly HttpClient _httpClient;

        public MomoService(IOptions<MomoOptionModel> options, HttpClient httpClient)
        {
            _options = options;
            _httpClient = httpClient;
        }

        public async Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model)
        {
            model.OrderId = DateTime.UtcNow.Ticks.ToString();
            model.OrderInfo = "Khách hàng: " + model.FullName + ". Nội dung: " + model.OrderInfo;

            // ✅ Dùng chữ ký theo chuẩn MoMo v2 (accessKey trước, alphabet sort)
            var rawData =
                $"accessKey={_options.Value.AccessKey}" +
                $"&amount={model.Amount}" +
                $"&extraData=" +
                $"&ipnUrl={_options.Value.NotifyUrl}" +
                $"&orderId={model.OrderId}" +
                $"&orderInfo={model.OrderInfo}" +
                $"&partnerCode={_options.Value.PartnerCode}" +
                $"&redirectUrl={_options.Value.ReturnUrl}" +
                $"&requestId={model.OrderId}" +
                $"&requestType={_options.Value.RequestType}";

            var signature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

            var requestData = new
            {
                partnerCode = _options.Value.PartnerCode,
                accessKey = _options.Value.AccessKey,
                requestId = model.OrderId,
                amount = model.Amount.ToString(),
                orderId = model.OrderId,
                orderInfo = model.OrderInfo,
                redirectUrl = _options.Value.ReturnUrl,
                ipnUrl = _options.Value.NotifyUrl,
                extraData = "",
                requestType = _options.Value.RequestType,
                signature = signature,
                lang = "vi"
            };

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(requestData),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await _httpClient.PostAsync(_options.Value.MomoApiUrl, jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();
                var momoResponse = JsonConvert.DeserializeObject<MomoCreatePaymentResponseModel>(responseContent);
                return momoResponse;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Lỗi gọi API Momo: " + ex.Message, ex);
            }
        }

        public MomoExecuteResponseModel PaymentExecute(IQueryCollection query)
        {
            return new MomoExecuteResponseModel
            {
                Amount = query["amount"].ToString(),
                OrderId = query["orderId"].ToString(),
                OrderInfo = query["orderInfo"].ToString(),
                FullName = query.ContainsKey("extraData") ? query["extraData"].ToString() : "",
                Signature = query.ContainsKey("signature") ? query["signature"].ToString() : ""  // ✅ LẤY SIGNATURE
            };
        }

        /// <summary>
        /// Xác thực chữ ký HMAC-SHA256 từ MoMo callback.
        /// MoMo ký theo thứ tự alphabet của các tham số trả về.
        /// </summary>
        public bool ValidateSignature(IQueryCollection query)
        {
            // Lấy chữ ký MoMo gửi về
            var receivedSignature = query["signature"].ToString();
            if (string.IsNullOrEmpty(receivedSignature))
                return false;

            // ✅ Tái tạo rawData đúng chuẩn MoMo callback (các tham số theo alphabet)
            var accessKey = _options.Value.AccessKey;
            var amount = query["amount"].ToString();
            var extraData = query.ContainsKey("extraData") ? query["extraData"].ToString() : "";
            var message = query.ContainsKey("message") ? query["message"].ToString() : "";
            var orderId = query["orderId"].ToString();
            var orderInfo = query["orderInfo"].ToString();
            var orderType = query.ContainsKey("orderType") ? query["orderType"].ToString() : "";
            var partnerCode = _options.Value.PartnerCode;
            var payType = query.ContainsKey("payType") ? query["payType"].ToString() : "";
            var requestId = query.ContainsKey("requestId") ? query["requestId"].ToString() : orderId;
            var responseTime = query.ContainsKey("responseTime") ? query["responseTime"].ToString() : "";
            var resultCode = query.ContainsKey("resultCode") ? query["resultCode"].ToString() : "";
            var transId = query.ContainsKey("transId") ? query["transId"].ToString() : "";

            var rawData =
                $"accessKey={accessKey}" +
                $"&amount={amount}" +
                $"&extraData={extraData}" +
                $"&message={message}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&orderType={orderType}" +
                $"&partnerCode={partnerCode}" +
                $"&payType={payType}" +
                $"&requestId={requestId}" +
                $"&responseTime={responseTime}" +
                $"&resultCode={resultCode}" +
                $"&transId={transId}";

            var computedSignature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

            return computedSignature.Equals(receivedSignature, StringComparison.OrdinalIgnoreCase);
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}