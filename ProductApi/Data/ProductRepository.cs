using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using ProductApi.Models;

namespace ProductApi.Data
{
    public class ProductRepository : IRepository<Product>
    {
        private readonly ProductApiContext db;

        public ProductRepository(ProductApiContext context)
        {
            db = context;
        }

        async Task<Product> IRepository<Product>.AddAsync(Product entity)
        {
            var newProduct = db.Products.Add(entity).Entity;
            await db.SaveChangesAsync();
            return newProduct;
        }

        async Task IRepository<Product>.EditAsync(Product entity)
        {
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        async Task<Product> IRepository<Product>.GetAsync(int id)
        {
            return await db.Products.FirstOrDefaultAsync(p => p.Id == id);
        }

        async Task<IEnumerable<Product>> IRepository<Product>.GetAllAsync()
        {
            return await db.Products.ToListAsync();
        }

        async Task IRepository<Product>.RemoveAsync(int id)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                db.Products.Remove(product);
                await db.SaveChangesAsync();
            }
        }
    }
}
