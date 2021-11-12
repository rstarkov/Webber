using Microsoft.AspNetCore.Mvc;

namespace Webber.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    [HttpGet]
    public string Get()
    {
        return "Demo!";
    }
}
