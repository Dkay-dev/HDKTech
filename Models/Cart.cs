namespace HDKTech.Models
{
    /// <summary>
    /// Đại diện cho toàn bộ giỏ hàng.
    /// Key xác định 1 dòng cart = (ProductId, ProductVariantId)
    /// — cùng product, khác variant ⇒ 2 dòng riêng.
    /// </summary>
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new();

        public int TotalItems => Items.Sum(x => x.Quantity);
        public decimal TotalPrice => Items.Sum(x => x.TotalPrice);
        public bool IsEmpty => Items.Count == 0;

        public void AddItem(CartItem item)
        {
            var existing = Items.FirstOrDefault(x =>
                x.ProductId == item.ProductId &&
                x.ProductVariantId == item.ProductVariantId);

            if (existing != null) existing.Quantity += item.Quantity;
            else Items.Add(item);
        }

        public void RemoveItem(int productId, int productVariantId)
        {
            Items.RemoveAll(x =>
                x.ProductId == productId &&
                x.ProductVariantId == productVariantId);
        }

        public void UpdateQuantity(int productId, int productVariantId, int quantity)
        {
            var item = Items.FirstOrDefault(x =>
                x.ProductId == productId &&
                x.ProductVariantId == productVariantId);
            if (item == null) return;

            if (quantity <= 0) RemoveItem(productId, productVariantId);
            else item.Quantity = quantity;
        }

        public void Clear() => Items.Clear();
    }
}
