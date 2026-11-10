using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Test;

/// <summary>A stand-in for Azure Blob Storage, so tests never need a real storage account.</summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public Task<string> UploadAsync(string containerName, IFormFile file)
        => Task.FromResult($"https://fake-storage.test/{containerName}/{Guid.NewGuid()}.png");

    public Task DeleteAsync(string containerName, string blobUrl) => Task.CompletedTask;
}

/// <summary>Starts the real API in memory, using an in-memory database and a fake image store.</summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    public const string OrganiserCode = "TEST-ORG-CODE";

    // A new name per factory means every test gets its own empty database
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OrganiserInviteCode"] = OrganiserCode
            }));

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registration and use the in-memory database instead
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);
            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));

            // Replace the real Azure Blob service with the fake one
            var blobDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IBlobStorageService));
            if (blobDescriptor != null) services.Remove(blobDescriptor);
            services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Creates the tables and the seeded Organiser/Participant roles
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Database.EnsureCreated();

        return host;
    }
}