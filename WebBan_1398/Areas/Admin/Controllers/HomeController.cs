using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBan_1398.Areas.Admin.Models;
using WebBan_1398.Models;

namespace WebBan_1398.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                ProductCount = await _context.Products.CountAsync(),
                CategoryCount = await _context.Categories.CountAsync(),
                OrderCount = await _context.Orders.CountAsync(),
                UserCount = await _context.Users.CountAsync(),
                RecentOrders = await _context.Orders
                    .Include(o => o.ApplicationUser)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }
    }
}
