using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VPay.Ols.Processor.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Configure();
builder.Logging.Configure();
builder.Services.Configure(builder.Configuration);

var app = builder.Build();

app.ConfigureMiddleware();
app.ConfigureEndpoints();

app.Run();
