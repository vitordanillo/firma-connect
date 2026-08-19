using Firma.Connect.Api.Domain;

namespace Firma.Connect.Api.Tests;

public class ConnectionRequestTests
{
    [Fact]
    public void New_request_starts_pending()
    {
        var request = new ConnectionRequest();
        Assert.Equal(ConnectionStatus.Pending, request.Status);
    }
}
