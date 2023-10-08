using System;
using System.Threading;
using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using ProductApi.Data;
using ProductApi.Models;
using RabbitMQ.Client.Logging;
using SharedModels;

namespace ProductApi.Infrastructure
{
    public class MessageListener
    {
        IServiceProvider provider;
        string connectionString;

        // The service provider is passed as a parameter, because the class needs
        // access to the product repository. With the service provider, we can create
        // a service scope that can provide an instance of the product repository.
        public MessageListener(IServiceProvider provider, string connectionString)
        {
            this.provider = provider;
            this.connectionString = connectionString;
        }

        public void Start()
        {
            using (var bus = RabbitHutch.CreateBus(connectionString))
            {
                bus.PubSub.Subscribe<OrderStatusChangedMessage>("productApiCompleted",
                    HandleOrderCompleted, x => x.WithTopic("completed"));

                bus.PubSub.Subscribe<OrderStatusChangedMessage>("productApiCancelled",
                    HandleOrderCancelled, x => x.WithTopic("cancelled"));

                bus.PubSub.Subscribe<OrderStatusChangedMessage>("productApiShipped",
                    HandleOrderShipped, x => x.WithTopic("shipped"));

                bus.PubSub.Subscribe<OrderStatusChangedMessage>("productApiPayed",
                    HandleOrderPayed, x => x.WithTopic("payed"));

                // Add code to subscribe to other OrderStatusChanged events:
                // * cancelled
                // * shipped
                // * paid
                // Implement an event handler for each of these events.
                // Be careful that each subscribe has a unique subscription id
                // (this is the first parameter to the Subscribe method). If they
                // get the same subscription id, they will listen on the same
                // queue.

                // Block the thread so that it will not exit and stop subscribing.
                lock (this)
                {
                    Monitor.Wait(this);
                }
            }

        }

        private async void HandleOrderCompleted(OrderStatusChangedMessage message)
        {
            // A service scope is created to get an instance of the product repository.
            // When the service scope is disposed, the product repository instance will
            // also be disposed.
            using (var scope = provider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var productRepos = services.GetService<IRepository<Product>>();

                // Reserve items of ordered product (should be a single transaction).
                // Beware that this operation is not idempotent.

                foreach (var orderLine in message.OrderLines)
                {
                    var product = await productRepos.GetAsync(orderLine.ProductId);
                    product.ItemsReserved += orderLine.Quantity;
                    await productRepos.EditAsync(product);
                }
            }
        }

        private async void HandleOrderCancelled(OrderStatusChangedMessage message)
        {
            // A service scope is created to get an instance of the product repository.
            // When the service scope is disposed, the product repository instance will
            // also be disposed.
            using (var scope = provider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var productRepos = services.GetService<IRepository<Product>>();

                // Reserve items of ordered product (should be a single transaction).
                // Beware that this operation is not idempotent.
                foreach (var orderLine in message.OrderLines)
                {
                    var product = await productRepos.GetAsync(orderLine.ProductId);
                    product.ItemsReserved -= orderLine.Quantity;
                    await productRepos.EditAsync(product);
                }
            }
        }

        private async void HandleOrderPayed(OrderStatusChangedMessage message)
        {
            await Task.Delay(500);
            Console.WriteLine("Payed");
        }

        private async void HandleOrderShipped(OrderStatusChangedMessage message)
        {
            // A service scope is created to get an instance of the product repository.
            // When the service scope is disposed, the product repository instance will
            // also be disposed.
            using (var scope = provider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var productRepos = services.GetService<IRepository<Product>>();

                // Reserve items of ordered product (should be a single transaction).
                // Beware that this operation is not idempotent.
                foreach (var orderLine in message.OrderLines)
                {
                    var product = await productRepos.GetAsync(orderLine.ProductId);
                    product.ItemsReserved -= orderLine.Quantity;
                    product.ItemsInStock -= orderLine.Quantity;

                    await productRepos.EditAsync(product);
                }
            }
        }
    }
}