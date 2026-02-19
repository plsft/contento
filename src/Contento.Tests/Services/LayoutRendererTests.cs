using System.Data;
using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="LayoutRenderer"/>. The service depends on <see cref="IDbConnection"/>,
/// <see cref="ILayoutService"/>, <see cref="IMarkdownService"/>, and <see cref="ILogger{LayoutRenderer}"/>.
/// All four constructor parameters use Guard.Against.Null. Since Tuxedo extension methods on
/// IDbConnection are difficult to mock, tests focus on:
///   - Constructor guard clauses
///   - BuildRenderContextAsync behavior when no layout is found
///   - LayoutRenderContext default values
/// </summary>
[TestFixture]
public class LayoutRendererTests
{
    private Mock<IDbConnection> _mockDb = null!;
    private Mock<ILayoutService> _mockLayoutService = null!;
    private Mock<IMarkdownService> _mockMarkdown = null!;
    private LayoutRenderer _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new Mock<IDbConnection>();
        _mockLayoutService = new Mock<ILayoutService>();
        _mockMarkdown = new Mock<IMarkdownService>();
        _service = new LayoutRenderer(
            _mockDb.Object,
            _mockLayoutService.Object,
            _mockMarkdown.Object,
            Mock.Of<ILogger<LayoutRenderer>>());
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullDbConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new LayoutRenderer(
                null!,
                _mockLayoutService.Object,
                _mockMarkdown.Object,
                Mock.Of<ILogger<LayoutRenderer>>()));
    }

    [Test]
    public void Constructor_NullLayoutService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new LayoutRenderer(
                _mockDb.Object,
                null!,
                _mockMarkdown.Object,
                Mock.Of<ILogger<LayoutRenderer>>()));
    }

    [Test]
    public void Constructor_NullMarkdownService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new LayoutRenderer(
                _mockDb.Object,
                _mockLayoutService.Object,
                null!,
                Mock.Of<ILogger<LayoutRenderer>>()));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new LayoutRenderer(
                _mockDb.Object,
                _mockLayoutService.Object,
                _mockMarkdown.Object,
                null!));
    }

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new LayoutRenderer(
                _mockDb.Object,
                _mockLayoutService.Object,
                _mockMarkdown.Object,
                Mock.Of<ILogger<LayoutRenderer>>()));
    }

    // ---------------------------------------------------------------
    // BuildRenderContextAsync — no layout found
    // ---------------------------------------------------------------

    [Test]
    public async Task BuildRenderContextAsync_NoLayoutFound_ReturnsContextWithHasLayoutFalse()
    {
        _mockLayoutService
            .Setup(x => x.GetDefaultAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Core.Models.Layout?)null);

        var result = await _service.BuildRenderContextAsync(Guid.NewGuid());

        Assert.That(result.HasLayout, Is.False);
    }

    // ---------------------------------------------------------------
    // LayoutRenderContext defaults
    // ---------------------------------------------------------------

    [Test]
    public void LayoutRenderContext_MaxWidth_DefaultsToNull()
    {
        var context = new LayoutRenderContext();

        Assert.That(context.MaxWidth, Is.Null);
    }

    [Test]
    public void LayoutRenderContext_Gap_DefaultsToNull()
    {
        var context = new LayoutRenderContext();

        Assert.That(context.Gap, Is.Null);
    }

    [Test]
    public void LayoutRenderContext_HasLayout_DefaultsToFalse()
    {
        var context = new LayoutRenderContext();

        Assert.That(context.HasLayout, Is.False);
    }

    [Test]
    public void LayoutRenderContext_StructureJson_DefaultsToNull()
    {
        var context = new LayoutRenderContext();

        Assert.That(context.StructureJson, Is.Null);
    }

    [Test]
    public void LayoutRenderContext_RegionContent_DefaultsToEmptyDictionary()
    {
        var context = new LayoutRenderContext();

        Assert.That(context.RegionContent, Is.Not.Null);
        Assert.That(context.RegionContent, Is.Empty);
    }
}
