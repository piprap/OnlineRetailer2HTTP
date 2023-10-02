using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CustomerApi.Data;
using CustomerApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly IRepository<Customer> repository;

        public CustomerController(IRepository<Customer> repos)
        {
            repository = repos;
        }

        [HttpGet]
        public async Task<IEnumerable<Customer>> Get()
        {
            return await repository.GetAllAsync();
        }

        [HttpGet("{id}", Name = "GetCustomer")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await repository.GetAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return new ObjectResult(item);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Customer customer)
        {
            if (customer == null)
            {
                return BadRequest();
            }

            var newCustomer = await repository.AddAsync(customer);

            return CreatedAtRoute("GetCustomer", new { id = newCustomer.Id }, newCustomer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Customer customer)
        {
            if (customer == null || customer.Id != id)
            {
                return BadRequest();
            }

            var modifiedCustomer = await repository.GetAsync(id);

            if (modifiedCustomer == null)
            {
                return NotFound();
            }

            modifiedCustomer.CompanyName = customer.CompanyName;
            modifiedCustomer.RegistrationNumber = customer.RegistrationNumber;
            modifiedCustomer.Email = customer.Email;
            modifiedCustomer.PhoneNumber = customer.PhoneNumber;
            modifiedCustomer.AddressBilling = customer.AddressBilling;
            modifiedCustomer.AddressShipping = customer.AddressShipping;
            modifiedCustomer.CreditStanding = customer.CreditStanding;

            await repository.EditAsync(modifiedCustomer);
            return new NoContentResult();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await repository.GetAsync(id) == null)
            {
                return NotFound();
            }

            await repository.RemoveAsync(id);
            return new NoContentResult();
        }
    }
}
