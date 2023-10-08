using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrderApi.Data;
using OrderApi.Helpers;
using OrderApi.Infrastructure;
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
        private readonly IMessagePublisher _messagePublisher;
        IServiceGateway<ProductDto> _productServiceGateway;


        public OrdersController(IRepository<Order> repos, EmailService emailService, IMessagePublisher messagePublisher, IServiceGateway<ProductDto> productServiceGateway)
        {
            repository = repos;
            _emailService = emailService;
            _messagePublisher = messagePublisher;
            _productServiceGateway = productServiceGateway;
        }

        [HttpGet]
        public async Task<IEnumerable<Order>> Get()
        {
            return await repository.GetAllAsync();
        }

        [HttpGet("{id}", Name = "GetOrder")]
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
        public async Task<IActionResult> Post([FromBody] Order order)
        {
            if (order == null)
            {
                return BadRequest();
            }

            RestClient c = new RestClient("http://customer-service/Customer/");
            var getRequest = new RestRequest(order.CustomerId.ToString());
            var getResponse = await c.GetAsync<CustomerDto>(getRequest);
            Console.Write("Credit stading: " + getResponse.CreditStanding);
            if (!getResponse.CreditStanding)
            {
                Console.WriteLine("Customer bad credit stading");
                return UnprocessableEntity();
            }


            if (await ProductItemsAvailable(order))
            {
                //if (await UpdateItemsReserved(order))
                //{
                    _messagePublisher.PublishOrderStatusChangedMessage(
                       order.CustomerId, order.OrderLines, "completed");

                    var newOrder = await repository.AddAsync(order);

                    await _emailService.SendEmailAsync();

                    order.Status = Order.OrderStatus.completed;

                    return CreatedAtRoute("GetOrder",
                        new { id = newOrder.Id }, newOrder);
                //}
            }

            return NoContent();
        }

        private async Task<bool> UpdateItemsReserved(Order order)
        {
            foreach (var orderLine in order.OrderLines)
            {
                RestClient c = new RestClient("http://product-service/products/");
                var request = new RestRequest(orderLine.ProductId.ToString());
                var response = await c.GetAsync<ProductDto>(request);
                var orderedProduct = response;

                orderedProduct.ItemsInStock -= orderLine.Quantity;
                orderedProduct.ItemsReserved += orderLine.Quantity;

                var updateRequest = new RestRequest(orderedProduct.Id.ToString());
                updateRequest.AddJsonBody(orderedProduct);
                var updateResponse = await c.PutAsync(updateRequest);
                if (!updateResponse.IsSuccessful)
                    return false;
            }
            return true;
        }

        private async Task<bool> ProductItemsAvailable(Order order)
        {
            foreach (var orderLine in order.OrderLines)
            {
                // Call product service to get the product ordered.
                var orderedProduct = await _productServiceGateway.GetAsync(orderLine.ProductId);
                if (orderLine.Quantity > orderedProduct.ItemsInStock - orderedProduct.ItemsReserved)
                {
                    return false;
                }
            }
            return true;
        }
     /*
        //med http
          private async Task<bool> ProductItemsAvailable(Order order)
          {
              foreach (var orderLine in order.OrderLines)
              {
                  RestClient c = new RestClient("http://product-service/products/");
                  var request = new RestRequest(orderLine.ProductId.ToString());
                  var response = await c.GetAsync<ProductDto>(request);
                  var orderedProduct = response;

                  if (orderLine.Quantity > orderedProduct.ItemsInStock - orderedProduct.ItemsReserved)
                  {
                      return false;
                  }
              }
              return true;
          }
     */
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Order order)
        {
            if (order == null || order.Id != id)
            {
                return BadRequest();
            }

            var modifiedOrder = await repository.GetAsync(id);

            if (modifiedOrder == null)
            {
                return NotFound();
            }

            modifiedOrder.CustomerId = order.CustomerId;
            modifiedOrder.Status = order.Status;

            await repository.EditAsync(modifiedOrder);
            return Ok(200);
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await repository.GetAsync(id);

            if (order == null)
            {
                return BadRequest("No order found");
            }

            _messagePublisher.PublishOrderStatusChangedMessage(
            order.CustomerId, order.OrderLines, "cancelled");

            order.Status = Order.OrderStatus.cancelled;
            await repository.EditAsync(order);



            foreach (var orderLine in order.OrderLines)
            {
                RestClient c = new RestClient("http://product-service/products/");
                var request = new RestRequest(orderLine.ProductId.ToString());
                var response = await c.GetAsync<ProductDto>(request);
                var orderedProduct = response;

                orderedProduct.ItemsReserved -= orderLine.Quantity;
                orderedProduct.ItemsInStock += orderLine.Quantity;

                var updateRequest = new RestRequest(orderedProduct.Id.ToString());
                updateRequest.AddJsonBody(orderedProduct);
                var updateResponse = await c.PutAsync(updateRequest);
            }

            return Ok("Very bad order cancel :(");
        }

        [HttpPut("{id}/ship")]
        public async Task<IActionResult> Ship(int id)
        {
            var order = await repository.GetAsync(id);

            if (order == null)
            {
                return BadRequest("No order found");
            }
            _messagePublisher.PublishOrderStatusChangedMessage(
                       order.CustomerId, order.OrderLines, "shipped");

            order.Status = Order.OrderStatus.shipped;
            await repository.EditAsync(order);

            foreach (var orderLine in order.OrderLines)
            {
                RestClient c = new RestClient("http://product-service/products/");
                var request = new RestRequest(orderLine.ProductId.ToString());
                var response = await c.GetAsync<ProductDto>(request);
                var orderedProduct = response;

                orderedProduct.ItemsReserved -= orderLine.Quantity;
                orderedProduct.ItemsInStock -= orderLine.Quantity;

                var updateRequest = new RestRequest(orderedProduct.Id.ToString());
                updateRequest.AddJsonBody(orderedProduct);
                var updateResponse = await c.PutAsync(updateRequest);
            }

            return Ok("order on the way");
        }

        /* [HttpPut("{id}/pay")]
         public async Task<IActionResult> Pay(int id)
         {
             var getResponse = await repository.GetAsync(id);

             if (getResponse == null)
             {
                 return BadRequest("No order found");
             }

             getResponse.Status = Order.OrderStatus.paid;
             await repository.EditAsync(getResponse);

             return Ok("$$");
         }
        */
        [HttpPut("{id}/pay")]
        public async Task<IActionResult> Pay(int id)
        {
            var order = await repository.GetAsync(id);

            if (order == null)
            {
                return BadRequest("No order found");
            }

            _messagePublisher.PublishOrderStatusChangedMessage(
                       order.CustomerId, order.OrderLines, "payed");

            order.Status = Order.OrderStatus.paid;
            await repository.EditAsync(order);

            return Ok("$$");
        }
    }
}
