using Microsoft.AspNetCore.Http;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Configuration.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Onyx.Tanuki.Simulation
{
    public class SimulationMiddleware
    {
        private readonly RequestDelegate _next;

        public SimulationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, JsonConfiguration config)
        {
            // Find a matching Path
            string requestPath = context.Request.Path;
            Path path = config.Tanuki.Paths.FirstOrDefault(path =>
               path.Uri == requestPath
            );
            if (path is null)
            {
                // Path not found
                // Return 404 - Not Found (https://tools.ietf.org/html/rfc7231#section-6.5.4)
                await CreateNotFoundResponse(context);
            }
            else
            {
                // Path found - Match Operation
                string requestAction = context.Request.Method;
                Operation operation = path.Operations.FirstOrDefault(operation =>
                    operation.Name.Equals(requestAction, StringComparison.InvariantCultureIgnoreCase)
                );
                if (operation is null)
                {
                    // Operation not found
                    // Return 405 - Method Not Allowed (https://tools.ietf.org/html/rfc7231#section-6.5.5)
                    await CreateMethodNotAllowedResponse(context);
                }
                else
                {
                    // Operation found
                    // Simulate operation
                    await context.Response.WriteAsync(operation.Responses.First().Content.First().Examples.First().Value);
                }
            }
        }

        /// <summary>
        /// Creates a 404 Not Found HTTP response
        /// https://tools.ietf.org/html/rfc7231#section-6.5.4
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Task CreateNotFoundResponse(HttpContext context)
        {
            context.Response.ContentType = context.Request.ContentType;
            context.Response.StatusCode = 404;
            return context.Response.WriteAsync("");
        }


        /// <summary>
        /// Creates a 405 Method Not Allowed HTTP response
        /// https://tools.ietf.org/html/rfc7231#section-6.5.5
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Task CreateMethodNotAllowedResponse(HttpContext context)
        {
            context.Response.ContentType = context.Request.ContentType;
            context.Response.StatusCode = 405;
            return context.Response.WriteAsync("");
        }
    }
}