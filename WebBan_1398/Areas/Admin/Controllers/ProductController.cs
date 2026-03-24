using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBan_1398.Models;
using WebBan_1398.Repositories;

namespace WebBan_1398.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index(string? searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var products = await _productRepository.GetAllAsync(searchString);
            return View(products);
        }

        public async Task<IActionResult> Add()
        {
            await LoadCategoriesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Product product, IFormFile? imageUrl, List<IFormFile>? imageUrls)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(product);
            }

            if (imageUrl != null)
            {
                product.ImageUrl = await SaveImageAsync(imageUrl);
            }

            product.Images = await SaveGalleryImagesAsync(imageUrls);

            await _productRepository.AddAsync(product);
            TempData["SuccessMessage"] = $"Đã thêm sản phẩm \"{product.Name}\".";
            return RedirectToAction(nameof(Index));
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

        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            await LoadCategoriesAsync(product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Product product, IFormFile? imageUrl, List<IFormFile>? imageUrls)
        {
            ModelState.Remove("ImageUrl");

            if (id != product.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(product.CategoryId);
                return View(product);
            }

            var existingProduct = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Description = product.Description;
            existingProduct.CategoryId = product.CategoryId;

            if (imageUrl != null)
            {
                existingProduct.ImageUrl = await SaveImageAsync(imageUrl);
            }

            if (imageUrls is { Count: > 0 })
            {
                if (existingProduct.Images.Any())
                {
                    _context.ProductImages.RemoveRange(existingProduct.Images);
                }

                existingProduct.Images = await SaveGalleryImagesAsync(imageUrls);
            }

            await _productRepository.UpdateAsync(existingProduct);
            TempData["SuccessMessage"] = $"Đã cập nhật sản phẩm \"{existingProduct.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            TempData["InfoMessage"] = "Đã xóa sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCategoriesAsync(int? selectedCategoryId = null)
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategoryId);
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            const long maxFileSize = 5 * 1024 * 1024;
            string[] allowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

            if (image.Length > maxFileSize)
            {
                throw new ArgumentException("File ảnh vượt quá dung lượng 5MB.");
            }

            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Định dạng ảnh không hợp lệ.");
            }

            var imageFolder = Path.Combine(_environment.WebRootPath, "Image");
            Directory.CreateDirectory(imageFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var savePath = Path.Combine(imageFolder, uniqueFileName);

            await using var fileStream = new FileStream(savePath, FileMode.Create);
            await image.CopyToAsync(fileStream);

            return $"/Image/{uniqueFileName}";
        }

        private async Task<List<ProductImage>> SaveGalleryImagesAsync(List<IFormFile>? imageUrls)
        {
            var images = new List<ProductImage>();
            if (imageUrls == null || imageUrls.Count == 0)
            {
                return images;
            }

            foreach (var image in imageUrls.Where(file => file is { Length: > 0 }))
            {
                images.Add(new ProductImage
                {
                    Url = await SaveImageAsync(image)
                });
            }

            return images;
        }
    }
}
