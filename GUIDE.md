# Учебное руководство: ASP.NET Core Web API с Entity Framework Core

Пошаговый гайд для студентов по созданию REST API с базой данных, паттерном Repository и Swagger.

---

## Содержание

1. [Подготовка проекта](#1-подготовка-проекта)
2. [Моделирование данных](#2-моделирование-данных)
3. [Шаблон Repository](#3-шаблон-repository)
4. [API-контроллеры](#4-api-контроллеры)
5. [Работа с данными](#5-работа-с-данными)
6. [Тестирование API (Swagger)](#6-тестирование-api-swagger)

---

## 1. Подготовка проекта

### 1.1 Создание проекта

```bash
dotnet new webapi -n ASP_API_sample -o ASP_API_sample
cd ASP_API_sample
```

### 1.2 Добавление NuGet-пакетов

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.0
dotnet add package Swashbuckle.AspNetCore --version 6.6.2
```

**Зачем нужны пакеты:**

| Пакет | Назначение |
|-------|------------|
| `Microsoft.EntityFrameworkCore.Sqlite` | Работа с SQLite — простой файловой БД без установки сервера |
| `Microsoft.EntityFrameworkCore.Design` | Инструменты для создания миграций (`dotnet ef`) |
| `Swashbuckle.AspNetCore` | Swagger UI — интерактивная документация API |

### 1.3 Настройка строки подключения (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  },
  ...
}
```

`Data Source=app.db` — путь к файлу SQLite. Файл создаётся автоматически в папке проекта при первом запуске.

---

## Почему SQLite?

Не требует установки отдельного сервера БД. Файл `app.db` — это вся база. Удобно для разработки и обучения. В продакшене обычно используют SQL Server или PostgreSQL.

---

## 2. Моделирование данных

### 2.1 Сущности (Models)

**Category.cs** — категория товаров:

```csharp
public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
```

**Product.cs** — товар:

```csharp
public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
```

**Связь:** Один Product относится к одной Category. Одна Category содержит много Product (связь «многие к одному»).

### 2.2 DbContext и DbSet

**AppDbContext.cs** — контекст базы данных:

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Конфигурация связей и ограничений
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);
    }
}
```

**DbSet** — представление таблицы в коде. `Categories` — таблица категорий, `Products` — товаров.

---

## DbContext

Это «сессия» работы с БД. Он знает о всех сущностях и их связях. `OnModelCreating` нужен для настройки ограничений (длина строк, точность decimal) и связей между таблицами.

---

## 3. Шаблон Repository

### 3.1 Зачем нужен Repository

- Отделяет логику доступа к данным от контроллеров
- Упрощает тестирование (можно подменить репозиторий)
- Централизует CRUD-операции

### 3.2 Интерфейс IRepository

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
}
```

### 3.3 Реализация Repository

Класс `Repository<T>` использует `DbContext` и `DbSet<T>` для выполнения операций. Реализация общая для всех сущностей.

### 3.4 Unit of Work

**IUnitOfWork** — объединяет репозитории и сохраняет изменения одной транзакцией:

```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<Category> Categories { get; }
    IRepository<Product> Products { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

Один вызов `SaveChangesAsync()` сохраняет все изменения (добавления, обновления, удаления) в рамках одной транзакции.

---

## Unit of Work

Это «пакет операций». Если нужно добавить категорию и несколько товаров, все они сохраняются одним вызовом. При ошибке откатывается вся транзакция.

---

## 4. API-контроллеры

### 4.1 DTO (Data Transfer Objects)

Отдельные модели для запросов и ответов API:

- **CreateProductDto** — данные для создания
- **UpdateProductDto** — данные для обновления
- **ProductDto** — данные в ответе

Так мы не «просачиваем» внутреннюю модель наружу и контролируем формат API.

### 4.2 CRUD-эндпоинты

| Метод | URL | Действие |
|-------|-----|----------|
| GET | /api/products | Все товары |
| GET | /api/products/{id} | Товар по ID |
| POST | /api/products | Создать товар |
| PUT | /api/products/{id} | Обновить товар |
| DELETE | /api/products/{id} | Удалить товар |

Аналогично для `/api/categories`.

### 4.3 Пример контроллера

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(CancellationToken ct)
    {
        var products = await _unitOfWork.Products.GetAllAsync(ct);
        return Ok(products.Select(p => MapToDto(p)));
    }
    // ... остальные методы
}
```

---

## REST

Стиль проектирования API. Ресурсы (products, categories) представлены URL. HTTP-методы (GET, POST, PUT, DELETE) обозначают действие. Код ответа (200, 201, 404) описывает результат.

---

## 5. Работа с данными

### 5.1 EnsureCreated vs Migrations

**Вариант A: EnsureCreated** (упрощённый, для учебного проекта)

```csharp
await db.Database.EnsureCreatedAsync();
await DataSeeder.SeedAsync(db);
```

- Создаёт БД по текущей модели
- Не поддерживает обновление схемы
- Подходит для быстрого старта

**Вариант B: Migrations** (для «продакшена»)

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

- Сохраняет историю изменений схемы
- Позволяет обновлять БД при изменении моделей

### 5.2 Seed-данные

Класс `DataSeeder` проверяет, пуста ли таблица `Categories`. Если пуста — добавляет категории и товары. При следующих запусках данные не дублируются.

---

## Seed

Это «начальные данные». Нужны, чтобы сразу после запуска можно было протестировать API, не добавляя данные вручную.

---

## 6. Тестирование API (Swagger)

### 6.1 Настройка Swagger в Program.cs

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Products API",
        Version = "v1",
        Description = "Учебный API для управления товарами и категориями"
    });
});

