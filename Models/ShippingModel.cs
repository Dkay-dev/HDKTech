namespace HDKTech.Models
{
    public class ShippingModel
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public string Ward { get; set; }
        public string District { get; set; }
        public string City { get; set; }
    }

    public class ShippingFeeRequest
    {
        public string CityId { get; set; }
        public string CityName { get; set; }
        public string DistrictId { get; set; }
        public string DistrictName { get; set; }
        public string WardId { get; set; }
        public string WardName { get; set; }
    }

    public class ShippingFeeResponse
    {
        public bool Success { get; set; }
        public decimal Fee { get; set; }
        public string FeeFormatted { get; set; }
        public string Message { get; set; }
        public string Zone { get; set; }
    }
}