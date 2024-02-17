# PixelSouls

This is the code used to make [Pixel Souls Demastered](https://www.nexusmods.com/darksoulsremastered/mods/647). Primarily, this was made to only be used by me, therefore there is no user friendly interface for running the conversion yourself, you will need to understand a bit of programming and visual studio to make this work if you want to try it out. The main purpose of this repository is to show examples of code that could be used to make changes to textures in Dark Souls using [Soulsformats](https://github.com/JKAnderson/SoulsFormats), [DirectXTexNet](https://github.com/deng0/DirectXTexNet), and [ImageMagick](https://github.com/dlemstra/Magick.NET). There are many functions this code performs, including converting pixels on a texture to the closest color in a palette, doing the same with material prams, draw params, and FXR1 parameters. It also utilizes DirectXTexNet to do conversion between any DDS format, and has examples for how to use [MeshDecimator](https://github.com/Whinarn/MeshDecimator) to decimate a flver model file (although this is no longer used in the program, since I select LODs instead of creating my own decimated meshes).

If you did wish to try it out, I will list some basic usage instructions.

1. 
