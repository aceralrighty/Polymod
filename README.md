# TBD Modular Monolith (.NET Learning Project)

This is a personal project aimed at deepening my understanding of the `.NET framework`, `Entity Framework Core`,
and building real-world backend systems. I'm using a **modular monolithic architecture** to structure the application
for better scalability, maintainability, and eventual transition toward microservices.

Each module encapsulates its own domain: Authentication, User, Address, Schedule, Service, and Metrics—each with its own
DbContext, models, repositories, and services. Logging, seeding, and testing are also organized per module.

---

## 🧱 Project Structure

```plaintext
.
├── API                      # Shared DTOs and request/response models
│   └── DTOs
│       ├── AuthDTO         # Login/Register DTOs
│       │   ├── AuthResponse.cs
│       │   ├── LoginRequest.cs
│       │   └── RegisterRequest.cs
│       ├── CreateServiceDTO.cs
│       ├── PagedResult.cs
│       ├── ServiceDTO.cs
│       ├── UserAddressRequest.cs
│       ├── UserAddressResponse.cs
│       ├── UserDTO.cs
│       └── UserSchedule.cs
├── AuthModule               # Authentication & JWT token logic
│   ├── Controllers, Data, Models, Repositories, Services
│   ├── Exceptions
│   ├── Seed
│   └── Views
├── AddressModule            # Address storage and region validation
│   ├── Controllers, Data, Models, Repositories, Services
│   ├── Exceptions
│   └── Seed
├── ScheduleModule           # Schedule tracking and availability
│   ├── Controllers, Data, Models, Repositories, Services
│   └── Seed
├── ServiceModule            # Service listings and domain logic
│   ├── Controllers, Data, Models, Repositories, Services
│   └── Seed
├── MetricsModule            # Custom metric tracking (API hits, seeding ops)
│   ├── MetricsModule.cs
│   └── Services             # Singleton services for metric collection
│       ├── IMetricsService.cs
│       ├── IMetricsServiceFactory.cs
│       ├── MetricsCollector.cs
│       ├── MetricsService.cs
│       └── MetricsServiceFactory.cs
├── Shared                   # Cross-cutting concerns
│   ├── Repositories         # Generic repository patterns
│   ├── CachingConfiguration
│   │   ├── CacheOptions.cs
│   │   ├── CachingRepositoryDecorator.cs
│   │   └── ConcurrentHashSet.cs
│   └── Utils                # Mapping, hashing, JWT generation
│       ├── AuthUserMapping.cs
│       ├── Hasher.cs
│       ├── IHasher.cs
│       ├── JwtTokenGenerator.cs
│       ├── ServiceMapping.cs
│       ├── UserAddressMapping.cs
│       ├── UserMapping.cs
│       └── UserScheduleMapping.cs
├── GenericDBProperties      # Base entity contracts (Id, timestamps, etc.)
├── DesignTimeFactories      # EF Core factories for migrations
├── Migrations               # EF Core migrations organized by module
│   ├── AuthDb, ScheduleDb, ServiceDb, UserDb, AddressDb
├── Logs                     # Per-module metric logs (auto-generated)
│   ├── authmodule-metrics*.log
│   ├── usermodule-metrics*.log
│   ├── schedulemodule-metrics*.log
│   └── servicemodule-metrics*.log
├── TestProject              # NUnit + Moq-based tests
│   ├── AuthServiceTests.cs
│   ├── CachingRepositoryDecoratorTests.cs
│   ├── ScheduleTest.cs
│   ├── UserAddressServiceTest.cs
│   ├── UserTest.cs
│   └── TestEntity.cs
├── TBD.http                 # HTTP test requests
├── TBD.csproj
├── TBD.sln
├── Dockerfile
├── docker-compose.yml
├── Program.cs
└── Properties
    └── launchSettings.json
```

---

## 🏗️ Architecture Highlights

- **Modular Design**: Self-contained modules with clearly defined service boundaries
- **Database Per Module**: Each module uses its own `DbContext` and owns its schema
- **Custom Metrics Tracking**: Singleton-based in-memory counters written to per-module logs
- **Generic Repository Pattern**: Shared data access logic via reusable base repositories
- **Caching Layer**: In-memory caching for service performance boosts
- **Exception Handling**: Domain-specific exceptions handled cleanly across modules
- **Automated Seeding**: Seeders for test/development with observable metrics
- **Comprehensive Testing**: NUnit test coverage on core business logic with Moq

---

## 🔁 CI/CD Pipeline

This project uses **GitHub Actions** to build, test, and validate Docker builds on every push to `main`.

### Workflow Steps:
- Restore and build solution
- Run NUnit tests
- Build Docker image to validate containerization

📝 **CI Config:** `.github/workflows/ci.yml`  
📌 *Deployment coming soon* — planned integration with Azure/AWS container services.

---

## 📦 Goals

- Deepen mastery of `.NET 8` and `Entity Framework Core`
- Practice building scalable systems with **modular monolith** patterns
- Explore metrics and observability within the backend
- Build out clean seeding, migrations, and modular data management
- Strengthen testing discipline using real-life scenarios
- Prepare the foundation for microservice splitting:
    - Inter-module isolation
    - REST or gRPC communication interfaces
    - Database-per-service readiness

---

## 🐳 Docker Setup

The app runs alongside a **SQL Server** container using `docker-compose`.

```bash
docker-compose up --build
```

This will:
- Start SQL Server with port 1433
- Build and launch the backend API
- Apply all pending migrations
- Log metric outputs to `/Logs` by module

⚠️ **Ensure Docker is running and port `1433` is available on your system.**

---

## 🧰 Technologies Used

- .NET 8
- Entity Framework Core
- SQL Server (via Docker)
- AutoMapper
- JWT Authentication
- NUnit + Moq
- Docker & Docker Compose
- GitHub Actions (CI)

---

## 🔮 What's Next?

- ✅ Finalize logging and metrics per module
- 🔲 Add Swagger/OpenAPI for API documentation
- 🔲 Introduce API versioning and rate limiting
- 🔲 Add integration tests for all major API flows
- 🔲 Begin splitting key modules into standalone services
- 🔲 Add Redis or MemoryCache for read performance
- 🔲 Refactor shared utility layer into namespaces (Crypto, Mapping, Extensions)
- 🔲 Explore Dapper or alternative ORMs for focused modules

---

## 🙌 Contributions

This is an educational and personal learning project. That said, feel free to fork it, open issues, or contribute ideas
if it piques your interest.

---

## 📄 License

MIT — use it, learn from it, or build upon it freely.
