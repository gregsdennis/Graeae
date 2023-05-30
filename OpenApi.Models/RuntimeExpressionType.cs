using System.ComponentModel;
using System.Text.Json.Serialization;
using Json.More;

namespace OpenApi.Models;

public enum RuntimeExpressionType
{
	Unspecified,
	Url,
	Method,
	StatusCode,
	Request,
	Response
}