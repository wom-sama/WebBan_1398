using Microsoft.AspNetCore.Mvc;
using WebBan_1398.Models;
using WebBan_1398.Repositories;

namespace WebBan_1398.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;

        public HomeController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index()
        {
            var products = (await _productRepository.GetAllAsync()).Take(8);
            return View(products);
        }
    }
}
