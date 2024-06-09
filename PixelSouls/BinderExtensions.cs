using ImageMagick;
using PixelSouls;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PixelSouls
{
    public static class BinderExtensions
    {
        public static void Lodify(this IBinder binder, Dictionary<int, string[]> lodSettings)
        {
            foreach (BinderFile file in binder.Files)
            {
                if (file.Name.EndsWith(".flver") || file.Name.EndsWith(".flver.dcx"))
                {
                    FLVER2 flvFile;
                    try
                    {
                        flvFile = FLVER2.Read(file.Bytes);
                    }
                    catch
                    {
                        Console.WriteLine("Invalid flver (possibly flver0)");
                        return;
                    }
                    
                    if (!flvFile.Meshes.Any() || flvFile.Meshes.Sum(nv => nv.Vertices.Count) <= 0) continue;
                    Console.WriteLine(file.Name);
                    int lodLevel = 1;
                    foreach (int key in lodSettings.Keys)
                    {
                        if (lodSettings[key].Any(Path.GetFileName(file.Name).Contains)) //checks if the file name contains any string in the array
                        {
                            lodLevel = key;
                            break;
                        }
                    }
                    flvFile.Lodify(lodLevel);
                    file.Bytes = flvFile.Write();
                }
            }
        }

        public static void Pixelize(this IBinder binder, PixelArtSetting setting)
        {
            foreach (BinderFile file in binder.Files)
            {
                Console.WriteLine(file.Name);


                if (file.Name.EndsWith(".tpf") || file.Name.EndsWith(".tpf.dcx"))
                {
                    if (file.Bytes.Length > 0)
                    {
                        PixelArtSetting settingToUse = setting;

                        settingToUse = PixelSoulsGlobals.internalBNDSettingsData.SearchFor(file.Name, setting);

                        TPF img = TPF.Read(file.Bytes);
                        img.Pixelize(settingToUse);

                        file.Bytes = img.Write();
                    }

                }
            }
        }

        public static void AdjustMaterialColor(this IBinder binder, IPixelCollection<byte> colors)
        {
            foreach (BinderFile file in binder.Files)
            {
                if (file.Name.EndsWith(".mtd"))
                {
                    MTD matFile = MTD.Read(file.Bytes);
                    Console.WriteLine(file.Name);
                    matFile.AdjustMaterialColor(colors);
                    file.Bytes = matFile.Write();
                }
            }
        }

        public static void AdjustDrawparamColor(this IBinder binder, DrawParamColorLayout[] layouts)
        {
            foreach (BinderFile file in binder.Files)
            {
                if (file.Name.EndsWith(".param"))
                {
                    Console.WriteLine(file.Name);
                    PARAMDEF def = PARAMDEF.Read(BND3.Read("paramdef\\paramdef.paramdefbnd.dcx").Files.Find(n => file.Name.Contains("_" + Path.GetFileNameWithoutExtension(n.Name))).Bytes);
                    PARAM paramFile = PARAM.Read(file.Bytes);
                    paramFile.ApplyParamdefCarefully(def);
                    foreach (DrawParamColorLayout layout in layouts)
                    {
                        if (file.Name.Contains(layout.rowName))
                        {
                            paramFile.AdjustDrawParam(layout);
                        }
                    }
                    file.Bytes = paramFile.Write();
                }
            }
        }
    }
}