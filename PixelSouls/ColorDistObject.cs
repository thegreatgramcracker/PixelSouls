// See https://aka.ms/new-console-template for more information
using ImageMagick;

public class ColorDistObject
{
    public IMagickColor<byte> color;
    public double dist;

    public ColorDistObject(IMagickColor<byte> _color, double _dist)
    {
        color = _color;
        dist = _dist;
    }
}
