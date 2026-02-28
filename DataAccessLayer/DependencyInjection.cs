using eCommerce.OrdersMicroservice.DataAccessLayer.Repositories;
using eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace eCommerce.OrderMicroservice.DataAccessLayer;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionStringTemplate = configuration.GetConnectionString("MongoDB")!;
        
        string mongoHost = Environment.GetEnvironmentVariable("MONGODB_HOST") ?? "localhost";
        string mongoPort = Environment.GetEnvironmentVariable("MONGODB_PORT") ?? "27017";
        
        string connectionString = connectionStringTemplate
            .Replace("$MONGO_HOST", mongoHost)
            .Replace("$MONGO_PORT", mongoPort);
        
        services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
        services.AddScoped<IMongoDatabase>(provider =>
        {
            IMongoClient client = provider.GetRequiredService<IMongoClient>();
            return client.GetDatabase(Environment.GetEnvironmentVariable("MONGODB_DATABASE"));
        });
        services.AddScoped<IOrdersRepository, OrdersRepository>();  
        return services;
    }
}
