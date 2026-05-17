namespace StorageLabelsApi.Tests.TestInfrastructure;

[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationDatabaseFixture>;
