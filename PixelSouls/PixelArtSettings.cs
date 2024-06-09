// See https://aka.ms/new-console-template for more information
using ImageMagick;
using PixelSouls;
using System.Text.RegularExpressions;

//public class PixelArtSettings
//{
//    public string paletteName = "";
//    public string[] fileNames;
//    public ClosestColorType closestColorType = ClosestColorType.None;
//    public float valueCorrection = 100f;
//    public float saturationCorrection = 1f;
//    public float hueCorrection = 100f;
//    public float contrastCorrection = 100f;
//    public int scaleFactor = 1;
//    public int maxColors = 256;
//    public IPixelCollection<byte> colors;
//    public PixelArtSettings(string _paletteName = "", ClosestColorType _closestColorType = ClosestColorType.None, float _valueCorrection = 100f, float _saturationCorrection = 100f, float _hueCorrection = 100f, int _scaleFactor = 8, float _contrastCorrection = 100f, int _maxColors = 256) 
//    {
//        paletteName = _paletteName;
//        closestColorType = _closestColorType;
//        colors = new MagickImage(PixelSoulsGlobals.paletteDirectory + _paletteName).UniqueColors().GetPixels();
//        valueCorrection = _valueCorrection;
//        saturationCorrection = _saturationCorrection;
//        hueCorrection = _hueCorrection;
//        scaleFactor = _scaleFactor;
//        contrastCorrection = _contrastCorrection;
//        maxColors = _maxColors;
//    }

//}

public class PixelArtSettingsData
{
    public List<PixelArtSetting> Settings {get; set;} = new List<PixelArtSetting> ();
    public PixelArtSetting SearchFor(string name, PixelArtSetting fallbackSetting)
    {
        foreach (PixelArtSetting setting in Settings)
        {
            foreach(string filename in setting.FileNames)
            {
                if (Regex.IsMatch(name, filename))
                {
                    Console.WriteLine("found setting");
                    return setting;
                }
            }
        }
        return fallbackSetting;
    }
    public PixelArtSetting SearchFor(string name, PixelArtSetting fallbackSetting, TextureType texType)
    {
        foreach (PixelArtSetting setting in Settings)
        {
            if (setting.AppliesToDiffuse == false && texType == TextureType.Diffuse) continue;
            if (setting.AppliesToNormal == false && texType == TextureType.Normal) continue;
            if (setting.AppliesToSpec == false && texType == TextureType.Specular) continue;
            foreach (string filename in setting.FileNames)
            {
                if (Regex.IsMatch(name, filename))
                {
                    Console.WriteLine("found setting");
                    return setting;
                }
            }
        }
        return fallbackSetting;
    }
}

public class PixelArtSetting
{
    public List<string> FileNames { get; set; } = new List<string>();
    public List<string> FileNameExceptions { get; set; } = new List<string> ();
    public bool AppliesToDiffuse { get; set; } = true;
    public bool AppliesToNormal { get; set; } = false;
    public bool AppliesToSpec { get; set; } = false;

    public int Brightness { get; set; } = 0;
    public int Contrast { get; set; } = 0;
    public int Hue { get; set; } = 100;
    public int Saturation { get; set; } = 100;
    public int Value { get; set; } = 100;
    public int TintOpacity { get; set; } = 0;
    public MagickColor TintColor { get; set; } = new MagickColor(0, 0, 0);

    public int MaxColors { get; set; } = 255;
    public int ScaleFactor { get; set; } = 8;

    public string ColorConvertMode { get; set; } = "None";
    public string DitherMatrix { get; set; } = "";

    public IPixelCollection<byte> Colors;



}
