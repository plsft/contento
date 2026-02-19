using System.Data;
using NUnit.Framework;
using Moq;
using Bogus;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="TaskSchedulerService"/>. The service depends on <see cref="IDbConnection"/>
/// and <see cref="ILogger{TaskSchedulerService}"/>. Since Tuxedo extension methods (QueryAsync,
/// InsertAsync, etc.) on IDbConnection cannot be easily mocked, these tests focus on:
///   - Constructor guard clauses
///   - Method-level argument validation (Guard.Against.*)
///   - Verifying the service can be instantiated with valid dependencies
///
/// Methods that exercise the full database path would require an integration test setup.
/// </summary>
[TestFixture]
public class TaskSchedulerServiceTests
{
    private Mock<IDbConnection> _mockDb = null!;
    private TaskSchedulerService _service = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new Mock<IDbConnection>();
        _service = new TaskSchedulerService(_mockDb.Object, Mock.Of<ILogger<TaskSchedulerService>>());
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullDbConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TaskSchedulerService(null!, Mock.Of<ILogger<TaskSchedulerService>>()));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TaskSchedulerService(_mockDb.Object, null!));
    }

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new TaskSchedulerService(_mockDb.Object, Mock.Of<ILogger<TaskSchedulerService>>()));
    }

    // ---------------------------------------------------------------
    // GetByIdAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetByIdAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByIdAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetAllAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetAllAsync_EmptySiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetAllAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // CreateAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateAsync_NullTask_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CreateAsync(null!));
    }

    [Test]
    public void CreateAsync_NullName_ThrowsArgumentException()
    {
        var task = CreateValidTask();
        task.Name = null!;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(task));
    }

    [Test]
    public void CreateAsync_EmptyName_ThrowsArgumentException()
    {
        var task = CreateValidTask();
        task.Name = "";

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(task));
    }

    [Test]
    public void CreateAsync_NullTaskType_ThrowsArgumentException()
    {
        var task = CreateValidTask();
        task.TaskType = null!;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(task));
    }

    [Test]
    public void CreateAsync_EmptyTaskType_ThrowsArgumentException()
    {
        var task = CreateValidTask();
        task.TaskType = "";

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(task));
    }

    // ---------------------------------------------------------------
    // UpdateAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void UpdateAsync_NullTask_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.UpdateAsync(null!));
    }

    [Test]
    public void UpdateAsync_DefaultTaskId_ThrowsArgumentException()
    {
        var task = CreateValidTask();
        task.Id = Guid.Empty;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdateAsync(task));
    }

    // ---------------------------------------------------------------
    // DeleteAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void DeleteAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.DeleteAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // RunNowAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void RunNowAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.RunNowAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetLogsAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetLogsAsync_EmptyTaskId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetLogsAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // ComputeNextRunAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void ComputeNextRunAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ComputeNextRunAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // ExecuteTaskAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void ExecuteTaskAsync_NullTask_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.ExecuteTaskAsync(null!));
    }

    // ---------------------------------------------------------------
    // Interface implementation
    // ---------------------------------------------------------------

    [Test]
    public void Service_ImplementsITaskSchedulerService()
    {
        Assert.That(_service, Is.InstanceOf<ITaskSchedulerService>());
    }

    // ---------------------------------------------------------------
    // Multiple instances
    // ---------------------------------------------------------------

    [Test]
    public void MultipleInstances_CanBeCreatedIndependently()
    {
        var db1 = new Mock<IDbConnection>();
        var db2 = new Mock<IDbConnection>();

        var service1 = new TaskSchedulerService(db1.Object, Mock.Of<ILogger<TaskSchedulerService>>());
        var service2 = new TaskSchedulerService(db2.Object, Mock.Of<ILogger<TaskSchedulerService>>());

        Assert.That(service1, Is.Not.SameAs(service2));
    }

    // ---------------------------------------------------------------
    // ScheduledTask model defaults
    // ---------------------------------------------------------------

    [Test]
    public void ScheduledTask_DefaultValues_AreCorrect()
    {
        var task = new ScheduledTask();

        Assert.That(task.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(task.Name, Is.EqualTo(string.Empty));
        Assert.That(task.TaskType, Is.EqualTo(string.Empty));
        Assert.That(task.CronExpression, Is.EqualTo(string.Empty));
        Assert.That(task.IsEnabled, Is.True);
        Assert.That(task.Settings, Is.EqualTo("{}"));
        Assert.That(task.LastRunAt, Is.Null);
        Assert.That(task.NextRunAt, Is.Null);
        Assert.That(task.LastResult, Is.Null);
        Assert.That(task.LastError, Is.Null);
    }

    [Test]
    public void ScheduledTaskLog_DefaultValues_AreCorrect()
    {
        var log = new ScheduledTaskLog();

        Assert.That(log.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(log.Status, Is.EqualTo(string.Empty));
        Assert.That(log.Message, Is.Null);
        Assert.That(log.CompletedAt, Is.Null);
        Assert.That(log.DurationMs, Is.EqualTo(0));
    }

    [Test]
    public void ScheduledTask_FieldsAreSettable()
    {
        var task = new ScheduledTask
        {
            Id = Guid.NewGuid(),
            SiteId = Guid.NewGuid(),
            Name = _faker.Lorem.Sentence(3),
            TaskType = "publish_scheduled",
            CronExpression = "*/5 * * * *",
            IsEnabled = false,
            LastRunAt = DateTime.UtcNow.AddHours(-1),
            NextRunAt = DateTime.UtcNow.AddMinutes(5),
            LastResult = "success",
            LastError = null,
            Settings = "{\"retryCount\":3}"
        };

        Assert.That(task.Name, Is.Not.Empty);
        Assert.That(task.TaskType, Is.EqualTo("publish_scheduled"));
        Assert.That(task.CronExpression, Is.EqualTo("*/5 * * * *"));
        Assert.That(task.IsEnabled, Is.False);
        Assert.That(task.LastRunAt, Is.Not.Null);
        Assert.That(task.LastResult, Is.EqualTo("success"));
    }

    // ---------------------------------------------------------------
    // Bogus-generated data for fuzz-style guard validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateAsync_BogusTask_WithValidFields_PassesGuards()
    {
        var task = new ScheduledTask
        {
            SiteId = Guid.NewGuid(),
            Name = _faker.Lorem.Sentence(3),
            TaskType = "cleanup_trash",
            CronExpression = "0 * * * *"
        };

        // The guard clauses should not throw for valid input. The actual InsertAsync
        // will throw because IDbConnection is a mock without Tuxedo extension wiring,
        // so we expect a different exception type.
        var ex = Assert.CatchAsync(async () => await _service.CreateAsync(task));

        // If an exception occurs, it should NOT be ArgumentNullException or ArgumentException
        // (those would indicate a guard failure)
        if (ex != null)
        {
            Assert.That(ex, Is.Not.TypeOf<ArgumentNullException>());
            Assert.That(ex, Is.Not.TypeOf<ArgumentException>());
        }
    }

    [Test]
    public void UpdateAsync_BogusTask_WithValidFields_PassesGuards()
    {
        var task = new ScheduledTask
        {
            Id = Guid.NewGuid(),
            SiteId = Guid.NewGuid(),
            Name = _faker.Lorem.Sentence(3),
            TaskType = "cleanup_spam",
            CronExpression = "30 2 * * *"
        };

        var ex = Assert.CatchAsync(async () => await _service.UpdateAsync(task));

        if (ex != null)
        {
            Assert.That(ex, Is.Not.TypeOf<ArgumentNullException>());
            Assert.That(ex, Is.Not.TypeOf<ArgumentException>());
        }
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private ScheduledTask CreateValidTask()
    {
        return new ScheduledTask
        {
            SiteId = Guid.NewGuid(),
            Name = "Publish Scheduled Posts",
            TaskType = "publish_scheduled",
            CronExpression = "*/5 * * * *"
        };
    }
}
