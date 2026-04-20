namespace HDKTech.Models.Requests
{
    public class AddToCartRequest
    {
        public int ProductId        { get; set; }
        public int ProductVariantId { get; set; }
        public int Quantity         { get; set; } = 1;
    }

    public class UpdateQuantityRequest
    {
        public int ProductId        { get; set; }
        public int ProductVariantId { get; set; }
        public int Quantity         { get; set; }
    }

    public class RemoveItemRequest
    {
        public int ProductId        { get; set; }
        public int ProductVariantId { get; set; }
    }
}
