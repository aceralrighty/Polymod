# TBD Modular Monolith (.NET Learning Project)

This is a personal project aimed at deepening my understanding of the `.NET framework`, `Entity Framework Core`,
and building real-world backend systems. I'm using a **modular monolithic architecture** to structure the application
for better scalability, maintainability, and eventual transition toward microservices.

Each module encapsulates its own domain: Authentication, User, Address, Schedule, Service, Metrics, and now Recommendations—each with its own
DbContext, models, repositories, and services. Logging, seeding, and testing are also organized per module.

---

## 🧱 Project Structure

```plaintext
.
├── API                      # Shared DTOs and data transfer objects
│   └── DTOs
│       ├── AuthDTO
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
├── AddressModule
│   ├── Controllers, Data, Models, Repositories, Services
│   ├── Exceptions
│   └── Seed
├── AuthModule
│   ├── Controllers, Data, Models, Repositories, Services
│   ├── Exceptions
│   ├── Seed
│   └── Views
├── ScheduleModule
│   ├── Controllers, Data, Models, Repositories, Services
│   └── Seed
├── ServiceModule
│   ├── Controllers, Data, Models, Repositories, Services
│   └── Seed
├── RecommendationModule     # User-based service recommendation logic
│   ├── Controllers, Data, Models, Repositories, Services
│   ├── Seed                 # Seeding and ML training integration
│   └── Background Services  # ModelTrainingBackgroundService.cs
├── MetricsModule            # Custom metric tracking (API hits, seeding ops)
│   └── Services             # Singleton-based in-memory metrics tracking
├── Shared                   # Cross-cutting concerns
│   ├── CachingConfiguration
│   ├── Repositories         # Generic repository interfaces
│   └── Utils                # Hashing, JWTs, mappers
├── GenericDBProperties      # Base table inheritance properties
├── DesignTimeFactories      # EF Core migration context factories
├── Migrations               # EF Core migrations by module
├── Logs                     # Module-specific logs
├── TestProject              # NUnit + Moq unit tests
├── Dockerfile
├── Program.cs
├── TBD.csproj
└── TBD.sln
```

---

## 🚀 Notable Additions

- ✅ **Recommendation Module**:
    - Stores user-service recommendation data
    - Includes future-facing ML scaffolding (e.g., `ServiceRatingPrediction.cs`)
    - Real-time click tracking and recommendation logs
    - Background model training service scaffolded

- ✅ **Seeder Integration**:
    - Users and services are seeded with deterministic IDs
    - RecommendationSeeder now pulls real seeded users/services
    - All seeding operations are tracked via `IMetricsService`

- ✅ **Improved Logging**:
    - Each module now outputs seed and runtime metrics to `/Logs`

---

## 🧰 Technologies Used

- .NET 8 (Or latest version available)
- Entity Framework Core
- SQL Server
- AutoMapper
- JWT Authentication
- NUnit + Moq
- Docker & Docker Compose
- GitHub Actions (CI)
- In-memory metrics service
- Future integration: ML.NET for intelligent recommendations

---

## 📦 What's Next

- 🔄 Replace naive recommendation logic with `MLRecommendationEngine`
- 🔲 Swagger UI & endpoint exploration
- 🔲 Introduce Redis for caching heavy read paths
- 🔲 Admin dashboard for managing and analyzing recs

---

MIT License. Build, break, refactor, repeat.
