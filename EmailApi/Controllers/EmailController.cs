using EmailApi.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmailApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmailController : ControllerBase
    {
        [HttpPost]
        public IActionResult SendEmail()
        {
            // If this was a real service, you would add code to send the email here.
            // In the mock service, we just return a success message.
            Console.WriteLine("Email sent");
            return Ok($"Confirmation email has been sent");
        }

    }
}
