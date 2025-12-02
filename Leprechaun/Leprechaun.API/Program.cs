using Leprechaun.API.Services;
using Amazon.Lambda.AspNetCoreServer;
using Leprecaun.Infra.Context;
using Leprecaun.Infra.Repositories;
using Leprechaun.Application.Services;
using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Leprechaun.API",
        Version = "5",
    });
});


var connectionString = builder.Configuration.GetConnectionString("LeprechaunDb");

builder.Services.AddDbContext<LeprechaunDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddHttpClient<ITelegramSender, TelegramSender>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IPersonService, PersonService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();