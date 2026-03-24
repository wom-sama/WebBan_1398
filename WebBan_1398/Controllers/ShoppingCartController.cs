using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBan_1398.Extensions;
using WebBan_1398.Models;
using WebBan_1398.Repositories;
using System.Threading.Tasks;

namespace WebBan_1398.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartController(IProductRepository productRepository, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View(GetCart());
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, string? returnUrl = null)
        {
            var product = await GetProductFromDatabase(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm cần thêm vào giỏ hàng.";
                return NotFound();
            }

            var cartItem = new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                Quantity = quantity
            };

            var cart = GetCart();
            cart.AddItem(cartItem);
            SaveCart(cart);
            TempData["SuccessMessage"] = $"Đã thêm \"{product.Name}\" vào giỏ hàng.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveFromCart(int productId, string? returnUrl = null)
        {
            var cart = GetCart();
            if (cart is not null)
            {
                cart.RemoveItem(productId);

                if (cart.Items.Any())
                {
                    SaveCart(cart);
                }
                else
                {
                    HttpContext.Session.Remove("Cart");
                }

                TempData["InfoMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<Product?> GetProductFromDatabase(int productId)
        {
            return await _productRepository.GetByIdAsync(productId);
        }

        [Authorize]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Items.Any())
            {
                TempData["InfoMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction(nameof(Index));
            }

            return View(new Order());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = GetCart();
            if (cart == null || !cart.Items.Any())
            {
                TempData["InfoMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin đặt hàng.";
                return View(order);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;
            order.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);

            order.OrderDetails = cart.Items.Select(i => new OrderDetail
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var confirmation = new OrderConfirmationViewModel
            {
                Order = order,
                Items = cart.Items.ToList()
            };

            HttpContext.Session.Remove("Cart");

            return View("OrderCompleted", confirmation);
        }

        private ShoppingCart GetCart()
        {
            return HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
        }

        private void SaveCart(ShoppingCart cart)
        {
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }
    }
}
