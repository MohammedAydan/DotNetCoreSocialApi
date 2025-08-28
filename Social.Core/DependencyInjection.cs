using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Core
{
    public static class DependencyInjection
    {
        public static WebApplicationBuilder AddCoreDI(this WebApplicationBuilder builder)
        {
            return builder;
        }
    }
}
