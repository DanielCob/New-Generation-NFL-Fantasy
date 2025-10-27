using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace NFL_Fantasy_API.Extensions
{
    public static class ControllerExtensions
    {
        public static int UserId(this ControllerBase c)
        {
            // 1) Primero, de HttpContext.Items (lo puso el middleware)
            if (c.HttpContext.Items.TryGetValue("UserID", out var val) && val is int idFromItems && idFromItems > 0)
                return idFromItems;

            // 2) Fallback desde Claims
            var claim = c.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        public static string? Ip(this ControllerBase c)
            => c.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
