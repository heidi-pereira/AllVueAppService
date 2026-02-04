using System.Drawing;
using System.Drawing.Imaging;

namespace BrandVue.Services.Heatmap
{
    public record HeatmapColour(int R, int G, int B);
    public static class HeatmapColourMap
    {
        private static List<HeatmapColour> _legacyColours = new List<HeatmapColour>()
        {
            new(125,1,125),
            new(151,1,151),
            new(151,1,151),
            new(151,1,151),
            new(178,1,178),
            new(178,1,178),
            new(178,1,178),
            new(211,3,211),
            new(211,3,211),
            new(211,3,211),
            new(247,10,247),
            new(247,10,247),
            new(247,10,247),
            new(242,97,234),
            new(241,97,234),
            new(241,96,234),
            new(241,96,234),
            new(241,91,234),
            new(241,94,233),
            new(242,106,234),
            new(243,138,240),
            new(247,167,244),
            new(247,169,244),
            new(247,168,244),
            new(247,169,243),
            new(247,169,243),
            new(247,167,243),
            new(247,164,243),
            new(247,161,234),
            new(247,159,232),
            new(247,156,232),
            new(247,151,228),
            new(247,149,219),
            new(247,144,195),
            new(253,125,142),
            new(254,103,101),
            new(253,83,81),
            new(254,63,63),
            new(255,42,46),
            new(255,25,31),
            new(255,12,18),
            new(252,4,6),
            new(253,1,3),
            new(254,0,2),
            new(253,0,2),
            new(253,1,1),
            new(253,2,0),
            new(253,2,2),
            new(253,5,1),
            new(254,9,2),
            new(253,11,2),
            new(253,16,3),
            new(254,21,2),
            new(253,25,3),
            new(253,29,3),
            new(254,34,5),
            new(255,38,6),
            new(253,45,3),
            new(251,53,1),
            new(254,57,1),
            new(255,62,0),
            new(255,68,1),
            new(255,75,1),
            new(255,82,0),
            new(254,89,0),
            new(254,96,0),
            new(255,101,1),
            new(255,107,4),
            new(254,114,2),
            new(253,122,0),
            new(254,128,1),
            new(253,134,2),
            new(253,142,2),
            new(255,148,4),
            new(255,154,1),
            new(255,160,0),
            new(255,165,0),
            new(254,172,1),
            new(255,179,4),
            new(254,186,4),
            new(253,193,0),
            new(254,198,0),
            new(255,203,1),
            new(253,207,2),
            new(253,213,1),
            new(253,219,0),
            new(252,225,1),
            new(252,229,1),
            new(254,232,2),
            new(255,236,2),
            new(255,238,4),
            new(255,241,3),
            new(254,244,2),
            new(255,249,3),
            new(254,252,3),
            new(253,252,3),
            new(251,252,1),
            new(250,252,1),
            new(248,251,0),
            new(243,251,1),
            new(240,252,3),
            new(237,251,4),
            new(232,250,5),
            new(226,253,6),
            new(221,252,6),
            new(217,251,6),
            new(212,252,8),
            new(207,251,9),
            new(201,252,7),
            new(194,252,7),
            new(190,252,8),
            new(186,248,8),
            new(184,245,10),
            new(181,243,11),
            new(169,242,11),
            new(159,243,12),
            new(158,240,14),
            new(152,236,15),
            new(144,233,15),
            new(137,231,14),
            new(132,229,16),
            new(126,228,17),
            new(119,226,17),
            new(114,223,17),
            new(110,219,17),
            new(102,217,18),
            new(92,215,24),
            new(86,211,27),
            new(82,209,28),
            new(76,208,29),
            new(71,206,29),
            new(67,203,28),
            new(61,201,25),
            new(56,197,26),
            new(52,194,32),
            new(47,191,41),
            new(42,189,44),
            new(34,190,40),
            new(28,189,39),
            new(25,187,41),
            new(21,185,44),
            new(16,184,47),
            new(11,184,49),
            new(7,183,49),
            new(4,182,51),
            new(1,182,53),
            new(0,180,55),
            new(0,179,59),
            new(1,180,63),
            new(1,180,65),
            new(0,180,67),
            new(0,180,70),
            new(1,182,73),
            new(1,182,76),
            new(1,182,79),
            new(1,183,82),
            new(0,186,86),
            new(2,186,91),
            new(2,187,93),
            new(1,188,96),
            new(0,191,102),
            new(0,194,109),
            new(0,195,116),
            new(0,196,119),
            new(0,198,124),
            new(0,200,127),
            new(0,202,131),
            new(0,204,135),
            new(1,206,140),
            new(0,208,144),
            new(1,209,148),
            new(2,212,154),
            new(0,213,158),
            new(1,214,161),
            new(1,217,169),
            new(2,219,176),
            new(1,222,182),
            new(1,225,188),
            new(1,229,195),
            new(2,231,203),
            new(2,233,210),
            new(1,233,216),
            new(1,233,220),
            new(3,232,222),
            new(3,233,227),
            new(1,233,232),
            new(0,234,239),
            new(0,234,242),
            new(1,235,245),
            new(1,235,248),
            new(1,234,249),
            new(3,234,252),
            new(2,233,254),
            new(0,230,253),
            new(0,227,253),
            new(2,223,254),
            new(2,217,253),
            new(0,211,249),
            new(0,205,248),
            new(0,199,248),
            new(0,193,245),
            new(0,184,240),
            new(0,175,238),
            new(0,166,235),
            new(0,159,229),
            new(0,149,224),
            new(2,137,219),
            new(4,129,214),
            new(2,121,208),
            new(2,114,204),
            new(2,110,201),
            new(1,106,198),
            new(0,101,196),
            new(0,94,192),
            new(0,83,187),
            new(2,75,183),
            new(3,68,178),
            new(0,62,172),
            new(0,58,167),
            new(0,51,163),
            new(1,44,159),
            new(2,38,154),
            new(2,31,148),
            new(2,28,143),
            new(2,23,138),
            new(0,19,131),
            new(0,17,127),
            new(0,14,122),
            new(0,12,115),
            new(2,9,110),
            new(2,6,104),
            new(1,5,99),
            new(1,5,97),
            new(4,4,92),
            new(6,4,88),
            new(6,5,82),
            new(5,4,79),
            new(8,8,77),
            new(8,9,76),
            new(10,11,76),
            new(13,13,74),
            new(14,15,74),
            new(17,18,75),
            new(19,18,75),
            new(21,21,72),
            new(23,24,71),
            new(28,27,70),
            new(31,31,70),
            new(32,32,69),
            new(34,34,68),
            new(39,38,71),
            new(41,41,69),
            new(43,44,63),
            new(44,45,63),
            new(46,48,64),
            new(48,50,62)
        };

