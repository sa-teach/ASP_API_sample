# Учебное руководство: ASP.NET Core Web API с Entity Framework Core

Пошаговый гайд для студентов по созданию REST API с базой данных, паттерном Repository и Swagger.

---

## Содержание

1. [Подготовка проекта](#1-подготовка-проекта)
2. [Моделирование данных](#2-моделирование-данных)
3. [Шаблон Repository](#3-шаблон-repository)
4. [API-контроллеры](#4-api-контроллеры)
5. [Работа с данными (миграции)](#5-работа-с-данными-миграции-и-начальные-данные)
6. [Тестирование API (Swagger)](#6-тестирование-api-swagger)

> **Подключение к разным СУБД.** Сравнение SQLite, SQL Server, PostgreSQL, MySQL — см. [README.md](README.md#подключение-entity-framework-core-к-разным-судб).

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

## Почему SQLite 

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

### 3.1 Репозиторий

Это слой между бизнес-логикой (контроллерами) и источником данных (базой данных). Он ведёт себя как «коллекция объектов в памяти»: вы добавляете, обновляете и удаляете сущности, а репозиторий отвечает за их сохранение в БД.

**Почему не вызывать DbContext напрямую из контроллера?**

1. **Разделение ответственности** — контроллер не должен знать, как устроена работа с БД (SQL, таблицы и т.д.).
2. **Тестирование** — репозиторий можно заменить «заглушкой» (mock) и тестировать контроллер без реальной БД.
3. **Единый стиль доступа** — все операции с данными идут через один интерфейс, а не через разные места в коде.

**Аналогия:** Репозиторий — это «посредник» между вашим приложением и складом (БД). Вы говорите «дай товар с id=5», а не «открой таблицу Products и выполни SELECT».

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

- `where T : class` — обобщённый тип, работает с любым классом (Category, Product и т.д.).
- `FindAsync` — поиск по условию (например, «все товары дороже 1000»).
- `AddAsync`, `Update`, `Delete` — изменения попадают в БД только после вызова `SaveChangesAsync` в Unit of Work.

### 3.3 Реализация Repository

Класс `Repository<T>` использует `DbContext` и `DbSet<T>` для выполнения операций. Реализация общая для всех сущностей: один и тот же код работает и для `Category`, и для `Product`. Это **generic-репозиторий**.

### 3.4 Unit of Work — что это и зачем

**Unit of Work** — паттерн, который объединяет несколько репозиториев и **одним вызовом** сохраняет все внесённые изменения. Он управляет транзакцией: либо сохраняется всё, либо откатывается всё.

**Почему одного репозитория мало?** Если нужно добавить категорию и несколько товаров, каждый репозиторий использует свой DbContext. Без Unit of Work при сбое могли бы сохраниться только часть данных. Unit of Work даёт один общий `DbContext` всем репозиториям и один `SaveChangesAsync`.

```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<Category> Categories { get; }
    IRepository<Product> Products { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

**Пример:** вы добавляете категорию и 3 товара. Без Unit of Work — 4 вызова сохранения. С Unit of Work — один вызов `SaveChangesAsync()`, и все изменения применяются атомарно. При ошибке БД — откат всей операции.

---

## 

- **Repository** — «витрина» данных: вы работаете с объектами, не думая о SQL.
- **Unit of Work** — «пакет операций»: все изменения копятся и сохраняются одним вызовом, как одна транзакция.

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

Это стиль проектирования API. Ресурсы (products, categories) представлены URL. HTTP-методы (GET, POST, PUT, DELETE) обозначают действие. Код ответа (200, 201, 404) описывает результат.

---

## 5. Работа с данными: миграции и начальные данные

### 5.1 Что такое миграции

**Миграция** — это способ описать изменения схемы БД (таблицы, столбцы, связи) в коде. Entity Framework Core сравнивает текущую модель (классы `Product`, `Category`) с тем, что уже в базе, и генерирует SQL-скрипт для приведения БД в соответствие с моделью.

**Зачем нужны миграции:**
- **Версионирование схемы** — каждое изменение БД хранится в отдельном файле (миграции лежат в папке `Migrations`).
- **Повторяемость** — на любом компьютере можно «накатить» миграции и получить такую же схему.
- **Обновление без потери данных** — при добавлении нового столбца миграция добавит его, не трогая существующие данные.

**Как это работает:** вы меняете модель (например, добавляете свойство в `Product`), создаёте миграцию командой, EF генерирует C#-код с инструкциями (Up/Down). Команда `Update-Database` применяет эти инструкции к БД.

### 5.2 EnsureCreated vs Migrations

**Вариант A: EnsureCreated** (упрощённый, для учебного проекта)

```csharp
await db.Database.EnsureCreatedAsync();
await DataSeeder.SeedAsync(db);
```

- Создаёт БД по текущей модели, если её ещё нет.
- **Не** создаёт папку `Migrations` и не ведёт историю изменений.
- **Не** поддерживает обновление схемы: при изменении модели придётся удалять файл БД и создавать заново.
- Подходит для прототипов и быстрого старта.

**Вариант B: Migrations** (рекомендуется для реальных проектов)

Миграции позволяют пошагово менять схему БД без удаления данных.

### 5.3 Где и как выполнять команды миграций

Команды Entity Framework Core можно выполнять двумя способами.

#### Способ 1: .NET CLI (Terminal / Командная строка)

Откройте терминал в папке решения (там, где лежит `.slnx` или `.csproj`):

```bash
# Создать миграцию с именем InitialCreate
dotnet ef migrations add InitialCreate

# Применить миграции к базе данных
dotnet ef database update
```

**Куда именно прописывать:**
- В Visual Studio: **View → Terminal** или **Ctrl+`** — откроется встроенный терминал.
- Вне Visual Studio: PowerShell или CMD в папке проекта (там же, где `ASP_API_sample.csproj`).

**Почему именно эти команды:**
- `dotnet ef` — утилита EF Core, добавляемая пакетом `Microsoft.EntityFrameworkCore.Design`.
- `migrations add <ИмяМиграции>` — сформировать новую миграцию на основе изменений в моделях.
- `database update` — выполнить все неприменённые миграции против БД.

**Если проект и стартовый проект разные** (например, API в одном проекте, DbContext в другом):

```bash
dotnet ef migrations add InitialCreate --project ASP_API_sample
dotnet ef database update --project ASP_API_sample
```

#### Способ 2: Package Manager Console (Visual Studio)

1. Откройте **View → Other Windows → Package Manager Console**.
2. В выпадающем списке выберите **Default project** — проект с `DbContext` (обычно это основной проект с API).
3. Выполните команды:

```powershell
Add-Migration InitialCreate
Update-Database
```

**Важно:** Package Manager Console — это PowerShell внутри Visual Studio. Команды `Add-Migration` и `Update-Database` — это обёртки над EF Core. Если `dotnet ef` не установлен глобально, Package Manager Console может использовать EF из пакетов проекта.

**Установка глобального инструмента `dotnet ef` (если команды не находятся):**

```bash
dotnet tool install --global dotnet-ef
```

### 5.4 Структура папки Migrations

После `migrations add InitialCreate` появится папка `Migrations`:

```
Migrations/
├── 20250209120000_InitialCreate.cs       — код миграции (Up/Down)
├── 20250209120000_InitialCreate.Designer.cs
└── AppDbContextModelSnapshot.cs          — «снимок» текущей модели
```

- `Up()` — применяет миграцию (создаёт таблицы, столбцы и т.д.).
- `Down()` — откатывает миграцию.
- `AppDbContextModelSnapshot.cs` — EF сравнивает его с моделями и понимает, что изменилось.

### 5.5 Пример: добавили свойство — как создать миграцию

1. Добавьте свойство в `Product.cs`, например `public DateTime CreatedAt { get; set; }`.
2. Выполните: `dotnet ef migrations add AddCreatedAtToProduct`.
3. Проверьте сгенерированный файл в `Migrations` — там будет `AddColumn` для нового столбца.
4. Примените: `dotnet ef database update`.

### 5.6 Seed-данные

Класс `DataSeeder` проверяет, пуста ли таблица `Categories`. Если пуста — добавляет категории и товары. При следующих запусках данные не дублируются. Можно перенести seed в миграцию через `modelBuilder.Entity<T>().HasData(...)` — тогда данные будут добавляться при применении миграций.

---

## 

- **Миграции** — это «рецепты» изменений БД в коде. Они версионируются вместе с приложением и позволяют обновлять схему без ручного написания SQL.
- **Команды** вводятся в терминал (dotnet ef) или в Package Manager Console (Add-Migration, Update-Database). В обоих случаях важно находиться в нужном проекте.

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

## Как объяснить это в гайде

**Swagger** — инструмент документирования и тестирования API. Схема генерируется автоматически из контроллеров. Студенты могут вызывать эндпоинты прямо из браузера без Postman или curl.

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
