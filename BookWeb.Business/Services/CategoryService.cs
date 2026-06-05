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
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _db;
        public CategoryService(ApplicationDbContext db)
        {  
            _db = db;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _db.Categories.ToListAsync();
        }
        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _db.Categories.FindAsync(id);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
          _db.Categories.Add(category);
           await _db.SaveChangesAsync();
            return category;

        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = _db.Categories.Find(id);
            if (category == null)
            {
                throw new KeyNotFoundException($"Category (id) not found");
            }
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
        }
        public async Task UpdateCategoryAsync(Category category)
        {
           _db.Categories.Update(category);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> IsCategoryNameUniqueAsync(string name, int? categoryId = null)
        {
            if(categoryId.HasValue)
            {
              return !await  _db.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.Id != categoryId.Value);
            }
            else
            {
                return !await _db.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower());
            }
        }
    }
}
