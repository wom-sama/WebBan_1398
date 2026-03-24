using WebBan_1398.Models;

namespace WebBan_1398.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        public int ProductCount { get; set; }
        public int CategoryCount { get; set; }
        public int OrderCount { get; set; }
        public int UserCount { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
    }
}
