Implementing a .NET Class Library at Different Maturity Levels

As a .NET project grows, its class library architecture should evolve. Below, we outline three stages of project maturity – Greenfield, Mid-stage, and Mature – with guidance on folder structure, design patterns, key abstractions, and common pitfalls at each stage. The focus is on a C# context using modern best practices (SOLID, DI, testability, separation of concerns), with code examples to illustrate each level.

1. Greenfield Stage (New Project, Minimal Structure)

In a greenfield project, keep the design lean and flexible, focusing on core features without over-engineering. The goal is to get a working structure quickly while laying a foundation for growth:
	•	Folder & Namespace Structure: Start with a simple layout. For example, in a single project you might separate concerns into folders like Models (or Domain) for entities and Services for logic. If this is a class library, it might hold just your domain models and minimal logic, referenced by a thin UI layer (e.g. a console app or ASP.NET minimal API). Keeping related classes under clear namespaces (e.g. MyApp.Domain, MyApp.Services) makes the code easier to navigate even in a small codebase. In the beginning, all code may live in one assembly, but organize it logically to ease future expansion.
	•	Patterns & Principles: Apply core SOLID principles from day one in simple ways. Use dependency injection (DI) even for a small project – for example, use a ServiceCollection to register services and build a ServiceProvider ￼. This avoids hard-coding dependencies and improves testability. Keep each class focused (Single Responsibility Principle): e.g. a single class might handle a specific operation or business logic, rather than doing UI, business, and data tasks at once. It’s fine at this stage to call EF Core or other APIs directly in your methods (no repository yet), but isolate data access logic in one place (e.g. within a service method) rather than scattering it. Prefer composition over inheritance for flexibility – e.g. inject a helper or client class rather than subclassing for variation. Overall, keep the design simple, obvious, and change-friendly.
	•	Key Abstractions & Contracts: Introduce minimal abstractions only where clearly needed. For instance, define domain models (POCO classes for your entities) with the essential properties – e.g. an Order class with an ID and relevant fields ￼. You usually don’t need many interfaces right now, except if an external dependency or a likely variation point exists. One useful abstraction in early stages is an interface for external services your logic calls (e.g. an INotificationSender if you plan to send emails) so you can provide a simple stub implementation now and swap a real one later. If you perform calculations or validations, you might keep those in a separate service class or static helper method for clarity, which can later be refactored into strategy patterns or validators. The greenfield level is more about establishing a clear domain language (classes, methods with meaningful names) than creating lots of layers. Focus on making the core model and operations understandable and decoupled from UI or infrastructure details.
	•	Typical Mistakes to Avoid: Don’t tightly couple everything in the rush of starting out. Common pitfalls include:
	•	Hard-coding or scattering logic: e.g. directly instantiating a DbContext or HTTP client inside methods instead of using DI – this makes testing and future refactoring harder. Instead, configure such services in DI and pass them in ￼.
	•	Over-engineering: creating complex class hierarchies or generic frameworks too early. For example, avoid prematurely abstracting every little operation into interfaces or using patterns like repository/UoW when you only have one or two entities – it adds unnecessary indirection at this stage. Keep it simple; you can always introduce patterns as requirements grow.
	•	No separation of concerns: e.g. doing file I/O or database calls in UI event handlers or mixing business logic into UI code. Even in a minimal app, maintain a clean separation – e.g. a console Program class should call a service method to do the work, not contain the work itself.
	•	Neglecting testability: you might be tempted to skip unit tests initially, but try to write a few for core logic (e.g. a critical calculation). Structure your code (using DI and pure functions where possible) such that adding tests is straightforward. This foresight pays off as the project grows.
	•	Example (Minimal Setup): Below is a simplified example of a greenfield setup – a domain model, a service, and a basic composition root using DI. This illustrates the minimal class library structure with proper DI and single-purpose classes:

// Domain model
namespace MyApp.Domain {
    public class Order {
        public string Id { get; set; }
        public decimal Total { get; set; }
    }
}

