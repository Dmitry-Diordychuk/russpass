using Microsoft.EntityFrameworkCore;
using QuizApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOrigin",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

// Добавление сервисов в контейнер.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Здесь ASP.NET Core автоматически обнаружит ваш QuestionsController
builder.Services.AddControllers();

// Добавление и конфигурация DbContext
builder.Services.AddDbContext<QuizContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseStaticFiles();

// Настройка конвейера HTTP запросов.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers(); // Важно! Это обеспечивает маршрутизацию к вашим контроллерам.

app.UseCors("AllowAnyOrigin");

app.Run();
