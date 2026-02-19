using NUnit.Framework;
using Moq;
using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="FileStorageService"/>. The service depends on
/// <see cref="SharpGrip.FileSystem.IFileSystem"/>, <see cref="IConfiguration"/>,
/// and <see cref="ILogger{T}"/>. Note that the constructor does NOT use Guard.Against.Null,
/// so null parameters will not throw during construction. These tests focus on:
///   - Verifying the service can be instantiated with valid dependencies
///   - GetPublicUrl behavior for local vs S3 storage prefixes
///   - File path prefixing logic
///
/// Methods that exercise the SharpGrip FileSystem would require an integration test setup.
/// </summary>
[TestFixture]
public class FileStorageServiceTests
{
    private Mock<SharpGrip.FileSystem.IFileSystem> _mockFileSystem = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockFileSystem = new Mock<SharpGrip.FileSystem.IFileSystem>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Storage:Provider"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Storage:S3:Endpoint"]).Returns((string?)null);
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor — valid dependencies
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new FileStorageService(
                _mockFileSystem.Object,
                _mockConfiguration.Object,
                Mock.Of<ILogger<FileStorageService>>()));
    }

    [Test]
    public void Constructor_LocalProvider_DoesNotThrow()
    {
        _mockConfiguration.Setup(c => c["Storage:Provider"]).Returns("local");

        Assert.DoesNotThrow(
            () => new FileStorageService(
                _mockFileSystem.Object,
                _mockConfiguration.Object,
                Mock.Of<ILogger<FileStorageService>>()));
    }

    [Test]
    public void Constructor_S3Provider_DoesNotThrow()
    {
        _mockConfiguration.Setup(c => c["Storage:Provider"]).Returns("s3");
        _mockConfiguration.Setup(c => c["Storage:S3:Endpoint"]).Returns("https://s3.example.com");

        Assert.DoesNotThrow(
            () => new FileStorageService(
                _mockFileSystem.Object,
                _mockConfiguration.Object,
                Mock.Of<ILogger<FileStorageService>>()));
    }

    // ---------------------------------------------------------------
    // GetPublicUrl — local storage (default when S3 not configured)
    // ---------------------------------------------------------------

    [Test]
    public void GetPublicUrl_LocalProvider_ReturnsUploadsPath()
    {
        var service = new FileStorageService(
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            Mock.Of<ILogger<FileStorageService>>());

        var url = service.GetPublicUrl("images/photo.jpg");

        Assert.That(url, Is.EqualTo("/uploads/images/photo.jpg"));
    }

    [Test]
    public void GetPublicUrl_LocalProvider_SimpleFilename_ReturnsUploadsPath()
    {
        var service = new FileStorageService(
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            Mock.Of<ILogger<FileStorageService>>());

        var url = service.GetPublicUrl("avatar.png");

        Assert.That(url, Is.EqualTo("/uploads/avatar.png"));
    }

    [Test]
    public void GetPublicUrl_ProviderNullEndpointNull_FallsBackToLocal()
    {
        // Both Provider and S3:Endpoint are null — should default to local prefix
        _mockConfiguration.Setup(c => c["Storage:Provider"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Storage:S3:Endpoint"]).Returns((string?)null);

        var service = new FileStorageService(
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            Mock.Of<ILogger<FileStorageService>>());

        var url = service.GetPublicUrl("docs/file.pdf");

        Assert.That(url, Is.EqualTo("/uploads/docs/file.pdf"));
    }

    // ---------------------------------------------------------------
    // GetPublicUrl — S3 storage
    // ---------------------------------------------------------------

    [Test]
    public void GetPublicUrl_S3Provider_ReturnsS3PublicUrl()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Storage:Provider"]).Returns("s3");
        mockConfig.Setup(c => c["Storage:S3:Endpoint"]).Returns("https://s3.example.com");
        mockConfig.Setup(c => c["Storage:S3:PublicUrl"]).Returns("https://cdn.example.com");

        var service = new FileStorageService(
            _mockFileSystem.Object,
            mockConfig.Object,
            Mock.Of<ILogger<FileStorageService>>());

        var url = service.GetPublicUrl("images/photo.jpg");

        Assert.That(url, Is.EqualTo("https://cdn.example.com/images/photo.jpg"));
    }

    [Test]
    public void GetPublicUrl_S3Provider_TrailingSlashInPublicUrl_NormalizesPath()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Storage:Provider"]).Returns("s3");
        mockConfig.Setup(c => c["Storage:S3:Endpoint"]).Returns("https://s3.example.com");
        mockConfig.Setup(c => c["Storage:S3:PublicUrl"]).Returns("https://cdn.example.com/");

        var service = new FileStorageService(
            _mockFileSystem.Object,
            mockConfig.Object,
            Mock.Of<ILogger<FileStorageService>>());

        var url = service.GetPublicUrl("images/photo.jpg");

        Assert.That(url, Is.EqualTo("https://cdn.example.com/images/photo.jpg"));
    }

    [Test]
    public void GetPublicUrl_S3Provider_NoPublicUrl_FallsBackToUploads()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Storage:Provider"]).Returns("s3");
        mockConfig.Setup(c => c["Storage:S3:Endpoint"]).Returns("https://s3.example.com");
        mockConfig.Setup(c => c["Storage:S3:PublicUrl"]).Returns((string?)null);

        var service = new FileStorageService(
            _mockFileSystem.Object,
            mockConfig.Object,
            Mock.Of<ILogger<FileStorageService>>());

        var url = service.GetPublicUrl("images/photo.jpg");

        Assert.That(url, Is.EqualTo("/uploads/images/photo.jpg"));
    }

    [Test]
    public void GetPublicUrl_S3Provider_PublicUrlStartsWithPlaceholder_FallsBackToUploads()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Storage:Provider"]).Returns("s3");
        mockConfig.Setup(c => c["Storage:S3:Endpoint"]).Returns("https://s3.example.com");
        mockConfig.Setup(c => c["Storage:S3:PublicUrl"]).Returns("${S3_PUBLIC_URL}");

        var service = new FileStorageService(
            _mockFileSystem.Object,
            mockConfig.Object,
            Mock.Of<ILogger<FileStorageService>>());

        var url = service.GetPublicUrl("images/photo.jpg");

        Assert.That(url, Is.EqualTo("/uploads/images/photo.jpg"));
    }

    // ---------------------------------------------------------------
    // Prefix — S3 endpoint with placeholder falls back to local
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_S3EndpointStartsWithPlaceholder_FallsBackToLocalPrefix()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Storage:Provider"]).Returns("s3");
        mockConfig.Setup(c => c["Storage:S3:Endpoint"]).Returns("${S3_ENDPOINT}");

        var service = new FileStorageService(
            _mockFileSystem.Object,
            mockConfig.Object,
            Mock.Of<ILogger<FileStorageService>>());

        // When endpoint starts with "${", the prefix falls back to "local"
        var url = service.GetPublicUrl("test.jpg");
        Assert.That(url, Is.EqualTo("/uploads/test.jpg"));
    }

    // ---------------------------------------------------------------
    // Bogus-generated data for fuzz-style validation
    // ---------------------------------------------------------------

    [Test]
    public void GetPublicUrl_BogusPath_ReturnsCorrectLocalPath()
    {
        var service = new FileStorageService(
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            Mock.Of<ILogger<FileStorageService>>());

        var path = $"{_faker.System.DirectoryPath().TrimStart('/')}/{_faker.System.FileName()}";

        var url = service.GetPublicUrl(path);

        Assert.That(url, Does.StartWith("/uploads/"));
        Assert.That(url, Does.EndWith(path));
    }
}