// Service class handling domain logic (e.g., processing orders)
namespace MyApp.Services {
    public class OrderService {
        // Suppose we had an external notifier dependency, it could be injected via interface
        public Task ProcessOrderAsync(Order order) {
            // Minimal business logic for demo
            if (order.Total <= 0) throw new ArgumentException("Total must be positive");
            Console.WriteLine($"Processing Order {order.Id} for ${order.Total}");
            // In a real app, perhaps save to DB or call external service here
            return Task.CompletedTask;
        }
    }
}

// Composition Root (e.g., in Program.cs of a console or startup of an API)
var services = new ServiceCollection();
services.AddTransient<OrderService>();              // Register service in DI
using var provider = services.BuildServiceProvider();
var orderSvc = provider.GetRequiredService<OrderService>();   // Resolve service
await orderSvc.ProcessOrderAsync(new Order { Id = "ORD-001", Total = 99.99m });

In this snippet, the OrderService is added to the DI container and then retrieved for use. Even in a tiny app, using the DI container (from Microsoft.Extensions.DependencyInjection) ensures the code is flexible and testable (you could swap out implementations or add new services easily) ￼. The Order model is simple, and logic is kept in the service, not strewn throughout the UI or data layers. This foundation makes it easier to expand the project without major restructuring.

2. Mid-Stage (Growing Project with Services and Scaffolding)

