using LabelDesignerAPI.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// Add QuestPDF license configuration
QuestPDF.Settings.License = LicenseType.Community; // or LicenseType.Professional if you have a license

// Add at the beginning of your application startup
QuestPDF.Settings.EnableDebugging = true;

var builder = WebApplication.CreateBuilder(args);

// Add DB Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();