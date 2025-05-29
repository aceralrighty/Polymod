# TBD Modular Monolith (.NET Learning Project)

This is a personal project aimed at deepening my understanding of the **.NET framework**, **Entity Framework Core**, and building real-world backend systems. I'm using a **modular monolithic architecture** to structure the application for better scalability and maintainability.

The project includes separate modules for User, Address, Schedule, and Service management, each with its own context, models, repositories, and services.

---

## 🧱 Project Structure

```plaintext
.
├── API                      # Shared DTOs and service interfaces
├── AddressModule           # Address-specific logic (DbContext, services, etc.)
├── ScheduleModule          # Schedule-specific logic
├── ServiceModule           # Service-specific logic
├── UserModule              # User-specific logic
├── Shared                  # Mapping profiles and shared utilities
├── Data/Seeding            # Initial seed data setup
├── DesignTimeFactories     # Design-time DbContext factories for migrations
├── Migrations              # EF Core migrations by module
├── Dockerfile              # Docker build file for app
├── docker-compose.yml      # Orchestrates SQL Server and app
├── TBD.http                # HTTP file for API testing
├── TestProject             # xUnit tests for services
└── Program.cs, appsettings.json, etc.
```

---

## 🧪 Goals

- Grow fluency in **.NET** and **Entity Framework Core**
- Practice building a backend using **modular monolith architecture**
- Learn how to **seed data**, manage **DbContexts**, and configure **dependency injection**
- Eventually transition this project into a **microservices** architecture to simulate real-world distributed systems

---

## 🐳 Docker Setup

This project depends on a running **SQL Server** instance in Docker. A `docker-compose.yml` file is included to simplify the setup process.

To spin everything up:

```bash
docker-compose up --build
```

This will:

- Start a SQL Server container with the correct environment variables and ports exposed
- Build and run your backend API project (once you've connected it properly)

> ⚠️ Make sure Docker is running on your machine and ports like `1433` are not blocked.

---

## 🗃️ Technologies Used

- .NET 8 (or your current target version)
- Entity Framework Core
- SQL Server (via Docker)
- AutoMapper
- xUnit (for testing)
- Docker + Docker Compose

---

## 📦 What's Next?

I'm still building out the full functionality for each module and service. Once the monolithic version is complete and stable, I plan to break it into **microservices** to explore:

- Inter-service communication (e.g., REST, gRPC)
- Service discovery
- Independent scaling and deployment
- Abstract Data Layers to learn not to rely on the **EntityFramework** so much

---

## 🙌 Contributions

This project is for educational purposes, but if you're curious or want to collaborate, feel free to fork or open an issue.

---

## 📄 License

MIT — you’re free to use, learn from, or modify this project.
