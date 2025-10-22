using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Seed sponsors
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Check if sponsors already exist
    if (!await context.Sponsors.AnyAsync())
    {
        var sponsors = new List<Sponsor>
        {
            new Sponsor { Name = "ACAA", Description = "Asociación Cubana de Ayuda Humanitaria", LogoPath = "/images/sponsors/acaa.png", IsActive = true, DisplayOrder = 1, CreatedDate = DateTime.Now },
            new Sponsor { Name = "Adriano Solidaire", Description = "Adriano Solidario - Organización de apoyo humanitario", LogoPath = "/images/sponsors/adranosolidaire.png", IsActive = true, DisplayOrder = 2, CreatedDate = DateTime.Now },
            new Sponsor { Name = "Apotheek", Description = "Farmacia solidaria de apoyo internacional", LogoPath = "/images/sponsors/apotheek.png", IsActive = true, DisplayOrder = 3, CreatedDate = DateTime.Now },
            new Sponsor { Name = "HSF", Description = "Health Support Foundation", LogoPath = "/images/sponsors/hsf.JPG", IsActive = true, DisplayOrder = 4, CreatedDate = DateTime.Now },
            new Sponsor { Name = "Janeiro Solidário", Description = "Janeiro Solidario - Iniciativa de ayuda humanitaria", LogoPath = "/images/sponsors/janeiro.png", IsActive = true, DisplayOrder = 5, CreatedDate = DateTime.Now },
            new Sponsor { Name = "Sutures Medical", Description = "Proveedor de suministros médicos", LogoPath = "/images/sponsors/suturesmedical.png", IsActive = true, DisplayOrder = 6, CreatedDate = DateTime.Now }
        };
        
        context.Sponsors.AddRange(sponsors);
        await context.SaveChangesAsync();
        
        Console.WriteLine("✅ 6 patrocinadores insertados exitosamente!");
        
        // Show results
        var allSponsors = await context.Sponsors.OrderBy(s => s.DisplayOrder).ToListAsync();
        Console.WriteLine("\n📋 Patrocinadores en la base de datos:");
        foreach (var sponsor in allSponsors)
        {
            Console.WriteLine($"  {sponsor.DisplayOrder}. {sponsor.Name} - {sponsor.LogoPath}");
        }
    }
    else
    {
        Console.WriteLine("ℹ️  Ya existen patrocinadores en la base de datos.");
        var count = await context.Sponsors.CountAsync();
        Console.WriteLine($"   Total: {count} patrocinadores");
    }
}

Console.WriteLine("\n✅ Proceso completado. Presiona cualquier tecla para salir...");
Console.ReadKey();
