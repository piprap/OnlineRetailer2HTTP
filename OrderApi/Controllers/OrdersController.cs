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
      //  private readonly OrderService _orderService;


        public OrdersController(IRepository<Order> repos, EmailService emailService/*, OrderService orderService*/)
        {
            repository = repos;
            _emailService = emailService;
            //_orderService = orderService;
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
        private async Task<bool> UpdateStatus(Order order, string newStatus) //parse ny status
        {
            RestClient c = new RestClient("http://order-service/Order/");
            var getRequest = new RestRequest(order.Id.ToString());
            var getResponse = await c.GetAsync<Order>(getRequest);

            if(newStatus == "shipped")
            {
                order.Status = Order.OrderStatus.shipped;
            }


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


        // PUT customers/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] Order order)
        {
            if (order == null || order.Id != id)
            {
                return BadRequest();
            }

            var modifiedOrder = repository.Get(id);

            Console.WriteLine("original order OrderLines: " + order.OrderLines);

            Console.WriteLine("BEFORE OrderLines: " + modifiedOrder.OrderLines);

            if (modifiedOrder == null)
            {
                return NotFound();
            }

            modifiedOrder.CustomerId = order.CustomerId;
            modifiedOrder.Status = order.Status;


            repository.Edit(modifiedOrder);
            return Ok(200);
        }

        // PUT orders/5/cancel
        // This action method cancels an order and publishes an OrderStatusChangedMessage
        // with topic set to "cancelled".
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            /*RestClient c = new RestClient("http://order-service/Order/");
            var getRequest = new RestRequest(id.ToString());
            var getResponse = await c.GetAsync<Order>(getRequest);*/

            var getResponse = repository.Get(id);
          //  var getResponse = await _orderService.UpdateOrderAsync(id);

            if (getResponse == null)
            {
                return BadRequest("No order found");
            }
            getResponse.Status = Order.OrderStatus.cancelled;



            repository.Edit(getResponse);


            foreach (var orderLine in getResponse.OrderLines)
            {

                RestClient c = new RestClient("http://product-service/products/");
                var request = new RestRequest(orderLine.ProductId.ToString());
                var response = c.GetAsync<ProductDto>(request);
                response.Wait();
                var orderedProduct = response.Result;

                orderedProduct.ItemsReserved -= orderLine.Quantity;


                var updateRequest = new RestRequest(response.Id.ToString());
                updateRequest.AddJsonBody(orderedProduct);
                var updateResponse = c.PutAsync(updateRequest);
                updateResponse.Wait();

            }

            return Ok("Very bad order cancel :(");

            // Add code to implement this method.
        }
     /*
        // PUT orders/5/ship
        // This action method ships an order and publishes an OrderStatusChangedMessage.
        // with topic set to "shipped".
        [HttpPut("{id}/ship")]
        public async Task<IActionResult> Ship(int id)
        {
            var getResponse = await _orderService.UpdateOrderAsync(id);

            if (getResponse != null)
            {
                return BadRequest("No order found");
            }

            getResponse.Status = Order.OrderStatus.shipped;

            return Ok("order on the way");

            // Add code to implement this method.
        }

        // PUT orders/5/pay
        // This action method marks an order as paid and publishes a CreditStandingChangedMessage
        // (which have not yet been implemented), if the credit standing changes.
        [HttpPut("{id}/pay")]
        public async Task<IActionResult> Pay(int id)
        {
            var getResponse = await _orderService.UpdateOrderAsync(id);

            if (getResponse != null)
            {
                return BadRequest("No order found");
            }

            getResponse.Status = Order.OrderStatus.paid;

            return Ok("$$");

            // Add code to implement this method.
        }
     */


    }
}
