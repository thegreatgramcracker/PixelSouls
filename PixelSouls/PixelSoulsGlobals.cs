using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum ColorConvertType { WhiteByLightness, PaletteMatch, Average, AlphaOnly };

public enum ValueNodeType { ConstFloat, FloatSequence, Float3Sequence, EmptyValueNode, Unknown };

public enum ClosestColorType { Perceptual, Value, LinearRGB };

namespace PixelSouls
{

    public static class PixelSoulsGlobals
    {
        public const string baseFilesDirectory = "G:\\Code and Video Game Stuff\\Dark Souls Modding\\Pixel Souls Files\\DS1\\";
        public const string paletteDirectory = "G:\\Code and Video Game Stuff\\Dark Souls Modding\\Pixel Souls Files\\palettes\\";
        public const string outputDirectory = "G:\\Code and Video Game Stuff\\Dark Souls Modding\\Pixel Souls Files\\DS1\\test2\\";
        const string normalMapPaletteName = "normals.png";
        const string blackWhitePaletteName = "greyscale.png";


        public const bool pixelizeCubes = false;

        public const ClosestColorType diffuseColorConvertMode = ClosestColorType.Perceptual;
        public const ClosestColorType normalColorConvertMode = ClosestColorType.LinearRGB;

        public static PixelArtSettings defaultPixelSettings = new PixelArtSettings("general palette.png", 100f, 120f, 100f, 8);


        public static IPixelCollection<byte> normalMapColors = new MagickImage(paletteDirectory + normalMapPaletteName).UniqueColors().GetPixels();
    }
}
