using System.Web;
using BrandVue.EntityFramework;

namespace BrandVue.Models
{
    public class ExportChartModel:ISubsetIdProvider
    {
        public int Height { get; }
        public int Width { get; }
        public string Url { get; }
        public string Name { get; set; }
        public ViewTypeEnum ViewType { get; set; }
        public string[] Metrics { get; set; }

        public string SubsetId
        {
            get
            {
                try
                {
                    // Should be authenticating against the subset requested in the URL
                    return HttpUtility.ParseQueryString(new Uri(Url).Query).Get("Subset");
                }
                catch 
                {
                    return null;
                }
            }
        }

        public ExportChartModel(string url, int width, int height, string name, ViewTypeEnum viewType, string[] metrics)
        {
            Url = url;
            Width = width;
            Height = height;
            Name = name;
            ViewType = viewType;
            Metrics = metrics;
        }
    }
}