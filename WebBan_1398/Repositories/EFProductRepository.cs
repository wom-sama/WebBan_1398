using Microsoft.EntityFrameworkCore;
using WebBan_1398.Models;

namespace WebBan_1398.Repositories
{
    public class EFProductRepository: IProductRepository
    {
        private readonly ApplicationDbContext _context;
        public EFProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Product>> GetAllAsync(string? searchString = null)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var normalizedSearch = searchString.Trim().ToUpper();
                products = products.Where(p =>
                    p.Name.ToUpper().Contains(normalizedSearch) ||
                    (p.Category != null && p.Category.Name.ToUpper().Contains(normalizedSearch)));
            }

            return await products.OrderByDescending(p => p.Id).ToListAsync();
        }
        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        public async Task AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Product?> GetNextProductAsync(int currentId)
        {
            return await _context.Products
                .Where(p => p.Id > currentId)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<Product?> GetPreviousProductAsync(int currentId)
        {
            return await _context.Products
                .Where(p => p.Id < currentId)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }
    }
}
