using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBan_1398.Models;

namespace WebBan_1398.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["InfoMessage"] = $"Đã xóa đơn hàng #{id}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
