using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedService.Services;

namespace SharedService.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddSharedServices(this IServiceCollection services)
        {
            services.AddScoped<ITokenService, TokenService>();
            services.AddSingleton<IUserIdProvider, NameUserIdProvider>(); // optional use in SignalR services
            return services;
        }
    }

}
