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
            string requestPath = context.Request.Path;

            Path path = config.Tanuki.Paths.FirstOrDefault(path =>
               path.Uri == requestPath
            );

            if (path is null)
            {
                // TODO: Handle path not found
            }
            else
            {
                string requestAction = context.Request.Method;
                Operation operation = path.Operations.FirstOrDefault(operation =>
                    operation.Name.Equals(requestAction, StringComparison.InvariantCultureIgnoreCase)
                );

                if (operation is null)
                {
                    // TODO: Handle operation not found
                }
                else
                {
                    await context.Response.WriteAsync(operation.Responses.First().Content.First().Examples.First().Value);
                }
            }
        }
    }
}