using WebBan_1398.Models;

namespace WebBan_1398.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync(string searchString = null);
        Task<Product> GetByIdAsync(int id);
        Task<Product> GetNextProductAsync(int currentId);
        Task<Product> GetPreviousProductAsync(int currentId);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
    }
}
