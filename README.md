# TBD Modular Monolith (.NET Learning Project)

![CI](https://github.com/your-username/your-repo/actions/workflows/ci.yml/badge.svg)

This is a personal project aimed at deepening my understanding of the **.NET framework**, **Entity Framework Core**, and building real-world backend systems. I'm using a **modular monolithic architecture** to structure the application for better scalability and maintainability.

The project includes distinct modules for User, Address, Schedule, and Service management, each with its own domain logic, DbContext, models, repositories, and services.

---

## 🧱 Project Structure

```plaintext
.
├── API                      # Shared DTOs and interfaces
│   ├── DTOs
│   └── Interfaces
├── AddressModule
│   ├── Controllers
│   ├── Data
│   ├── Exceptions
│   ├── Models
│   ├── Repositories
│   └── Services
├── ScheduleModule
│   ├── Controllers
│   ├── Data
│   ├── Models
│   ├── Repositories
│   └── Services
├── ServiceModule
│   ├── Controllers
│   ├── Data
│   ├── Models
│   ├── Repositories
│   └── Services
├── UserModule
│   ├── Controllers
│   ├── Data
│   ├── Models
│   ├── Repositories
│   └── Services
├── Shared                  # Mapping and utility classes
│   └── Utils
├── GenericDBProperties     # Base model interfaces and shared properties
├── Data/Seeding            # Seeder classes per module
├── DesignTimeFactories     # EF Core context factories for migrations
├── Migrations              # Separated per DbContext
├── TestProject             # xUnit test project
├── TBD.http                # HTTP requests for manual API testing
├── Dockerfile              # Docker build setup for API
├── docker-compose.yml      # SQL Server and API orchestration
├── Program.cs, appsettings.json, etc.
└── .github/workflows/ci.yml  # GitHub Actions CI pipeline
🔁 CI/CD Pipeline

This project includes a CI pipeline powered by GitHub Actions.

Trigger: Runs on pushes and pull requests to the main branch.
Jobs:
Restore and build the project
Run all unit tests (xUnit)
Validate the Docker image build
📄 CI configuration: .github/workflows/ci.yml

You can monitor the workflow under the Actions tab in GitHub.

🛠️ Future goals: Add deployment steps to push Docker images to a registry or deploy to cloud infrastructure like Azure or AWS.
🧪 Goals

Deepen understanding of .NET and EF Core
Build a scalable system using modular monolith architecture
Practice seeding data, managing multiple DbContexts, and setting up dependency injection
Prepare for migrating to a microservices architecture:
Learn service boundaries
Implement inter-service communication
Practice database-per-service pattern
🐳 Docker Setup

The project uses Docker to spin up SQL Server and the backend API together.

Run this to get started:

docker-compose up --build
This will:

Start a SQL Server container
Build and run the backend API
⚠️ Make sure Docker is running and port 1433 is available on your system.
🗃️ Technologies Used

.NET 9
Entity Framework Core
SQL Server (Docker)
AutoMapper
xUnit
Docker + Docker Compose
GitHub Actions (CI)
📦 What's Next?

Finalize business logic for each module
Add Swagger/OpenAPI support
Begin splitting into microservices
Implement centralized logging and configuration
Explore alternative ORMs to reduce reliance on EF Core
🙌 Contributions

This project is a personal learning effort. That said, feel free to open issues, share ideas, or fork it if you're interested in contributing.

📄 License

MIT — use it, learn from it, or build upon it freely.
