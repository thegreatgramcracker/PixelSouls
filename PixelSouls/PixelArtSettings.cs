// See https://aka.ms/new-console-template for more information
using ImageMagick;
using PixelSouls;

public class PixelArtSettings
{
    public string paletteName = "";
    public float valueCorrection = 100f;
    public float saturationCorrection = 100f;
    public float hueCorrection = 100f;
    public int scaleFactor = 1;
    public IPixelCollection<byte> colors;
    public PixelArtSettings(string _paletteName, float _valueCorrection = 100f, float _saturationCorrection = 100f, float _hueCorrection = 100f, int _scaleFactor = 8) 
    {
        paletteName = _paletteName;
        colors = new MagickImage(PixelSoulsGlobals.paletteDirectory + _paletteName).UniqueColors().GetPixels();
        valueCorrection = _valueCorrection;
        saturationCorrection = _saturationCorrection;
        hueCorrection = _hueCorrection;
        scaleFactor = _scaleFactor;
    }
}
