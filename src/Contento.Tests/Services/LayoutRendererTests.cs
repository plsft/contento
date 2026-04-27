using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Services;

namespace Contento.Tests.Services;

[TestFixture]
public class LayoutRendererTests
{
    private Mock<ILayoutService> _mockLayoutService = null!;
    private Mock<IComponentRendererRegistry> _mockRegistry = null!;
    private Mock<ILogger<LayoutRenderer>> _mockLogger = null!;
    private LayoutRenderer _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLayoutService = new Mock<ILayoutService>();
        _mockRegistry = new Mock<IComponentRendererRegistry>();
        _mockLogger = new Mock<ILogger<LayoutRenderer>>();
        _service = new LayoutRenderer(
            _mockLayoutService.Object,
            _mockRegistry.Object,
            _mockLogger.Object);
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullLayoutService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new LayoutRenderer(
                null!,
                _mockRegistry.Object,
                _mockLogger.Object));
    }

    [Test]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new LayoutRenderer(
                _mockLayoutService.Object,
                null!,
                _mockLogger.Object));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new LayoutRenderer(
                _mockLayoutService.Object,
                _mockRegistry.Object,
                null!));
    }

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new LayoutRenderer(
                _mockLayoutService.Object,
                _mockRegistry.Object,
                _mockLogger.Object));
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
