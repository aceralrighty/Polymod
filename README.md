**TBD Modular Monolith (.NET Learning Project)**

This is a personal project aimed at deepening my understanding of the ```.NET framework```, ```Entity Framework Core```,
and building real-world backend systems. I'm using a **modular monolithic architecture** to structure the application
for better scalability and maintainability.

The project includes separate modules for User, Address, Schedule, and Service management—each with its own domain
logic, DbContext, models, repositories, and services.


**🧱 Project Structure**

```plaintext
.

├── API                      # Shared DTOs and service interfaces
│   ├── DTOs
│   └── Interfaces
├── AddressModule           # Address-specific logic (controllers, data, models, etc.)
├── ScheduleModule          # Schedule-specific logic
├── ServiceModule           # Service-specific logic
├── UserModule              # User-specific logic
├── Shared                  # Mapping profiles and shared utilities
│   └── Utils
├── GenericDBProperties     # Base model interfaces and shared properties
├── Data/Seeding            # Seeder classes per module
├── DesignTimeFactories     # EF Core design-time context factories for migrations
├── Migrations              # EF Core migrations organized by module
├── TestProject             # xUnit tests for services and repositories
├── TBD.http                # HTTP requests for manual API testing
├── Dockerfile              # Docker build setup for API
├── docker-compose.yml      # SQL Server and API orchestration
├── .github/workflows/ci.yml # GitHub Actions CI pipeline
└── Program.cs, appsettings.json, etc.
```

**🔁 CI/CD Pipeline**

This project uses GitHub Actions to automatically build and test on every push or pull request to the main branch.

Workflow includes:\
Restoring and building the solution\
Running all unit tests with xUnit\
Validating Docker image build

**📄 CI configuration file: .github/workflows/ci.yml**

You can monitor the workflow under the Actions tab on GitHub.

🛠️ Future goals: Add deployment steps to publish Docker images to a container registry or deploy to cloud infrastructure
like Azure or AWS.

**🧪 Goals**

Grow fluency in .NET and Entity Framework Core
Practice building a backend using modular monolith architecture
Learn to seed data, manage multiple DbContexts, and configure dependency injection
Prepare to transition into a microservices architecture:
Define clear service boundaries
Implement inter-service communication (REST, gRPC)
Explore database-per-service and event-driven designs

**🐳 Docker Setup**

This project uses Docker to spin up a SQL Server container alongside the backend API.

To get started:

```docker-compose up --build```

This will:
Start a SQL Server container with the correct ports and environment variables\
Build and run the backend API

⚠️ Ensure Docker is running and port ```1433``` is available on your system.

**🗃️ Technologies Used**

.NET 8 (or latest .NET version)\
Entity Framework Core\
SQL Server (via Docker)\
AutoMapper\
xUnit\
Docker and Docker Compose\
GitHub Actions (CI)

**📦 What's Next?**

Finalize business logic and validations across all modules\
Add Swagger/OpenAPI support
Begin breaking into microservices\
Add centralized logging and configuration management\
Experiment with alternative ORMs for deeper understanding

**🙌 Contributions**

This is an educational and personal learning project. That said, feel free to fork it, open issues, or contribute ideas
if it piques your interest.

**📄 License**

MIT — use it, learn from it, or build upon it freely.
