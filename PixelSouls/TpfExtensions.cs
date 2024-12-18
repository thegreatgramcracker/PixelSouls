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
            
            //diffuse pass
            foreach (TPF.Texture tex in tpf.Textures)
            {
                DXGI_FORMAT format = PixelSoulsHelpers.GetFormat(tex.Format);
                TextureType textureType = PixelSoulsHelpers.GetTextureType(tex.Name, format);
                if (textureType != TextureType.Diffuse) continue;
                Console.WriteLine(tex.Name);
                Console.WriteLine(format);

                PixelArtSetting settingToUse = setting;

                if (textureType == TextureType.Heightmap ||
                    (textureType == TextureType.Cube && PixelSoulsGlobals.pixelizeCubes == false) ||
                    (textureType == TextureType.Lightmap && PixelSoulsGlobals.pixelizeLightmaps == false))
                {
                    textureType = TextureType.Ignore;
                }
                Console.WriteLine(textureType);
                if (textureType == TextureType.Ignore) continue;

                settingToUse = PixelSoulsGlobals.internalTpfSettingsData.SearchFor(tex.Name, settingToUse, textureType);

                if (textureType == TextureType.Cube)
                {
                    tex.PixelizeCube(settingToUse);
                    continue;
                }

                if (PixelSoulsGlobals.pixelizeDiffuseWithNormalAndSpecular)
                {
                    TPF.Texture? normal = tpf.Textures.Find(t => (t.Name == tex.Name + "_n") || (t.Name == tex.Name + "_N"));
                    TPF.Texture? spec = tpf.Textures.Find(t => (t.Name == tex.Name + "_s") || (t.Name == tex.Name + "_S"));
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
                else
                {
                    tex.Pixelize(settingToUse, format);
                }
            }

            //remaining textures pass
            foreach (TPF.Texture tex in tpf.Textures)
            {
                DXGI_FORMAT format = PixelSoulsHelpers.GetFormat(tex.Format);
                TextureType textureType = PixelSoulsHelpers.GetTextureType(tex.Name, format);
                if (textureType == TextureType.Diffuse) continue;
                Console.WriteLine(tex.Name);
                Console.WriteLine(format);

                PixelArtSetting settingToUse = setting;

                if (textureType == TextureType.Heightmap ||
                    (textureType == TextureType.Cube && PixelSoulsGlobals.pixelizeCubes == false) ||
                    (textureType == TextureType.Lightmap && PixelSoulsGlobals.pixelizeLightmaps == false))
                {
                    textureType = TextureType.Ignore;
                }
                Console.WriteLine(textureType);
                if (textureType == TextureType.Ignore) continue;

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

                tex.Pixelize(settingToUse, format);
            }

        }
        public static unsafe void Pixelize(this TPF.Texture tex, PixelArtSetting setting, DXGI_FORMAT format, byte[]? passPixels = null) 
        {
            if (tex.Header.Unk1 == 120) return;
            
            byte[] bytes = PixelSoulsGlobals.game == GameType.DeS ? tex.Headerize() : tex.Bytes;

            GCHandle pinnedArray = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            ScratchImage texImage = TexHelper.Instance.LoadFromDDSMemory(pinnedArray.AddrOfPinnedObject(), bytes.Length, DDS_FLAGS.NONE);

            if (format != DXGI_FORMAT.R8G8B8A8_UNORM) texImage =  texImage.Decompress(DXGI_FORMAT.R8G8B8A8_UNORM);
            
            var stream = texImage.SaveToDDSMemory(DDS_FLAGS.NONE);
            stream.Seek(0, SeekOrigin.Begin);
            byte[] decompressedBytes = new byte[stream.Length];
            stream.Read(decompressedBytes);

            pinnedArray.Free();

            MagickImage magickImage = new MagickImage(decompressedBytes);
            
            magickImage.Settings.SetDefine(MagickFormat.Dds, "compression", "none");
            
            MagickImage originalImage = new MagickImage(magickImage);
            uint originalWidth = magickImage.Width;
            uint originalHeight = magickImage.Height;

            if (passPixels != null)
            {
                magickImage.ImportPixels(passPixels, new PixelImportSettings(magickImage.Width, magickImage.Height, StorageType.Char, "RGBA"));
            }
            magickImage.FilterType = FilterType.Gaussian;
            
            Console.WriteLine(magickImage.ColorSpace.ToString());

            bool resize = true;
            
            //if (magickImage.Width % setting.ScaleFactor != 0 || magickImage.Height % setting.ScaleFactor != 0)
            //{
            //    resize = false;
            //    Console.WriteLine("Unable to resize");
            //}
            if (resize && setting.ScaleFactor != 1)
            {
                if (magickImage.Width == setting.ScaleFactor && magickImage.Height == setting.ScaleFactor)
                {
                    magickImage.Resize(new MagickGeometry(1));
                }
                else if (magickImage.Width % setting.ScaleFactor == 0 && magickImage.Height % setting.ScaleFactor == 0)
                {
                    magickImage.Resize(magickImage.Width / setting.ScaleFactor, magickImage.Height / setting.ScaleFactor);
                    
                }
                else
                {
                    magickImage.Resize((uint)MathF.Ceiling((float)magickImage.Width / setting.ScaleFactor),
                        (uint)MathF.Ceiling((float)magickImage.Height / setting.ScaleFactor));
                }
            }
            byte[] alphaPixels = null;
            if (magickImage.HasAlpha)
            {
                alphaPixels = magickImage.GetPixels().ToByteArray("A");
                foreach (Pixel pixel in magickImage.GetPixels())
                {
                    pixel.SetChannel(3, 255);
                }
            }
            
            
            //magickImage.HasAlpha = true;
            //magickImage.Format = MagickFormat.Dds;
            //magickImage.BackgroundColor = MagickColors.Black;
            //magickImage.Alpha(AlphaOption.Remove);


            magickImage.Modulate(new Percentage(setting.Value), new Percentage(setting.Saturation), new Percentage(setting.Hue));
            magickImage.BrightnessContrast(new Percentage(setting.Brightness), new Percentage(setting.Contrast));
            magickImage.Tint(new MagickGeometry(setting.TintOpacity), setting.TintColor);

            
            //MagickImage noAlpImage = (MagickImage)magickImage.UniqueColors();
            //noAlpImage.Alpha(AlphaOption.Opaque);

            if (setting.MaxColors < 255 && magickImage.TotalColors > setting.MaxColors)
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
                if (setting.DitherMatrix == "ps1")
                {
                    PixelSoulsHelpers.PS1Dither(magickImage);
                }
                else
                {
                    magickImage.OrderedDither(setting.DitherMatrix);
                }
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
                        //pixel.SetChannel(3, PixelSoulsHelpers.ConvertToPS1ColorChannel(pixel.ToColor().A));
                    }
                    else
                    {
                        IMagickColor<byte> closestColor = PixelSoulsHelpers.ClosestColor(pixel.ToColor(), setting.Colors, setting.ColorConvertMode);

                        pixel.SetChannel(0, closestColor.R);
                        pixel.SetChannel(1, closestColor.G);
                        pixel.SetChannel(2, closestColor.B);
                        //pixel.SetChannel(3, closestColor.A);
                    }
                }
            }
            
            //magickImage.BackgroundColor = MagickColors.Transparent;
            //magickImage.BackgroundColor.
            if (alphaPixels != null)
            {
                int alphaIndex = 0;
                foreach (Pixel pixel in magickImage.GetPixels())
                {
                    pixel.SetChannel(3, PixelSoulsHelpers.ConvertToPS1AlphaChannel(alphaPixels[alphaIndex]));
                    alphaIndex++;
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
            magickImage.Crop(originalImage.Width, originalImage.Height);
            magickImage.ResetPage();




            GCHandle pinnedArray2 = GCHandle.Alloc(magickImage.ToByteArray(), GCHandleType.Pinned);

            ScratchImage finalImage = TexHelper.Instance.LoadFromDDSMemory(pinnedArray2.AddrOfPinnedObject(), magickImage.ToByteArray().Length, DDS_FLAGS.NONE);
            finalImage = finalImage.GenerateMipMaps(TEX_FILTER_FLAGS.POINT, 0);
            if (format != DXGI_FORMAT.R8G8B8A8_UNORM) finalImage = finalImage.Compress(format, format == DXGI_FORMAT.BC7_UNORM ? TEX_COMPRESS_FLAGS.BC7_QUICK : TEX_COMPRESS_FLAGS.DEFAULT, 0.5f);



            if (PixelSoulsGlobals.game == GameType.DeS)
            {
                byte* pixels = (byte*)finalImage.GetImage(0).Pixels;
                int len = tex.Bytes.Length;
                Console.WriteLine(len);
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
                    Console.WriteLine(imageBytes.Length);
                    memoryStream.Read(imageBytes);
                    tex.Bytes = imageBytes;
                }
            }
            pinnedArray2.Free();
        }

        public static unsafe void PixelizeCube(this TPF.Texture tex, PixelArtSetting setting)
        {

            byte[] bytes = PixelSoulsGlobals.game == GameType.DeS ? tex.Headerize() : tex.Bytes;
            GCHandle pinnedArray = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            ScratchImage cubeImage = TexHelper.Instance.LoadFromDDSMemory(pinnedArray.AddrOfPinnedObject(), bytes.Length, DDS_FLAGS.NONE);

            cubeImage = cubeImage.Decompress(DXGI_FORMAT.R8G8B8A8_UNORM);

            for (int i = 0; i < cubeImage.GetImageCount(); i++)
            {
                Console.WriteLine("Image " + i);
                var img = cubeImage.GetImage(i);
                Console.WriteLine(img.Format);
                Console.WriteLine(img.Width + ", " + img.Height);
                byte* ptr = (byte*)img.Pixels;
                int len = img.Width * img.Height * 4;

                int offset = 0;
                for (int j = 0; j < img.Width * img.Height; j++)
                {
                    
                    if (setting.ColorConvertMode == "PS1")
                    {
                        ptr[offset] = PixelSoulsHelpers.ConvertToPS1ColorChannel(ptr[offset]);
                        ptr[offset + 1] = PixelSoulsHelpers.ConvertToPS1ColorChannel(ptr[offset + 1]);
                        ptr[offset + 2] = PixelSoulsHelpers.ConvertToPS1ColorChannel(ptr[offset + 2]);
                        ptr[offset + 3] = PixelSoulsHelpers.ConvertToPS1AlphaChannel(ptr[offset + 3]);
                    }
                    else
                    {
                        MagickColor pixelColor = new MagickColor(ptr[offset], ptr[offset + 1], ptr[offset + 2], ptr[offset + 3]);
                        IMagickColor<byte> closestColor = PixelSoulsHelpers.ClosestColor(pixelColor, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
                        ptr[offset] = closestColor.R;
                        ptr[offset + 1] = closestColor.G;
                        ptr[offset + 2] = closestColor.B;
                        ptr[offset + 3] = closestColor.A;
                    }
                    offset += 4;
                }
            }
            cubeImage = cubeImage.Compress(DXGI_FORMAT.BC6H_UF16, TEX_COMPRESS_FLAGS.PARALLEL, 0.5f);
            var stream = cubeImage.SaveToDDSMemory(DDS_FLAGS.NONE);
            stream.Seek(0, SeekOrigin.Begin);
            pinnedArray.Free();

            byte[] finalBytes = new byte[stream.Length];
            stream.Read(finalBytes);
            tex.Bytes = finalBytes;
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
                
                if (pixels.Length * 2 == pixels.Length + normalPixels.Length)
                {
                    //if at least the normal dimensions match, use normal
                    tex.PixelizeWithNormal(normal, setting, format);
                    return;
                }
                else if (pixels.Length * 2 == pixels.Length + specPixels.Length)
                {
                    //if at least the specular dimensions match, use specular
                    tex.PixelizeWithSpecular(specular, setting, format);
                    return;
                }
                tex.Pixelize(setting, format);
                return;
            }

            for (int i = 0; i < pixels.Length; i += 4)
            {
                IMagickColor<byte> currentPixel = new MagickColor(pixels[i], pixels[i + 1], pixels[i + 2]);
                IMagickColor<byte> currentPixelNormal = new MagickColor(normalPixels[i], normalPixels[i + 1], normalPixels[i + 2]);
                IMagickColor<byte> currentPixelSpec = new MagickColor(specPixels[i], specPixels[i + 1], specPixels[i + 2]);
                //Console.WriteLine(currentPixelSpec.ToHexString());
                //float lightDif = PixelSoulsHelpers.ColorDifferenceLinearRGB(currentPixelNormal, PixelSoulsGlobals.lightDirection); //the difference between the normal pixel and normal color that would be directly facing the light source
                float lightDif = Vector3.Dot(CalculateNormal(PixelSoulsGlobals.lightDirection.R, PixelSoulsGlobals.lightDirection.G),
                    CalculateNormal(currentPixelNormal.R, currentPixelNormal.G));
                lightDif = lightDif * 0.5f + 0.5f;
                float lightMult = Math.Clamp(lightDif + 0.1f, 0f, 1f); //the brightness of the pixel according to normal map and light direction
                lightMult = lightMult * lightMult; // steeper falloff
                float specMult = 0.5f * (float)Math.Pow(lightDif, 15f); //the specular highlight power according to the pixel brightness
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
                //float lightDif = PixelSoulsHelpers.ColorDifferenceLinearRGB(currentPixelNormal, PixelSoulsGlobals.lightDirection); //the difference between the normal pixel and normal color that would be directly facing the light source
                float lightDif = Vector3.Dot(CalculateNormal(PixelSoulsGlobals.lightDirection.R, PixelSoulsGlobals.lightDirection.G),
                    CalculateNormal(currentPixelNormal.R, currentPixelNormal.G));
                lightDif = lightDif * 0.5f + 0.5f;
                float lightMult = Math.Clamp(lightDif + 0.1f, 0f, 1f); //the brightness of the pixel according to normal map and light direction
                lightMult = lightMult * lightMult; // steeper falloff
                float specMult = 0.5f * (float)Math.Pow(lightDif, 15f); //the specular highlight power according to the pixel brightness
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
                //float lightDif = PixelSoulsHelpers.ColorDifferenceLinearRGB(currentPixelNormal, PixelSoulsGlobals.lightDirection); //the difference between the normal pixel and normal color that would be directly facing the light source
                float lightDif = Vector3.Dot(CalculateNormal(PixelSoulsGlobals.lightDirection.R, PixelSoulsGlobals.lightDirection.G),
                    CalculateNormal(currentPixelNormal.R, currentPixelNormal.G));
                lightDif = lightDif * 0.5f + 0.5f;
                float lightMult = Math.Clamp(lightDif + 0.1f, 0f, 1f); //the brightness of the pixel according to normal map and light direction
                lightMult = lightMult * lightMult; // steeper falloff
                float specMult = 0.5f * (float)Math.Pow(lightDif, 15f); //the specular highlight power according to the pixel brightness
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
            float z = (float)Math.Sqrt(1f - (x * x) - (y * y));

            return Vector3.Normalize(new Vector3(x, y, z));

        }

    }
}