At the mid-stage, the project has grown in complexity. You likely have multiple features, more domain entities, and perhaps a real database or external APIs in play. Now is the time to introduce some layering and patterns to manage complexity while preserving flexibility:
	•	Folder & Namespace Structure: As the codebase expands, split the solution into clearer layers. A common approach is having separate projects or at least distinct folders for Domain, Application/Services, and Infrastructure:
	•	Domain – your core models and business rules (e.g. entities, value objects, perhaps domain-specific interfaces). Keep this layer free of heavy dependencies so it remains reusable and easy to test.
	•	Application (Service Layer) – classes that coordinate tasks like use cases or business workflows. For example, an OrderService or an EntityManager class that knows how to perform create/update operations by using the domain models and infrastructure.
	•	Infrastructure – implementation details like data access (EF Core DbContext, file system, external service clients, etc.). For instance, you might have a Data folder or project containing YourDbContext and initial repository classes or data mappers.
	•	If you started with one class library, you might now split it (e.g. one for Domain and one for Infrastructure) or continue with one project but clearly separate namespaces (e.g. MyApp.Infrastructure vs MyApp.Domain). The key is that each layer has a clear responsibility and can be worked on independently. This structure is evident in the sample projects – e.g., an Application project containing services and an Infrastructure project for data context emerged as the project grew ￼.
	•	Patterns & Principles: At this stage, introduce patterns to better manage dependencies and logic:
	•	Use dependency injection pervasively to supply infrastructure to your services. For example, configure your EF Core context in the DI container and inject it into services that need database access ￼. A typical mid-stage setup in Startup or Program will call services.AddDbContext<YourDbContext>(...) to register the context and services.AddScoped<YourService>() for each service class.
	•	Service classes become important – each major part of the domain might have a service or manager class encapsulating operations. For instance, an OrderService might handle creating, updating, or validating orders. Defining interfaces for these services (e.g. IOrderService) can be beneficial if you anticipate multiple implementations or want to facilitate mocking in tests ￼. However, if an interface doesn’t add value (only one implementation, no plan to swap it), you can postpone creating it.
	•	Continue applying SOLID: Open/Closed Principle – as features grow, prefer extending the code (adding new classes or methods) over modifying core classes. For example, if new validation rules are needed, you might add new validator classes or strategies instead of cluttering the OrderService with if/else. Interface Segregation – keep interfaces focused. Instead of one monolithic interface, you might have separate ones (e.g. an IOrderRepository for order persistence, IEmailSender for notifications). This prevents classes from depending on methods they don’t use.
	•	You might start using domain-driven patterns. For instance, ensure each aggregate or domain entity is managed through a clear interface or service – this is akin to the Repository pattern but you may still be using EF Core directly at this point. Some teams implement simple repository classes per aggregate at mid-stage (e.g. an OrderRepository that wraps YourDbContext for order-specific queries). If you do, keep them lightweight and focused on data operations, leaving business logic in the service layer.
	•	Another pattern at this stage is using Domain Events or Mediator if your domain needs to react to changes. For example, you might introduce a simple in-memory event dispatcher to decouple parts of the logic. In the sample project, an event-driven validation consumer was used to handle save operations asynchronously ￼ ￼ – a mid-stage project could similarly introduce background processing or message handling if needed. The guiding principle is to reduce tight coupling: parts of the system communicate through clear contracts (method calls or events) rather than direct references.
	•	Emphasize testability in your patterns. For example, if you integrate with external systems (email, payment, etc.), abstract those behind interfaces and provide a fake implementation for unit tests. Ensure your services can be constructed with test doubles (e.g. an in-memory repository or a stub context) so you can write unit tests for business logic without needing a real database.
	•	Key Abstractions & Contracts: With more moving parts, explicitly define contracts for each concern:
	•	Service Interfaces: If not done already, define interfaces for core services (e.g. IOrderService) especially if the service will be consumed in many places or you plan to swap implementations (e.g. a different IOrderService for a new module). This also helps with unit testing by allowing mocking. In mid-stage, you might still be fine testing concrete service classes with a test DB, but having an interface is a clean boundary.
	•	Data Access Abstractions: You may introduce simple repository interfaces at this point. For example, an IOrderRepository with methods like GetById(int id), Add(Order order), etc., or even a more general IRepository<T> contract. This sets the stage for a full repository pattern later. If you aren’t ready for a full generic repository, you can have minimal data access methods on the service or a thin wrapper around your DbContext (essentially acting as a repository). The Unit of Work concept might still be implicit (you call _context.SaveChanges() in your service methods to commit a transaction), but you could define an IUnitOfWork interface with just SaveChanges() to formalize the commit operation ￼. This can make transaction management clearer and ready the code for expansion.
	•	Domain Constraints and Validators: As business rules grow, consider defining abstractions for validation logic. For example, an IValidator<T> for entities, or specific rule classes. This keeps rules separate from the core logic. The project example uses a validation pipeline where validators are registered via DI and executed on save ￼. In a mid-stage project, you might achieve something similar simply by calling validation methods from your service, but having a clear interface or base class for validators helps keep things organized.
	•	Logging and Config: Introduce cross-cutting concerns through abstraction. For instance, use the ILogger interface (built into .NET) in your classes rather than Console.WriteLine – DI will supply an appropriate logger. Also, read configuration (connection strings, etc.) from config files or environment, not as magic constants in code. This isn’t exactly a “contract” but a best practice to avoid scattering config values in the code.
	•	Typical Mistakes to Avoid: As the architecture grows, be wary of these issues:
	•	Service bloat: Watch out for “God classes” in the service layer. If one service is doing too much (e.g. OrderService handling orders, payments, notifications all together), split it into focused services or use helper classes. Overloaded services become hard to maintain and test. Adhere to single responsibility – one service handles one area of logic.
	•	Insufficient layering: Sometimes developers skip creating an application layer and end up with heavy logic in controllers or UI classes. Avoid this by pushing logic down into the class library (services and domain). Controllers (in a web app) or UI event handlers should delegate to your library’s services. This separation makes the core logic reusable (e.g. you could call it from a console app or tests without a web server) and keeps UI simple.
	•	Tight coupling to infrastructure: At mid-stage, a common mistake is letting EF Core or other framework details leak into your service or domain code. For example, using IQueryable<Order> all over the place or relying on EF-specific lazy loading in domain logic. This can make later refactoring to a repository or different data source difficult. Try to isolate EF usage within the data layer. If you have introduced repository interfaces, code against those in your service instead of the DbContext directly (even if the repository is a thin wrapper). That way, switching implementations or adding caching later is easier.
	•	No strategy for transactions: As multiple operations per request emerge, ensure you handle transactions properly. If still using DbContext directly, be mindful to call SaveChanges() at the appropriate time and consider using IDbContextTransaction if needed for multiple steps. Neglecting this can lead to partial updates. This is where introducing a unit-of-work (even a basic one) helps coordinate multi-step operations (all or nothing).
	•	Poor test coverage: With added complexity, the risk of regression grows. A mid-stage codebase should have a test project exercising at least the service layer with various scenarios. Avoid the mistake of deferring tests – it’s much harder to retrofit tests later. Thanks to DI and the abstractions you’ve introduced, you can use an in-memory database (e.g. EF Core’s InMemory provider) or mock repositories to test service logic without full infrastructure. For example, the sample mid-stage setup uses an in-memory database for demo and tests, configured via a one-line setup method ￼. Emulate that approach to keep tests fast and isolated.
	•	Example (Mid-stage Service with EF Core): The snippet below demonstrates how an application service might directly use a DbContext in a mid-stage project. It also shows the DI registration that wires things together:

