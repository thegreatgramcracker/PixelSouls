
using ImageMagick;

namespace PixelSouls
{
    public static class DeSGlobals
    {
        public static PixelArtSetting diffusePixelSetting = new PixelArtSetting
        {
            Colors = new MagickImage(PixelSoulsGlobals.paletteDirectory + "general palette.png").UniqueColors().GetPixels(),
            ColorConvertMode = "PS1",
            //DitherMatrix = "o4x4,32",
            MaxColors = 16,
            Contrast = 12
        };
        public static PixelArtSetting normalPixelSetting = new PixelArtSetting
        {
            Colors = new MagickImage(PixelSoulsGlobals.paletteDirectory + "no normal.png").UniqueColors().GetPixels(),
            ColorConvertMode = "LinearRGB"
        };
        public static PixelArtSetting specularPixelSetting = new PixelArtSetting
        {
            Colors = new MagickImage(PixelSoulsGlobals.paletteDirectory + "no specular.png").UniqueColors().GetPixels(),
            ColorConvertMode = "LinearRGB"
        };
        public static PixelArtSetting detailPixelSetting = new PixelArtSetting
        {
            Colors = new MagickImage(PixelSoulsGlobals.paletteDirectory + "no normal.png").UniqueColors().GetPixels(),
            ColorConvertMode = "LinearRGB"
        };
        public static PixelArtSetting lightmapPixelSetting = new PixelArtSetting
        {
            Colors = new MagickImage(PixelSoulsGlobals.paletteDirectory + "general palette.png").UniqueColors().GetPixels(),
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
                },
                new PixelArtSetting //soul sequence
                {
                    FileNames = new List<string> {"soul_sequence"},
                    Colors = new MagickImage(PixelSoulsGlobals.paletteDirectory + "des_important_colors.png").UniqueColors().GetPixels(),
                    ColorConvertMode = "Perceptual",
                    DitherMatrix = "",
                    MaxColors = 64,
                    ScaleFactor = 3
                },
            }
        };
    }
}
