using System.Drawing;

namespace BrandVue.Services
{
    public static class ColourExtensions
    {
        public static Color Lighten(this Color colour, double percentage) => ChangeColourBrightness(colour, percentage);

        public static Color Darken(this Color colour, double percentage) => ChangeColourBrightness(colour, -1 * percentage);

        //Formula from https://www.w3.org/WAI/GL/wiki/Contrast_ratio - simplifies to approx 0.179
        public static Color ContrastingColour(this Color colour) => colour.RelativeLuminance() > 0.179 ? Color.Black : Color.White;

        public static Color GrayScale(this Color colour)
        {
            var luminance = RelativeLuminance(colour);
            var rgb = (int)Math.Round(luminance * 255);
            return Color.FromArgb(255, rgb, rgb, rgb);
        }

        public static Color Invert(this Color colour) => Color.FromArgb(colour.A, 255 - colour.R, 255 - colour.G, 255 - colour.B);

        public static Color PickHigherContrast(this Color colour, params Color[] comparisons) =>
            comparisons.Select(c => (Colour: c, Contrast: Contrast(colour, c)))
                .OrderByDescending(c => c.Contrast)
                .First().Colour;

        private static double Contrast(Color a, Color b)
        {
            var luminanceA = a.RelativeLuminance();
            var luminanceB = b.RelativeLuminance();
            var min = Math.Min(luminanceA, luminanceB);
            var max = Math.Max(luminanceA, luminanceB);
            return (max + 0.05) / (min + 0.05);
        }

        private static double RelativeLuminance(this Color colour)
        {
            //https://www.w3.org/WAI/GL/wiki/Relative_luminance
            var LinearValue = (double colourChannel) =>
            {
                var decimalValue = colourChannel / 255.0;
                if (decimalValue <= 0.04045)
                {
                    return decimalValue / 12.92;
                }
                else
                {
                    return Math.Pow((decimalValue + 0.055) / 1.055, 2.4);
                }
            };

            double red = LinearValue(colour.R);
            double green = LinearValue(colour.G);
            double blue = LinearValue(colour.B);
            return (0.2126 * red) + (0.7152 * green) + (0.0722 * blue);
        }

        private static Color ChangeColourBrightness(Color colour, double percentage)
        {
            //https://stackoverflow.com/a/12598573
            if (percentage < -1 || percentage > 1)
            {
                throw new ArgumentException("Percentage to modify colour brightness should be between -1 and 1");
            }

            double red = colour.R;
            double green = colour.G;
            double blue = colour.B;

            if (percentage < 0)
            {
                percentage = 1 + percentage;
                red *= percentage;
                green *= percentage;
                blue *= percentage;
            }
            else
            {
                red = (255 - red) * percentage + red;
                green = (255 - green) * percentage + green;
                blue = (255 - blue) * percentage + blue;
            }

            return Color.FromArgb(colour.A, (int)red, (int)green, (int)blue);
        }
    }
}
