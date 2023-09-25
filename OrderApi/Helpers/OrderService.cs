using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using OrderApi.Models;


namespace OrderApi.Helpers
{
    public class OrderService
    {
        public async Task<Order> UpdateOrderAsync(int id)
        {
            RestClient c = new RestClient("http://order-service/Order/");
            var getRequest = new RestRequest(id.ToString());
            var getResponse = await c.GetAsync<Order>(getRequest);

            return getResponse;
        }
    }

}

