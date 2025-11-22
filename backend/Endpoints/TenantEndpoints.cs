using Microsoft.EntityFrameworkCore;
using MyHackathonAPI.Data;
using MyHackathonAPI.Models;

namespace MyHackathonAPI.Endpoints;

public static class TenantEndpoints {
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app) {
        
        app.MapGet("/tenants", async (AppDb db) => 
            await db.Tenants.ToListAsync());
    }
}