// Application service in mid-stage, directly using DbContext (injected via DI)
namespace MyApp.Services {
    public class OrderService : IOrderService {                // interface for testability
        private readonly YourDbContext _db;
        public OrderService(YourDbContext db) { _db = db; }

        public async Task<Order?> GetOrderAsync(int id) {
            // Directly query the DbContext for data
            return await _db.Orders.FindAsync(id);
        }

        public async Task<int> CreateOrderAsync(Order order) {
            _db.Orders.Add(order);
            // Business logic: for example, enforce some rule before saving
            if (order.Total < 50 && order.RequiresApproval) {
                throw new InvalidOperationException("Orders under $50 require no approval."); 
            }
            return await _db.SaveChangesAsync();  // commit transaction
        }

        // ... other methods like UpdateOrderAsync, DeleteOrderAsync ...
    }
}

// In Startup.cs or Program.cs for DI configuration (mid-stage)
services.AddDbContext<YourDbContext>(opts =>
    opts.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));  // Register EF Core context
services.AddScoped<IOrderService, OrderService>();  // Register the service with interface

In this example, OrderService is injected with YourDbContext and uses it to perform CRUD operations. This is a typical mid-stage approach: the service contains business logic and uses EF Core directly. We register the DbContext and the service in the DI container so that controllers or other parts of the app can get an IOrderService instance. Note how the service method encapsulates a business rule (throwing an exception if an order is below $50 and requires approval) – such rules stay in the service or domain layer, not in the controller or UI. By mid-stage, the code is organized so that if we needed to switch out the data layer (say, use a different database or add caching), we could introduce a repository layer with minimal changes to the service interface. The patterns in use (DI, layered architecture, focused interfaces) ensure the project can scale in complexity without a large rewrite.

3. Mature Stage (Full Patterns: Repository, Unit of Work, etc.)

