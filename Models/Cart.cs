namespace HDKTech.Models
{
    /// <summary>
    /// Đại diện cho toàn bộ giỏ hàng
    /// </summary>
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        /// <summary>
        /// Tổng số lượng item trong giỏ
        /// </summary>
        public int TotalItems => Items.Sum(x => x.Quantity);

        /// <summary>
        /// Tổng tiền của toàn bộ giỏ hàng
        /// </summary>
        public decimal TotalPrice => Items.Sum(x => x.TotalPrice);

        /// <summary>
        /// Thêm item vào giỏ
        /// </summary>
        public void AddItem(CartItem item)
        {
            var existingItem = Items.FirstOrDefault(x => x.ProductId == item.ProductId);
            
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                Items.Add(item);
            }
        }

        /// <summary>
        /// Xóa item khỏi giỏ
        /// </summary>
        public void RemoveItem(int productId)
        {
            Items.RemoveAll(x => x.ProductId == productId);
        }

        /// <summary>
        /// Cập nhật số lượng item
        /// </summary>
        public void UpdateQuantity(int productId, int quantity)
        {
            var item = Items.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    RemoveItem(productId);
                }
                else
                {
                    item.Quantity = quantity;
                }
            }
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        public void Clear()
        {
            Items.Clear();
        }

        /// <summary>
        /// Kiểm tra giỏ hàng có trống không
        /// </summary>
        public bool IsEmpty => Items.Count == 0;
    }
}
