using System.Net;

namespace OpenApi.Models;

public class ResponseCollection : Dictionary<HttpStatusCode, Response>
{
	public Response? Default { get; set; }
	public ExtensionData? ExtensionData { get; set; }
}