namespace OpenEnds.BackEnd.Model
{
    public class OpenEndTheme
    {
        public string ThemeText { get; set; }
        public int Count { get; set; }
        public int Score { get; set; }
        public double Percentage { get; set; }
        public int ThemeIndex { get; set; }
        public int ThemeId { get; set; }
        public double ThemeSensitivity { get; set; }
        public int? ParentId { get; set; }
    }
}