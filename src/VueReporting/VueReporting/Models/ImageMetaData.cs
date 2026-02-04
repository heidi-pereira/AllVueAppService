namespace VueReporting.Models
{
    public class ImageMetaData
    {
        public string Bookmark { get; set; }
        public string AppBase { get; set; }
        public string AppUrl { get; set; }
        public string Name { get; set; }
        public string ViewType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string[] Metrics { get; set; }
    }
}