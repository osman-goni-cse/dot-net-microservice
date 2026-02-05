using Microsoft.EntityFrameworkCore;
using PlatformService.Models;

namespace PlatformService.Data;

public static class PrepDb
{
     public static void PrepPopulation(IApplicationBuilder app)
     {
          using var scope = app.ApplicationServices.CreateScope();
          var isProduction = scope.ServiceProvider.GetService<IWebHostEnvironment>().IsProduction();
          SeedData(scope.ServiceProvider.GetService<AppDbContext>(), isProduction);
     }

     private static void SeedData(AppDbContext context, bool isProduction)
     {
          if (isProduction)
          {
               Console.WriteLine("Attempting to apply migrations...");
               try
               {
                    context.Database.Migrate();
               }
               catch (Exception e)
               {
                    Console.WriteLine(e);
                    throw;
               }
          }
          if (!context.Platforms.Any())
          {
               Console.WriteLine("Seeding data...");
               context.Platforms.AddRange(
                    new Platform() { Name = "C#", Publisher = "Microsoft", Cost = "Free" },
                    new Platform() { Name = "DotNet", Publisher = "Microsoft", Cost = "Free" },
                    new Platform() { Name = "SQL Server", Publisher = "Microsoft", Cost = "Free" }
               );
               context.SaveChanges();
          }
          else
          {
               Console.WriteLine("Already have data...");
          }
     }
}