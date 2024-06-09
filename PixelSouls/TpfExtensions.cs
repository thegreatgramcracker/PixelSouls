using DirectXTexNet;
using ImageMagick;
using SoulsFormats;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Numerics;

namespace PixelSouls
{
    public static class TpfExtensions
    {
        public static void Pixelize(this TPF tpf, PixelArtSetting setting)
        {
            foreach (TPF.Texture tex in tpf.Textures)
            {
                Console.WriteLine(tex.Name);
                DXGI_FORMAT format = PixelSoulsHelpers.GetFormat(tex.Format);
                Console.WriteLine(format);
                TextureType textureType = PixelSoulsHelpers.GetTextureType(tex.Name, format);

                PixelArtSetting settingToUse = setting;

                if (textureType == TextureType.Heightmap ||
                    (textureType == TextureType.Cube && PixelSoulsGlobals.pixelizeCubes == false))
                {
                    textureType = TextureType.Ignore;
                }
                Console.WriteLine(textureType);
                if (textureType == TextureType.Ignore) continue;

                /*
                 * 1. get default settings based on texture type
                 * 2. search for a match in the internal tpf settings (regex)
                 * 3. if none found, use the default
                 * 
                 * 
                 * 
                 * 
                 */
                if (textureType == TextureType.Normal)
                {
                    settingToUse = PixelSoulsGlobals.normalPixelSetting;
                    settingToUse.ScaleFactor = setting.ScaleFactor;
                }
                else if (textureType == TextureType.Specular)
                {
                    settingToUse = PixelSoulsGlobals.specularPixelSetting;
                    settingToUse.ScaleFactor = setting.ScaleFactor;
                }
                else if (textureType == TextureType.Lightmap)
                {
                    settingToUse = PixelSoulsGlobals.lightmapPixelSetting;
                }
                settingToUse = PixelSoulsGlobals.internalTpfSettingsData.SearchFor(tex.Name, settingToUse, textureType);


                if (textureType == TextureType.Diffuse && PixelSoulsGlobals.pixelizeDiffuseWithNormalAndSpecular)
                {
                    TPF.Texture? normal = tpf.Textures.Find(t => t.Name == tex.Name + "_n");
                    TPF.Texture? spec = tpf.Textures.Find(t => t.Name == tex.Name + "_s");
                    if (normal != null && spec != null)
                    {
                        Console.WriteLine("Diff + Norm + Spec");
                        tex.PixelizeWithNormalAndSpecular(normal, spec, settingToUse, format);
                        continue;
                    }
                    else if (normal != null)
                    {
                        Console.WriteLine("Diff + Norm");
                        tex.PixelizeWithNormal(normal, settingToUse, format);
                        continue;
                    }
                    else if (spec != null)
                    {
                        Console.WriteLine("Diff + Spec");
                        tex.PixelizeWithSpecular(spec, settingToUse, format);
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("Diff");
                        tex.Pixelize(settingToUse, format);
                        continue;
                    }
                }



                if (textureType != TextureType.Ignore)
                {
                    tex.Pixelize(settingToUse, format);
                }

                //tex.Bytes = PixelSoulsHelpers.Pixelize(tex, setting);
            }
        }
        public static unsafe void Pixelize(this TPF.Texture tex, PixelArtSetting setting, DXGI_FORMAT format, byte[]? passPixels = null) 
        {
            if (tex.Header.Unk1 == 120) return;
            
            byte[] bytes = PixelSoulsGlobals.game == GameType.DeS ? tex.Headerize() : tex.Bytes;

            GCHandle pinnedArray = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            ScratchImage texImage = TexHelper.Instance.LoadFromDDSMemory(pinnedArray.AddrOfPinnedObject(), bytes.Length, DDS_FLAGS.NONE);

            texImage = texImage.Decompress(DXGI_FORMAT.R8G8B8A8_UNORM);
            
            var stream = texImage.SaveToDDSMemory(DDS_FLAGS.NONE);
            stream.Seek(0, SeekOrigin.Begin);
            byte[] decompressedBytes = new byte[stream.Length];
            stream.Read(decompressedBytes);

            pinnedArray.Free();

            MagickImage magickImage = new MagickImage(decompressedBytes);
            magickImage.Settings.SetDefine(MagickFormat.Dds, "compression", "none");

            if (passPixels != null)
            {
                magickImage.ImportPixels(passPixels, new PixelImportSettings(magickImage.Width, magickImage.Height, StorageType.Char, "RGBA"));
            }
            magickImage.FilterType = FilterType.Gaussian;
            
            Console.WriteLine(magickImage.ColorSpace.ToString());

            bool resize = true;
            if (magickImage.Width % setting.ScaleFactor != 0 || magickImage.Height % setting.ScaleFactor != 0)
            {
                resize = false;
                Console.WriteLine("Unable to resize");
            }
            if (resize && setting.ScaleFactor != 1)
            {
                if (magickImage.Width == setting.ScaleFactor && magickImage.Height == setting.ScaleFactor)
                {
                    magickImage.Resize(new MagickGeometry(1));
                }
                else
                {
                    magickImage.Resize(magickImage.Width / setting.ScaleFactor, magickImage.Height / setting.ScaleFactor);
                    
                }
            }

            magickImage.Modulate(new Percentage(setting.Value), new Percentage(setting.Saturation), new Percentage(setting.Hue));
            magickImage.BrightnessContrast(new Percentage(setting.Brightness), new Percentage(setting.Contrast));
            magickImage.Tint(new MagickGeometry(setting.TintOpacity), setting.TintColor);


            MagickImage noAlpImage = (MagickImage)magickImage.UniqueColors();
            noAlpImage.Alpha(AlphaOption.Off);

            if (setting.MaxColors < 255 && noAlpImage.TotalColors > setting.MaxColors)
            {
                
                magickImage.Quantize(new QuantizeSettings
                {
                    Colors = setting.MaxColors,
                    DitherMethod = DitherMethod.No,
                    ColorSpace = ColorSpace.sRGB,
                    TreeDepth = 8,

                });
            }

            if (setting.DitherMatrix != "")
            {
                MagickImage originalColors = new MagickImage(magickImage.UniqueColors());
                magickImage.OrderedDither(setting.DitherMatrix);
                magickImage.Map(originalColors);
            }
            if (setting.ColorConvertMode != "None")
            {
                foreach (Pixel pixel in magickImage.GetPixels())
                {
                    if (setting.ColorConvertMode == "PS1")
                    {
                        pixel.SetChannel(0, PixelSoulsHelpers.ConvertToPS1ColorChannel(pixel.ToColor().R));
                        pixel.SetChannel(1, PixelSoulsHelpers.ConvertToPS1ColorChannel(pixel.ToColor().G));
                        pixel.SetChannel(2, PixelSoulsHelpers.ConvertToPS1ColorChannel(pixel.ToColor().B));
                        pixel.SetChannel(3, PixelSoulsHelpers.ConvertToPS1ColorChannel(pixel.ToColor().A));
                    }
                    else
                    {
                        IMagickColor<byte> closestColor = PixelSoulsHelpers.ClosestColor(pixel.ToColor(), setting.Colors, setting.ColorConvertMode);

                        pixel.SetChannel(0, closestColor.R);
                        pixel.SetChannel(1, closestColor.G);
                        pixel.SetChannel(2, closestColor.B);
                        pixel.SetChannel(3, closestColor.A);
                    }
                }
            }
            Console.WriteLine("final total colors: " + magickImage.TotalColors);
            
            //else if (textureType == TextureType.Normal)
            //{
            //    foreach (Pixel pixel in magickImage.GetPixels())
            //    {
            //        Console.WriteLine(CalculateNormal(pixel.GetChannel(0), pixel.GetChannel(1)));
            //    }
            //}

            //magickImage.FilterType = FilterType.Point;


            if (resize) magickImage.Sample(magickImage.Width * setting.ScaleFactor, magickImage.Height * setting.ScaleFactor);
            //magickImage.InterpolativeResize(magickImage.Width * setting.ScaleFactor, magickImage.Height * setting.ScaleFactor, PixelInterpolateMethod.Nearest);




            GCHandle pinnedArray2 = GCHandle.Alloc(magickImage.ToByteArray(), GCHandleType.Pinned);

            ScratchImage finalImage = TexHelper.Instance.LoadFromDDSMemory(pinnedArray2.AddrOfPinnedObject(), magickImage.ToByteArray().Length, DDS_FLAGS.NONE);
            finalImage = finalImage.GenerateMipMaps(TEX_FILTER_FLAGS.POINT, 0);
            finalImage = finalImage.Compress(format, format == DXGI_FORMAT.BC7_UNORM ? TEX_COMPRESS_FLAGS.BC7_QUICK : TEX_COMPRESS_FLAGS.DEFAULT, 0.5f);

            

            if (PixelSoulsGlobals.game == GameType.DeS)
            {
                byte* pixels = (byte*)finalImage.GetImage(0).Pixels;
                int len = tex.Bytes.Length;
                using (UnmanagedMemoryStream memoryStream = new UnmanagedMemoryStream(pixels, len, len, FileAccess.Read))
                {
                    
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    byte[] imageBytes = new byte[len];
                    memoryStream.Read(imageBytes, 0, len);
                    tex.Bytes = imageBytes;
                }
            }
            else
            {
                using (UnmanagedMemoryStream memoryStream = finalImage.SaveToDDSMemory(DDS_FLAGS.NONE))
                {
                    
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    byte[] imageBytes = new byte[memoryStream.Length];
                    memoryStream.Read(imageBytes);
                    tex.Bytes = imageBytes;
                }
            }
            pinnedArray2.Free();
        }

