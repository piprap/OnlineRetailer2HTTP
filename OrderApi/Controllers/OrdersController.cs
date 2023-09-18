﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using OrderApi.Data;
using OrderApi.Models;
using RestSharp;
using SharedModels;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IRepository<Order> repository;

        public OrdersController(IRepository<Order> repos)
        {
            repository = repos;
        }

        // GET: orders
        [HttpGet]
        public IEnumerable<Order> Get()
        {
            return repository.GetAll();
        }

        // GET orders/5
        [HttpGet("{id}", Name = "GetOrder")]
        public IActionResult Get(int id)
        {
            var item = repository.Get(id);
            if (item == null)
            {
                return NotFound();
            }
            return new ObjectResult(item);
        }

        // POST orders
        [HttpPost]
        public IActionResult Post([FromBody]Order order)
        {
            if (order == null)
            {
                return BadRequest();
            }

            if (ProductItemsAvailable(order))
            {
                // Update the number of items reserved for the ordered products,
                // and create a new order.
                if (UpdateItemsReserved(order))
                {
                    // Create order.
                    order.Status = Order.OrderStatus.completed;
                    var newOrder = repository.Add(order);
                    return CreatedAtRoute("GetOrder",
                        new { id = newOrder.Id }, newOrder);
                }
            }

            // If the order could not be created, "return no content".
            return NoContent();
        }


        private bool ProductItemsAvailable(Order order)
        {
            foreach (var orderLine in order.OrderLines)
            {
                // Call product service to get the product ordered.
                // You may need to change the port number in the BaseUrl below
                // before you can run the request.
                RestClient c = new RestClient("https://localhost:5001/products/");
                var request = new RestRequest(orderLine.ProductId.ToString());
                var response = c.GetAsync<ProductDto>(request);
                response.Wait();
                var orderedProduct = response.Result;
                if (orderLine.Quantity > orderedProduct.ItemsInStock - orderedProduct.ItemsReserved)
                {
                    return false;
                }
            }
            return true;
        }

        private bool UpdateItemsReserved(Order order)
        {
            foreach (var orderLine in order.OrderLines)
            {
                // Call product service to get the product ordered.
                // You may need to change the port number in the BaseUrl below
                // before you can run the request.
                RestClient c = new RestClient("https://localhost:5001/products/");
                var request = new RestRequest(orderLine.ProductId.ToString());
                var response = c.GetAsync<ProductDto>(request);
                response.Wait();
                var orderedProduct = response.Result;
                orderedProduct.ItemsReserved += orderLine.Quantity;

                // Call product service to update the number of items reserved
                var updateRequest = new RestRequest(orderedProduct.Id.ToString());
                updateRequest.AddJsonBody(orderedProduct);
                var updateResponse = c.PutAsync(updateRequest);
                updateResponse.Wait();
                if (!updateResponse.IsCompletedSuccessfully)
                    return false;

            }
            return true;
        }

        // tilføj update function med ændring af status til shipped eller cancelled. husk update product in stock
        //order.status = OrderDto.OrderStatus.shipped;
        //order.status = OrderDto.OrderStatus.cancelled;


        //email function  -- need fixing

        /*
        public async Task<String> SendEmail(OrderDto order) {
            // http://emailapi/Email/   docker-compose url
            RestClient _client = new RestClient("http://localhost:5004/Email/");
            // Process the order...
            // needs some fixing
            var customerId = order.customerId;
            RestClient customer_client = new RestClient("http://localhost:5001/Customer/");
            var customerEmail = new RestRequest("Email", Method.Get);
            // Then send the invoice email.
            var request = new RestRequest("email", Method.Post);
            request.AddJsonBody(new { EmailAddress = customerEmail, InvoiceContent = "..." });

            var response = await _client.ExecuteAsync<string>(request);

            if (response.IsSuccessful)
            {
                Console.WriteLine(response.Data);
            }
            else
            {
                // Handle error...
                Console.WriteLine("No customer email was found, please try again");
            }
            return "";
        
        } */

    }
}
