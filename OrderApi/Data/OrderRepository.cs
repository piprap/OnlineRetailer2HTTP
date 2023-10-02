using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OrderApi.Models;
using System.Threading.Tasks;

namespace OrderApi.Data
{
    public class OrderRepository : IRepository<Order>
    {
        private readonly OrderApiContext db;

        public OrderRepository(OrderApiContext context)
        {
            db = context;
        }

        public async Task<Order> AddAsync(Order entity)
        {
            if (entity.Date == null)
                entity.Date = DateTime.Now;

            var newOrder = await db.Orders.AddAsync(entity);
            await db.SaveChangesAsync();
            return newOrder.Entity;
        }

        public async Task EditAsync(Order entity)
        {
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        public async Task<Order> GetAsync(int id)
        {
            return await db.Orders.Include(o => o.OrderLines).FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await db.Orders.Include(o => o.OrderLines).ToListAsync();
        }

        public async Task RemoveAsync(int id)
        {
            var order = await db.Orders.FirstOrDefaultAsync(p => p.Id == id);
            if (order != null)
            {
                db.Orders.Remove(order);
                await db.SaveChangesAsync();
            }
        }
    }
}
