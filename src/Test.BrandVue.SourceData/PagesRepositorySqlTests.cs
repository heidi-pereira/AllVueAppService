using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData;

public class PagesRepositorySqlTests
{
    private readonly IProductContext _productContext = Substitute.For<IProductContext>();
    private readonly ISubsetRepository _subsets = new FallbackSubsetRepository();
    private readonly IPanesRepository _panesRepository = Substitute.For<IPanesRepository>();
    
    [Test]
    public void PagesShouldBeOrderedByIndexThenId()
    {
        // We don't ever want to order alphabetically
        var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
        var pagesRepositorySql = new PagesRepositorySql(_productContext, dbContextFactory, _panesRepository);

        using var ctx = dbContextFactory.CreateDbContext();
        ctx.Pages.RemoveRange(ctx.Pages);
        ctx.SaveChanges();

        var dbPages = new []
        {
            GetPopulatedDbPage(2, "Parent1"),
            GetPopulatedDbPage(3, "C_Child1", 2, 3),
            GetPopulatedDbPage(4, "A_Child1", 2, 2),
            GetPopulatedDbPage(5, "Z_Child1", 2, 1),
            GetPopulatedDbPage(6, "B_Child1", 2, 4),
            GetPopulatedDbPage(7, "Parent2"),
            GetPopulatedDbPage(70, "A_Child2", 7),
            GetPopulatedDbPage(20, "B_Child2", 7),
            GetPopulatedDbPage(80, "C_Child2", 7),
            GetPopulatedDbPage(1, "Parent3"),
        };
        ctx.AddRange(dbPages);
        ctx.SaveChanges();

        var pages = pagesRepositorySql.GetTopLevelPagesWithChildPages().ToList();
        Assert.Multiple(() =>
        {
            Assert.That(pages.Count, Is.EqualTo(3));

            Assert.That(pages[0].Name, Is.EqualTo("Parent1"));
            Assert.That(pages[0].ChildPages.Count, Is.EqualTo(4));
            Assert.That(pages[0].ChildPages[0].Name, Is.EqualTo("Z_Child1"));
            Assert.That(pages[0].ChildPages[1].Name, Is.EqualTo("A_Child1"));
            Assert.That(pages[0].ChildPages[2].Name, Is.EqualTo("C_Child1"));
            Assert.That(pages[0].ChildPages[3].Name, Is.EqualTo("B_Child1"));

            Assert.That(pages[1].Name, Is.EqualTo("Parent2"));
            Assert.That(pages[1].ChildPages.Count, Is.EqualTo(3));
            Assert.That(pages[1].ChildPages[0].Name, Is.EqualTo("B_Child2"));
            Assert.That(pages[1].ChildPages[1].Name, Is.EqualTo("A_Child2"));
            Assert.That(pages[1].ChildPages[2].Name, Is.EqualTo("C_Child2"));

            Assert.That(pages[2].Name, Is.EqualTo("Parent3"));
            Assert.That(pages[2].ChildPages.Count, Is.EqualTo(0));
        });
    }

    private DbPage GetPopulatedDbPage(int id, string name, int? parentId = null, int? pageDisplayIndex = null) => new()
    {
        Id = id, 
        Name = name, 
        ParentId = parentId,
        PageDisplayIndex = pageDisplayIndex,
        ProductShortCode = _productContext.ShortCode,
        SubProductId = _productContext.SubProductId
    };
}