// В pipeline:
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Products API v1");
});
```

### 6.2 Запуск и проверка

1. Запустить приложение: `dotnet run`
2. Открыть в браузере: `https://localhost:5xxx/swagger` (порт см. в выводе консоли)
3. В Swagger UI можно выполнять все запросы: GET, POST, PUT, DELETE

### 6.3 Примеры тестовых запросов

**GET /api/categories** — список категорий (должен вернуть 3 шт. из seed).

**POST /api/products** — создать товар:
```json
{
  "name": "Мышь беспроводная",
  "description": "Эргономичная",
  "price": 1990,
  "stockQuantity": 50,
  "categoryId": 1
}
```

---

## Swagger

Это инструмент документирования и тестирования API. Схема генерируется автоматически из контроллеров. Студенты могут вызывать эндпоинты прямо из браузера без Postman или curl.

---

## Запуск проекта

```bash
dotnet restore
dotnet run
```

Откройте `https://localhost:<порт>/swagger` в браузере.

---

## Частые ошибки и решения

| Ошибка | Решение |
|--------|---------|
| `NU1301` при restore | Проверьте интернет и прокси. Выполните `dotnet restore` вручную. |
| `404` на /swagger | Убедитесь, что приложение запущено в среде Development. |
| `CategoryId` не найден | Сначала создайте категорию (POST /api/categories), затем товар с этим `categoryId`. |
| Файл app.db не создаётся | Проверьте права на запись в папку проекта. |

---

## Структура проекта

```
ASP_API_sample/
├── Controllers/
│   ├── CategoriesController.cs
│   └── ProductsController.cs
├── Data/
│   ├── AppDbContext.cs
│   └── DataSeeder.cs
├── DTOs/
│   ├── CategoryDto.cs
│   └── ProductDto.cs
├── Models/
│   ├── Category.cs
│   └── Product.cs
├── Repositories/
│   ├── IRepository.cs
│   ├── IUnitOfWork.cs
│   ├── Repository.cs
│   └── UnitOfWork.cs
├── appsettings.json
├── Program.cs
└── ASP_API_sample.csproj
```
