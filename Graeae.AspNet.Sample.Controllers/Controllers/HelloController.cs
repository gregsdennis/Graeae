using Microsoft.AspNetCore.Mvc;

namespace Graeae.AspNet.Sample.Controllers.Controllers;

[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public ActionResult<string> Get([FromQuery] string? name)
    {
        return Ok($"Hello, {name ?? "World"}");
    }
}