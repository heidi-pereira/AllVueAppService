using System.Collections.Generic;
using System.Linq;
using MIG.SurveyPlatform.MapGeneration.Serialization;

namespace MIG.SurveyPlatform.MapGeneration.Model
{
    internal class DashPage
    {
        public IReadOnlyCollection<DashPane> Panes { get; }
        public IReadOnlyCollection<DashPage> Pages { get; }

        public DashPage(IEnumerable<DashPane> panes, IEnumerable<DashPage> pages)
        {
            Panes = panes.ToList();
            Pages = pages.ToList();
        }

        public string Name { get; set; }
        public string MenuIcon { get; set; }
        public string PageType { get; set; }
        public string HelpText { get; set; }
        public string MinUserLevel { get; set; }
        public string StartPage { get; set; }
        public string Layout { get; set; }
        public string PageTitle { get; set; }
        public string Disabled { get; set; }
        public string Subset { get; set; }
        public string Environment { get; set; }
        public string Roles { get; set; }

        public IEnumerable<DashPage> OrderedDescendants
        {
            get
            {
                return Pages.OrderBy(x => x.Name, NaturalStringComparer.Instance)
                    .SelectMany(p => p.ThisAndDescendantsOrdered);
            }
        }

        private IEnumerable<DashPage> ThisAndDescendantsOrdered => new[] {this}.Concat(OrderedDescendants);
    }
}