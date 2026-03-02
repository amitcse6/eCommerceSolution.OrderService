using FluentValidation.AspNetCore;
using OrdersMicroservice.API.Middleware;
using eCommerce.OrderMicroservice.DataAccessLayer;
using eCommerce.OrderMicroservice.BusinessLogicLayer;
using BusinessLogicLayer.HttpClients;
using BusinessLogicLayer.Policy;

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

builder.Services.AddSingleton<IUserMicroservicePolicy, UserMicroservicePolicy>();

builder.Services.AddHttpClient<UsersMicroserviceClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri($"http://{configuration["UsersMicroserviceName"]}:{configuration["UsersMicroservicePort"]}");
})
.AddPolicyHandler((serviceProvider, request) =>
{
    var policy = serviceProvider.GetRequiredService<IUserMicroservicePolicy>();
    return policy.GetRetryPolicy();
})
.AddPolicyHandler((serviceProvider, request) =>
{
    var policy = serviceProvider.GetRequiredService<IUserMicroservicePolicy>();
    return policy.GetCircuitBreakerPolicy();
});

builder.Services.AddHttpClient<ProductsMicroserviceClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri($"http://{configuration["ProductsMicroserviceName"]}:{configuration["ProductsMicroservicePort"]}");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandlingMiddleware();

app.UseCors();

app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
