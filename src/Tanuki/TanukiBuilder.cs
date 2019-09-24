using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Onyx.Tanuki
{
    public static class TanukiBuilder
    {
        public static IApplicationBuilder UseTanuki(this IApplicationBuilder app)
        {
            UseSimulator(app);
            return app;
        }

        public static IApplicationBuilder UseSimulator(this IApplicationBuilder app)
        {
            app.UseMiddleware<Onyx.Tanuki.Simulation.SimulationMiddleware>();
            return app;
        }
    }
}
