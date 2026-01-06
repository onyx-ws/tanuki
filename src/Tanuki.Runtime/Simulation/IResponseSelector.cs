using Microsoft.AspNetCore.Http;
using Onyx.Tanuki.Configuration;

namespace Onyx.Tanuki.Simulation;

/// <summary>
/// Service for selecting responses and examples based on request context
/// </summary>
public interface IResponseSelector
{
    /// <summary>
    /// Selects a response based on status code or returns the first available
    /// </summary>
    Response? SelectResponse(Operation operation, HttpContext context);

    /// <summary>
    /// Selects content based on content-type negotiation
    /// </summary>
    Content? SelectContent(Response response, HttpContext context);

    /// <summary>
    /// Selects an example based on query parameters or randomly
    /// </summary>
    Example? SelectExample(Content content, HttpContext context);
}
