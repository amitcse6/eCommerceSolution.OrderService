using BusinessLogicLayer.HttpClients;
using BusinessLogicLayer.Policies;
using eCommerce.OrderMicroservice.BusinessLogicLayer;
using eCommerce.OrderMicroservice.DataAccessLayer;
using FluentValidation.AspNetCore;
using OrdersMicroservice.API.Middleware;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add DAL and BLL services to the container.
builder.Services.AddDataAccessLayer(builder.Configuration);
builder.Services.AddBusinessLogicLayer(builder.Configuration);

// Add Controllers
builder.Services.AddControllers();

// Fluent validation
builder.Services.AddFluentValidationAutoValidation();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder.WithOrigins("http://localhost:4200")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Create a single policy instance so circuit breaker state is shared
builder.Services.AddSingleton<IUserMicroservicePolicy, UserMicroservicePolicy>();
builder.Services.AddSingleton<IProductMicroservicePolicy, ProductMicroservicePolicy>();

var serviceProvider = builder.Services.BuildServiceProvider();
var userMicroservicePolicy = serviceProvider.GetRequiredService<IUserMicroservicePolicy>();
var retryPolicy = userMicroservicePolicy.GetRetryPolicy();
var circuitBreakerPolicy = userMicroservicePolicy.GetCircuitBreakerPolicy();
var timeoutPolicy = userMicroservicePolicy.GetTimeoutPolicy();

builder.Services.AddHttpClient<UsersMicroserviceClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri($"http://{configuration["UsersMicroserviceName"]}:{configuration["UsersMicroservicePort"]}");
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy)
.AddPolicyHandler(timeoutPolicy);

var fallbackPolicy = new ProductMicroservicePolicy(serviceProvider.GetRequiredService<ILogger<ProductMicroservicePolicy>>()).GetFallbackPolicy();

builder.Services.AddHttpClient<ProductsMicroserviceClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri($"http://{configuration["ProductsMicroserviceName"]}:{configuration["ProductsMicroservicePort"]}");
})
.AddPolicyHandler(fallbackPolicy);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandlingMiddleware();

app.UseCors();

app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
