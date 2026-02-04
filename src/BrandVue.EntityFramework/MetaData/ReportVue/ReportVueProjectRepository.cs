using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrandVue.EntityFramework.MetaData.ReportVue
{

    public interface IReportVueProjectRepository
    {
        public IList<ReportVueProject> GetActiveProjects();
        public ReportVueProjectRelease GetRelease(int id);
        public void Publish(string projectName, string userName, string reason, ReportVueProjectRelease item, string sourcePath, string targetPath, string extraTextDetails);
        public string GetResultsForSpecificQuestionsForActiveProjects();
    }

    public class ReportVueProjectRepository : IReportVueProjectRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;
        private readonly ILogger<ReportVueProjectRepository> _logger;

        public ReportVueProjectRepository(IDbContextFactory<MetaDataContext> dbContextFactory,
            IProductContext productContext,
            ILogger<ReportVueProjectRepository> logger) 
        {
            _dbContextFactory= dbContextFactory;
            _productContext= productContext;
            _logger= logger;
        }

        public IList<ReportVueProject> GetActiveProjects()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return context.ReportVueProjects.
                Where( x=> x.SubProductId == _productContext.SubProductId && x.ProductShortCode == _productContext.ShortCode).Include(x => x.ProjectReleases).ToArray();
        }

        public ReportVueProjectRelease GetRelease(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return context.ReportVueProjectReleases.Include(x=>x.ProjectPages).Where(x=> x.Id == id).FirstOrDefault();
        }


        public void Publish(string projectName, string userName, string reason, ReportVueProjectRelease item, string sourcePath, string targetPath, string extraTextDetails)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var currentProject = context.
                ReportVueProjects.Include(x=> x.ProjectReleases).
                SingleOrDefault(x => x.SubProductId == _productContext.SubProductId && x.ProductShortCode == _productContext.ShortCode && x.Name == projectName);

            if (currentProject == null)
            {
                currentProject = new ReportVueProject(_productContext, projectName);
            }

            item.ReleaseDate = DateTime.UtcNow;
            item.VersionOfRelease = (currentProject.ProjectReleases != null && currentProject.ProjectReleases.Any()) ? 
                        currentProject.ProjectReleases.Max(x => x.VersionOfRelease) + 1 : 1;
            item.ParentProjectId = currentProject.Id;
            item.UserName = userName;
            item.ReasonForRelease = reason;
            item.UniqueFolderName = Guid.NewGuid().ToString("N");
            item.IsActive= true;
            item.ResultsForSpecificQuestions = extraTextDetails;

            var previousActiveVersion = currentProject.ProjectReleases.SingleOrDefault(x => x.IsActive);
            currentProject.ProjectReleases.Add(item);
            if (previousActiveVersion != null)
            {
                previousActiveVersion.IsActive = false;
            }
            context.ReportVueProjects.Update(currentProject);
            context.SaveChanges();
            Directory.Move(sourcePath, Path.Combine(targetPath, item.UniqueFolderName));
        }

        public string GetResultsForSpecificQuestionsForActiveProjects()
        {
            var resultsForSpecificQuestions = new StringBuilder();
            var projects = GetActiveProjects();
            foreach (var project in projects)
            {
                var currentProject = project.ProjectReleases.SingleOrDefault(x => x.IsActive);
                if (currentProject != null)
                {
                    resultsForSpecificQuestions.Append($"*{project.Name}:");
                    resultsForSpecificQuestions.Append(currentProject.ResultsForSpecificQuestions);
                }
            }
            return resultsForSpecificQuestions.ToString();
        }
    }
}
