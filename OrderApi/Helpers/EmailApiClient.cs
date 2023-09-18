using RestSharp;

namespace OrderApi.Helpers
{
    public class EmailApiClient
    {
        private readonly RestClient _client;

        public EmailApiClient(string baseUrl)
        {
            _client = new RestClient(baseUrl);
        }

        public async Task<RestResponse> SendEmailAsync()
        {
            var request = new RestRequest("Email", Method.Post);
            return await _client.ExecuteAsync(request);
        }
    }
}
