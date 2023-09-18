using System;
using System.Collections.Generic;
using CustomerApi.Data;
using CustomerApi.Models;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

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

        // GET: customers
        [HttpGet]
        public IEnumerable<Customer> Get()
        {
            return repository.GetAll();
        }

        // GET customers/5
        [HttpGet("{id}", Name = "GetCustomer")]
        public IActionResult Get(int id)
        {
            var item = repository.Get(id);
            if (item == null)
            {
                return NotFound();
            }
            return new ObjectResult(item);
        }

        // POST customers
        [HttpPost]
        public IActionResult Post([FromBody] Customer customer)
        {
            if (customer == null)
            {
                return BadRequest();
            }

            var newCustomer = repository.Add(customer);

            return CreatedAtRoute("GetCustomer", new { id = newCustomer.Id }, newCustomer);
        }

        // PUT customers/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] Customer customer)
        {
            if (customer == null || customer.Id != id)
            {
                return BadRequest();
            }

            var modifiedCustomer = repository.Get(id);

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


            repository.Edit(modifiedCustomer);
            return new NoContentResult();
        }

        // DELETE customers/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            if (repository.Get(id) == null)
            {
                return NotFound();
            }

            repository.Remove(id);
            return new NoContentResult();
        }

    }
}
