using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using OrderApi.Data;
using OrderApi.Helpers;
using OrderApi.Models;
using RestSharp;
using SharedModels;
using SharedModels.Services;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IRepository<Order> repository;
        private readonly EmailService _emailService;

        public OrdersController(IRepository<Order> repos, EmailService emailService)
        {
            repository = repos;
            _emailService = emailService;
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
        public async Task<IActionResult> Post([FromBody] Order order) // Make the method asynchronous
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
                    var newOrder = repository.Add(order);

                    // Send email
                   await _emailService.SendEmailAsync();

                    // Set status to completed
                    order.Status = Order.OrderStatus.completed;

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
                RestClient c = new RestClient("http://product-service/products/");
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
                RestClient c = new RestClient("http://product-service/products/");
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

        
        // Update status function
        // mangler stadig noget arbejde - har ikke lige gjort mig tanker om hvordan den skal kunne kaldes endnu og den skal vel også tage imod
        // et id som der skal opdateres på. men dette er et meget godt udkast taget i betragtning hvor zank jeg er lige nu xD
        private async Task<bool> UpdateStatus(Order order)
        {
            RestClient c = new RestClient("http://order-service/Order/");
            var getRequest = new RestRequest(order.Id.ToString());
            var getResponse = await c.GetAsync<Order>(getRequest);

            if (getResponse != null)
            {
                if (order.Status == Order.OrderStatus.completed)
                {
                    getResponse.Status = Order.OrderStatus.shipped;
                    // her skal product in stock opdateres (-)
                }
                else
                {
                    getResponse.Status = Order.OrderStatus.cancelled;
                    // her skal product reserved opdateres (+) (i guess?)
                }

                var updateRequest = new RestRequest(getResponse.Id.ToString(), Method.Put);
                updateRequest.AddJsonBody(getResponse);
                var updateResponse = await c.ExecuteAsync(updateRequest);

                if (updateResponse.IsSuccessful)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
