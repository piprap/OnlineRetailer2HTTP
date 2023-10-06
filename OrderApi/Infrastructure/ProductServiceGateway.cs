using System;
using RestSharp;
using SharedModels;

namespace OrderApi.Infrastructure
{
    public class ProductServiceGateway : IServiceGateway<ProductDto>
    {
        string productServiceBaseUrl;

        public ProductServiceGateway(string baseUrl)
        {
            productServiceBaseUrl = baseUrl;
        }

        public async Task<ProductDto> GetAsync(int id)
        {
            RestClient c = new RestClient(productServiceBaseUrl);

            var request = new RestRequest(id.ToString());
            var response = await c.GetAsync<ProductDto>(request);
 
            return response;
        }
    }
}