using Json.Schema.Generation;
using Json.Schema.Generation.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Graeae.AspNet.Sample.Controllers.Controllers;

[ApiController]
[Route("[controller]")]
public class GoodbyeController : ControllerBase
{
    [HttpPost]
    public ActionResult<string> Post([FromBody] Person person)
    {
        return Ok($"Hello, {person.Name}");
    }
}

[GenerateJsonSchema]
public record Person(
    [property: Required]
    string Name,
    [property: ExclusiveMinimum(0)]
    int Age);