using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace SCV.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            Thread.Sleep(2500);
            return new List<string> { "Received data from api." };
        }
    }
}
