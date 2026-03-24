using Microsoft.AspNetCore.Mvc;
using WebBan_1398.Repositories;

namespace WebBan_1398.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;

        public ProductController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index(string? searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var products = await _productRepository.GetAllAsync(searchString);
            return View(products);
        }

        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var nextProduct = await _productRepository.GetNextProductAsync(id);
            var previousProduct = await _productRepository.GetPreviousProductAsync(id);

            ViewBag.NextProductId = nextProduct?.Id;
            ViewBag.PreviousProductId = previousProduct?.Id;

            return View(product);
        }
    }
}
