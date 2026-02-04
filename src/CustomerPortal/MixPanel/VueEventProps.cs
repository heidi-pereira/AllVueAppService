namespace CustomerPortal.MixPanel
{
    public class VueEventProps
    {
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Tag { get; set; }

        public VueEventProps(string category, string subCategory, string tag)
        {
            Category = category;
            SubCategory = subCategory;
            Tag = tag;
        }
    }
}