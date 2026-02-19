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
/// Tests for <see cref="MenuService"/>. The service depends on <see cref="IDbConnection"/>
/// and <see cref="ILogger{MenuService}"/>. Since Tuxedo extension methods (QueryAsync,
/// InsertAsync, etc.) on IDbConnection cannot be easily mocked, these tests focus on:
///   - Constructor guard clauses
///   - Method-level argument validation (Guard.Against.*)
///   - Verifying the service can be instantiated with valid dependencies
///   - MenuItemNode tree-building logic (in-memory, no DB)
///   - Model defaults
/// </summary>
[TestFixture]
public class MenuServiceTests
{
    private Mock<IDbConnection> _mockDb = null!;
    private MenuService _service = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new Mock<IDbConnection>();
        _service = new MenuService(_mockDb.Object, Mock.Of<ILogger<MenuService>>());
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullDbConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MenuService(null!, Mock.Of<ILogger<MenuService>>()));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MenuService(_mockDb.Object, null!));
    }

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new MenuService(_mockDb.Object, Mock.Of<ILogger<MenuService>>()));
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
    // GetBySiteAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetBySiteAsync_EmptySiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetBySiteAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetByLocationAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetByLocationAsync_EmptySiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByLocationAsync(Guid.Empty, "header"));
    }

    [Test]
    public void GetByLocationAsync_NullLocation_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByLocationAsync(Guid.NewGuid(), null!));
    }

    [Test]
    public void GetByLocationAsync_EmptyLocation_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByLocationAsync(Guid.NewGuid(), ""));
    }

    [Test]
    public void GetByLocationAsync_WhitespaceLocation_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByLocationAsync(Guid.NewGuid(), "   "));
    }

    // ---------------------------------------------------------------
    // CreateAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateAsync_NullMenu_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CreateAsync(null!));
    }

    [Test]
    public void CreateAsync_NullName_ThrowsArgumentException()
    {
        var menu = CreateValidMenu();
        menu.Name = null!;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(menu));
    }

    [Test]
    public void CreateAsync_EmptyName_ThrowsArgumentException()
    {
        var menu = CreateValidMenu();
        menu.Name = "";

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(menu));
    }

    [Test]
    public void CreateAsync_DefaultSiteId_ThrowsArgumentException()
    {
        var menu = CreateValidMenu();
        menu.SiteId = Guid.Empty;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(menu));
    }

    // ---------------------------------------------------------------
    // UpdateAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void UpdateAsync_NullMenu_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.UpdateAsync(null!));
    }

    [Test]
    public void UpdateAsync_DefaultMenuId_ThrowsArgumentException()
    {
        var menu = CreateValidMenu();
        menu.Id = Guid.Empty;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdateAsync(menu));
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
    // GetItemsAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetItemsAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetItemsAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetItemTreeAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetItemTreeAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetItemTreeAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // AddItemAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void AddItemAsync_NullItem_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.AddItemAsync(null!));
    }

    [Test]
    public void AddItemAsync_NullLabel_ThrowsArgumentException()
    {
        var item = CreateValidMenuItem();
        item.Label = null!;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.AddItemAsync(item));
    }

    [Test]
    public void AddItemAsync_EmptyLabel_ThrowsArgumentException()
    {
        var item = CreateValidMenuItem();
        item.Label = "";

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.AddItemAsync(item));
    }

    [Test]
    public void AddItemAsync_DefaultMenuId_ThrowsArgumentException()
    {
        var item = CreateValidMenuItem();
        item.MenuId = Guid.Empty;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.AddItemAsync(item));
    }

    // ---------------------------------------------------------------
    // UpdateItemAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void UpdateItemAsync_NullItem_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.UpdateItemAsync(null!));
    }

    [Test]
    public void UpdateItemAsync_DefaultItemId_ThrowsArgumentException()
    {
        var item = CreateValidMenuItem();
        item.Id = Guid.Empty;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdateItemAsync(item));
    }

    // ---------------------------------------------------------------
    // RemoveItemAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void RemoveItemAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.RemoveItemAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // ReorderItemsAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void ReorderItemsAsync_EmptyMenuId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ReorderItemsAsync(Guid.Empty, [Guid.NewGuid()]));
    }

    [Test]
    public void ReorderItemsAsync_NullList_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.ReorderItemsAsync(Guid.NewGuid(), null!));
    }

    // ---------------------------------------------------------------
    // Interface implementation
    // ---------------------------------------------------------------

    [Test]
    public void Service_ImplementsIMenuService()
    {
        Assert.That(_service, Is.InstanceOf<IMenuService>());
    }

    // ---------------------------------------------------------------
    // Multiple instances
    // ---------------------------------------------------------------

    [Test]
    public void MultipleInstances_CanBeCreatedIndependently()
    {
        var db1 = new Mock<IDbConnection>();
        var db2 = new Mock<IDbConnection>();

        var service1 = new MenuService(db1.Object, Mock.Of<ILogger<MenuService>>());
        var service2 = new MenuService(db2.Object, Mock.Of<ILogger<MenuService>>());

        Assert.That(service1, Is.Not.SameAs(service2));
    }

    // ---------------------------------------------------------------
    // Menu model defaults
    // ---------------------------------------------------------------

    [Test]
    public void Menu_DefaultValues_AreCorrect()
    {
        var menu = new Menu();

        Assert.That(menu.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(menu.Name, Is.EqualTo(string.Empty));
        Assert.That(menu.Slug, Is.EqualTo(string.Empty));
        Assert.That(menu.Location, Is.EqualTo(string.Empty));
        Assert.That(menu.IsActive, Is.True);
    }

    [Test]
    public void Menu_FieldsAreSettable()
    {
        var menu = new Menu
        {
            Id = Guid.NewGuid(),
            SiteId = Guid.NewGuid(),
            Name = _faker.Commerce.Department(),
            Slug = _faker.Internet.DomainWord(),
            Location = "header",
            IsActive = false
        };

        Assert.That(menu.Name, Is.Not.Empty);
        Assert.That(menu.Slug, Is.Not.Empty);
        Assert.That(menu.Location, Is.EqualTo("header"));
        Assert.That(menu.IsActive, Is.False);
    }

    // ---------------------------------------------------------------
    // MenuItem model defaults
    // ---------------------------------------------------------------

    [Test]
    public void MenuItem_DefaultValues_AreCorrect()
    {
        var item = new MenuItem();

        Assert.That(item.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(item.Label, Is.EqualTo(string.Empty));
        Assert.That(item.LinkType, Is.EqualTo("custom"));
        Assert.That(item.Target, Is.EqualTo("_self"));
        Assert.That(item.SortOrder, Is.EqualTo(0));
        Assert.That(item.IsVisible, Is.True);
        Assert.That(item.ParentId, Is.Null);
        Assert.That(item.LinkId, Is.Null);
        Assert.That(item.Url, Is.Null);
        Assert.That(item.CssClass, Is.Null);
    }

    [Test]
    public void MenuItem_FieldsAreSettable()
    {
        var item = new MenuItem
        {
            Id = Guid.NewGuid(),
            MenuId = Guid.NewGuid(),
            Label = _faker.Lorem.Word(),
            Url = _faker.Internet.Url(),
            LinkType = "post",
            LinkId = Guid.NewGuid(),
            Target = "_blank",
            CssClass = "highlight",
            SortOrder = 3,
            IsVisible = false,
            ParentId = Guid.NewGuid()
        };

        Assert.That(item.Label, Is.Not.Empty);
        Assert.That(item.Url, Is.Not.Empty);
        Assert.That(item.LinkType, Is.EqualTo("post"));
        Assert.That(item.Target, Is.EqualTo("_blank"));
        Assert.That(item.CssClass, Is.EqualTo("highlight"));
        Assert.That(item.SortOrder, Is.EqualTo(3));
        Assert.That(item.IsVisible, Is.False);
        Assert.That(item.ParentId, Is.Not.Null);
    }

    // ---------------------------------------------------------------
    // MenuItemNode defaults
    // ---------------------------------------------------------------

    [Test]
    public void MenuItemNode_DefaultValues_AreCorrect()
    {
        var node = new MenuItemNode();

        Assert.That(node.Label, Is.EqualTo(string.Empty));
        Assert.That(node.Url, Is.EqualTo(string.Empty));
        Assert.That(node.Target, Is.EqualTo("_self"));
        Assert.That(node.CssClass, Is.Null);
        Assert.That(node.Children, Is.Empty);
    }

    [Test]
    public void MenuItemNode_ChildrenCanBeNested()
    {
        var parent = new MenuItemNode
        {
            Id = Guid.NewGuid(),
            Label = "Parent",
            Url = "/parent",
            Children =
            [
                new MenuItemNode
                {
                    Id = Guid.NewGuid(),
                    Label = "Child 1",
                    Url = "/child-1"
                },
                new MenuItemNode
                {
                    Id = Guid.NewGuid(),
                    Label = "Child 2",
                    Url = "/child-2"
                }
            ]
        };

        Assert.That(parent.Children, Has.Count.EqualTo(2));
        Assert.That(parent.Children[0].Label, Is.EqualTo("Child 1"));
        Assert.That(parent.Children[1].Label, Is.EqualTo("Child 2"));
    }

    // ---------------------------------------------------------------
    // Bogus-generated data for fuzz-style guard validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateAsync_BogusMenu_WithValidFields_PassesGuards()
    {
        var menu = new Menu
        {
            SiteId = Guid.NewGuid(),
            Name = _faker.Commerce.Department(),
            Location = "header"
        };

        var ex = Assert.CatchAsync(async () => await _service.CreateAsync(menu));

        if (ex != null)
        {
            Assert.That(ex, Is.Not.TypeOf<ArgumentNullException>());
            Assert.That(ex, Is.Not.TypeOf<ArgumentException>());
        }
    }

    [Test]
    public void AddItemAsync_BogusItem_WithValidFields_PassesGuards()
    {
        var item = new MenuItem
        {
            MenuId = Guid.NewGuid(),
            Label = _faker.Lorem.Word(),
            Url = _faker.Internet.Url()
        };

        var ex = Assert.CatchAsync(async () => await _service.AddItemAsync(item));

        if (ex != null)
        {
            Assert.That(ex, Is.Not.TypeOf<ArgumentNullException>());
            Assert.That(ex, Is.Not.TypeOf<ArgumentException>());
        }
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private Menu CreateValidMenu()
    {
        return new Menu
        {
            SiteId = Guid.NewGuid(),
            Name = "Main Navigation",
            Slug = "main-navigation",
            Location = "header"
        };
    }

    private MenuItem CreateValidMenuItem()
    {
        return new MenuItem
        {
            MenuId = Guid.NewGuid(),
            Label = "Home",
            Url = "/"
        };
    }
}
