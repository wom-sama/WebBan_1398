namespace WebBan_1398.Models
{
    public class OrderConfirmationViewModel
    {
        public Order Order { get; set; } = new();
        public List<CartItem> Items { get; set; } = new();
    }
}
