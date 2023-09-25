using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Services
{
    public class EmailService
    {
        public async Task SendEmailAsync()
        {
            RestClient c = new RestClient("http://email-service/");
            var request = new RestRequest("Email", Method.Post);
            await c.ExecuteAsync(request);
        }
    }
}
