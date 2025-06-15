# TBD Modular Monolith (.NET Learning Project)

This is a personal project aimed at deepening my understanding of the `.NET framework`, `Entity Framework Core`, and building real-world backend systems. I'm using a **modular monolithic architecture** to structure the application for better scalability, maintainability, and eventual transition toward microservices.

Each module encapsulates its own domain: Authentication, User, Address, Schedule, Service, Metrics, and Recommendations—each with its own DbContext, models, repositories, and services. Logging, seeding, and testing are also organized per module.

---

## 🧱 Project Structure

```plaintext
.
├── API/                                    # Shared DTOs & contracts
│   └── DTOs/
│       ├── AuthDTO/
│       ├── CreateServiceDTO.cs
│       ├── PagedResult.cs
│       └── [Other DTOs...]
│
├── AddressModule/                          # Geographic management
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Repositories/
│   ├── Services/
│   └── Exceptions/
│
├── AuthModule/                             # Authentication
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Repositories/
│   ├── Services/
│   ├── Seed/
│   └── Views/
│
├── ScheduleModule/                         # User scheduling
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Repositories/
│   └── Services/
│
├── ServiceModule/                          # Service catalog
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Repositories/
│   └── Services/
│
├── RecommendationModule/                   # ML recommendations
│   ├── Controllers/
│   ├── Data/Configuration/
│   ├── Models/
│   ├── Repositories/Interfaces/
│   ├── Services/
│   └── Seed/
│
├── MetricsModule/                          # Analytics & monitoring
│   └── Services/
│
├── Shared/                                 # Cross-cutting concerns
│   ├── CachingConfiguration/
│   ├── Repositories/
│   └── Utils/
│
├── GenericDBProperties/                    # Base DB properties
├── DesignTimeFactories/                    # EF Core factories
├── Migrations/                             # DB migrations by module
├── Logs/                                   # Module-specific logs
├── TestProject/                            # Testing suite
│
└── Configuration Files/
    ├── Program.cs
    ├── TBD.csproj
    ├── TBD.sln
    ├── Dockerfile
    └── README.md
```

---

## 🚀 Key Features & Recent Additions

### ✅ **Complete Modular Architecture**
- **7 Core Modules**: Auth, User, Address, Schedule, Service, Recommendation, Metrics
- **Independent DbContexts**: Each module manages its own database context and migrations
- **Separation of Concerns**: Controllers, repositories, services, and models are module-specific
- **Custom Exception Handling**: Module-specific exceptions for better error management
- **Design-Time Factories**: Complete EF Core migration support for all modules

### ✅ **Advanced Recommendation System**
- **ML-Ready Infrastructure**: Complete scaffolding with `MLRecommendationEngine`
- **Analytics Models**: `RecommendationAnalytics`, `ServiceRating`, `ServiceRatingPrediction`
- **Background Training Service**: `ModelTrainingBackgroundService` for automated ML model updates
- **Intelligent Repository Layer**: Specialized recommendation repositories with ML integration
- **Comprehensive Data Models**: `RecommendationOutput`, `UserRecommendation` for complete tracking

### ✅ **Robust Data Management**
- **Generic Repository Pattern**: Shared base repository with caching decorators
- **Comprehensive Seeding**: Deterministic seeding across all 7 modules with real data relationships
- **Complete Migration Support**: All modules have independent migration paths
- **Base Properties**: Shared inheritance (`BaseTableProperties`, `DateableObject`, `IWithId`)
- **Advanced Configuration**: Entity-specific configurations for complex relationships

### ✅ **Enterprise-Level Caching**
- **Repository Caching Decorator**: Transparent caching layer with `CachingRepositoryDecorator`
- **Concurrent Collections**: Thread-safe utilities (`ConcurrentHashSet`)
- **Configurable Cache Options**: Module-specific caching strategies
- **Performance Optimization**: In-memory caching for frequently accessed data

### ✅ **Comprehensive Metrics & Monitoring**
- **Advanced Metrics Service**: `MetricsCollector`, `MetricsService`, `MetricsServiceFactory`
- **Detailed Module Logging**: Individual log files for all 7 modules with daily rotation
- **Seeding Statistics**: Comprehensive logging of seeding operations and performance
- **Real-time Analytics**: API performance tracking and recommendation system metrics
- **Factory Pattern**: Centralized metrics service creation and dependency injection

### ✅ **Extensive Testing Infrastructure**
- **Multi-Module Testing**: Complete test coverage across all major services
- **Repository Testing**: Generic repository and caching decorator validation
- **Service Layer Testing**: Business logic validation for authentication, recommendations, etc.
- **Entity Testing**: Model validation and complex relationship testing
- **Caching Tests**: Performance and reliability testing for caching decorators

