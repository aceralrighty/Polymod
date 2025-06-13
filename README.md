# TBD Modular Monolith (.NET Learning Project)

This is a personal project aimed at deepening my understanding of the `.NET framework`, `Entity Framework Core`, and building real-world backend systems. I'm using a **modular monolithic architecture** to structure the application for better scalability, maintainability, and eventual transition toward microservices.

Each module encapsulates its own domain: Authentication, User, Address, Schedule, Service, Metrics, and Recommendations—each with its own DbContext, models, repositories, and services. Logging, seeding, and testing are also organized per module.

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
├── UserModule                # User management and profiles
│   ├── Controllers, Data, Models, Repositories, Services
│   └── Seed
├── RecommendationModule     # ML-powered service recommendations
│   ├── Controllers, Data, Models, Repositories, Services
│   ├── Seed                 # Seeding and ML training integration
│   └── Background Services  # ModelTrainingBackgroundService.cs
├── MetricsModule            # Custom metric tracking (API hits, seeding ops)
│   └── Services             # Singleton-based in-memory metrics tracking
├── Shared                   # Cross-cutting concerns
│   ├── CachingConfiguration # Caching decorators and utilities
│   ├── Repositories         # Generic repository interfaces
│   └── Utils                # Hashing, JWTs, mappers
├── GenericDBProperties      # Base table inheritance properties
├── DesignTimeFactories      # EF Core migration context factories
├── Migrations               # EF Core migrations by module
│   ├── AuthDb
│   ├── UserDb
│   ├── AddressDb
│   ├── ScheduleDb
│   ├── ServiceDb
│   └── RecommendationDb
├── Logs                     # Module-specific logs and metrics
├── TestProject              # NUnit + Moq unit tests
├── Dockerfile
├── Program.cs
├── TBD.csproj
└── TBD.sln
```

---

## 🚀 Key Features & Recent Additions

### ✅ **Modular Architecture**
- **6 Core Modules**: Auth, User, Address, Schedule, Service, Recommendation
- **Independent DbContexts**: Each module manages its own database context and migrations
- **Separation of Concerns**: Controllers, repositories, services, and models are module-specific
- **Custom Exception Handling**: Module-specific exceptions for better error management

### ✅ **Recommendation System**
- **ML-Ready Infrastructure**: Scaffolded for future ML.NET integration
- **Real-time Analytics**: User interaction tracking and recommendation performance metrics
- **Background Training Service**: Automated model training and updates
- **Intelligent Engine**: `MLRecommendationEngine` for sophisticated recommendation logic
- **Multiple Model Support**: `ServiceRating`, `ServiceRatingPrediction`, and analytics models

### ✅ **Advanced Data Management**
- **Generic Repository Pattern**: Shared base repository with caching decorators
- **Comprehensive Seeding**: Deterministic seeding across all modules with real data relationships
- **Migration Management**: Organized migrations by module with design-time factories
- **Base Properties**: Shared table properties (`BaseTableProperties`, `DateableObject`, `IWithId`)

### ✅ **Caching & Performance**
- **Repository Caching Decorator**: Transparent caching layer for repositories
- **Concurrent Collections**: Thread-safe caching utilities (`ConcurrentHashSet`)
- **Configurable Cache Options**: Flexible caching configuration per module

### ✅ **Metrics & Monitoring**
- **Custom Metrics Service**: In-memory metrics tracking across all modules
- **Detailed Logging**: Module-specific log files with seeding statistics and performance data
- **Metrics Factory Pattern**: Centralized metrics service creation and management
- **Real-time Monitoring**: API hit tracking, seeding operations, and recommendation analytics

### ✅ **Testing Infrastructure**
- **Comprehensive Unit Tests**: NUnit + Moq testing across multiple modules
- **Repository Testing**: Generic repository and caching decorator tests
- **Service Layer Testing**: Business logic validation for all major services
- **Entity Testing**: Model validation and behavior testing

---

## 🧰 Technologies Used

- **.NET 8** (Latest LTS)
- **Entity Framework Core** with SQL Server
- **AutoMapper** for object mapping
- **JWT Authentication** with custom token generation
- **NUnit + Moq** for unit testing
- **Docker & Docker Compose** for containerization
- **GitHub Actions** for CI/CD
- **In-memory metrics service** for real-time monitoring
- **ML.NET** (future integration) for intelligent recommendations
- **Serilog** for structured logging

---

## 🏗️ Architecture Highlights

### Modular Design
Each module follows a consistent structure:
- **Controllers**: API endpoints and request handling
- **Data**: DbContext and database configuration
- **Models**: Domain entities and data models
- **Repositories**: Data access layer with interfaces
- **Services**: Business logic and orchestration
- **Seed**: Data seeding and initialization
- **Exceptions**: Custom exception types

### Cross-Cutting Concerns
- **Shared Utilities**: JWT generation, password hashing, AutoMapper profiles
- **Generic Repository**: Base repository pattern with CRUD operations
- **Caching Layer**: Transparent caching decorator pattern
- **Metrics Collection**: Centralized performance and usage tracking

---

## 📈 Current Module Status

| Module | Status | Features |
|--------|--------|----------|
| **Auth** | ✅ Complete | Registration, login, JWT tokens, seeding |
| **User** | ✅ Complete | User profiles, management, relationships |
| **Address** | ✅ Complete | Address management, city/state grouping |
| **Schedule** | ✅ Complete | User scheduling, availability management |
| **Service** | ✅ Complete | Service catalog, CRUD operations |
| **Recommendation** | 🚧 In Progress | Basic recommendations, ML scaffolding |
| **Metrics** | ✅ Complete | Real-time metrics, logging, monitoring |

---

## 📦 What's Next

### Short Term
- 🔄 **ML Integration**: Replace naive recommendation logic with trained ML.NET models
- 🔲 **API Documentation**: Swagger UI integration and comprehensive endpoint documentation
- 🔲 **Redis Caching**: External caching layer for improved performance
- 🔲 **Advanced Testing**: Integration tests and end-to-end testing suite

### Medium Term
- 🔲 **Admin Dashboard**: Web interface for managing recommendations and analytics
- 🔲 **Real-time Features**: SignalR integration for live updates
- 🔲 **Performance Optimization**: Query optimization and database indexing
- 🔲 **Security Enhancements**: Rate limiting, advanced authentication

### Long Term
- 🔲 **Microservices Migration**: Gradual transition from modular monolith
- 🔲 **Event Sourcing**: Event-driven architecture implementation
- 🔲 **Cloud Deployment**: Azure/AWS deployment with CI/CD pipelines
- 🔲 **Mobile API**: Dedicated mobile API endpoints and optimization

---

## 🚀 Getting Started

1. **Clone the repository**
2. **Update connection strings** in `appsettings.json`
3. **Run migrations** for each module
4. **Build and run** the application
5. **Explore endpoints** via the included `.http` file

---

## 📊 Project Metrics

- **6 Modules** with independent concerns
- **20+ Database migrations** across all modules
- **Comprehensive test coverage** with NUnit
- **Real-time metrics tracking** across all operations
- **Modular seeding** with deterministic data relationships

---

*MIT License. Build, break, refactor, repeat.* 🔧
