namespace Bilito.UnitTests;

public sealed class FoundationTests
{
    [Fact]
    public void IdentityContractsAreSeparateFromPersistenceModels()
    {
        Assert.Equal("Bilito.Identity.Contracts", typeof(Bilito.Identity.Contracts.Users.CurrentUserResponse).Assembly.GetName().Name);
    }
}
