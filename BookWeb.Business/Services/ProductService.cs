using bookweb.Data;
using BookWeb.Business.Services.IServices;
using BookWeb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookWeb.Business.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;
        public ProductService(ApplicationDbContext db)
        {  
            _db = db;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync(bool includeCategory = false)
        {
            if (includeCategory)
            {
                return await _db.Products.Include(u => u.Category).ToListAsync();
            }
           else
            {
                return await _db.Products.ToListAsync();
            }
        }
        public async Task<Product?> GetProductByIdAsync(int id, bool includeCategory = false)
        {
            if (includeCategory)
            {
                return await _db.Products.Include(u=>u.Category).FirstOrDefaultAsync(u=>u.Id==id);
            }
            else
            {
                return await _db.Products.FirstOrDefaultAsync(u => u.Id == id);
            }
            
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
          _db.Products.Add(product);
           await _db.SaveChangesAsync();
            return product;

        }

        public async Task DeleteProductAsync(int id)
        {
            var product = _db.Products.Find(id);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product (id) not found");
            }
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
        }
        public async Task UpdateProductAsync(Product product)
        {
           _db.Products.Update(product);
            await _db.SaveChangesAsync();
        }

       
    }
}
