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
        public const string baseFilesDirectory = "C:\\Users\\thegr\\Games\\rpcs3-v0.0.31-16163-ef8afa78_win64\\games\\Demon's Souls (USA)\\PS3_GAME\\USRDIR\\"; //the path to your game files
        public const string paletteDirectory = "C:\\Users\\thegr\\source\\repos\\PixelSouls\\PixelSouls\\palettes\\"; //the path to where you palette images are
        public const string outputDirectory = "G:\\Code and Video Game Stuff\\Dark Souls Modding\\Pixel Souls Files\\DeS\\_build\\"; //the path to where the modified fiels should output
        
        public const bool pixelizeCubes = false; //makes pixelization ignore cubes (they are very slow to pixelize and don't work for DeS)
        public const bool pixelizeLightmaps = false; //makes pixelization ignore lightmaps
        public const bool pixelizeDiffuseWithNormalAndSpecular = true; //will make it so pixelization will try to find normal and specular maps for each texture and be influenced by them
        public const bool pixelizeDCX = true; //if false, will skip over dcx files
        public const bool pixelizeNonDCX = false; //if false, will skip over non dcx files

        public const GameType game = GameType.DeS;

        public static IMagickColor<byte> lightDirection = new MagickColor(74, 113, 255); //pixel representing the vector from the map to the light

        
        public static PixelArtSetting diffusePixelSetting = DeSGlobals.diffusePixelSetting;
        public static PixelArtSetting normalPixelSetting = DeSGlobals.normalPixelSetting;
        public static PixelArtSetting specularPixelSetting = DeSGlobals.specularPixelSetting;
        public static PixelArtSetting detailPixelSetting = DeSGlobals.detailPixelSetting;
        public static PixelArtSetting lightmapPixelSetting = DeSGlobals.lightmapPixelSetting;

        public static PixelArtSettingsData internalTpfSettingsData = DeSGlobals.internalTpfSettingsData;
        public static PixelArtSettingsData internalBNDSettingsData = new PixelArtSettingsData();

    }
}