---

## 🧰 Technologies Used

- **.NET 9.0** (Latest)
- **C# 13.0** with the latest language features
- **ASP.NET Core** with Razor Pages
- **Entity Framework Core** with SQL Server
- **AutoMapper** with custom extensions and profiles
- **JWT Authentication** with custom token generation
- **NUnit + Moq** for comprehensive unit testing
- **Docker & Docker Compose** for containerization
- **Custom Metrics Service** for real-time monitoring
- **ML.NET** infrastructure for intelligent recommendations
- **Structured Logging** with module-specific log files

---

## 🏗️ Architecture Highlights

### Modular Design
Each module follows a consistent, enterprise-ready structure:
- **Controllers**: API endpoints and request handling
- **Data**: DbContext with entity configurations
- **Models**: Domain entities with complex relationships
- **Repositories**: Data access layer with interfaces and ML integration
- **Services**: Business logic and orchestration
- **Seed**: Data seeding with cross-module relationships
- **Exceptions**: Custom, module-specific exception types

### Cross-Cutting Concerns
- **Advanced Utilities**: JWT generation, secure hashing, comprehensive AutoMapper profiles
- **Generic Repository**: Base repository pattern with CRUD and caching
- **Caching Infrastructure**: Multi-layer caching with concurrent collections
- **Metrics Collection**: Real-time performance and usage analytics across all modules

### Database Architecture
- **7 Independent DbContexts**: Complete separation of concerns
- **Design-Time Factories**: Full EF Core tooling support
- **Complex Relationships**: Cross-module entity relationships with proper configuration
- **Migration Management**: Organized, module-specific migration paths

---

## 📈 Current Module Status

| Module             | Status     | Key Features                                                                 |
|--------------------|------------|------------------------------------------------------------------------------|
| **Auth**           | ✅ Complete | JWT tokens, secure registration, custom exceptions, seeding                  |
| **User**           | ✅ Complete | User profiles, relationships, comprehensive management                       |
| **Address**        | ✅ Complete | Geographic management, city/state grouping, custom exceptions                |
| **Schedule**       | ✅ Complete | User scheduling, availability, statistics tracking                           |
| **Service**        | ✅ Complete | Service catalog, CRUD operations, relationship management                    |
| **Recommendation** | ✅ Advanced | ML infrastructure, analytics, background training, real-time recommendations |
| **Metrics**        | ✅ Complete | Real-time monitoring, factory pattern, comprehensive logging                 |

---

## 📊 Project Metrics

- **7 Complete Modules** with independent concerns and full separation
- **25+ Database Migrations** across all modules with complex relationships
- **Enterprise-Level Testing** with comprehensive coverage
- **Real-time Metrics Tracking** across all operations and modules
- **Advanced ML Infrastructure** ready for production recommendation systems
- **Modular Seeding System** with deterministic cross-module data relationships
- **Complete Logging Infrastructure** with module-specific analytics

---

## 📦 What's Next

### Short Term
- 🔄 **ML Model Training**: Implement actual ML.NET models with historical data
- 🔲 **API Documentation**: Complete Swagger UI integration with all endpoints
- 🔲 **Redis Integration**: External caching layer for production performance
- 🔲 **Integration Testing**: End-to-end testing across all modules

### Medium Term
- 🔲 **Admin Dashboard**: Management interface for recommendations and analytics
- 🔲 **Real-time Features**: SignalR integration for live recommendation updates
- 🔲 **Performance Optimization**: Database indexing and query optimization
- 🔲 **Security Enhancements**: Advanced authentication and rate limiting

### Long Term
- 🔲 **Microservices Migration**: Gradual transition from modular monolith
- 🔲 **Event Sourcing**: Event-driven architecture with cross-module communication
- 🔲 **Cloud Deployment**: Azure/AWS with CI/CD and container orchestration
- 🔲 **Advanced Analytics**: Business intelligence and recommendation performance analysis

---

## 🚀 Getting Started

1. **Clone the repository**
2. **Update connection strings** in `appsettings.json` for all 7 modules
3. **Run migrations** for each module: Auth, User, Address, Schedule, Service, Recommendation
4. **Build and run** the application with .NET 9.0
5. **Explore endpoints** via the comprehensive `.http` file
6. **Monitor metrics** through the real-time logging system

---

*MIT License. Enterprise-ready modular architecture for learning and production.* 🔧
