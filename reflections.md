# Week 6 Lab 1: Clean Architecture Reflections

This document provides answers to the architectural reflection questions posed in [week06-lab1-instructions.md](docs/week06-lab1-instructions.md) based on the completed Clean Architecture refactoring of **W5-UC01: View Student Enrolments**.

---

## 1. Which project owns `IEnrolmentGateway`, and why?

**Project:** `PgrStudentManagement.Business`

**Rationale:**
- In Clean Architecture and the Dependency Inversion Principle (DIP), high-level business logic must not depend on low-level implementation details (such as Entity Framework Core, SQLite, or MongoDB).
- Instead, the use case owns and defines the abstraction for whatever external persistence operations it requires.
- Placing `IEnrolmentGateway` in `PgrStudentManagement.Business.Students.Gateway` ensures that the data access requirements are dictated by business use cases rather than by the structure of a database or ORM framework.

---

## 2. What is the difference between the input port and the output port?

| Port Type | Interface | Primary Purpose | Invocation Flow |
|---|---|---|---|
| **Input Port** | `IViewStudentEnrolmentsUseCase` | Defines the API through which incoming drivers (e.g., MVC controller, REST API controller) request business operations. | **Incoming Adapter &rarr; Business** (Driver &rarr; Application core) |
| **Output Port** | `IEnrolmentGateway` | Defines the contract that the use case requires from external driven components (e.g., database, message bus, third-party service). | **Business &rarr; Outgoing Adapter** (Application core &rarr; Driven adapter) |

- **Input Port:** The use case *implements* the input port interface.
- **Output Port:** The persistence adapter *implements* the output port interface, while the use case *calls* it.

---

## 3. How can runtime control reach the EF gateway when Business does not reference Persistence?

Control reaches the persistence gateway through **Dependency Inversion** configured in the **Composition Root**:

1. **Compile-time decoupling:**
   `PgrStudentManagement.Business` has no reference to `PgrStudentManagement.Adapters.Persistence`. At compile time, `Business` only knows about the interface `IEnrolmentGateway`.
2. **Composition Root binding:**
   The executable host (`PgrStudentManagement.Application`) references both `Business` and `Adapters.Persistence`. During startup in `Program.cs`, the dependency injection container registers the implementation:
   ```csharp
   builder.Services.AddScoped<IEnrolmentGateway, EfEnrolmentGateway>();
   ```
3. **Runtime dependency injection:**
   When a controller invokes `ViewStudentEnrolmentsUseCase`, the DI container instantiates `EfEnrolmentGateway` and passes it into the use case constructor as `IEnrolmentGateway`. Control passes dynamically through polymorphism.

---

## 4. Why are Domain entities separate from EF records?

1. **Separation of Concerns:**
   - **Domain entities** (`Enrolment`, `Student`) represent core business entities, business invariants, and domain logic. They must remain pure and free from framework-specific annotations.
   - **Persistence records** (`EnrolmentRecord`, `StudentRecord`) represent relational schema details, database column mappings, foreign keys, table relationships, and ORM tracking configurations.
2. **Preventing Framework Leakage:**
   Embedding EF Core attributes (e.g., `[Key]`, `[ForeignKey]`, `[Table]`, `[Index]`) or navigation collections into domain models couples the domain to a specific ORM and storage mechanism.
3. **Independent Evolution:**
   A schema change (e.g., partitioning, denormalization, column renaming, or migrating to NoSQL) should only affect persistence records and mappers without rippling into business rules or domain models.

---

## 5. Why do MVC and REST use different boundary models?

MVC and REST serve fundamentally different consumers:

1. **MVC View Models (`StudentEnrolmentsViewModel`, `EnrolmentViewModel`):**
   - Designed for human presentation in HTML templates.
   - Values are pre-formatted for rendering (e.g., dates as `"01 October 2024"`, missing values as `"Not recorded"`).
   - May contain UI-specific state, display helper properties (e.g., `HasEnrolments`), and validation tags for forms.