        public static void PixelizeWithNormalAndSpecular(this TPF.Texture tex, TPF.Texture normal, TPF.Texture specular, PixelArtSetting setting, DXGI_FORMAT format)
        {
            MagickImage diffuseImage = new MagickImage(tex.Headerize());
            MagickImage normalImage = new MagickImage(normal.Headerize());
            MagickImage specularImage = new MagickImage(specular.Headerize());

            byte[] pixels = diffuseImage.GetPixels().ToByteArray("RGBA");
            byte[] normalPixels = normalImage.GetPixels().ToByteArray("RGBA");
            byte[] specPixels = specularImage.GetPixels().ToByteArray("RGBA");


            if (pixels.Length * 3 != pixels.Length + normalPixels.Length + specPixels.Length)
            {
                Console.WriteLine("mismatched maps");
                tex.Pixelize(setting, format);
                return;
            }

            for (int i = 0; i < pixels.Length; i += 4)
            {
                IMagickColor<byte> currentPixel = new MagickColor(pixels[i], pixels[i + 1], pixels[i + 2]);
                IMagickColor<byte> currentPixelNormal = new MagickColor(normalPixels[i], normalPixels[i + 1], normalPixels[i + 2]);
                IMagickColor<byte> currentPixelSpec = new MagickColor(specPixels[i], specPixels[i + 1], specPixels[i + 2]);
                //Console.WriteLine(currentPixelSpec.ToHexString());
                byte lightDif = PixelSoulsHelpers.ColorDifferenceLinearRGB(currentPixelNormal, PixelSoulsGlobals.lightDirection);
                float lightMult = 1f - (Math.Clamp((float)lightDif - 85f, 0f, 255f) / (255f - 85f));
                float specMult = lightDif / 255f;
                pixels[i] = PixelSoulsHelpers.ClampToByte(pixels[i] * lightMult + (currentPixelSpec.R * specMult));
                pixels[i + 1] = PixelSoulsHelpers.ClampToByte(pixels[i + 1] * lightMult + (currentPixelSpec.G * specMult));
                pixels[i + 2] = PixelSoulsHelpers.ClampToByte(pixels[i + 2] * lightMult + (currentPixelSpec.B * specMult));
            }
            tex.Pixelize(setting, format, pixels);
        }

