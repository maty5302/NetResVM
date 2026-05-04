namespace NetResVM.IntegrationTests;

[CollectionDefinition("Integration DB", DisableParallelization = true)]
public sealed class IntegrationDatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

