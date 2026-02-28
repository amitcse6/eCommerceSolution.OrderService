using FluentValidation.AspNetCore;
using OrdersMicroservice.API.Middleware;
using eCommerce.OrderMicroservice.DataAccessLayer;
using eCommerce.OrderMicroservice.BusinessLogicLayer;
using BusinessLogicLayer.HttpClients;

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
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:4200")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddHttpClient<UserMicroserviceClient>(client =>
{
    client.BaseAddress = new Uri($"http://{builder.Configuration["UsersMicroserviceName"]}:{builder.Configuration["UsersMicroservicePort"]}");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandlingMiddleware();
app.UseRouting();

app.UseCors();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
