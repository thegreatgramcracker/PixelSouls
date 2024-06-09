using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

public enum ColorConvertType { WhiteByLightness, PaletteMatch, Average, AlphaOnly };

public enum ValueNodeType { ConstFloat, FloatSequence, Float3Sequence, EmptyValueNode, Unknown };

public enum ClosestColorType { Perceptual, Value, LinearRGB, None };

public enum GameType { DSR, DeS}

public enum TextureType { Diffuse, Normal, Specular, Cube, Lightmap, DetailBump, Heightmap, Ignore}

namespace PixelSouls
{

    public static class PixelSoulsGlobals
    {
        public const string baseFilesDirectory = "G:\\Code and Video Game Stuff\\Dark Souls Modding\\Pixel Souls Files\\DeS\\";
        public const string paletteDirectory = "G:\\Code and Video Game Stuff\\Dark Souls Modding\\Pixel Souls Files\\palettes\\";
        public const string outputDirectory = "G:\\Code and Video Game Stuff\\Dark Souls Modding\\Pixel Souls Files\\DeS\\_out\\";
        
        public const bool pixelizeCubes = false;
        public const bool pixelizeDiffuseWithNormalAndSpecular = true;
        public const bool pixelizeDCX = true;
        public const bool pixelizeNonDCX = true;

        public const GameType game = GameType.DeS;

        public static IMagickColor<byte> lightDirection = new MagickColor(74, 113, 255); //pixel representing the vector from the map to the light

        
        public static PixelArtSetting diffusePixelSetting = new PixelArtSetting
        {
            Colors = new MagickImage(paletteDirectory + "general palette.png").UniqueColors().GetPixels(),
            ColorConvertMode = "PS1",
            DitherMatrix = "o4x4,32",
            MaxColors = 16,
            Contrast = 12
        };
        public static PixelArtSetting normalPixelSetting = new PixelArtSetting
        {
            Colors = new MagickImage(paletteDirectory + "no normal.png").UniqueColors().GetPixels(),
            ColorConvertMode = "LinearRGB"
        };
        public static PixelArtSetting specularPixelSetting = new PixelArtSetting
        {
            Colors = new MagickImage(paletteDirectory + "no specular.png").UniqueColors().GetPixels(),
            ColorConvertMode = "LinearRGB"
        };
        public static PixelArtSetting detailPixelSetting = new PixelArtSetting
        {
            Colors = new MagickImage(paletteDirectory + "no normal.png").UniqueColors().GetPixels(),
            ColorConvertMode = "LinearRGB"
        };
        public static PixelArtSetting lightmapPixelSetting = new PixelArtSetting
        {
            Colors = new MagickImage(paletteDirectory + "general palette.png").UniqueColors().GetPixels(),
            ColorConvertMode = "PS1",
            MaxColors = 16,
            ScaleFactor = 1
        };

        public static PixelArtSettingsData internalTpfSettingsData = new PixelArtSettingsData
        {
            Settings = new List<PixelArtSetting>
            {
                new PixelArtSetting //give water waves
                {
                    AppliesToDiffuse = false,
                    AppliesToNormal = true,
                    FileNames = new List<string> {"water_n"},
                    Colors = normalPixelSetting.Colors,
                    ColorConvertMode = "None",
                    MaxColors = 16

                },
                new PixelArtSetting
                {
                    FileNames = new List<string> {"m02_dirt_floor$"},
                    Colors = diffusePixelSetting.Colors,
                    ColorConvertMode = diffusePixelSetting.ColorConvertMode,
                    DitherMatrix = diffusePixelSetting.DitherMatrix,
                    MaxColors = diffusePixelSetting.MaxColors,
                    Contrast = 15,
                    Brightness = 3,
                    ScaleFactor = 16
                },
                new PixelArtSetting //boletarian palace desat exceptions
                {
                    FileNames = new List<string> {"m02_tapestry", "m02_fallen_leaves"},
                    Colors = diffusePixelSetting.Colors,
                    ColorConvertMode = diffusePixelSetting.ColorConvertMode,
                    DitherMatrix = diffusePixelSetting.DitherMatrix,
                    MaxColors = diffusePixelSetting.MaxColors,
                    Contrast = 15,
                    Saturation = 115,
                },
                new PixelArtSetting //boletarian palace greenery
                {
                    FileNames = new List<string> {"m02_ground_01", "m02_grass", "m02_bush"},
                    Colors = diffusePixelSetting.Colors,
                    ColorConvertMode = diffusePixelSetting.ColorConvertMode,
                    DitherMatrix = diffusePixelSetting.DitherMatrix,
                    MaxColors = diffusePixelSetting.MaxColors,
                    Contrast = 15,
                    Saturation = 115,
                    Hue = 115
                },
                new PixelArtSetting //shrine of storms bricks
                {
                    FileNames = new List<string> {"^m03.*floor_stone", "^m03.*ground_stone", "^m03.*_pillar_", "^m03.*_kaidan_", "^m03.*_relief_", "^m03.*_stairs_",
                    "^m03.*_renga_", "^m03.*_brokc_", "^m03.*_floor_0", "^m03.*_inout_", "^m03.*_steps_", "^m03.*_broken_", "^m03.*_arch_"},
                    Colors = diffusePixelSetting.Colors,
                    ColorConvertMode = diffusePixelSetting.ColorConvertMode,
                    DitherMatrix = diffusePixelSetting.DitherMatrix,
                    MaxColors = diffusePixelSetting.MaxColors,
                    Contrast = 10,
                    Saturation = 105,
                    Brightness = 8
                }
            }
        };
        public static PixelArtSettingsData internalBNDSettingsData = new PixelArtSettingsData();

    }
}
