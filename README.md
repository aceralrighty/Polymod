**TBD Modular Monolith (.NET Learning Project)**

This is a personal project aimed at deepening my understanding of the ```.NET framework```, ```Entity Framework Core```,
and building real-world backend systems. I'm using a **modular monolithic architecture** to structure the application
for better scalability and maintainability.

The project includes separate modules for Authentication, User, Address, Schedule, and Service management—each with its own domain
logic, DbContext, models, repositories, and services. Each module follows a clean architecture pattern with clear separation of concerns.

**🧱 Project Structure**

```plaintext
.
├── API                      # Shared DTOs and data transfer objects
│   └── DTOs                 # Request/Response models and shared DTOs
│       └── AuthDTO          # Authentication-specific DTOs
├── AuthModule               # Authentication and authorization logic
│   ├── Controllers, Data, Models, Repositories, Services
│   ├── Exceptions           # Auth-specific exception handling
│   └── Seed                 # Authentication data seeding
├── AddressModule            # Address management logic
│   ├── Controllers, Data, Models, Repositories, Services
│   ├── Exceptions           # Address-specific exception handling
│   └── Seed                 # Address data seeding
├── ScheduleModule           # Schedule management logic
│   ├── Controllers, Data, Models, Repositories, Services
│   └── Seed                 # Schedule data seeding
├── ServiceModule            # Service management logic
│   ├── Controllers, Data, Models, Repositories, Services
│   └── Seed                 # Service data seeding
├── UserModule               # User management logic (if present)
├── Shared                   # Cross-cutting concerns and utilities
│   ├── Repositories         # Generic repository patterns
│   └── Utils                # Mapping profiles, authentication utilities, and helpers
├── GenericDBProperties      # Base model interfaces and shared database properties
├── DesignTimeFactories      # EF Core design-time context factories for migrations
├── Migrations               # EF Core migrations organized by module/database
│   ├── AuthDb, ScheduleDb, ServiceDb, UserDb
├── TestProject              # xUnit tests for services and repositories
├── TBD.http                 # HTTP requests for manual API testing
├── Dockerfile               # Docker build setup for API
├── docker-compose.yml       # SQL Server and API orchestration
├── .github/workflows/ci.yml # GitHub Actions CI pipeline
└── Program.cs, appsettings.json, etc.
```

**🏗️ Architecture Highlights**

- **Modular Design**: Each business domain (Auth, Address, Schedule, Service) is encapsulated in its own module
- **Database Per Module**: Each module maintains its own DbContext and database schema
- **Generic Repository Pattern**: Shared repository abstractions for common CRUD operations
- **Exception Handling**: Module-specific exceptions for better error management
- **Comprehensive Testing**: Unit tests covering services and repositories across all modules
- **Data Seeding**: Automated data seeding capabilities for each module

**🔁 CI/CD Pipeline**

This project uses GitHub Actions to automatically build and test on every push or pull request to the main branch.

Workflow includes:
- Restoring and building the solution
- Running all unit tests with NUnit
- Validating Docker image build

**📄 CI configuration file: .github/workflows/ci.yml**

You can monitor the workflow under the Actions tab on GitHub.

🛠️ **Future goals**: Add deployment steps to publish Docker images to a container registry or deploy to cloud infrastructure
like Azure or AWS.

**🧪 Goals**

- Grow fluency in .NET and Entity Framework Core
- Practice building a backend using modular monolith architecture
- Learn to seed data, manage multiple DbContexts, and configure dependency injection
- Implement comprehensive authentication and authorization
- Master exception handling and error management patterns
- Prepare to transition into a microservices architecture:
    - Define clear service boundaries
    - Implement inter-service communication (REST, gRPC)
    - Explore database-per-service and event-driven designs

**🐳 Docker Setup**

This project uses Docker to spin up a SQL Server container alongside the backend API.

To get started:

```bash
docker-compose up --build
```

This will:
- Start a SQL Server container with the correct ports and environment variables
- Build and run the backend API
- Apply database migrations for all modules

⚠️ **Ensure Docker is running and port ```1433``` is available on your system.**

**🗃️ Technologies Used**

- .NET 8 (or latest .NET version)
- Entity Framework Core
- SQL Server (via Docker)
- AutoMapper
- JWT Authentication
- NUnit
- Docker and Docker Compose
- GitHub Actions (CI)

**📦 What's Next?**

- Finalize business logic and validations across all modules
- Add Swagger/OpenAPI support for better API documentation
- Implement comprehensive logging and monitoring
- Add API versioning and rate limiting
- Begin breaking into microservices
- Add centralized configuration management
- Experiment with alternative ORMs and patterns for deeper understanding
- Implement caching strategies
- Add integration tests

**🙌 Contributions**

This is an educational and personal learning project. That said, feel free to fork it, open issues, or contribute ideas
if it piques your interest.

**📄 License**

MIT — use it, learn from it, or build upon it freely.
