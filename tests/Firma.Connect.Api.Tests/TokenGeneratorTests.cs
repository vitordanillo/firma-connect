using Firma.Connect.Api.Security;

namespace Firma.Connect.Api.Tests;

public class TokenGeneratorTests
{
    [Fact]
    public void Generated_tokens_are_unique_and_only_hash_is_persisted()
    {
        var first = TokenGenerator.CreateSecureToken();
        var second = TokenGenerator.CreateSecureToken();

        Assert.NotEqual(first, second);
        Assert.Equal(64, TokenGenerator.Hash(first).Length);
        Assert.NotEqual(first, TokenGenerator.Hash(first));
    }
}