        public static void PixelizeWithNormal(this TPF.Texture tex, TPF.Texture normal, PixelArtSetting setting, DXGI_FORMAT format)
        {
            MagickImage diffuseImage = new MagickImage(tex.Headerize());
            MagickImage normalImage = new MagickImage(normal.Headerize());

            byte[] pixels = diffuseImage.GetPixels().ToByteArray("RGBA");
            byte[] normalPixels = normalImage.GetPixels().ToByteArray("RGBA");


            if (pixels.Length * 2 != pixels.Length + normalPixels.Length)
            {
                Console.WriteLine("mismatched maps");
                tex.Pixelize(setting, format);
                return;
            }

            for (int i = 0; i < pixels.Length; i += 4)
            {
                IMagickColor<byte> currentPixel = new MagickColor(pixels[i], pixels[i + 1], pixels[i + 2]);
                IMagickColor<byte> currentPixelNormal = new MagickColor(normalPixels[i], normalPixels[i + 1], normalPixels[i + 2]);
                IMagickColor<byte> currentPixelSpec = new MagickColor(0, 0, 0);
                //Console.WriteLine(currentPixelSpec.ToHexString());
                byte lightDif = PixelSoulsHelpers.ColorDifferenceLinearRGB(currentPixelNormal, PixelSoulsGlobals.lightDirection);
                float lightMult = 1f - (Math.Clamp((float)lightDif - 85f, 0f, 255f) / (255f - 85f));
                float specMult = lightDif / 255f;
                pixels[i] = PixelSoulsHelpers.ClampToByte(pixels[i] * lightMult + (currentPixelSpec.R * specMult));
                pixels[i + 1] = PixelSoulsHelpers.ClampToByte(pixels[i + 1] * lightMult + (currentPixelSpec.G * specMult));
                pixels[i + 2] = PixelSoulsHelpers.ClampToByte(pixels[i + 2] * lightMult + (currentPixelSpec.B * specMult));
            }
            tex.Pixelize(setting, format, pixels);
        }
        public static void PixelizeWithSpecular(this TPF.Texture tex, TPF.Texture specular, PixelArtSetting setting, DXGI_FORMAT format)
        {
            MagickImage diffuseImage = new MagickImage(tex.Headerize());
            MagickImage specularImage = new MagickImage(specular.Headerize());

            byte[] pixels = diffuseImage.GetPixels().ToByteArray("RGBA");
            byte[] specPixels = specularImage.GetPixels().ToByteArray("RGBA");


            if (pixels.Length * 2 != pixels.Length + specPixels.Length)
            {
                Console.WriteLine("mismatched maps");
                tex.Pixelize(setting, format);
                return;
            }

            for (int i = 0; i < pixels.Length; i += 4)
            {
                IMagickColor<byte> currentPixel = new MagickColor(pixels[i], pixels[i + 1], pixels[i + 2]);
                IMagickColor<byte> currentPixelNormal = new MagickColor(128, 128, 255);
                IMagickColor<byte> currentPixelSpec = new MagickColor(specPixels[i], specPixels[i + 1], specPixels[i + 2]);
                byte lightDif = PixelSoulsHelpers.ColorDifferenceLinearRGB(currentPixelNormal, PixelSoulsGlobals.lightDirection);
                float lightMult = 1f - (Math.Clamp((float)lightDif - 85f, 0f, 255f) / (255f - 85f));
                float specMult = lightDif / 255f;
                pixels[i] = PixelSoulsHelpers.ClampToByte(pixels[i] * lightMult + (currentPixelSpec.R * specMult));
                pixels[i + 1] = PixelSoulsHelpers.ClampToByte(pixels[i + 1] * lightMult + (currentPixelSpec.G * specMult));
                pixels[i + 2] = PixelSoulsHelpers.ClampToByte(pixels[i + 2] * lightMult + (currentPixelSpec.B * specMult));
            }
            tex.Pixelize(setting, format, pixels);
        }
        public static Vector3 CalculateNormal(byte red, byte green)
        {
            float x = (2f * (red / 255f)) - 1f;
            float y = (2f * (green / 255f)) - 1f;
            float z =  (float)Math.Sqrt(1f - (x * x) - (y * y));

            return new Vector3(x, y, z);

        }

    }
}
