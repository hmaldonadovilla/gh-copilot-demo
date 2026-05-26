using albums_api.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace albums_api.Tests;

public class UnsecuredControllerTests
{
    [Fact]
    public void ReadFile_ReturnsContent_WhenPathIsInsideAllowedBasePath()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var filePath = Path.Combine(tempRoot, "allowed.txt");
        File.WriteAllText(filePath, "safe-content");

        try
        {
            var controller = CreateController(tempRoot);

            var content = controller.ReadFile("allowed.txt");

            Assert.Equal("safe-content", content);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void ReadFile_Throws_WhenPathEscapesAllowedBasePath()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var controller = CreateController(tempRoot);

            Assert.Throws<UnauthorizedAccessException>(() => controller.ReadFile("../outside.txt"));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void ReadFile_Throws_WhenPathTargetsSiblingDirectoryWithMatchingPrefix()
    {
        var parentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var allowedRoot = Path.Combine(parentRoot, "base");
        var siblingRoot = Path.Combine(parentRoot, "base2");

        Directory.CreateDirectory(allowedRoot);
        Directory.CreateDirectory(siblingRoot);
        File.WriteAllText(Path.Combine(siblingRoot, "secret.txt"), "should-not-be-readable");

        try
        {
            var controller = CreateController(allowedRoot);

            Assert.Throws<UnauthorizedAccessException>(() => controller.ReadFile("../base2/secret.txt"));
        }
        finally
        {
            Directory.Delete(parentRoot, true);
        }
    }

    [Fact]
    public void ReadFile_AllowsRelativeFilenameThatStartsWithDots_WhenInsideBasePath()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var filePath = Path.Combine(tempRoot, "..safe.txt");
        File.WriteAllText(filePath, "dot-prefix-file");

        try
        {
            var controller = CreateController(tempRoot);

            var content = controller.ReadFile("..safe.txt");

            Assert.Equal("dot-prefix-file", content);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    private static UnsecuredController CreateController(string contentRoot)
    {
        var settings = new Dictionary<string, string?>
        {
            ["FileAccess:BasePath"] = "."
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = contentRoot
        };

        return new UnsecuredController(configuration, environment, NullLogger<UnsecuredController>.Instance);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "albums-api-tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
