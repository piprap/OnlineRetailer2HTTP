using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Threading.Tasks;
using CustomerApi.Models;

namespace CustomerApi.Data
{
    public class CustomerRepository : IRepository<Customer>
    {
        private readonly CustomerApiContext db;

        public CustomerRepository(CustomerApiContext context)
        {
            db = context;
        }

        async Task<Customer> IRepository<Customer>.AddAsync(Customer entity)
        {
            var newCustomer = db.Customers.Add(entity).Entity;
            await db.SaveChangesAsync();
            return newCustomer;
        }

        async Task IRepository<Customer>.EditAsync(Customer entity)
        {
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        async Task<Customer> IRepository<Customer>.GetAsync(int id)
        {
            return await db.Customers.FirstOrDefaultAsync(o => o.Id == id);
        }

        async Task<IEnumerable<Customer>> IRepository<Customer>.GetAllAsync()
        {
            return await db.Customers.ToListAsync();
        }

        async Task IRepository<Customer>.RemoveAsync(int id)
        {
            var customer = await db.Customers.FirstOrDefaultAsync(p => p.Id == id);
            if (customer != null)
            {
                db.Customers.Remove(customer);
                await db.SaveChangesAsync();
            }
        }
    }
}
