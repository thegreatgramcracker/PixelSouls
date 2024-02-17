# PixelSouls

This is the code used to make [Pixel Souls Demastered](https://www.nexusmods.com/darksoulsremastered/mods/647). Primarily, this was made to only be used by me, therefore there is no user friendly interface for running the conversion yourself, you will need to understand a bit of programming and visual studio to make this work if you want to try it out. The main purpose of this repository is to show examples of code that could be used to make changes to textures in Dark Souls using [Soulsformats](https://github.com/JKAnderson/SoulsFormats), [DirectXTexNet](https://github.com/deng0/DirectXTexNet), and [ImageMagick](https://github.com/dlemstra/Magick.NET). There are many functions this code performs, including converting pixels on a texture to the closest color in a palette, doing the same with material prams, draw params, and FXR1 parameters. It also utilizes DirectXTexNet to do conversion between any DDS format, and has examples for how to use [MeshDecimator](https://github.com/Whinarn/MeshDecimator) to decimate a flver model file (although this is no longer used in the program, since I select LODs instead of creating my own decimated meshes).

If you did wish to try it out, I will list some basic usage instructions (intended for visual studio).

1. Pull/download the repo.
2. Acquire DirectXTexNet and Magick.NET-Q8 from NuGet package manager (or any other source)
3. Open "Program.cs", this is the code where everything is located (yes, it's 2000 lines of code all on one script. I was basically just using this as utility).
4. The field "baseDirectory" is the folder path you are putting your game files in. This could be your Dark Souls Remastered game folder, but I'd recommend copying them to another place. You also will need to put the color palettes included in the repo into that folder, or you can make your own from any png image.
5. Add/comment in and out the game file folders in that directior you want to perform actions on. These folders are in the "dirs" list.
6. Add/comment in and out the actions you'd like to perform on these files in the main loop. To simply pixelize the files, you'd use the "PixelizeFiles" function
7. Run the program (it will take quite a while depending on how many files you have, but you will see the progress in the console window.
8. The modified files should be placed in folders of the same name as their input, but with the "outputPrefix" appended to just before the folder name.
9. Copy the modified files to your game folder.


## Libraries Used
* [Soulsformats](https://github.com/JKAnderson/SoulsFormats)
* [DirectXTexNet](https://github.com/deng0/DirectXTexNet)
* [Magick.NET](https://github.com/dlemstra/Magick.NET)
* [MeshDecimator](https://github.com/Whinarn/MeshDecimator)

## Special Thanks
* Dropoff for teaching me about DirectXTexNet and the basics of 3D model stuff
* Shadowth117 for allowing me to use the code for calculating tangents.
