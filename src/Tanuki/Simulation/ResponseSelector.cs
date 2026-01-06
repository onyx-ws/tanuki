using Microsoft.AspNetCore.Http;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Constants;

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

/// <summary>
/// Service for selecting responses and examples based on request context
/// </summary>
public class ResponseSelector : IResponseSelector
{

    /// <summary>
    /// Selects a response based on status code or returns the first available
    /// </summary>
    public Response? SelectResponse(Operation operation, HttpContext context)
    {
        // Check if status code is specified in query string
        if (context.Request.Query.TryGetValue(TanukiConstants.StatusQueryParameter, out var statusCodeStr) &&
            int.TryParse(statusCodeStr, out var statusCode))
        {
            var response = operation.Responses.FirstOrDefault(r => 
                r.StatusCode == statusCode.ToString() || 
                r.StatusCode == statusCodeStr.ToString());
            
            if (response != null)
                return response;
        }

        // Return first response by default
        return operation.Responses.FirstOrDefault();
    }

    /// <summary>
    /// Selects content based on content-type negotiation
    /// </summary>
    public Content? SelectContent(Response response, HttpContext context)
    {
        // Get Accept header
        var acceptHeader = context.Request.Headers.Accept.ToString();
        
        if (!string.IsNullOrWhiteSpace(acceptHeader))
        {
            // Parse Accept header and find best match
            var acceptedTypes = ParseAcceptHeader(acceptHeader);
            
            foreach (var acceptedType in acceptedTypes.OrderByDescending(t => t.Quality))
            {
                var content = response.Content.FirstOrDefault(c => 
                    MatchesContentType(c.MediaType, acceptedType.MediaType));
                
                if (content != null)
                    return content;
            }
        }

        // Return first content by default
        return response.Content.FirstOrDefault();
    }

    /// <summary>
    /// Selects an example based on query parameters or randomly
    /// </summary>
    public Example? SelectExample(Content content, HttpContext context)
    {
        // Check if example name is specified in query string
        if (context.Request.Query.TryGetValue(TanukiConstants.ExampleQueryParameter, out var exampleName))
        {
            var example = content.Examples.FirstOrDefault(e => 
                e.Name.Equals(exampleName.ToString(), StringComparison.OrdinalIgnoreCase));
            
            if (example != null)
                return example;
        }

        // Check if random selection is requested
        if (context.Request.Query.ContainsKey(TanukiConstants.RandomQueryParameter) || 
            context.Request.Query.ContainsKey(TanukiConstants.RandomQueryParameterAlt))
        {
            if (content.Examples.Count > 0)
            {
                var randomIndex = Random.Shared.Next(content.Examples.Count);
                return content.Examples[randomIndex];
            }
        }

        // Return first example by default
        return content.Examples.FirstOrDefault();
    }

    private static List<AcceptType> ParseAcceptHeader(string acceptHeader)
    {
        var types = new List<AcceptType>();
        var parts = acceptHeader.Split(',');

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var semicolonIndex = trimmed.IndexOf(';');
            
            var mediaType = semicolonIndex >= 0 
                ? trimmed[..semicolonIndex].Trim() 
                : trimmed;
            
            var quality = 1.0;
            if (semicolonIndex >= 0)
            {
                var qualityPart = trimmed[(semicolonIndex + 1)..].Trim();
                if (qualityPart.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(qualityPart[2..], out var q))
                        quality = q;
                }
            }

            types.Add(new AcceptType { MediaType = mediaType, Quality = quality });
        }

        return types;
    }

    private static bool MatchesContentType(string contentType, string acceptType)
    {
        // Exact match
        if (contentType.Equals(acceptType, StringComparison.OrdinalIgnoreCase))
            return true;

        // Wildcard match
        if (acceptType == "*/*")
            return true;

        // Partial match (e.g., application/* matches application/json)
        if (acceptType.EndsWith("/*", StringComparison.OrdinalIgnoreCase))
        {
            var acceptBase = acceptType[..^2];
            if (contentType.StartsWith(acceptBase, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private class AcceptType
    {
        public string MediaType { get; set; } = string.Empty;
        public double Quality { get; set; } = 1.0;
    }
}
