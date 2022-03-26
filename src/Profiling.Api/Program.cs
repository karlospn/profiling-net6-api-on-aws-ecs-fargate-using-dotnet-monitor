using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Profiling.Api.Services.BlockingThreads;
using Profiling.Api.Services.HighCpuUsage;
using Profiling.Api.Services.MemoryLeak;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IHighCpuUsageService, HighCpuUsageService>();
builder.Services.AddTransient<IBlockingThreadsService, BlockingThreadsService>();
builder.Services.AddSingleton<IMemoryLeakService, MemoryLeakService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
