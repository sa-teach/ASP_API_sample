using ASP_API_sample.Data;
using ASP_API_sample.Repositories;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace ASP_API_sample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Подключение к базе данных (Entity Framework Core + SQLite)
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

        // 2. Регистрация Unit of Work и репозиториев
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        builder.Services.AddControllers();

        // 3. Swagger / OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Products API",
                Version = "v1",
                Description = "Учебный API для управления товарами и категориями"
            });
        });

        var app = builder.Build();

        // 4. Создание БД и заполнение начальными данными
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(db);
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Products API v1");
                options.DocExpansion(DocExpansion.List);
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