2. **REST Response Models (`StudentEnrolmentsResponse`, `EnrolmentResponse`):**
   - Designed for programmatic consumption and machine-to-machine integrations.
   - Preserve typed data representations (e.g., ISO-8601 strings or `DateTime` properties, nullable fields instead of presentation strings like `"Not recorded"`).
   - Adhere to strict API versioning and serialization contracts.

Sharing a single model across MVC and REST would compromise either presentation usability or API contract stability.

---

## 6. Which classes and behaviour are shared by the MVC and REST paths?

Both incoming adapter paths share:

- **Input Port Interface:** `IViewStudentEnrolmentsUseCase`
- **Use Case Interactor:** `ViewStudentEnrolmentsUseCase`
- **Use Case DTOs:** `ViewStudentEnrolmentsInput`, `StudentEnrolmentsOutput`, `EnrolmentOutput`
- **Output Port Interface:** `IEnrolmentGateway`
- **Persistence Gateway Implementation:** `EfEnrolmentGateway`
- **Domain Entities:** `Enrolment`, `EnrolmentStatus`
- **Shared Business Behaviour:**
  - Enrolment retrieval for a specific student number.
  - Filtering to exclude unrelated students' enrolments.
  - Strict chronological ordering (earliest to latest by `StartDate`).
  - Read-only execution with zero mutation of persistent data.
  - Preserving an empty collection for students with no recorded enrolments.

---

## 7. What would have to change if SQLite were replaced?

If SQLite were replaced with another database technology (such as PostgreSQL, Microsoft SQL Server, or MongoDB):

1. **Inside `PgrStudentManagement.Adapters.Persistence`:**
   - Update the EF provider package (e.g., replace `Microsoft.EntityFrameworkCore.Sqlite` with `Npgsql.EntityFrameworkCore.PostgreSQL` or `Microsoft.EntityFrameworkCore.SqlServer`).
   - Adjust column data types, value generators, or migrations if required by the new dialect.
   - If migrating to a document database like MongoDB, implement `IEnrolmentGateway` with a Mongo gateway and document mapper.
2. **Inside `PgrStudentManagement.Application`:**
   - Update the connection string and DI configuration in `Program.cs` / `appsettings.json` (e.g., `options.UseNpgsql(...)` or `options.UseSqlServer(...)`).
3. **No changes required in:**
   - `PgrStudentManagement.Domain`
   - `PgrStudentManagement.Business`
   - `PgrStudentManagement.Adapters.Web`
   - `PgrStudentManagement.Adapters.API`
   - `PgrStudentManagement.Tests` (business use case tests remain 100% valid because they use `FakeEnrolmentGateway`).

---

## 8. How do project references help enforce the Dependency Rule?

In .NET, source code dependencies are strictly constrained by MSBuild project references (`<ProjectReference>` in `.csproj` files):

1. **Compile-Time Enforcement:**
   - If a developer in `PgrStudentManagement.Business` attempts to import `using Microsoft.EntityFrameworkCore;` or `using PgrStudentManagement.Adapters.Persistence;`, the compiler throws a CS0246 error because `Business.csproj` only references `Domain.csproj`.
   - Incoming adapters (`Adapters.Web` and `Adapters.API`) do not reference `Adapters.Persistence`, preventing controllers from bypassing the use case and directly injecting `DbContext`.
2. **Prevention of Circular Dependencies:**
   MSBuild disallows circular project references, preventing bad architectural drift and keeping the dependency hierarchy clear and unidirectional.
3. **Complementary Automated Architecture Tests:**
   Unit tests like [`DomainDependencyTests.cs`](PgrStudentManagement.Tests/Architecture/DomainDependencyTests.cs) inspect assembly metadata at test execution time to guarantee that the compiled assemblies do not inadvertently pull in prohibited dependencies.

