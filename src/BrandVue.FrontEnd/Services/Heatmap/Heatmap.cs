using BrandVue.Models;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace BrandVue.Services.Heatmap
{
    public class HeatMap
    {
        private record Coord(int X, int Y);

        private const float Byte_To_Percentage_Scale_Factor = 1.0F / byte.MaxValue;

        public ClickPoint[] ClickPoints { get;}
        public int CircleRadiusPixels { get; }
        public int ImageWidth { get; }
        public int ImageHeight { get; }
        public int Intensity {get; }
        public const int DefaultIntensity = 10;
        public const int DefaultRadiusInPixels = 12;

        private float PercentageIntensity => (byte.MaxValue - Intensity) * Byte_To_Percentage_Scale_Factor;

        private readonly int[] _transparencyColours = [253, 254, 255];

        private ColorMap[] ColorMap
        {
            get
            {
                if (_colourMap == null)
                {
                    _colourMap = HeatmapColourMap.LegacyMap;
                }

                return _colourMap;
            }
        }
        private ColorMap[] _colourMap = null;

        public HeatMap(ClickPoint[] clickPoints, int circleRadiusPixels, int imageWidth, int imageHeight, int intensity)
        {
            ClickPoints = clickPoints.Where(x=>x.IsValid).ToArray();
            CircleRadiusPixels = circleRadiusPixels;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            Intensity = intensity;
        }

        public HeatMap WithRainbowColourMap()
        {
            _colourMap = HeatmapColourMap.RainbowMap;
            return this;
        }

        public HeatMap WithLegacyColourMap()
        {
            _colourMap = HeatmapColourMap.LegacyMap;
            return this;
        }

        public string GetImageAsBase64String()
        {
            return Convert.ToBase64String(GetImageAsBytes());
        }

        private byte[] GetImageAsBytes()
        {
            return BitmapToBytes(GetImage());
        }

        public Bitmap GetImage()
        {
            var bitmap = CreateIntensityMask();
            bitmap = Colorize(bitmap);
            return bitmap;
        }

        public string GetKeyImageAsBase64String()
        {
            return Convert.ToBase64String(BitmapToBytes(GetKeyImage()));
        }

        public Bitmap GetKeyImage()
        {
            var bitmap = new Bitmap(256 - _transparencyColours.Length, 20);
            using (var surface = Graphics.FromImage(bitmap))
            {
                surface.Clear(Color.Transparent);
                var x = bitmap.Width - 1;
                for (var idx = 0; idx < ColorMap.Length; idx++)
                {
                    if (_transparencyColours.Contains(idx))
                    {
                        continue;
                    }
                    var color = ColorMap[idx].NewColor;
                    surface.FillRectangle(new SolidBrush(color), x--, 0, 1, 20);
                }
            }
            return bitmap;
        }

        private byte[] BitmapToBytes(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private Coord CoordFromClickPoint(ClickPoint clickPoint) =>
            new Coord((int)Math.Round(clickPoint.XPercent / 100 * ImageWidth, 0, MidpointRounding.AwayFromZero),
                (int)(clickPoint.YPercent / 100 * ImageHeight));

        private Bitmap CreateIntensityMask()
        {
            var bSurface = new Bitmap(ImageWidth, ImageHeight);
            using (var drawSurface = Graphics.FromImage(bSurface))
            {
                drawSurface.Clear(Color.White);
                foreach (var dataPoint in ClickPoints.Select(CoordFromClickPoint))
                {
                    DrawHeatPoint(drawSurface, dataPoint, CircleRadiusPixels);
                }
            }
            return bSurface;
        }

        private void DrawHeatPoint(Graphics graphics, Coord heatPoint, int radius)
        {
            if (radius <= 0)
            {
                return;
            }
            var circumferencePointList = new List<Point>();

            // Loop through all angles of a circle
            // Define loop variable as a double to prevent casting in each iteration
            // Iterate through loop on 10 degree deltas, this can change to improve performance
            for (double i = 0; i <= 360; i += 1)
            {
                var circumferencePoint = new Point
                {
                    X = Convert.ToInt32(heatPoint.X + radius * Math.Cos(ConvertDegreesToRadians(i))),
                    Y = Convert.ToInt32(heatPoint.Y + radius * Math.Sin(ConvertDegreesToRadians(i)))
                };
                circumferencePointList.Add(circumferencePoint);
            }
            var circumferencePoints = circumferencePointList.ToArray();
            var gradientShaper = new PathGradientBrush(circumferencePoints);
            var gradientSpecifications = new ColorBlend(3);

            // Define positions of gradient colors, use intesity to adjust the middle color to
            // show more mask or less mask
            gradientSpecifications.Positions = new float[3] { 0, PercentageIntensity, 1 };
            // Define gradient colors and their alpha values, adjust alpha of gradient colors to match intensity
            gradientSpecifications.Colors = new Color[3] { Color.FromArgb(0, Color.White), Color.FromArgb(Intensity, Color.Black), Color.FromArgb(Intensity, Color.Black) };

            // Pass off color blend to PathGradientBrush to instruct it how to generate the gradient
            gradientShaper.InterpolationColors = gradientSpecifications;

            // Draw polygon (circle) using our point array and gradient brush
            graphics.FillPolygon(gradientShaper, circumferencePoints);
        }

        private double ConvertDegreesToRadians(double degrees)
        {
            return Math.PI / 180 * degrees;
        }

        private Bitmap Colorize(Bitmap mask)
        {
            // Create new bitmap to act as a work surface for the colorization process
            var output = new Bitmap(mask.Width, mask.Height, PixelFormat.Format32bppArgb);

            // Create a graphics object from our memory bitmap so we can draw on it and clear it's drawing surface
            using (var surface = Graphics.FromImage(output))
            {
                surface.Clear(Color.Transparent);

                // Create new image attributes class to handle the color remappings
                // Inject our color map array to instruct the image attributes class how to do the colorization
                using var imageAttributes = new ImageAttributes();
                imageAttributes.SetRemapTable(ColorMap);

                // Draw our mask onto our memory bitmap work surface using the new color mapping scheme
                surface.DrawImage(mask, new Rectangle(0, 0, mask.Width, mask.Height), 0, 0, mask.Width, mask.Height,
                    GraphicsUnit.Pixel, imageAttributes);
            }

            foreach (var idx in _transparencyColours)
            {
                output.MakeTransparent(ColorMap[idx].NewColor);
            }
            return output;
        }
    }
}