In a mature project, the architecture is robust and enterprise-grade. The class library now likely spans multiple projects (or well-defined modules) and employs advanced patterns like the Repository and Unit of Work to enforce separation of concerns. The emphasis is on scalability, maintainability, and flexibility to change out implementations. At this stage, no significant logic is tied to a framework – it’s all abstracted behind interfaces – and the codebase is well-tested.
	•	Folder & Namespace Structure: A mature solution typically has clear multi-project separation and stricter layering boundaries (possibly following Clean Architecture or onion architecture concepts):
	•	Domain Project: Contains domain entities, domain services, and interfaces or abstractions that others implement (for example, repository interfaces might live here or in an application core project). This project should have no dependencies on EF Core, web, or any infrastructure – it represents pure business knowledge. In our context, this could include interfaces like IRepository<T> or IUnitOfWork and base classes like BaseEntity (with common properties) that all entities inherit ￼ ￼.
	•	Application Project: Orchestrates use cases and holds application logic (could also be called Application Core). It might contain unit-of-work interfaces, use case classes, and perhaps implementation of some domain interfaces if they don’t belong in pure domain. Sometimes the repository interfaces are defined at this layer (if domain should be persistence-agnostic) and only the implementations are in infrastructure.
	•	Infrastructure Project: Contains concrete implementations of the abstractions – e.g. EF Core repositories, the DbContext, Unit of Work implementation, external service integrations, etc. For example, you’d have EfGenericRepository<T> implementing your repository interface, and UnitOfWork implementing IUnitOfWork by coordinating the DbContext ￼. This project references Entity Framework Core, logging frameworks, external APIs, etc., but the other layers do not – they only interact through the interfaces.
	•	UI/Composition Root: (could be an ASP.NET Web API project, a GUI, or a console app) – this is where everything gets wired up. It references the above projects and configures DI. All concrete classes are injected here. For instance, Startup will add the EF DbContext, the concrete repository and UoW classes, etc., to the service container ￼. The UI layer’s controllers or presenters then rely on the abstractions (like calling an IOrderService or using IUnitOfWork) which have been provided by DI.
	•	Within each project, further organize by feature or pattern. For example, in the infrastructure project you might have folders Repositories, Data (for context and migrations), Services (for external adapters), etc. In the domain project, you might separate Entities vs. ValueObjects vs. Domain Services. The sample “Plan2RepositoryUoW” demonstrates this layout: there are distinct namespaces for Plan2RepositoryUoW.Domain.Entities, Plan2RepositoryUoW.Application.Services, and Plan2RepositoryUoW.Infrastructure.Data, etc., reflecting a clear separation ￼. This structure ensures each part of the codebase can evolve with minimal impact on others (for instance, you could swap the data layer from EF to MongoDB by changing the Infrastructure project, without altering domain or application logic).
	•	Patterns & Principles: The mature stage solidifies use of design patterns to enforce a clean architecture:
	•	Repository Pattern: All data access goes through repository interfaces now, rather than using DbContext directly in business code. For example, you might have a generic repository interface IGenericRepository<T> with methods for common data ops ￼, and specific repository interfaces or methods for aggregate-specific queries. The implementation (e.g. EfGenericRepository<T>) lives in Infrastructure and uses EF Core under the hood ￼ ￼. This decouples your business logic from EF Core – your application code deals with an interface that could be backed by EF, a web API, or anything else in the future.
	•	Unit of Work Pattern: This coordinates multiple repository operations and manages the transaction. Often implemented as an interface IUnitOfWork with methods like Repository<T>() (to get a repository) and SaveChangesAsync() ￼. The EF Core DbContext naturally acts as a UoW (it tracks changes and saves them together), but wrapping it in IUnitOfWork provides a cleaner abstraction for the app layer. In practice, you might use it like: var repo = _unitOfWork.Repository<Order>(); ... perform operations ... await _unitOfWork.SaveChangesAsync();. The sample implementation shows a generic UnitOfWork<TContext> that holds a DbContext and provides a generic repo factory and SaveChanges ￼. By using a unit of work, you ensure that all changes within a business operation either succeed or fail together, and the calling code doesn’t need to know about the underlying transaction details.
	•	Dependency Inversion Principle (DIP): At full maturity, DIP is strictly followed: high-level modules (domain, app services) define abstractions, and low-level modules (infrastructure) implement them. For example, the domain might define ISaveAuditRepository or IOrderRepository interfaces, and the infrastructure implements them with EF Core or MongoDB. The service layer depends on IOrderRepository, not on EfOrderRepository or DbContext. All concrete classes are provided via DI. This inversion keeps your core logic independent of technical details. In code, you’ll see this as constructors taking interfaces (e.g. OrderService(IOrderRepository repo, IUnitOfWork uow, ...)) and registration like services.AddScoped<IOrderRepository, EfOrderRepository>() in the composition root.
	•	Advanced SOLID & Best Practices: The Single Responsibility Principle might manifest in even more granular classes now (e.g. distinct command handler classes for different operations). Open/Closed is enforced by leveraging polymorphism – e.g. if a new data validation rule is needed, you add a new validator class implementing a common interface rather than altering existing validators. Liskov Substitution and Interface Segregation are considered in designing interfaces – for instance, your repository interfaces should be substitutable with alternative implementations (test doubles or different DBs) without affecting correctness, and you might split read vs write interfaces if some services only need read access (Query object vs Repository separation). You also likely use cross-cutting concerns in a structured way – e.g. using decorators or middleware for logging, caching, or retry policies, instead of peppering this logic throughout the code.
	•	CQRS and Mediator (optional): In mature systems, Command-Query Responsibility Segregation (CQRS) and mediator patterns often appear. You might segregate write operations (commands) and reads (queries) into different models or use a library like MediatR for handling requests and responses within the application layer. This can further decouple logic. If the project complexity warrants, this is a good stage to implement it. For example, instead of an OrderService with many methods, you might have separate command handlers like CreateOrderCommandHandler and query handlers like GetOrderQueryHandler, each focused on one action. This isn’t mandatory, but many mature architectures adopt it for clarity and scalability of teams.
	•	Comprehensive Testing & Quality Practices: At the mature stage, you should have a comprehensive suite of unit and integration tests. Repositories can be tested against an in-memory database or a test database to ensure they respect invariants (like the soft-delete filter on Validated entities ￼). Business logic is tested with mock repositories or using the real implementations against a lightweight in-memory setup. For example, the repository’s sample uses an EF Core in-memory database in tests to simulate the full repository/UoW behavior ￼. Additionally, static code analysis, style enforcement, and continuous integration are typically in place to maintain code quality as the team grows.
	•	Key Abstractions & Contracts: By now, many important interfaces and contracts are defined. Some of these include:
	•	Generic Repository Interface: As mentioned, an interface like IGenericRepository<T> for data access operations ￼. This often lives in the core library (so that services can use it without referencing EF). It defines methods for retrieving, adding, deleting, and counting entities. In a mature design, you might further refine this contract or extend it for specialized needs (e.g. an IOrderRepository that inherits IGenericRepository<Order> and adds an GetRecentOrders() method if needed).
	•	Unit of Work Interface: e.g. IUnitOfWork with a method to get repositories and to commit transactions ￼. This provides a higher-level abstraction over the database transaction. Sometimes this interface also includes methods for beginning/committing transactions or a Dispose if manually controlling lifetime (though if scoped per request in DI, disposing is automatic).
	•	Domain Interfaces and Base Classes: You likely have some base types or interfaces that enforce domain consistency. For example, in the sample library, all entities implement IValidatable and IBaseEntity ￼, and there’s a base class BaseEntity providing common Id and Validated properties. This is an example of a domain contract that all entities follow. Similarly, you may have domain service interfaces (e.g. IOrderValidator, IPricingStrategy) that allow plugging in different business rules. These abstractions make the domain model richer and more adaptable.
	•	Service Interfaces (Application Contracts): All externally used services (application layer) have interfaces. If you have implemented CQRS/mediator, the “contracts” are the request/response DTOs and handler interfaces. Also, any DTOs or data transfer contracts are well-defined at this point (you might have separated internal domain models from external-facing models to protect domain integrity).
	•	External Integration Interfaces: Any integration (email, messaging, file storage, etc.) should be behind an interface. For example, an IPaymentGateway interface with a concrete implementation in infrastructure. This ensures that external changes (say a new payment vendor) don’t ripple through your core code – you just implement a new adapter. This also keeps third-party SDKs out of your core logic; only your interface is referenced there.
	•	Configuration and Settings: It’s common to define POCO classes for application settings (bound from config files) and use interfaces like IOptions<T> (from Microsoft.Extensions.Options) to access them in your services. This abstracts raw configuration into typed contracts. While not an interface you define, it exemplifies the mature approach of abstraction – your code doesn’t fetch config from environment directly; it asks for an IOptions<AppSettings> which is provided via DI.
	•	Typical Mistakes to Avoid: Even in mature projects, pitfalls exist. Here are some to watch out for:
	•	Accidental complexity: With many patterns in play, there’s a risk of over-engineering. Avoid creating abstractions that don’t have a clear benefit. For instance, a repository that simply wraps EF Core calls 1:1 without adding any logic might be unnecessary – ensure your repository pattern adds value (such as encapsulating the soft-delete query filter, as done in the example ￼ ￼). Every layer should have a purpose; don’t add layers “just because.” Keep the architecture as simple as possible, but no simpler.
	•	Leaky abstractions: Ensure that your abstractions truly isolate concerns. If your repository interface returns an IQueryable<T> or DbSet<T>, it’s leaking EF Core specifics to the service layer, defeating the purpose of the abstraction. Instead, return domain collections or list types. Another example is unit-of-work: if the service has to know about EF transactions or call DbContext methods directly even when using IUnitOfWork, then the UoW abstraction is not complete. Strive to keep higher layers ignorant of what’s underneath.
	•	Ignoring performance considerations: In a mature app, data volumes and load are higher. Mistakes like loading entire tables into memory via the repository (e.g. a naive GetAll() that doesn’t paginate) can hurt. Use patterns like Specification or Query Objects to allow efficient data retrieval without exposing LINQ or SQL in the service layer. Also, ensure your repository methods are tuned (use projections, filters, and proper indexing). In the sample, the repository applies a global filter for soft deletes ￼ ￼ – ensure such global behaviors are well-understood by the team to avoid confusion (e.g. document that querying IGenericRepository<T> only returns validated entities by default).
	•	Weak enforcement of invariants: As complexity grows, it’s easy to let business rules slip or scatter. A mature codebase should centralize invariants either in domain model methods or in domain services. A mistake would be allowing inconsistent operations by bypassing the service layer – for example, using a repository directly in a controller to save an Order without going through OrderService (thus missing business rules). To avoid this, make your application services the primary way to modify domain state and consider making the repository lower-level (or even internal to the infrastructure) so that inappropriate usage is discouraged. Some teams generate warnings or have code analyzers to prevent misuse (for instance, disallowing direct DbContext usage outside the infrastructure).
	•	Insufficient testing or documentation: A mature project is often worked on by multiple developers/teams. Not having up-to-date tests is a mistake because changes can break something in subtle ways. Ensure you have unit tests for all critical business logic and integration tests for data access (using a test database or in-memory). Also, document the architecture decisions for new team members – e.g. document that “we use Repository/UoW, here’s how to add a new repository” so that patterns are applied consistently. The sample repository, for instance, provided a guide for replicating the EF Core setup with these patterns ￼ ￼. Following such guides helps avoid misuse of the patterns.
	•	Neglecting SOLID as the code scales: Sometimes teams introduce the right patterns but don’t fully adhere to principles in implementation. For example, violating Single Responsibility by combining too much in a repository (it should mainly handle data operations, not business logic or validation), or violating DIP by having the domain call into infrastructure (the dependency should be the other way around). Keep an eye on these – do periodic reviews or refactorings to ensure the architecture stays clean.
	•	Example (Mature Usage of Repository and UoW): The following pseudocode illustrates how a service might be implemented in a mature system using repository and unit of work patterns, and how these are wired up via DI:

