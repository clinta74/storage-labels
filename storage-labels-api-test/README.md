# storage-labels-api-test

Test project for the `storage-labels-api`. Contains unit tests and integration tests.

## Test Categories

Tests are tagged with the `Category` trait so they can be run independently:

| Trait | Command | Requires DB |
|---|---|---|
| Unit (no tag) | `dotnet test --filter "Category!=Integration"` | No |
| `Integration` | `dotnet test --filter "Category=Integration"` | Yes — PostgreSQL |
| All | `dotnet test` | Yes — PostgreSQL |

---

## Unit Tests

Unit tests exercise handler logic using in-memory or mocked dependencies. No external services are needed.

```bash
dotnet test --filter "Category!=Integration"
```

Test files live under `Handlers/`, `Services/`, and `Middleware/`.

---

## Integration Tests

Integration tests spin up the full API using `WebApplicationFactory<Program>` and run against a real PostgreSQL database. [Respawn](https://github.com/jbogard/Respawn) resets the database between every test, so each test starts with a clean slate.

### Prerequisites

- Docker (or Docker Desktop) running locally

### Running Locally

```powershell
# 1. Start the isolated test database (port 5433)
docker compose -f docker-compose.test.yml up -d --wait

# 2. Run integration tests
dotnet test --filter "Category=Integration"

# 3. Tear down when done
docker compose -f docker-compose.test.yml down
```

The test database runs on port **5433** so it does not conflict with a development database on the default port 5432.

### Writing Integration Tests

Place test classes in the `Integration/` folder and inherit from `IntegrationTestBase`:

```csharp
public class MyFeatureIntegrationTests(IntegrationDatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task MyEndpoint_WithValidToken_ReturnsOk()
    {
        // Seed a user + location (required by UserExistsFilter on most endpoints)
        var (userId, locationId) = await SeedTestUserWithLocationAsync();

        // Authenticated HTTP client — uses a real JWT signed with the test secret
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/my-endpoint");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
```

`IntegrationTestBase` provides:

| Member | Purpose |
|---|---|
| `Fixture` | The shared `IntegrationDatabaseFixture` (factory + Respawn) |
| `CreateAuthenticatedClient(userId, permissions[])` | `HttpClient` with a Bearer JWT for the given user |
| `CreateDbScope()` | `IServiceScope` for direct EF Core access |
| `SeedTestUserWithLocationAsync(accessLevel)` | Seeds a `User` + `Location` + `UserLocation`; returns `(UserId, LocationId)` |

### Infrastructure Classes

| File | Purpose |
|---|---|
| `TestInfrastructure/IntegrationTestWebAppFactory.cs` | `WebApplicationFactory<Program>` that injects test DB config and runs EF migrations on startup |
| `TestInfrastructure/IntegrationDatabaseFixture.cs` | xUnit collection fixture — owns the factory and the Respawn respawner |
| `TestInfrastructure/IntegrationTestCollection.cs` | Declares the `"Integration"` xUnit collection |
| `TestInfrastructure/IntegrationTestBase.cs` | Abstract base class — DB reset before each test, auth helpers |
| `TestInfrastructure/JwtTokenHelper.cs` | Generates real JWTs matching the app's validation parameters |

### Environment Variables

`IntegrationTestWebAppFactory` reads these environment variables with the following defaults:

| Variable | Default |
|---|---|
| `POSTGRES_HOST` | `localhost` |
| `POSTGRES_PORT` | `5433` |
| `POSTGRES_DATABASE` | `StorageLabelsTest` |
| `POSTGRES_USERNAME` | `storage_test_user` |
| `POSTGRES_PASSWORD` | `storage_test_password` |

Override any of these before running tests if your local setup differs.

### CI/CD

Integration tests run automatically in GitHub Actions via a `postgres:17-alpine` service container. The `POSTGRES_*` environment variables are set on the test step automatically. No manual setup is needed in CI.
