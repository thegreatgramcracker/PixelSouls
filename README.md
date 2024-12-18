# PixelSouls

This is the code used to make [Pixel Souls Demastered](https://www.nexusmods.com/darksoulsremastered/mods/647). It works as a library that adds several extension methods to the classes in [Soulsformats](https://github.com/JKAnderson/SoulsFormats), such as pixelizing TPFs, palette matching PARAMs, and converting FLVER models to a specified LOD level. Along with the extension methods, there are two scripts: PixelSoulsGlobals, and PixelSoulsHelpers, that contain static information used by the program as well as additional functions used to create Pixel Souls.

## Basic Usage

You can pixelize all texture files in a binder simply by calling

```
BND3 binder = BND3.Read(path);

binder.Pixelize(new PixelArtSetting
  {
    ScaleFactor = 8,
    MaxColors = 16
  }
);
binder.Write(outputPath);
```
A similar process can be used for TPF files, and every other format that the library extends, albeit with varying functionality.

## Libraries Used
* [Soulsformats](https://github.com/JKAnderson/SoulsFormats)
* [DirectXTexNet](https://github.com/deng0/DirectXTexNet)
* [Magick.NET](https://github.com/dlemstra/Magick.NET)

## Special Thanks
* Dropoff for teaching me about DirectXTexNet and the basics of 3D model stuff
* Shadowth117 for allowing me to use the code for calculating tangents.
* Stayd3D for PS1 Dither Matrix
