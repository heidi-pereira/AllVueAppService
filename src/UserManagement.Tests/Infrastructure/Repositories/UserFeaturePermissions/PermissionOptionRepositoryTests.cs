namespace UserManagement.Tests.Infrastructure.Repositories.UserFeaturePermissions
{
    public class PermissionOptionRepositoryTests
    {
        private MetaDataContext? _context;
        private PermissionOptionRepository? _repository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<MetaDataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MetaDataContext(options);

            // Seed data
            _context.PermissionOptions.AddRange(
                new PermissionOption { Id = 1, Name = "Option1", Feature = new PermissionFeature { Id = 1, Name = "Feature1" } },
                new PermissionOption { Id = 2, Name = "Option2", Feature = new PermissionFeature { Id = 2, Name = "Feature2" } },
                new PermissionOption { Id = 3, Name = "Option3", Feature = new PermissionFeature { Id = 3, Name = "Feature3" } }
            );
            _context.SaveChanges();

            _repository = new PermissionOptionRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context!.Database.EnsureDeleted();
            _context.Dispose();
            _context = null;
        }

        [Test]
        public async Task GetAllAsync_ReturnsAllPermissionOptions()
        {
            var result = await _repository.GetAllAsync(CancellationToken.None);
            Assert.That(result.Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task GetAllByIdsAsync_ReturnsCorrectPermissionOptions()
        {
            var ids = new List<int> { 1, 3 };
            var result = await _repository.GetAllByIdsAsync(ids, CancellationToken.None);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Any(po => po.Id == 1), Is.True);
            Assert.That(result.Any(po => po.Id == 3), Is.True);
        }
    }
}