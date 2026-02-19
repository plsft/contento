using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Contento.Web.BackgroundServices;

namespace Contento.Tests.BackgroundServices;

/// <summary>
/// Tests for <see cref="TaskSchedulerBackgroundService"/>. The service is a <see cref="BackgroundService"/>
/// that polls every 30 seconds for due scheduled tasks and executes them.
/// </summary>
[TestFixture]
public class TaskSchedulerBackgroundServiceTests
{
    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        Assert.DoesNotThrow(() => new TaskSchedulerBackgroundService(
            mockScopeFactory.Object,
            Mock.Of<ILogger<TaskSchedulerBackgroundService>>()));
    }

    [Test]
    public void Service_ImplementsBackgroundService()
    {
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var service = new TaskSchedulerBackgroundService(
            mockScopeFactory.Object,
            Mock.Of<ILogger<TaskSchedulerBackgroundService>>());
        Assert.That(service, Is.InstanceOf<Microsoft.Extensions.Hosting.BackgroundService>());
    }

    [Test]
    public async Task StartAsync_CanBeCancelled()
    {
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScope = new Mock<IServiceScope>();
        var mockProvider = new Mock<IServiceProvider>();
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockProvider.Object);

        var service = new TaskSchedulerBackgroundService(
            mockScopeFactory.Object,
            Mock.Of<ILogger<TaskSchedulerBackgroundService>>());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        Assert.Pass("Service started and stopped gracefully");
    }

    [Test]
    public async Task StopAsync_CompletesWithoutError()
    {
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScope = new Mock<IServiceScope>();
        var mockProvider = new Mock<IServiceProvider>();
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockProvider.Object);

        var service = new TaskSchedulerBackgroundService(
            mockScopeFactory.Object,
            Mock.Of<ILogger<TaskSchedulerBackgroundService>>());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await service.StartAsync(cts.Token);
        await Task.Delay(100);

        Assert.DoesNotThrowAsync(async () => await service.StopAsync(CancellationToken.None));
    }
}
