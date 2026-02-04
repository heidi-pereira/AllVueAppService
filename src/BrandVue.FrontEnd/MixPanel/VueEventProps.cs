namespace BrandVue.MixPanel
{
    public class VueEventProps(string category, string subCategory, string tag)
    {
        public string Category { get; set; } = category;
        public string SubCategory { get; set; } = subCategory;
        public string Tag { get; set; } = tag;
    }
}