// Domain entity (with base interfaces)
public class Order : IValidatable, IBaseEntity {
    public int Id { get; set; }
    public decimal Total { get; set; }
    public bool Validated { get; set; }
    // ... other properties ...
}

// Repository interface (domain or application layer)
public interface IGenericRepository<T> where T : class, IValidatable, IBaseEntity {
    Task<T?> GetByIdAsync(int id);
    Task<IList<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task DeleteAsync(T entity, bool hardDelete = false);
    // ...other methods like Update, or attach if needed...
}

// Unit of Work interface
public interface IUnitOfWork {
    IGenericRepository<T> Repository<T>() where T : class, IValidatable, IBaseEntity;
    Task<int> SaveChangesAsync();
}

// Application service using repository and UoW (depends on abstractions only)
public class OrderService : IOrderService {
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGenericRepository<Order> _orderRepo;

    public OrderService(IUnitOfWork unitOfWork) {
        _unitOfWork = unitOfWork;
        _orderRepo = _unitOfWork.Repository<Order>();  // get repository for Order
    }

    public async Task<int> CreateOrderAsync(Order order) {
        // Business logic: e.g., ensure Validated flag based on some rule before adding
        order.Validated = (order.Total <= 1000);  // trivial rule: orders <=1000 are auto-validated
        await _orderRepo.AddAsync(order);
        // commit all changes (in this case, the new Order) as a single transaction
        return await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Order?> GetOrderAsync(int id) {
        return await _orderRepo.GetByIdAsync(id);  // behind the scenes uses DbContext
    }

    // ... UpdateOrderAsync, DeleteOrderAsync, etc., similarly using repo and then SaveChangesAsync() ...
}

// Infrastructure: EF Core implementation of GenericRepository and UnitOfWork
public class EfGenericRepository<T> : IGenericRepository<T> where T : class, IValidatable, IBaseEntity {
    private readonly YourDbContext _context;
    private readonly DbSet<T> _set;
    public EfGenericRepository(YourDbContext context) {
        _context = context;
        _set = context.Set<T>();
    }
    public Task<T?> GetByIdAsync(int id) => _set.FirstOrDefaultAsync(e => e.Id == id);
    public Task<IList<T>> GetAllAsync() => _set.ToListAsync();
    public Task AddAsync(T entity) => _set.AddAsync(entity).AsTask();
    public Task DeleteAsync(T entity, bool hardDelete = false) {
        if (hardDelete) _set.Remove(entity);
        else {
            entity.Validated = false;    // soft-delete: mark as not validated
            _set.Update(entity);
        }
        return Task.CompletedTask;
    }
    // Note: SaveChanges is not in repository, it’s in UnitOfWork to commit multiple operations together
}

public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext {
    private readonly TContext _context;
    public UnitOfWork(TContext context) { _context = context; }
    public IGenericRepository<T> Repository<T>() where T : class, IValidatable, IBaseEntity
        => new EfGenericRepository<T>(_context);
    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}

// Composition Root (Startup.cs) - registering services and implementations
services.AddDbContext<YourDbContext>(opts => opts.UseSqlServer(connString));
services.AddScoped<IUnitOfWork, UnitOfWork<YourDbContext>>();       // UoW with EF Core context
services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));  // generic repo for DI (optional, UoW can supply)
services.AddScoped<IOrderService, OrderService>();                  // application service

