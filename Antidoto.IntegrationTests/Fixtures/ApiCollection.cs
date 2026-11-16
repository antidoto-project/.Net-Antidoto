namespace Antidoto.IntegrationTests.Fixtures;

/// <summary>Todas as classes da collection "Api" compartilham a mesma API em memória.</summary>
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<AntidotoApiFactory> { }
