using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrandVue.EntityFramework.MetaData
{
    public interface IPageAboutRepository
    {
        IEnumerable<PageAbout> GetAllForPage(int PageId);
        PageAbout Get(int id);
        void Create(PageAbout PageAbout);
        void Update(PageAbout PageAbout);
        void UpdateList(PageAbout[] PageAbouts);
        void Delete(PageAbout PageAbout);
    }
}
