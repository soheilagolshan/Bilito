namespace Bilito.IntegrationTests;

public sealed class ApiSmokeTests
{
    [Fact]
    public void IntegrationTestProjectIsReadyForApiHostTests()
    {
        Assert.NotNull(typeof(Program).Assembly);
    }
}
