# Endpoint Pattern Evaluation: MediatR + Ardalis.Result → Direct Handlers + TypedResults

## Overview

This project migrated through two significant architectural shifts for its API endpoints:

1. **Phase 1 (original):** MediatR + Ardalis.Result + `IResult` / `.ToMinimalApiResult()`
2. **Phase 2 (current):** Direct static handlers + `TypedResults.*` + `Results<T1, T2, ...>` union return types

This document evaluates both patterns across maintainability, type safety, performance, testability, and .NET idiomatics.

---

## What Changed

### Before — MediatR + Ardalis.Result

```csharp
// Handler
public record CreateItem(string UserId, string Name, int BoxId) : IRequest<Result<ItemResponse>>;

public class CreateItemHandler : IRequestHandler<CreateItem, Result<ItemResponse>>
{
    public async ValueTask<Result<ItemResponse>> Handle(CreateItem request, CancellationToken ct)
    {
        // ...
        return Result.Success(new ItemResponse(item));
    }
}

// Endpoint
app.MapPost("/", async (HttpContext ctx, ItemRequest req, ISender sender) =>
    (await sender.Send(new CreateItem(ctx.GetUserId(), req.Name, req.BoxId)))
        .ToMinimalApiResult()
);
```

### After — Direct Handler + TypedResults

```csharp
// Handler (same file, same class)
private static async Task<Results<Created<ItemResponse>, ValidationProblem>> CreateItem(
    HttpContext context, ItemRequest request,
    [FromServices] StorageLabelsDbContext dbContext,
    [FromServices] TimeProvider timeProvider,
    ILogger logger, CancellationToken cancellationToken)
{
    // ...
    return TypedResults.Created((string?)null, new ItemResponse(item.Entity));
}

// Map file
routeBuilder.MapPost("/", CreateItem).WithName("Create Item");
```

---

## Comparison

### 1. Type Safety

| Aspect | MediatR + Ardalis.Result | TypedResults |
|---|---|---|
| Return type | `Task<IResult>` (erased) | `Task<Results<Ok<T>, NotFound<string>, ...>>` (union) |
| OpenAPI inference | Manual `.Produces<T>()` required | Compiler-inferred from union type |
| Mismatched status code | Runtime mistake | Compile error |
| Nullable response body | `Result.NotFound()` silently returns no body | `NotFound<string>` enforces string body |

**Winner: TypedResults.** The `Results<T1, T2, ...>` union type makes every possible HTTP response an explicit compile-time contract. There is no way to accidentally return a 404 where a 200 was expected, and OpenAPI documentation is generated directly from the type signature rather than from manually maintained `.Produces<T>()` annotations that could drift from the actual implementation.

---

### 2. Dependency Chain & Package Count

| | MediatR + Ardalis.Result | TypedResults |
|---|---|---|
| Extra packages | `MediatR`, `Ardalis.Result`, `Ardalis.Result.AspNetCore`, `Ardalis.Result.FluentValidation` | `Ardalis.Result.AspNetCore` + `Ardalis.Result.FluentValidation` (auth only) |
| Indirection layers | Request → MediatR pipeline → Handler → Result → `.ToMinimalApiResult()` | Request → Handler → TypedResult |
| Abstraction leakage | `Result<T>` carries status+value+errors bundle across all layers | HTTP concern (status code) stays in the HTTP layer |

The MediatR pattern added four packages solely to bridge ASP.NET Core's `IResult` with a domain result type. `Ardalis.Result` is a domain concern wrapper that shouldn't need to know about HTTP status codes — the bridge packages exist because the pattern mixed domain and HTTP concerns.

The current pattern keeps the domain concern (did this operation succeed?) as a native C# decision point (`if (thing is null) return TypedResults.NotFound(...)`) and the HTTP concern (what status code and body?) directly in the `TypedResults.*` call.

---

### 3. Maintainability

| Aspect | MediatR + Ardalis.Result | TypedResults |
|---|---|---|
| Files per feature | 3–4 (Request record, Handler, Validator, Map entry) | 2 (Handler + Map entry; validator inline or same file) |
| Navigation to find logic | Find the right `IRequest`, find its registered handler | Open the relevant `Map{Domain}.cs`, click through to the partial class method |
| Adding a new response type | Add to `Result` enum, update `.ToMinimalApiResult()` mapping, update `.Produces<T>()` | Add to union type, add branch, OpenAPI updates automatically |
| Onboarding | Developer must understand MediatR pipeline, behaviors, `Result<T>` semantics | Developer reads the method signature and the switch on TypedResults |

**Winner: TypedResults** for small-to-medium projects. The reduction from 3–4 files to 2, and the elimination of the MediatR pipeline, makes each feature directly traceable. The tradeoff is described in the cons section below.

