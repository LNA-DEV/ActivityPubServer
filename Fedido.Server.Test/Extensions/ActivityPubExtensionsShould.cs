using Fedido.Server.Extensions;
using Shouldly;
using Xunit;

namespace Fedido.Server.Test.Extensions;

public class ActivityPubExtensionsShould
{
    [Theory]
    [InlineData("https://lna-dev.net")]
    [InlineData("http://lna-dev.net")]
    [InlineData("https://lna-dev.net/posts/cool-post/")]
    public void ExtractServerName(string url)
    {
        // Arrange

        // Act 
        var extractedUrl = url.ExtractServerName();

        // Assert
        extractedUrl.ShouldNotContain("http");
        extractedUrl.ShouldNotContain("/");
    }
}