In this code:
	•	The OrderService uses IUnitOfWork and IGenericRepository<Order> (retrieved from the UoW) – it knows nothing about EF Core or how data is persisted. This service focuses purely on business logic (e.g. setting validation flags, deciding when to commit) and uses the repository for data operations. This aligns with the Dependency Inversion Principle: the service depends on abstractions, not concrete EF Core classes.
	•	The EfGenericRepository and UnitOfWork are the concrete implementations in the infrastructure layer. They encapsulate EF Core details. For instance, EfGenericRepository.DeleteAsync implements a soft delete by marking the entity’s Validated flag to false instead of removing the record ￼. Such details are invisible to the higher layers – a service calling DeleteAsync doesn’t need to know if the record is soft-deleted or hard-deleted. This is a powerful use of the Repository pattern: it centralizes data concerns (like query filters, as seen in the global filter for Validated in the DbContext ￼ ￼) so that all parts of the app adhere to them consistently.
	•	The DI configuration shows how everything is wired: the DbContext is registered, then the UnitOfWork and GenericRepository are registered so that when IUnitOfWork or IGenericRepository<Order> is needed, the DI container will provide UnitOfWork<YourDbContext> and EfGenericRepository<Order> respectively. The OrderService is registered to be consumed by controllers or other UI components. This corresponds to the example in the documentation ￼, where the DbContext, UnitOfWork, and repository were all added to the service collection.
With this mature setup, adding a new feature (say a new entity type) involves minimal friction: define the entity in the Domain, add a repository interface if you need custom queries (or use the generic one), maybe add a new service or handler for its operations, and everything else (transaction management, DI wiring) is largely handled by the existing infrastructure. The code is highly testable – you can swap the EfGenericRepository with an in-memory implementation or fake if needed, or use the real one against a test database to test the whole stack. The design supports extension (new types or behaviors can be added with new classes) without modifying the core workflow, exemplifying the Open/Closed Principle in practice.
Finally, a mature project benefits from accumulated knowledge: revisit earlier stages’ code and refactor if needed. For example, ensure earlier assumptions (like that Validated flag logic) still hold, and that no shortcut taken in the mid-stage now hinders adaptability. By adhering to these patterns and principles, the class library can handle complexity while remaining clean, maintainable, and adaptable to change ￼.