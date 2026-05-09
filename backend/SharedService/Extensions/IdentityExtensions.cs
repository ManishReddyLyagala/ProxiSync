using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SharedService.Extensions
{
    public class NameUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection) =>
            connection.User?.Identity?.Name ?? string.Empty;
    }
}
