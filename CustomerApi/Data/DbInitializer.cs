using System.Collections.Generic;
using System.Linq;
using System;
using CustomerApi.Models;

namespace CustomerApi.Data
{
    public class DbInitializer : IDbInitializer
    {
        // This method will create and seed the database.
        public void Initialize(CustomerApiContext context)
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            if (context.Customers.Any())
            {
                return;   // DB has been seeded
            }

            List<Customer> customers = new List<Customer>
            {
                new Customer { CompanyName = "TestCompany", RegistrationNumber = 1, Email = "test@mail.dk", PhoneNumber = "75757575", AddressBilling = "testvej", AddressShipping = "testvej 2", CreditStanding = true },
                new Customer { CompanyName = "ggCompany", RegistrationNumber = 1, Email = "ggCompany@mail.dk", PhoneNumber = "55542055", AddressBilling = "ostervej", AddressShipping = "ostvej 2", CreditStanding = false }
            };

            Console.WriteLine(customers);

        context.Customers.AddRange(customers);
            context.SaveChanges();
        }
    }
}
