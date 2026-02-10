# ASP.NET Core Web API — учебный проект

REST API с Entity Framework Core, паттерном Repository и Swagger. См. **[GUIDE.md](GUIDE.md)** для пошагового руководства.

---

## Подключение Entity Framework Core к разным СУБД

Entity Framework Core поддерживает разные **поставщики** (providers). Один и тот же код (модели, DbContext, запросы) работает с разными базами данных — меняются только пакет и строка подключения.

### Сравнение поставщиков

| Поставщик | Пакет NuGet | Строка подключения | Когда использовать |
|-----------|-------------|--------------------|--------------------|
| **SQLite** | `Microsoft.EntityFrameworkCore.Sqlite` | `Data Source=app.db` | Разработка, обучение, встроенные приложения |
| **SQL Server** | `Microsoft.EntityFrameworkCore.SqlServer` | см. ниже | Windows-сервер, Azure, корпоративная среда |
| **PostgreSQL** | `Npgsql.EntityFrameworkCore.PostgreSQL` | см. ниже | Кроссплатформенность, Linux-серверы |
| **MySQL** | `Pomelo.EntityFrameworkCore.MySql` | см. ниже | LAMP/LNMP стек |
| **In-Memory** | `Microsoft.EntityFrameworkCore.InMemory` | не требуется | Тестирование без реальной БД |

### 1. SQLite

**Пакет:**
```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

**appsettings.json:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=app.db"
}
```

**Program.cs:**
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**Особенности:**
- Один файл (`app.db`) — вся база. Не нужен отдельный сервер.
- Удобно для разработки и обучения: скопировал папку — скопировал и БД.
- Ограничения: нет полноценных `FULL OUTER JOIN`, часть типов и фич SQL отличается от SQL Server.

---

### 2. SQL Server

**Пакет:**
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

**appsettings.json:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProductsDb;Trusted_Connection=True;"
}
```

Или для именованного экземпляра:
```
Server=localhost;Database=ProductsDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

**Program.cs:**
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**Особенности:**
- LocalDB — лёгкий SQL Server для разработки на Windows (часто уже установлен с Visual Studio).
- В продакшене — полноценный SQL Server или Azure SQL.
- Хорошая поддержка всех возможностей EF Core.

---

### 3. PostgreSQL

**Пакет:**
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

**appsettings.json:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=productsdb;Username=postgres;Password=postgres"
}
```

**Program.cs:**
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**Особенности:**
- Кроссплатформенный, часто используется на Linux.
- Синтаксис JSON, массивов и т.д. отличается от SQL Server, но для базовых операций код тот же.

---

### 4. MySQL (через Pomelo)

**Пакет:**
```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql
```

**appsettings.json:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=productsdb;User=root;Password=password;"
}
```

**Program.cs:**
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var serverVersion = ServerVersion.AutoDetect(connectionString);
    options.UseMySql(connectionString, serverVersion);
});
```

---

### 5. In-Memory (для тестов)

**Пакет:**
```bash
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

**Код (обычно в тестах):**
```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDb")
    .Options;
var context = new AppDbContext(options);
```

**Особенности:**
- БД существует только в памяти. После закрытия контекста данные исчезают.
- Удобно для unit-тестов без настройки реальной БД.
- Не заменяет интеграционные тесты с настоящей СУБД.

---

## Как переключиться на другую БД

1. Добавьте нужный пакет вместо (или вместе с) SQLite.
2. Поменяйте строку подключения в `appsettings.json`.
3. В `Program.cs` замените `UseSqlite` на `UseSqlServer`, `UseNpgsql`, `UseMySql` и т.д.
4. Модели, DbContext, репозитории и контроллеры менять не нужно — EF Core адаптирует запросы под выбранный поставщик.

---

## Запуск проекта

```bash
dotnet restore
dotnet run
```

Swagger: `https://localhost:7286/swagger` (порт может отличаться).
