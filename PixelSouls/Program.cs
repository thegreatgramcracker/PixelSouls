// See https://aka.ms/new-console-template for more information
using ImageMagick;
using SoulsFormats;
using DirectXTexNet;
using ImageMagick.Formats;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using SoulsFormats.Kuon;
using MeshDecimator;
using MeshDecimator.Math;
using MeshDecimator.Algorithms;
using System.Xml.Schema;
using System.Xml;
using PixelSouls;


internal class Program
{
    private static void Main(string[] args)
    {
        Dictionary<string[], PixelArtSettings> fileSettings = new Dictionary<string[], PixelArtSettings>()
        {
            [new string[] { "c2390", "c3440", "c3500", "c4500", "c4510", "c4511" }] = new PixelArtSettings("abyss32.png", 90f, 200f, 98f),
            [new string[] { "c1200", "c1201", "c1202", "c1203", "c2730", "c2731", "c3270", "c3340", "c3450", "c3451", "c3470", "c3471", "c3472",
                "c3510", "c3511", "c4190", "c4520", "c4530", "c4531", "c5210", "c5360", "c5361", "c5362" }] = new PixelArtSettings("animal32.png", 100f, 145f),
            [new string[] { "c2320", "c2360", "c2410", "c2570", "c2790", "c2792", "c2793", "c2794",
                "c2794", "c2870", "c3460", "c3461", "c4120", "c5270", "c5271", "c5300",
                "c5351", "c5353", "c4100", "c9990" }] = new PixelArtSettings("armor32.png", 105f, 185f),
            [new string[] { "c3230", "c2710", "c2711", "c2800", "c3250", "c3300", "c3330", "c5290", "c5291" }] = new PixelArtSettings("crystal32.png", 115f, 200f),
            [new string[] { "c2230", "c2231", "c2232", "c2250", "c2260", "c2260", "c2290", "c2300",
                "c2310", "c2430", "c2820", "c2830", "c2840", "c3090", "c3090", "c3110",
                "c3200", "c3210", "c3240", "c3290", "c3341", "c3390", "c3400", "c3480",
                "c4150", "c4160", "c4180", "c5200", "c5201", "c5202", "c5250", "c5330",
                "c5350", "c5352", "c5400", "c5401", "c5280", "c5340" }] = new PixelArtSettings("demon32.png", 95f, 175f, 99f),
            [new string[] { "c2060", "c2210", "c2500", "c2510", "c2530", "c2540", "c2550",
                "c2560", "c2640", "c2650", "c2660", "c2810", "c2811", "c2860", "c2860",
                "c3520", "c3421", "c3422", "c4110", "c5370", "c2240" }] = new PixelArtSettings("hollow32.png", 100f, 135f),
            [new string[] { "c0000", "c2370", "c2370", "c2400", "c2520", "c2590", "c2591", "c2600", "c2750",
                "c3320", "c4090", "c4091", "c4095", "c5310", "c5320" }] = new PixelArtSettings("human32.png", 105, 110),
            [new string[] { "c2270", "c2280", "c2330", "c2380", "c3350", "c4130", "c4140", "c5230", "c5231" }] = new PixelArtSettings("plant32.png", 100, 130),
            [new string[] { "c2700", "c2690", "c3370", "c3380", "c3410", "c3430", "c3431", "c3520", "c3530",
                "c3531", "c5260", "c5261" }] = new PixelArtSettings("serpent32.png", 100, 155),
            [new string[] { "c2670", "c2680", "c2791", "c2900", "c2910", "c2920", "c2930", "c2940", "c2950", "c2960",
                "c3220", "c3490", "c3491", "c3501", "c5220", "c5390" }] = new PixelArtSettings("skeleton32.png", 110, 110),
            [new string[] { "m10_" }] = new PixelArtSettings("general palette.png", 100f, 120f),
            [new string[] { "m11_" }] = new PixelArtSettings("general palette.png", 99f, 105f),
            [new string[] { "m12_" }] = new PixelArtSettings("general palette.png", 100f, 120f),
            [new string[] { "m12dlc_" }] = new PixelArtSettings("general palette.png", 100f, 130f),
            [new string[] { "m13_" }] = new PixelArtSettings("general palette.png", 100f, 115f),
            [new string[] { "m14_" }] = new PixelArtSettings("general palette.png", 100f, 125f, 98f),
            [new string[] { "m15_" }] = new PixelArtSettings("general palette.png", 104f, 125f),
            [new string[] { "m16_" }] = new PixelArtSettings("general palette.png", 96f, 120f),
            [new string[] { "m17_" }] = new PixelArtSettings("general palette.png", 100f, 135f),
            [new string[] { "m18_" }] = new PixelArtSettings("general palette.png")
        };

        Dictionary<int, string[]> lodSettings = new Dictionary<int, string[]>()
        {
            [0] = new string[] { "c0000", "c2730", "c2731", "c4110", "c4115", "c4170", "c4171", "c4172", "c5320" },
            [1] = new string[] { "c1200", "c1201", "c1202", "c1203", "c2060", "c2210", "c2260", "c2290", "c2300", "c2310", "c2330", "c2360",
                                 "c2370", "c2400", "c2430", "c2510", "c2530", "c2540", "c2550", "c2590", "c2591", "c2600", "c2640", "c2680",
                                 "c2690", "c2750", "c2780", "c2790", "c2791", "c2792", "c2793", "c2794", "c2795", "c2800", "c2840", "c2900",
                                 "c2910", "c2920", "c2940", "c2950", "c3210", "c3230", "c3250", "c3270", "c3290", "c3330", "c3400", "c3410",
                                 "c3440", "c3450", "c3451", "c3470", "c3471", "c3472", "c4090", "c4091", "c4095", "c4130", "c4520", "c4530",
                                 "c4531", "c5200", "c5201", "c5202", "c5220", "c5280", "c5340", "c5360", "c5361", "c5362", "c5390", "c9990" },
            [2] = new string[] { "c2230", "c2231", "c2232", "c2240", "c2250", "c2270", "c2280", "c2320", "c2380", "c2390", "c2410", "c2500",
                                 "c2520", "c2560", "c2570", "c2650", "c2660", "c2670", "c2700", "c2710", "c2711", "c2810", "c2811", "c2820",
                                 "c2830", "c2860", "c2870", "c2930", "c2960", "c3090", "c3110", "c3200", "c3220", "c3240", "c3300", "c3320",
                                 "c3340", "c3341", "c3350", "c3370", "c3380", "c3390", "c3420", "c3421", "c3422", "c3430", "c3431", "c3460",
                                 "c3461", "c3480", "c3490", "c3491", "c3500", "c3501", "c3510", "c3511", "c3520", "c3530", "c3531", "c4100",
                                 "c4120", "c4140", "c4150", "c4160", "c4180", "c4190", "c4500", "c4510", "c4511", "c5210", "c5230", "c5231",
                                 "c5240", "c5250", "c5260", "c5261", "c5270", "c5271", "c5290", "c5291", "c5300", "c5310", "c5330",
                                 "c5350", "c5351", "c5352", "c5353", "c5370", "c5400", "c5401"}
        };

        Dictionary<string[], PixelArtSettings> internalBNDFileSettings = new Dictionary<string[], PixelArtSettings>()
        {
            [new string[] { "m10_00_wall_02.tpf.dcx", "m10_00_wall_02_s.tpf.dcx", "m10_00_wall_02_n.tpf.dcx" }] = new PixelArtSettings("general palette.png", 100f, 120f, 100f, 16),

            [new string[] { "m13_N_wall_03.tpf.dcx", "m13_N_wall_03_s.tpf.dcx", "m13_N_wall_03_n.tpf.dcx" }] = new PixelArtSettings("general palette.png", 100f, 120f, 100f, 16),
            [new string[] { "sfx\\Tex\\s93031.tpf" }] = new PixelArtSettings("general palette.png", 100f, 100f, 100f, 16),
            [new string[] { "m10_nest_eggs.tpf.dcx", "m10_nest_eggs_s.tpf.dcx", "m10_nest_eggs_n.tpf.dcx" }] = new PixelArtSettings("general palette.png", 100f, 50f),
            [new string[] { "sfx\\Tex\\s20032.tpf" }] = new PixelArtSettings("general palette.png", 100f, 90f)
        };

        DrawParamColorLayout[] colorLayouts = new DrawParamColorLayout[]{
            new DrawParamColorLayout("_EnvLightTexBank", "colR_00", "colG_00", "colB_00", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_EnvLightTexBank", "colR_01", "colG_01", "colB_01", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_EnvLightTexBank", "colR_02", "colG_02", "colB_02", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_EnvLightTexBank", "colR_03", "colG_03", "colB_03", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_EnvLightTexBank", "colR_04", "colG_04", "colB_04", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_EnvLightTexBank", "colR_05", "colG_05", "colB_05", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_EnvLightTexBank", "colR_06", "colG_06", "colB_06", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_EnvLightTexBank", "colR_07", "colG_07", "colB_07", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_EnvLightTexBank", "colR_08", "colG_08", "colB_08", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_EnvLightTexBank", "colR_09", "colG_09", "colB_09", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_FogBank", "colR", "colG", "colB", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_LensFlareBank", "colR", "colG", "colB", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_LensFlareExBank", "colR", "colG", "colB", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_LightBank", "colR_0", "colG_0", "colB_0", ColorConvertType.PaletteMatch), //WhiteByLightness
            new DrawParamColorLayout("_LightBank", "colR_1", "colG_1", "colB_1", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_LightBank", "colR_2", "colG_2", "colB_2", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_LightBank", "colR_u", "colG_u", "colB_u", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_LightBank", "colR_d", "colG_d", "colB_d", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_LightBank", "colR_s", "colG_s", "colB_s", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_LightBank", "envDif_colR", "envDif_colG", "envDif_colB", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_LightBank", "envSpc_colR", "envSpc_colG", "envSpc_colB", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_LightScatteringBank", "sunR", "sunG", "sunB", ColorConvertType.PaletteMatch), //palette match
            new DrawParamColorLayout("_LightScatteringBank", "reflectanceR", "reflectanceG", "reflectanceB", ColorConvertType.PaletteMatch),//white by lightness
            new DrawParamColorLayout("_PointLightBank", "colR", "colG", "colB", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_ShadowBank", "colR", "colG", "colB", ColorConvertType.PaletteMatch),
            new DrawParamColorLayout("_ToneCorrectBank", "brightnessR", "brightnessG", "brightnessB", ColorConvertType.Average),
            new DrawParamColorLayout("_ToneCorrectBank", "contrastR", "contrastG", "contrastB", ColorConvertType.Average),
        };



        string[] dirs = new string[]
        {
            //"m10",
            //"m11",
            "chr",
            "mtd"
        };


        
        foreach (string directory in dirs)
        {
            string[] files = Directory.GetFiles(PixelSoulsGlobals.baseFilesDirectory + directory);
            //LodifyFiles(files);
            //AdjustMaterialFiles(files);
            //AdjustDrawParamFiles(files);
            PixelizeFiles(files, PixelSoulsGlobals.outputDirectory + directory + "\\");
            //ModifyFfxXmlFiles(files);
        }
        /* Command prompt for generating xml files
         * 
         * for /R %f in (*.ffx) do ("C:\Users\thegr\source\repos\PixelSouls\PixelSouls\bin\Debug\net6.0\fxmlr_better_version\FXMLR.exe" "%f")
         */


        void LodifyFiles(string[] files, string outputDir = "")
        {
            //sets files to use preferred LOD if they are the right kind of binder
            foreach (string file in files)
            {
                if (file.EndsWith(".chrbnd.dcx") || file.EndsWith(".objbnd.dcx") || file.EndsWith("partsbnd.dcx"))
                {
                    BND3 bndFile = BND3.Read(file);
                    bndFile.Lodify(lodSettings);
                    bndFile.Write("conv_" + outputDir + Path.GetFileName(file));
                    //flverBND.Write("conv_" + filePath);
                }
            }
        }

        void AdjustDrawParamFiles(string[] files, string outputDir = "")
        {
            //adjusts draw params for files if they are parambnd
            foreach (string file in files)
            {
                if ((file.EndsWith(".parambnd.dcx") || file.EndsWith(".parambnd")) && !file.Contains("a99_") && !file.Contains("default_"))
                {
                    BND3 paramBND = BND3.Read(file);
                    paramBND.AdjustDrawparamColor(colorLayouts);
                    paramBND.Write(outputDir + Path.GetFileName(file));
                }
            }
        }

        void AdjustMaterialFiles(string[] files, string outputDir = "")
        {
            //adjusts the materials of a file if its a mtdbnd
            foreach (string file in files)
            {
                if (file.EndsWith(".mtdbnd.dcx") || file.EndsWith(".mtdbnd"))
                {
                    BND3 bndFile = BND3.Read(file);
                    bndFile.AdjustMaterialColor(PixelSoulsGlobals.defaultPixelSettings.colors);
                    bndFile.Write(outputDir + Path.GetFileName(file));
                }
            }
        }

        void PixelizeFiles(string[] files, string outputDir = "")
        {
            //Pixelizes the files in the array of file paths if they are TPF or have TPF files inside their binder
            int totalFiles = files.Length;
            int currentFiles = 0;


            foreach (string file in files)
            {
                currentFiles += 1;
                PixelArtSettings pixelSettings = PixelSoulsGlobals.defaultPixelSettings;
                Console.WriteLine();
                Console.WriteLine(file);
                foreach (string[] key in fileSettings.Keys)
                {
                    if (key.Any(file.Contains))
                    {
                        pixelSettings = fileSettings[key];
                        break;
                    }
                }
                if (file.EndsWith(".tpf") || file.EndsWith(".tpf.dcx"))
                {
                    TPF img = TPF.Read(file);

                    img.Pixelize(pixelSettings);

                    img.Write(outputDir + Path.GetFileName(file));
                }
                else if (file.EndsWith(".partsbnd.dcx") || file.EndsWith(".partsbnd") || file.EndsWith(".chrbnd.dcx") || file.EndsWith(".chrbnd")
                    || file.EndsWith(".ffxbnd.dcx") || file.EndsWith(".ffxbnd") || file.EndsWith(".objbnd.dcx") || file.EndsWith(".objbnd"))
                {
                    BND3 bnd = BND3.Read(file);

                    if (bnd.Files.Any(f => f.Name.EndsWith(".tpf") || file.EndsWith(".tpf.dcx")))
                    {
                        bnd.Pixelize(pixelSettings, internalBNDFileSettings);
                        bnd.Write(outputDir + Path.GetFileName(file));
                    }
                }
                else if (file.EndsWith(".chrtpfbdt"))
                {
                    string bndID = Path.GetFileNameWithoutExtension(file);
                    int bndIndex = Array.FindIndex(files, n => n.Contains(bndID + ".chrbnd.dcx"));
                    if (bndIndex < 0)
                    {
                        Console.WriteLine("no bnd found for " + bndID);
                        Console.WriteLine("finished " + currentFiles + " of " + totalFiles);
                        continue;
                    }
                    BND3 headerBND = BND3.Read(files[bndIndex]);
                    BinderFile headerBinderFile = headerBND.Files.Find(f => f.Name.EndsWith(".chrtpfbhd"));
                    if (headerBinderFile != null)
                    {
                        BXF3 bdtFiles = BXF3.Read(headerBinderFile.Bytes, file);

                        if (bdtFiles.Files.Any(f => f.Name.EndsWith(".tpf") || f.Name.EndsWith(".tpf.dcx")))
                        {
                            bdtFiles.Pixelize(pixelSettings, internalBNDFileSettings);

                            byte[] finalHeaderBytes = null;
                            bdtFiles.Write(out finalHeaderBytes, outputDir + Path.GetFileName(file));
                            headerBinderFile.Bytes = finalHeaderBytes;
                            headerBND.Write(PixelSoulsGlobals.outputDirectory + "chr\\" + bndID + ".chrbnd.dcx");
                        }
                    }
                }
                else if (file.EndsWith(".tpfbdt"))
                {
                    if (file.Contains("GI_EnvM_") && PixelSoulsGlobals.pixelizeCubes == false) continue;
                    string headerBNDPath = Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + ".tpfbhd";
                    BXF3 bdtFiles = BXF3.Read(headerBNDPath, file);
                    if (bdtFiles.Files.Any(f => f.Name.EndsWith(".tpf") || f.Name.EndsWith(".tpf.dcx")))
                    {
                        bdtFiles.Pixelize(pixelSettings, internalBNDFileSettings);

                        bdtFiles.Write(outputDir + headerBNDPath, outputDir + Path.GetFileName(file));
                    }

                }
                Console.WriteLine("finished " + currentFiles + " of " + totalFiles);
            }
        }

    }
}
