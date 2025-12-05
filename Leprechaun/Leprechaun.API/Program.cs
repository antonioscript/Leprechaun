using Amazon.Lambda.AspNetCoreServer;
using Leprecaun.Infra.Context;
using Leprecaun.Infra.Repositories;
using Leprechaun.Application.Services;
using Leprechaun.Application.Telegram.Flows;
using Leprechaun.Application.Telegram.Flows.CostCenterBalance;
using Leprechaun.Application.Telegram.Flows.CostCenterExpense;
using Leprechaun.Application.Telegram.Flows.CostCenterStatement;
using Leprechaun.Application.Telegram.Flows.CreateCostCenter;
using Leprechaun.Application.Telegram.Flows.Patrimony;
using Leprechaun.Application.Telegram.Flows.SalaryAccumulatedInfo;
using Leprechaun.Application.Telegram.Flows.SalaryExpense;
using Leprechaun.Application.Telegram.Flows.SalaryIncome;
using Leprechaun.Application.Telegram.Flows.SalaryStatement;
using Leprechaun.Application.Telegram.Flows.SupportSuggestion;
using Leprechaun.Application.Telegram.Flows.TransferBetweenCostCenters;
using Leprechaun.Application.Telegram.Flows.TransferSalaryToCostCenter;
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
        Version = "7",
    });
});


var connectionString = builder.Configuration.GetConnectionString("LeprechaunDb");

builder.Services.AddDbContext<LeprechaunDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});


//Configure
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddHttpClient<ITelegramSender, TelegramSender>();

//Repositories
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IInstitutionRepository, InstitutionRepository>();
builder.Services.AddScoped<ICostCenterRepository, CostCenterRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IFinanceTransactionRepository, FinanceTransactionRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IChatStateRepository, ChatStateRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<ISupportSuggestionRepository, SupportSuggestionRepository>();


//Servcies
builder.Services.AddScoped<IChatStateService, ChatStateService>();
builder.Services.AddScoped<IInstitutionService, InstitutionService>();
builder.Services.AddScoped<ICostCenterService, CostCenterService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IFinanceTransactionService, FinanceTransactionService>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ISupportSuggestionService, SupportSuggestionService>();


//Flows
builder.Services.AddScoped<IChatFlow, SalaryIncomeFlowService>();
builder.Services.AddScoped<IChatFlow, SalaryAccumulatedInfoFlowService>();
builder.Services.AddScoped<IChatFlow, CreateCostCenterFlowService>();
builder.Services.AddScoped<IChatFlow, TransferBetweenCostCentersFlowService>();
builder.Services.AddScoped<IChatFlow, TransferSalaryToCostCenterFlowService>();
builder.Services.AddScoped<IChatFlow, CostCenterBalanceFlowService>();
builder.Services.AddScoped<IChatFlow, SalaryExpenseFlowService>();
builder.Services.AddScoped<IChatFlow, RegisterCostCenterExpenseFlowService>();
builder.Services.AddScoped<IChatFlow, CostCenterMonthlyStatementFlowService>();
builder.Services.AddScoped<IChatFlow, SalaryAccumulatedMonthlyStatementFlowService>();
builder.Services.AddScoped<IChatFlow, SupportSuggestionFlowService>();
builder.Services.AddScoped<IChatFlow, PatrimonyFlowService>();


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