---

### 4. Testability

**MediatR handlers** are straightforward to unit test in isolation — they accept a plain record and return a `Result<T>`. No HTTP context required.

**Direct handlers** are `private static` methods on a partial class, which means:
- They cannot be invoked directly in unit tests without reflection.
- Integration tests (via `WebApplicationFactory<T>`) remain the primary testing mechanism.
- `[FromServices]` injection still works with the test host.

This is the **largest practical tradeoff**. The existing test suite in this project uses integration tests throughout, which means the distinction is minimal in practice. For a project relying heavily on handler-level unit tests, the MediatR pattern's testability advantage would be significant.

---

### 5. Cross-Cutting Concerns (Pipeline Behaviors)

MediatR pipeline behaviors (e.g., logging, validation, transaction wrapping) apply uniformly across all handlers via `IPipelineBehavior<TRequest, TResponse>`. This is a genuine strength — one behavior file can enforce a pattern project-wide.

The current pattern handles cross-cutting concerns through:
- **ASP.NET Core middleware** for request-scoped concerns (authentication, rate limiting, cancellation).
- **Endpoint filters** (`AddEndpointFilter<T>()`) for route-group-scoped concerns (e.g., `UserExistsEndpointFilter`).
- **Explicit calls** inside handlers for validation and logging.

Endpoint filters cover most of what pipeline behaviors did in this codebase. However, a behavior like "wrap every handler in a database transaction" requires either a filter or explicit per-handler code in the direct pattern, whereas MediatR could do this in one place.

---

### 6. Performance

The MediatR pattern added one `ISender.Send()` call per request, which involves reflection-based handler resolution and a pipeline traversal. For a typical CRUD API this overhead is negligible (microseconds), but it is nonzero.

Direct static handler invocation has no dispatch overhead. Parameters are bound by the framework's parameter binder once; the method is called directly.

**Winner: TypedResults** on paper, irrelevant in practice for this workload.

---

### 7. OpenAPI / Swagger Integration

The `.Produces<T>()` annotations in the MediatR version were:
- Manually written and could drift from actual handler behavior.
- Duplicated knowledge that was already in the `Result<T>` return type.
- Verbose (3–5 extra lines per endpoint).

With `Results<T1, T2, ...>`, ASP.NET Core's OpenAPI generator reads the union type directly. Adding a new response branch automatically updates the spec. Removing a branch removes it from the spec. **The documentation cannot lie about the handler unless the type signature itself is wrong.**

---

## Summary Verdict

| Criterion | MediatR + Ardalis.Result | TypedResults | Winner |
|---|---|---|---|
| Compile-time correctness | ✗ (runtime `IResult`) | ✓ (union types) | TypedResults |
| OpenAPI accuracy | ✗ (manual, can drift) | ✓ (inferred from types) | TypedResults |
| Package footprint | Heavy (4+ extra packages) | Minimal (framework-only) | TypedResults |
| Files per feature | More (3–4) | Fewer (2) | TypedResults |
| Onboarding clarity | Medium (need MediatR knowledge) | High (plain C# methods) | TypedResults |
| Handler unit testability | ✓ (easy) | ✗ (private static, integration test only) | MediatR |
| Cross-cutting behaviors | ✓ (IPipelineBehavior) | Partial (filters + middleware) | MediatR |
| CQRS separation | ✓ (explicit Request/Handler split) | ✗ (mixed in one class) | MediatR |
| Runtime performance | Marginal overhead | None | TypedResults |

### When to choose TypedResults (direct handlers)
- Small to medium projects where integration tests are the norm.
- Teams that want HTTP response correctness enforced at compile time.
- Projects where OpenAPI spec accuracy is a priority.
- When the number of cross-cutting behaviors is small and covered by middleware/filters.

### When to keep MediatR
- Large projects with many developers where CQRS separation is an organizational boundary.
- When pipeline behaviors provide non-trivial uniform logic (transactions, distributed tracing, auditing).
- When handler-level unit tests are a hard requirement and spinning up `WebApplicationFactory` is too heavy.

### This project's context
The direct-handler pattern is the right fit here. The project is a single-owner API, the test suite uses `WebApplicationFactory` throughout, cross-cutting concerns (auth, rate limiting, user existence) are cleanly handled by middleware and filters, and the reduction in package dependencies + compile-time response type safety are concrete, observable improvements over the previous pattern.

The one remaining use of `Ardalis.Result` + `.ToMinimalApiResult()` in `MapAuthentication.cs` is intentional: the `IAuthenticationService` returns `Result<T>` because it is consumed by code outside the HTTP pipeline (services that don't know about `TypedResults`). That boundary is correct.