        public static ColorMap[] LegacyMap
        {
            get
            {
                var colours = new ColorMap[256];
                for (var idx = 0; idx < _legacyColours.Count; idx++)
                {
                    var heatmapColour = _legacyColours[idx];
                    colours[idx] = new ColorMap()
                    {
                        OldColor = Color.FromArgb(idx, idx, idx),
                        NewColor = Color.FromArgb(255, heatmapColour.R, heatmapColour.G, heatmapColour.B)
                    };
                }

                return colours;
            }
        }

        public static ColorMap[] RainbowMap
        {
            get
            {
                var maxPixelValue = 255;
                var outputMap = new ColorMap[256];
                var rainbow = GetRainbowArray();
                for (var x = 0; x <= maxPixelValue; x++)
                {
                    outputMap[x] = new ColorMap()
                    {
                        OldColor = Color.FromArgb(x, x, x),
                        NewColor = Color.FromArgb(255, rainbow[x])
                    };
                }

                return outputMap;
            }
        }

        private static Color[] GetRainbowArray()
        {
            var pixelLength = 288;
            var rainbow = new List<Color>();
            for (var idx = 0; idx < pixelLength; idx++)
            {
                rainbow.Add(Rainbow(pixelLength - 1, idx));
            }

            var skewedRainbow = rainbow.Take(256);
            return skewedRainbow.ToArray();
        }

        private static Color Rainbow(int maxPixelValue, int currentPixelValue)
        {
            float progress = (float)currentPixelValue / maxPixelValue;
            float div = (Math.Abs(progress % 1) * 6);
            int ascending = (int)((div % 1) * 255);
            int descending = 255 - ascending;

            switch ((int)div)
            {
                case 1:
                    return Color.FromArgb(255, 255, ascending, 0);
                case 2:
                    return Color.FromArgb(255, descending, 255, 0);
                case 3:
                    return Color.FromArgb(255, 0, 255, ascending);
                case 4:
                    return Color.FromArgb(255, 0, descending, 255);
                case 5:
                    return Color.FromArgb(255, ascending, 0, 255);
                default:
                    return Color.FromArgb(255, 255, 0, descending);
            }
        }
    }
}
