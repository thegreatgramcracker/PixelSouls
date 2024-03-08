using DirectXTexNet;
using ImageMagick;
using MeshDecimator.Algorithms;
using MeshDecimator.Math;
using MeshDecimator;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace PixelSouls
{
    public static class PixelSoulsHelpers
    {
        public static byte[] Pixelize(TPF.Texture tex, PixelArtSettings settings)
        {
            //converts a DDS texture from Dark Souls in to pixel art
            bool isDiffuse = true;
            if (tex.Name.EndsWith("_s") || tex.Name.EndsWith("_n") || tex.Name.Contains("HeightMap") || tex.Name.EndsWith("_h") ||
                        tex.Name.EndsWith("_t") || tex.Name.EndsWith("_M"))
            {
                isDiffuse = false;
            }
            DXGI_FORMAT format = GetFormat(tex.Format);

            byte[] finalBytes = null;
            byte[] convertBytes = null;

            Console.WriteLine(format);
            if (tex.Name.Contains("_lit_") || format == DXGI_FORMAT.R16G16_FLOAT)
            {
                return tex.Bytes;
            }
            if (format == DXGI_FORMAT.BC6H_UF16)
            {

                return PixelizeCube(tex.Bytes);
            }
            else
            {
                convertBytes = tex.Bytes;
            }
            TEX_COMPRESS_FLAGS compressFlags = TEX_COMPRESS_FLAGS.DEFAULT;
            MagickImage imageFromBytes = new MagickImage(convertBytes, MagickFormat.Dds);
            bool resize = true;
            if (imageFromBytes.Width % settings.scaleFactor != 0 || imageFromBytes.Height % settings.scaleFactor != 0)
            {
                resize = false;
                Console.WriteLine("Unable to resize");
            }
            if (resize && settings.scaleFactor != 1)
            {


                if (imageFromBytes.Width == settings.scaleFactor && imageFromBytes.Height == settings.scaleFactor)
                {
                    imageFromBytes.Resize(new MagickGeometry(1));
                }
                else
                {
                    imageFromBytes.Blur(0, 1.8);
                    imageFromBytes.WaveletDenoise(10);
                    imageFromBytes.InterpolativeResize(imageFromBytes.Width / settings.scaleFactor, imageFromBytes.Height / settings.scaleFactor, PixelInterpolateMethod.Nearest);
                }
            }

            if (isDiffuse)
            {

                imageFromBytes.Modulate(new Percentage(settings.valueCorrection), new Percentage(settings.saturationCorrection), new Percentage(settings.hueCorrection));

                foreach (Pixel pixel in imageFromBytes.GetPixels())
                {

                    IMagickColor<byte> closestColor = ClosestColor(pixel.ToColor(), settings.colors, PixelSoulsGlobals.diffuseColorConvertMode);
                    pixel.SetChannel(0, closestColor.R);
                    pixel.SetChannel(1, closestColor.G);
                    pixel.SetChannel(2, closestColor.B);
                    pixel.SetChannel(3, closestColor.A);
                }
            }
            else
            {

                if (tex.Name.Contains("DetailBump") || tex.Name.Contains("detailbump"))
                {
                    //flatten normal
                    foreach (Pixel pixel in imageFromBytes.GetPixels())
                    {
                        pixel.SetChannel(0, 127);
                        pixel.SetChannel(1, 127);
                    }
                }
                else if (tex.Name.EndsWith("_n"))
                {
                    //currentPaletteName = normalMapPaletteName;
                    //uniquePixels = normalMapPixels;
                    foreach (Pixel pixel in imageFromBytes.GetPixels())
                    {

                        IMagickColor<byte> closestColor = ClosestColor(pixel.ToColor(), PixelSoulsGlobals.normalMapColors, PixelSoulsGlobals.normalColorConvertMode);
                        pixel.SetChannel(0, closestColor.R);
                        pixel.SetChannel(1, closestColor.G);
                        pixel.SetChannel(2, closestColor.B);
                        pixel.SetChannel(3, closestColor.A);
                    }
                }
                else if (tex.Name.EndsWith("_s"))
                {
                    bool add = false;
                    foreach (Pixel pixel in imageFromBytes.GetPixels())
                    {
                        if (pixel.ToColor().A < 253)
                        {
                            if (!add)
                            {
                                Console.WriteLine("ADD, alpha: " + pixel.ToColor().A);
                                add = true;
                            }
                            // if the specular is not full alpha, it means it's an add texture, so color it
                            IMagickColor<byte> closestColor = ClosestColor(pixel.ToColor(), settings.colors, PixelSoulsGlobals.diffuseColorConvertMode);
                            pixel.SetChannel(0, closestColor.R);
                            pixel.SetChannel(1, closestColor.G);
                            pixel.SetChannel(2, closestColor.B);
                        }
                    }
                }
            }

            if (resize) imageFromBytes.Scale(imageFromBytes.Width * settings.scaleFactor, imageFromBytes.Height * settings.scaleFactor);

            if (format == DXGI_FORMAT.BC3_UNORM) //bc3
            {
                imageFromBytes.Settings.SetDefine(MagickFormat.Dds, "compression", "dxt5");
                finalBytes = imageFromBytes.ToByteArray(MagickFormat.Dxt5);
            }
            else
            {
                if (format == DXGI_FORMAT.BC7_UNORM)
                {
                    compressFlags = TEX_COMPRESS_FLAGS.BC7_QUICK;
                }
                imageFromBytes.Settings.SetDefine(MagickFormat.Dds, "compression", "dxt5");
                finalBytes = ConvertDDSArrayOfBytes(imageFromBytes.ToByteArray(), format, compressFlags);

                //GCHandle pinnedArray = GCHandle.Alloc(imageFromBytes.ToByteArray(), GCHandleType.Pinned);
                //var image = TexHelper.Instance.LoadFromDDSMemory(pinnedArray.AddrOfPinnedObject(), imageFromBytes.ToByteArray().Length, DDS_FLAGS.NONE);
                //image = image.Decompress(DXGI_FORMAT.B8G8R8A8_UNORM);
                //image = image.Compress(format, compressFlags, 0.15f);
                //image.OverrideFormat(format);
                //var stream = image.SaveToDDSMemory(DDS_FLAGS.NONE);
                //stream.Seek(0, SeekOrigin.Begin);
                //pinnedArray.Free();

                //finalBytes = new byte[stream.Length];
                //stream.Read(finalBytes);
            }
            return finalBytes;
        }

        public static unsafe byte[] PixelizeCube(byte[] tex)
        {
            //pixelizes cubemaps, uses a lot of CPU power to do so.
            GCHandle pinnedArray = GCHandle.Alloc(tex, GCHandleType.Pinned);

            ScratchImage cubeImage = TexHelper.Instance.LoadFromDDSMemory(pinnedArray.AddrOfPinnedObject(), tex.Length, DDS_FLAGS.NONE);

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
                    MagickColor pixelColor = new MagickColor(ptr[offset], ptr[offset + 1], ptr[offset + 2], ptr[offset + 3]);
                    IMagickColor<byte> closestColor = ClosestColor(pixelColor, PixelSoulsGlobals.defaultPixelSettings.colors, PixelSoulsGlobals.diffuseColorConvertMode);
                    ptr[offset] = closestColor.R;
                    ptr[offset + 1] = closestColor.G;
                    ptr[offset + 2] = closestColor.B;
                    ptr[offset + 3] = closestColor.A;
                    offset += 4;
                }
            }
            cubeImage = cubeImage.Compress(DXGI_FORMAT.BC6H_UF16, TEX_COMPRESS_FLAGS.PARALLEL, 0.15f);
            cubeImage.OverrideFormat(DXGI_FORMAT.BC6H_UF16);
            var stream = cubeImage.SaveToDDSMemory(DDS_FLAGS.NONE);
            stream.Seek(0, SeekOrigin.Begin);
            pinnedArray.Free();

            byte[] finalBytes = new byte[stream.Length];
            stream.Read(finalBytes);
            return finalBytes;
        }

        static byte[] ConvertDDSArrayOfBytes(byte[] inputBytes, DXGI_FORMAT targetFormat, TEX_COMPRESS_FLAGS compressFlags = TEX_COMPRESS_FLAGS.DEFAULT, DXGI_FORMAT decompressFormat = DXGI_FORMAT.B8G8R8A8_UNORM)
        {
            //shorthand function to change the DDS format of a texture
            GCHandle pinnedArray = GCHandle.Alloc(inputBytes, GCHandleType.Pinned);
            ScratchImage image = TexHelper.Instance.LoadFromDDSMemory(pinnedArray.AddrOfPinnedObject(), inputBytes.Length, DDS_FLAGS.NONE);
            ScratchImage image2 = image.CreateImageCopy(0, true, CP_FLAGS.NONE);
            Console.WriteLine(image2.GetImageCount());
            image = image.Decompress(decompressFormat);
            image = image.Compress(targetFormat, compressFlags, 0.15f);
            image.OverrideFormat(targetFormat);
            var stream = image.SaveToDDSMemory(DDS_FLAGS.NONE);
            stream.Seek(0, SeekOrigin.Begin);
            pinnedArray.Free();

            byte[] finalBytes = new byte[stream.Length];
            stream.Read(finalBytes);
            return finalBytes;
        }


        public static DXGI_FORMAT GetFormat(byte formatFlag)
        {
            //returns the dds format of a dds texture depending on the format flag in the tpf relating to the texture
            switch (formatFlag)
            {
                case 0:
                    return DXGI_FORMAT.BC1_UNORM;
                case 1:
                    return DXGI_FORMAT.BC1_UNORM;
                case 3:
                    return DXGI_FORMAT.BC2_UNORM;
                case 5:
                    return DXGI_FORMAT.BC3_UNORM;
                case 24:
                    return DXGI_FORMAT.BC1_UNORM; //weird normal texture
                case 35:
                    return DXGI_FORMAT.R16G16_FLOAT;
                case 36:
                    return DXGI_FORMAT.BC5_UNORM;
                case 37:
                    return DXGI_FORMAT.BC6H_UF16;
                case 38:
                    return DXGI_FORMAT.BC7_UNORM; // DXGI_FORMAT.BC7_UNORM
                default:
                    Console.WriteLine("unknown format flag " + formatFlag + " found, defaulting to BC1");
                    return DXGI_FORMAT.BC1_UNORM;
            }
        }

        public static IMagickColor<byte> ClosestColor(IMagickColor<byte> inputColor, IPixelCollection<byte> palette, ClosestColorType mode)
        {
            //calculates the closest color on a given palette to the input color, different modes can be used to find the closest color.
            if (inputColor.A <= 0)
            {
                return inputColor;
            }
            double minDist = 9999999;
            IMagickColor<byte> convertColor = palette.GetPixel(0, 0).ToColor();

            if (mode == ClosestColorType.Perceptual)
            {
                //float meanValue = (inputColor.R + inputColor.G + inputColor.B) / 3f;
                //float lumi = (158f - (0.299f * inputColor.R + 0.587f * inputColor.G + 0.114f * inputColor.B)) / 255f;
                foreach (IPixel<byte> pixel in palette)
                {
                    //double dist = Math.Pow((inputColor.R + ( ((inputColor.R - meanValue) * lumi)) - pixel.ToColor().R) * 0.31f, 2) //0.31 og, 0.855 2nd run
                    //    + Math.Pow((inputColor.G + (((inputColor.G - meanValue) * lumi))  - pixel.ToColor().G) * 0.55f, 2) //0.55 og, 0.776 2nd run
                    //    + Math.Pow((inputColor.B + (((inputColor.B - meanValue) * lumi)) - pixel.ToColor().B) * 0.18f, 2); //0.18 og, 0.61 2nd run
                    double dist = ColorDifference(inputColor, pixel.ToColor());
                    if (dist < minDist)
                    {
                        convertColor = pixel.ToColor();
                        minDist = dist;
                    }

                }
            }
            else if (mode == ClosestColorType.Value)
            {
                byte inputVal = new byte[] { inputColor.R, inputColor.G, inputColor.B }.Max();
                if (inputVal < 8)
                {
                    convertColor = new MagickColor(0, 0, 0);
                }
                else if (inputVal < 64)
                {
                    convertColor = palette.GetPixel(0, 0).ToColor();
                }
                else if (inputVal < 128)
                {
                    convertColor = palette.GetPixel(1, 0).ToColor();
                }
                else if (inputVal < 192)
                {
                    convertColor = palette.GetPixel(2, 0).ToColor();
                }
                else
                {
                    convertColor = palette.GetPixel(3, 0).ToColor();
                }
            }
            else if (mode == ClosestColorType.LinearRGB)
            {
                foreach (IPixel<byte> pixel in palette)
                {
                    double dist = ColorDifferenceLinearRGB(inputColor, pixel.ToColor());
                    if (dist < minDist)
                    {
                        convertColor = pixel.ToColor();
                        minDist = dist;
                    }

                }
            }
            if (inputColor.A < 36)
            {

                convertColor.A = 0;
            }
            else if (inputColor.A < 115)
            {
                convertColor.A = 72;
            }
            else if (inputColor.A < 183)
            {
                convertColor.A = 157;
            }
            else if (inputColor.A < 232)
            {
                convertColor.A = 209;
            }
            else
            {
                convertColor.A = 255;
            }
            return convertColor;
        }

        public static double ColorDifference(IMagickColor<byte> color1, IMagickColor<byte> color2)
        {
            //returns the distance between two color's RGB values using perceptual weighting
            double redMean = 0.5f * (color1.R + color2.R);
            double deltaRed = color1.R - color2.R;
            double deltaGreen = color1.G - color2.G;
            double deltaBlue = color1.B - color2.B;

            double dist = Math.Sqrt((2 + (redMean / 256f)) * Math.Pow(deltaRed, 2) + 4 * Math.Pow(deltaGreen, 2) + (2 + ((255 - redMean) / 256f)) * Math.Pow(deltaBlue, 2));

            return dist;
        }

        public static byte ColorDifferenceLinearRGB(IMagickColor<byte> color1, IMagickColor<byte> color2)
        {
            //returns the linear total distance of two colors RGB values
            byte dist = ClampToByte(Math.Abs(color1.R - color2.R) + Math.Abs(color1.G - color2.G) + Math.Abs(color1.B - color2.B));

            return dist;
        }
        public static byte ClampToByte(float val)
        {
            //shorthand function
            return (byte)Math.Clamp(val, 0, 255);
        }

        public static bool IsBlackAndWhite(MagickImage img, int threshold)
        {
            //returns if a magickImage is black and white within a specified threshold (obsolete)
            foreach (Pixel pixel in img.GetPixels())
            {
                byte[] rgb = new byte[] { pixel.ToColor().R, pixel.ToColor().G, pixel.ToColor().B };
                if (Math.Abs(rgb.Min() - rgb.Max()) > threshold)
                {
                    return false;
                }
            }
            Console.WriteLine("black and white");
            return true;
        }


        public static void ModifyFfxXmlFiles(string[] files)
        {
            //adjusts xml colors if they are xml (buhh)
            foreach (string file in files)
            {
                if (file.EndsWith(".xml"))
                {
                    Console.WriteLine(file);
                    ModifyFfxXml(file);
                }
            }
        }

        public static void ModifyFfxXml(string filePath)
        {
            //adjusts all colors in an ffx xml file to match the palette
            XmlDocument ffxdoc = new XmlDocument();
            ffxdoc.Load(filePath);
            bool changed = false;
            foreach (XmlNode node in ffxdoc.GetElementsByTagName("fx_value_node"))
            {

                XmlAttribute nameAttribute = node.Attributes["name"];
                if (nameAttribute != null)
                {
                    XmlAttribute structAttribute = node.Attributes["struct_type"];
                    if (Regex.IsMatch(nameAttribute.Value, @"Color\dR") || nameAttribute.Value == "ColorR" ||
                        (nameAttribute.Value == "UnkField7_1" && node.ParentNode.Name == "fx_action_data" && node.ParentNode.Attributes["struct_type"].Value == "ActionParamStructType43") ||
                        (nameAttribute.Value == "UnkField7_5" && node.ParentNode.Name == "fx_action_data" && node.ParentNode.Attributes["struct_type"].Value == "ActionParamStructType59"))
                    {
                        if (structAttribute != null)
                        {
                            EditValueNodeColor(node, node.NextSibling, node.NextSibling.NextSibling);
                            changed = true;
                        }
                    }
                    //else if (nameAttribute.Value == "DS1R_ColorModR")
                    //{
                    //    if (structAttribute != null && structAttribute.Value == "ConstFloat")
                    //    {
                    //        float[] currentColor = new float[] { float.Parse(node.Attributes["Value"].Value),
                    //            float.Parse(node.NextSibling.Attributes["Value"].Value),
                    //            float.Parse(node.NextSibling.NextSibling.Attributes["Value"].Value) 
                    //        };
                    //        float currentMult = Math.Max(1f, currentColor.Max());
                    //        MagickColor currentXmlColor = new MagickColor(
                    //            ClampToByte(currentColor[0] * 255 / currentMult),
                    //            ClampToByte(currentColor[1] * 255 / currentMult),
                    //            ClampToByte(currentColor[2] * 255 / currentMult));
                    //        IMagickColor<byte> currentClosestColor = ClosestColor(currentXmlColor, uniquePixels, diffuseColorConvertMode);
                    //        node.Attributes["Value"].Value = (currentClosestColor.R / 255f * currentMult).ToString("N9");
                    //        node.NextSibling.Attributes["Value"].Value = (currentClosestColor.G / 255f * currentMult).ToString("N9");
                    //        node.NextSibling.NextSibling.Attributes["Value"].Value = (currentClosestColor.B / 255f * currentMult).ToString("N9");
                    //        changed = true;
                    //    }
                    //}

                }
            }
            foreach (XmlNode node in ffxdoc.GetElementsByTagName("color_tick"))
            {
                MagickColor xmlColor = new MagickColor(
                    ClampToByte(float.Parse(node.Attributes["r"].Value) * 255),
                    ClampToByte(float.Parse(node.Attributes["g"].Value) * 255),
                    ClampToByte(float.Parse(node.Attributes["b"].Value) * 255));
                IMagickColor<byte> closestColor = PixelSoulsHelpers.ClosestColor(xmlColor, PixelSoulsGlobals.defaultPixelSettings.colors, PixelSoulsGlobals.diffuseColorConvertMode);
                node.Attributes["r"].Value = (closestColor.R / 255f).ToString("N9");
                node.Attributes["g"].Value = (closestColor.G / 255f).ToString("N9");
                node.Attributes["b"].Value = (closestColor.B / 255f).ToString("N9");
                //node.Attributes["r"].Value = "1";
                //node.Attributes["g"].Value = "1";
                //node.Attributes["b"].Value = "1";
                changed = true;
            }
            foreach (XmlNode node in ffxdoc.GetElementsByTagName("color1"))
            {
                MagickColor xmlColor = new MagickColor(
                    ClampToByte(float.Parse(node.Attributes["r"].Value) * 255),
                    ClampToByte(float.Parse(node.Attributes["g"].Value) * 255),
                    ClampToByte(float.Parse(node.Attributes["b"].Value) * 255));
                IMagickColor<byte> closestColor = PixelSoulsHelpers.ClosestColor(xmlColor, PixelSoulsGlobals.defaultPixelSettings.colors, PixelSoulsGlobals.diffuseColorConvertMode);
                node.Attributes["r"].Value = (closestColor.R / 255f).ToString("N9");
                node.Attributes["g"].Value = (closestColor.G / 255f).ToString("N9");
                node.Attributes["b"].Value = (closestColor.B / 255f).ToString("N9");
                changed = true;
            }
            foreach (XmlNode node in ffxdoc.GetElementsByTagName("color2"))
            {
                MagickColor xmlColor = new MagickColor(
                    ClampToByte(float.Parse(node.Attributes["r"].Value) * 255),
                    ClampToByte(float.Parse(node.Attributes["g"].Value) * 255),
                    ClampToByte(float.Parse(node.Attributes["b"].Value) * 255));
                IMagickColor<byte> closestColor = PixelSoulsHelpers.ClosestColor(xmlColor, PixelSoulsGlobals.defaultPixelSettings.colors, PixelSoulsGlobals.diffuseColorConvertMode);
                node.Attributes["r"].Value = (closestColor.R / 255f).ToString("N9");
                node.Attributes["g"].Value = (closestColor.G / 255f).ToString("N9");
                node.Attributes["b"].Value = (closestColor.B / 255f).ToString("N9");
                changed = true;
            }
            foreach (XmlNode node in ffxdoc.GetElementsByTagName("color3"))
            {
                MagickColor xmlColor = new MagickColor(
                    ClampToByte(float.Parse(node.Attributes["r"].Value) * 255),
                    ClampToByte(float.Parse(node.Attributes["g"].Value) * 255),
                    ClampToByte(float.Parse(node.Attributes["b"].Value) * 255));
                IMagickColor<byte> closestColor = PixelSoulsHelpers.ClosestColor(xmlColor, PixelSoulsGlobals.defaultPixelSettings.colors, PixelSoulsGlobals.diffuseColorConvertMode);
                node.Attributes["r"].Value = (closestColor.R / 255f).ToString("N9");
                node.Attributes["g"].Value = (closestColor.G / 255f).ToString("N9");
                node.Attributes["b"].Value = (closestColor.B / 255f).ToString("N9");
                changed = true;
            }
            if (changed)
            {
                ffxdoc.Save(PixelSoulsGlobals.outputDirectory + filePath);
            }


        }

        static void EditValueNodeColor(XmlNode redNode, XmlNode greenNode, XmlNode blueNode)
        {
            /*
             * acquire red, blue, and green node
             * get max number of sequence (only 1 sequence if all are const)
             * for each sequence, get the rgb of each node. Consts are always the same regardless of sequence number. Empty value node counts as 0
             * find closest color for that sequence. 
             * change the color of sequence nodes as it goes, but for const change them at the end with the last sequence color. do not change empty
             * 
             */

            int maxWidth = 1;
            ValueNodeType redNodeType = ValueNodeType.ConstFloat;
            ValueNodeType greenNodeType = ValueNodeType.ConstFloat;
            ValueNodeType blueNodeType = ValueNodeType.ConstFloat;

            switch (redNode.Attributes["struct_type"].Value)
            {
                case "ConstFloat":
                    redNodeType = ValueNodeType.ConstFloat; break;
                case "RangedFloat":
                    redNodeType = ValueNodeType.ConstFloat; break;
                case "FloatSequence":
                    redNodeType = ValueNodeType.FloatSequence; break;
                case "Float3Sequence":
                    redNodeType = ValueNodeType.Float3Sequence;
                    maxWidth = 3;
                    break;
                case "RangedFloat3Sequence":
                    redNodeType = ValueNodeType.Float3Sequence;
                    maxWidth = 3;
                    break;
                case "RepeatingFloatSequence":
                    redNodeType = ValueNodeType.FloatSequence; break;
                case "RangedFloatSequence":
                    redNodeType = ValueNodeType.FloatSequence; break;
                case "EmptyValueNode":
                    Console.WriteLine("red node empty");
                    redNodeType = ValueNodeType.EmptyValueNode; break;
                default:
                    Console.WriteLine("Unknown struct type in red node: " + redNode.Attributes["struct_type"].Value); return;
            }
            switch (greenNode.Attributes["struct_type"].Value)
            {
                case "ConstFloat":
                    greenNodeType = ValueNodeType.ConstFloat; break;
                case "RangedFloat":
                    greenNodeType = ValueNodeType.ConstFloat; break;
                case "FloatSequence":
                    greenNodeType = ValueNodeType.FloatSequence; break;
                case "Float3Sequence":
                    greenNodeType = ValueNodeType.Float3Sequence;
                    maxWidth = 3;
                    break;
                case "RangedFloat3Sequence":
                    greenNodeType = ValueNodeType.Float3Sequence;
                    maxWidth = 3;
                    break;
                case "RepeatingFloatSequence":
                    greenNodeType = ValueNodeType.FloatSequence; break;
                case "RangedFloatSequence":
                    greenNodeType = ValueNodeType.FloatSequence; break;
                case "EmptyValueNode":
                    Console.WriteLine("green node empty");
                    greenNodeType = ValueNodeType.EmptyValueNode; break;
                default:
                    Console.WriteLine("Unknown struct type in green node: " + greenNode.Attributes["struct_type"].Value); return;
            }
            switch (blueNode.Attributes["struct_type"].Value)
            {
                case "ConstFloat":
                    blueNodeType = ValueNodeType.ConstFloat; break;
                case "RangedFloat":
                    blueNodeType = ValueNodeType.ConstFloat; break;
                case "FloatSequence":
                    blueNodeType = ValueNodeType.FloatSequence; break;
                case "Float3Sequence":
                    blueNodeType = ValueNodeType.Float3Sequence;
                    maxWidth = 3;
                    break;
                case "RangedFloat3Sequence":
                    blueNodeType = ValueNodeType.Float3Sequence;
                    maxWidth = 3;
                    break;
                case "RepeatingFloatSequence":
                    blueNodeType = ValueNodeType.FloatSequence; break;
                case "RangedFloatSequence":
                    blueNodeType = ValueNodeType.FloatSequence; break;
                case "EmptyValueNode":
                    Console.WriteLine("blue node empty");
                    blueNodeType = ValueNodeType.EmptyValueNode; break;
                default:
                    Console.WriteLine("Unknown struct type in blue node: " + blueNode.Attributes["struct_type"].Value); return;
            }

            if (!(redNodeType == ValueNodeType.EmptyValueNode && greenNodeType == ValueNodeType.EmptyValueNode && blueNodeType == ValueNodeType.EmptyValueNode))
            {
                if (redNodeType == ValueNodeType.EmptyValueNode)
                {
                    redNode.Attributes["struct_type"].Value = "ConstFloat";
                    XmlAttribute valueAttribute = redNode.OwnerDocument.CreateAttribute("Value");
                    redNode.Attributes.Append(valueAttribute);
                    redNode.Attributes["Value"].Value = "0";
                    redNodeType = ValueNodeType.ConstFloat;
                }
                if (greenNodeType == ValueNodeType.EmptyValueNode)
                {
                    greenNode.Attributes["struct_type"].Value = "ConstFloat";
                    XmlAttribute valueAttribute = greenNode.OwnerDocument.CreateAttribute("Value");
                    greenNode.Attributes.Append(valueAttribute);
                    greenNode.Attributes["Value"].Value = "0";
                    greenNodeType = ValueNodeType.ConstFloat;
                }
                if (blueNodeType == ValueNodeType.EmptyValueNode)
                {
                    blueNode.Attributes["struct_type"].Value = "ConstFloat";
                    XmlAttribute valueAttribute = blueNode.OwnerDocument.CreateAttribute("Value");
                    blueNode.Attributes.Append(valueAttribute);
                    blueNode.Attributes["Value"].Value = "0";
                    blueNodeType = ValueNodeType.ConstFloat;
                }
            }

            int maxSequences = new[] { redNode.ChildNodes.Count, greenNode.ChildNodes.Count, blueNode.ChildNodes.Count }.Max();


            MagickColor constXmlColor = new MagickColor(
                    ClampToByte(GetNodeColor(redNode, redNodeType, 0) * 255),
                    ClampToByte(GetNodeColor(greenNode, greenNodeType, 0) * 255),
                    ClampToByte(GetNodeColor(blueNode, blueNodeType, 0) * 255));
            IMagickColor<byte> constClosestColor = PixelSoulsHelpers.ClosestColor(constXmlColor, PixelSoulsGlobals.defaultPixelSettings.colors, PixelSoulsGlobals.diffuseColorConvertMode);
            for (int i = 0; i < maxSequences; i++)
            {
                for (int j = 0; j < maxWidth; j++)
                {
                    float[] currentColor = new float[] { GetNodeColor(redNode, redNodeType, i, j), GetNodeColor(greenNode, greenNodeType, i, j), GetNodeColor(blueNode, blueNodeType, i, j) };
                    float currentMult = Math.Max(1f, currentColor.Max());
                    MagickColor currentXmlColor = new MagickColor(
                        ClampToByte(currentColor[0] * 255 / currentMult),
                        ClampToByte(currentColor[1] * 255 / currentMult),
                        ClampToByte(currentColor[2] * 255 / currentMult));
                    IMagickColor<byte> currentClosestColor = PixelSoulsHelpers.ClosestColor(currentXmlColor, PixelSoulsGlobals.defaultPixelSettings.colors, PixelSoulsGlobals.diffuseColorConvertMode);
                    if (redNodeType == ValueNodeType.FloatSequence) { redNode.ChildNodes[i].Attributes["value"].Value = (currentClosestColor.R / 255f * currentMult).ToString("N9"); }
                    if (redNodeType == ValueNodeType.Float3Sequence)
                    {
                        if (j == 0) { redNode.ChildNodes[i].Attributes["x"].Value = (currentClosestColor.R / 255f * currentMult).ToString("N9"); }
                        if (j == 1) { redNode.ChildNodes[i].Attributes["y"].Value = (currentClosestColor.R / 255f * currentMult).ToString("N9"); }
                        if (j == 2) { redNode.ChildNodes[i].Attributes["z"].Value = (currentClosestColor.R / 255f * currentMult).ToString("N9"); }
                    }
                    if (greenNodeType == ValueNodeType.FloatSequence) { greenNode.ChildNodes[i].Attributes["value"].Value = (currentClosestColor.G / 255f * currentMult).ToString("N9"); }
                    if (greenNodeType == ValueNodeType.Float3Sequence)
                    {
                        if (j == 0) { greenNode.ChildNodes[i].Attributes["x"].Value = (currentClosestColor.G / 255f * currentMult).ToString("N9"); }
                        if (j == 1) { greenNode.ChildNodes[i].Attributes["y"].Value = (currentClosestColor.G / 255f * currentMult).ToString("N9"); }
                        if (j == 2) { greenNode.ChildNodes[i].Attributes["z"].Value = (currentClosestColor.G / 255f * currentMult).ToString("N9"); }
                    }
                    if (blueNodeType == ValueNodeType.FloatSequence) { blueNode.ChildNodes[i].Attributes["value"].Value = (currentClosestColor.B / 255f * currentMult).ToString("N9"); }
                    if (blueNodeType == ValueNodeType.Float3Sequence)
                    {
                        if (j == 0) { blueNode.ChildNodes[i].Attributes["x"].Value = (currentClosestColor.B / 255f * currentMult).ToString("N9"); }
                        if (j == 1) { blueNode.ChildNodes[i].Attributes["y"].Value = (currentClosestColor.B / 255f * currentMult).ToString("N9"); }
                        if (j == 2) { blueNode.ChildNodes[i].Attributes["z"].Value = (currentClosestColor.B / 255f * currentMult).ToString("N9"); }
                    }
                }

            }
            if (redNodeType == ValueNodeType.ConstFloat) { redNode.Attributes["Value"].Value = (constClosestColor.R / 255f).ToString("N9"); }
            if (greenNodeType == ValueNodeType.ConstFloat) { greenNode.Attributes["Value"].Value = (constClosestColor.G / 255f).ToString("N9"); }
            if (blueNodeType == ValueNodeType.ConstFloat) { blueNode.Attributes["Value"].Value = (constClosestColor.B / 255f).ToString("N9"); }
        }

        static float GetNodeColor(XmlNode node, ValueNodeType nodeType, int sequenceIndex, int widthIndex = 0)
        {
            //gets the color value as a float of an xml node in a ffx xml (sequence index is essentially the which node in the sequence, width is the r, g, or b of that node)
            float colorValue = 0;
            if (nodeType == ValueNodeType.ConstFloat)
            {
                colorValue = float.Parse(node.Attributes["Value"].Value);
            }
            else if (nodeType == ValueNodeType.FloatSequence)
            {
                colorValue = float.Parse(node.ChildNodes[sequenceIndex].Attributes["value"].Value);
            }
            else if (nodeType == ValueNodeType.Float3Sequence)
            {
                if (widthIndex == 0)
                {
                    colorValue = float.Parse(node.ChildNodes[sequenceIndex].Attributes["x"].Value);
                }
                else if (widthIndex == 1)
                {
                    colorValue = float.Parse(node.ChildNodes[sequenceIndex].Attributes["y"].Value);
                }
                else if (widthIndex == 2)
                {
                    colorValue = float.Parse(node.ChildNodes[sequenceIndex].Attributes["z"].Value);
                }
            }
            else if (nodeType == ValueNodeType.EmptyValueNode)
            {
                colorValue = 0f;
            }
            return colorValue;
        }



        #region Experimental

        static byte[] PixelizeDiffuseWithNormal(TPF.Texture diffuse, TPF.Texture normal, PixelArtSettings settings)
        {
            //unfinished function
            byte[] finalBytes = null;

            MagickImage diffuseImage = new MagickImage(diffuse.Bytes, MagickFormat.Dds);
            MagickImage normalImage = new MagickImage(normal.Bytes, MagickFormat.Dds);

            if (diffuseImage.Width != normalImage.Width || diffuseImage.Height != normalImage.Height)
            {
                Console.WriteLine("diffuse and normal do not match");
            }
            diffuseImage.Blur(0, 1.8);
            normalImage.Blur(0, 1.8);
            diffuseImage.WaveletDenoise(10);
            normalImage.WaveletDenoise(10);
            diffuseImage.InterpolativeResize(diffuseImage.Width / settings.scaleFactor, diffuseImage.Height / settings.scaleFactor, PixelInterpolateMethod.Nearest);
            normalImage.InterpolativeResize(normalImage.Width / settings.scaleFactor, normalImage.Height / settings.scaleFactor, PixelInterpolateMethod.Nearest);


            return finalBytes;
        }

        static void DiversePaletteFromImage(IPixelCollection<byte> imagePixels, int colorCount)
        {
            //unused, wip, unfinished, cast in the fire
            List<IMagickColor<byte>> palette = new List<IMagickColor<byte>>();

            foreach (IPixel<byte> pixel in imagePixels)
            {
                if (pixel.ToColor().A <= 0) continue;
                palette.Add(pixel.ToColor());
            }
            int totalIterations = palette.Count;
            int currentIterations = 0;
            Console.WriteLine(currentIterations + " out of " + totalIterations + " iterations done");

            List<ColorDistObject> colorDists = new List<ColorDistObject>();

            for (int i = 0; i < palette.Count; i++)
            {
                IMagickColor<byte> currentColor = palette[i];
                double minDist = 999999;
                for (int j = 0; j < palette.Count; j++)
                {
                    if (i == j) continue;
                    double dist = PixelSoulsHelpers.ColorDifference(palette[i], palette[j]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                    }
                }
                colorDists.Add(new ColorDistObject(currentColor, minDist));
                currentIterations++;
                Console.WriteLine(currentIterations + " out of " + totalIterations + " iterations done");
            }
            colorDists = colorDists.OrderByDescending(o => o.dist).ToList();
            while (colorDists.Count > colorCount)
            {
                IMagickColor<byte>[] lowestPair = new IMagickColor<byte>[] { colorDists[colorDists.Count - 1].color, colorDists[colorDists.Count - 2].color };
                MagickColor newColor = new MagickColor((byte)((lowestPair[0].R + lowestPair[1].R) / 2), (byte)((lowestPair[0].G + lowestPair[1].G) / 2), (byte)((lowestPair[0].B + lowestPair[1].B) / 2));
                double newDist = (colorDists[colorDists.Count - 1].dist + colorDists[colorDists.Count - 2].dist) / 2;
                colorDists.RemoveAt(colorDists.Count - 1);
                colorDists.RemoveAt(colorDists.Count - 1);
                colorDists.Add(new ColorDistObject(newColor, newDist));
            }
            //colorDists.Reverse();
            palette.Clear();
            for (int i = 0; i < colorCount; i++)
            {
                if (i < colorDists.Count)
                {
                    palette.Add(colorDists[i].color);
                }
            }

            //List<ColorDistObject> colorPairs = new List<ColorDistObject>();
            //for (int i = 0; i < palette.Count; i++)
            //{
            //    for (int j = i + 1; j < palette.Count; j++)
            //    {
            //        colorPairs.Add(new ColorDistObject(palette[i], palette[j], ColorDifference(palette[i], palette[j])));
            //    }
            //}
            //colorPairs = colorPairs.OrderByDescending(o => o.dist).ToList();
            //while (palette.Count > colorCount)
            //{

            //    IMagickColor<byte>[] lowestPair = new IMagickColor<byte>[] {colorPairs.Last().color1, colorPairs.Last().color2 };
            //    MagickColor newColor = new MagickColor((byte)((lowestPair[0].R + lowestPair[1].R) / 2), (byte)((lowestPair[0].G + lowestPair[1].G) / 2), (byte)((lowestPair[0].B + lowestPair[1].B) / 2));
            //    colorPairs.RemoveAt(colorPairs.Count - 1);
            //    palette.Remove(lowestPair[0]);
            //    palette.Remove(lowestPair[1]);
            //    List<ColorDistObject> pairsToInsert = new List<ColorDistObject>();
            //    for (int i = 0; i < palette.Count; i++)
            //    {
            //        pairsToInsert.Add(new ColorDistObject(newColor, palette[i], ColorDifference(newColor, palette[i])));
            //    }
            //    for(int i = 0; i < pairsToInsert.Count; i++)
            //    {
            //        for (int j = 0; j < colorPairs.Count; j++)
            //        {
            //            if (pairsToInsert[i].dist < colorPairs[j].dist)
            //            {
            //                colorPairs.Insert(j + 1, pairsToInsert[i]);
            //                break;
            //            }
            //        }
            //    }
            //    palette.Add(newColor);
            //    currentIterations++;
            //    Console.WriteLine(currentIterations + " out of " + totalIterations + " iterations done");
            //}
            int num = 0;
            MagickImage image = new MagickImage(new MagickColor(255, 255, 255, 255), colorCount, 1);
            IPixelCollection<byte> pixels = image.GetPixels();
            foreach (IMagickColor<byte> color in palette)
            {
                pixels.SetPixel(num, 0, color.ToByteArray());
                Console.WriteLine(num + ": " + color.ToString());
                num++;
            }
            image.Write("bingus.png");


        }

        #endregion


        #region UnusedFunctions

        static void CopyLowQualityFlver(string[] destFiles, string[] sourceFiles)
        {
            //unused, requires ds1 PTDE
            BND3 part = null;
            BND3 matchingPart = null;

            BinderFile partFlver = null;
            BinderFile matchingPartFlver = null;
            FLVER2 thing = null;
            int totalFiles = destFiles.Length;
            int currentFiles = 0;

            foreach (string file in destFiles)
            {
                currentFiles++;
                bool isHollow = false;
                bool isFemale = false;
                if (!file.EndsWith(".partsbnd.dcx") && !file.EndsWith(".partsbnd"))
                {
                    continue;
                }
                if (file.EndsWith("_M.partsbnd.dcx") && !file.EndsWith("_M.partsbnd"))
                {
                    isHollow = true;
                }
                if (file.Contains("_F_"))
                {
                    isFemale = true;
                }

                string fileID = Path.GetFileName(file).Substring(0, 9);
                Console.WriteLine(fileID);
                int matchingPartIndex = Array.IndexOf(sourceFiles, "ds1_parts\\" + fileID + "_L.partsbnd");
                if (matchingPartIndex < 0)
                {
                    continue;
                }
                part = BND3.Read(file);
                matchingPart = BND3.Read(sourceFiles[matchingPartIndex]);
                partFlver = part.Files.Find(f => f.Name.EndsWith(".flver"));
                matchingPartFlver = matchingPart.Files.Find(f => f.Name.EndsWith(".flver"));
                string originName = partFlver.Name;
                List<FLVER2.Material> originMTD = FLVER2.Read(partFlver.Bytes).Materials;
                //Dictionary<FLVER2.Material, List<FLVER2.Texture>> textureDict = new Dictionary<FLVER2.Material, List<FLVER2.Texture>>();
                //foreach (FLVER2.Material mtd in FLVER2.Read(partFlver.Bytes).Materials) 
                //{ 
                //    textureDict.Add(mtd, mtd.Textures);
                //}

                partFlver.Bytes = matchingPartFlver.Bytes;
                partFlver.Name = originName;
                FLVER2 tempFlver = FLVER2.Read(partFlver.Bytes);
                foreach (FLVER2.Material mat in tempFlver.Materials)
                {
                    foreach (FLVER2.Texture texture in mat.Textures)
                    {
                        texture.Path = texture.Path.Replace("_L.tga", ".tga");
                        texture.Path = texture.Path.Replace("_L_s.tga", "_s.tga");
                        texture.Path = texture.Path.Replace("_L_n.tga", "_n.tga");
                        if (isHollow)
                        {
                            texture.Path = texture.Path.Replace("M_body", "M_body_M");
                            texture.Path = texture.Path.Replace("FC_M_face.tga", "FG_m_face_org_M_d.tga");
                            texture.Path = texture.Path.Replace("FC_M_face_s.tga", "FG_m_face_org_M_s.tga");
                            texture.Path = texture.Path.Replace("FC_M_face_n.tga", "FG_m_face_org_M_n.tga");
                            if (isFemale)
                            {
                                texture.Path = texture.Path.Replace("F_body", "M_body_M");
                                texture.Path = texture.Path.Replace("FC_F_face.tga", "FG_m_face_org_M_d.tga");
                                texture.Path = texture.Path.Replace("FC_F_face_s.tga", "FG_m_face_org_M_s.tga");
                                texture.Path = texture.Path.Replace("FC_F_face_n.tga", "FG_m_face_org_M_n.tga");
                            }
                            //texture.Path = texture.Path.Replace("M_face", "face_M");
                            //texture.Path = texture.Path.Replace("M_eye", "eye_M");
                        }
                    }
                }

                //tempFlver.Materials = originMTD;
                partFlver.Bytes = tempFlver.Write();

                //foreach (FLVER2.Material mtd in FLVER2.Read(partFlver.Bytes).Materials)
                //{
                //    mtd.Textures = textureDict[mtd];
                //}


                part.Write("conv_" + file);

                Console.WriteLine(currentFiles + " finished out of " + totalFiles + " files.");
            }
        }
        static FLVER2 DecimateFlver(FLVER2 flv)
        {
            //decimates a flver
            //FLVER2 flv = FLVER2.Read(filePath);
            List<Vector3d> meshVerts = new List<Vector3d>();
            List<Vector3> uvs = new List<Vector3>();
            List<BoneWeight> boneWeights = new List<BoneWeight>();
            List<List<int>> indices = new List<List<int>>();
            List<Vector4> colors = new List<Vector4>();
            List<Vector4> tangents = new List<Vector4>();


            float meshID = 0;
            int indicesOffset = 0;
            foreach (FLVER2.BufferLayout layout in flv.BufferLayouts)
            {
                Console.WriteLine("----");
                foreach (FLVER.LayoutMember layoutMember in layout)
                {
                    Console.WriteLine(layoutMember.Semantic);
                }
            }
            foreach (FLVER2.Mesh mesh in flv.Meshes)
            {

                List<int> currentMeshIndices = new List<int>();
                int vertCount = 0;

                foreach (FLVER.Vertex vert in mesh.Vertices)
                {

                    meshVerts.Add(new Vector3d(vert.Position.X, vert.Position.Y, vert.Position.Z));
                    //Console.WriteLine(vert.UVs[0].X + ", " + vert.UVs[0].Y + ", " + vert.UVs[0].Z);
                    uvs.Add(new Vector3(vert.UVs[0].X, vert.UVs[0].Y, vert.UVs[0].Z));

                    if (mesh.BoneIndices.Count > 0)
                    {
                        boneWeights.Add(new BoneWeight(mesh.BoneIndices[vert.BoneIndices[0]], mesh.BoneIndices[vert.BoneIndices[1]], mesh.BoneIndices[vert.BoneIndices[2]], mesh.BoneIndices[vert.BoneIndices[3]],
    vert.BoneWeights[0], vert.BoneWeights[1], vert.BoneWeights[2], vert.BoneWeights[3]));
                    }

                    colors.Add(new Vector4(vert.Colors[0].R, vert.Colors[0].G, vert.Colors[0].B, vert.Colors[0].A));
                    if (flv.BufferLayouts[mesh.VertexBuffers[0].LayoutIndex].Exists(i => i.Semantic == FLVER.LayoutSemantic.Tangent)) { tangents.Add(new Vector4(vert.Tangents[0].X, vert.Tangents[0].Y, vert.Tangents[0].Z, vert.Tangents[0].W)); }
                    vertCount++;
                }
                foreach (int i in mesh.FaceSets[0].Triangulate(false))
                {


                    currentMeshIndices.Add(i + indicesOffset);

                }
                //Console.WriteLine(currentMeshIndices.Count);
                indices.Add(currentMeshIndices);

                meshID++;
                indicesOffset += vertCount;
            }
            Mesh bingus = new Mesh(meshVerts.ToArray(), indices.Select(a => a.ToArray()).ToArray());
            bingus.SetUVs(0, uvs.ToArray());
            bingus.BoneWeights = boneWeights.ToArray();
            bingus.Colors = colors.ToArray();
            //bingus.Tangents = tangents.ToArray();
            bingus.RecalculateTangents();





            FastQuadricMeshSimplification alg = new FastQuadricMeshSimplification();
            alg.Initialize(bingus);
            //alg.Agressiveness = 10;
            //alg.MaxIterationCount = 200000;
            alg.PreserveBorders = true;
            alg.PreserveFoldovers = true;
            //alg.PreserveSeams = true;
            //alg.DecimateMesh((int)Math.Ceiling(bingus.Vertices.Count() / 3 * 0.85));
            alg.DecimateMeshLossless();
            Mesh bingusNoEars = alg.ToMesh();

            bingusNoEars.RecalculateNormals();
            bingusNoEars.RecalculateTangents();

            int meshIDCheck = 0;
            int vertOffset = 0;
            foreach (FLVER2.Mesh mesh in flv.Meshes)
            {
                FLVER.Vertex template = mesh.Vertices[0];
                int verticesRange = bingusNoEars.GetSubMeshIndices()[meshIDCheck].ToList().Distinct().Count();
                if (verticesRange <= 0) continue;
                //bingusNoEars.GetSubMeshIndices()[meshIDCheck].Length / 3;
                mesh.Vertices.Clear();
                //mesh.BoneIndices.Clear();


                //for (int i = 0; i < verticesRange; i++)
                //{
                //    mesh.Vertices.Add(new FLVER.Vertex(template));
                //    debugFinalVertCount++;
                //}
                //Dictionary<int, int> indicesToVertex = new Dictionary<int, int>();
                //mesh.FaceSets = new List<FLVER2.FaceSet> { new FLVER2.FaceSet() };
                //int vertIndex = 0;
                //foreach (int indice in bingusNoEars.GetSubMeshIndices()[meshIDCheck])
                //{

                //    Console.WriteLine(bingusNoEars.BoneWeights.Length);
                //    Console.WriteLine(bingusNoEars.VertexCount);
                //    if (indice > debugHighestIndice)
                //    {
                //        debugHighestIndice = indice;
                //    }
                //    if (!indicesToVertex.ContainsKey(indice))
                //    {
                //        indicesToVertex[indice] = vertIndex;
                //        mesh.Vertices[vertIndex].Position = new System.Numerics.Vector3((float)bingusNoEars.Vertices[indice].x,
                //        (float)bingusNoEars.Vertices[indice].y, (float)bingusNoEars.Vertices[indice].z);
                //        mesh.Vertices[vertIndex].Normal = MathV3toNumericsV3(bingusNoEars.Normals[indice]) * -1;

                //        mesh.Vertices[vertIndex].UVs[0] = MathV3toNumericsV3(bingusNoEars.GetUVs3D(0)[indice]);
                //        mesh.Vertices[vertIndex].Tangents = new List<System.Numerics.Vector4> { new System.Numerics.Vector4(bingusNoEars.Tangents[indice].x,
                //            bingusNoEars.Tangents[indice].y, bingusNoEars.Tangents[indice].z, bingusNoEars.Tangents[indice].w) };
                //        mesh.Vertices[vertIndex].BoneIndices[0] = bingusNoEars.BoneWeights[indice].boneIndex0;
                //        mesh.Vertices[vertIndex].BoneIndices[1] = bingusNoEars.BoneWeights[indice].boneIndex1;
                //        mesh.Vertices[vertIndex].BoneIndices[2] = bingusNoEars.BoneWeights[indice].boneIndex2;
                //        mesh.Vertices[vertIndex].BoneIndices[3] = bingusNoEars.BoneWeights[indice].boneIndex3;
                //        mesh.Vertices[vertIndex].BoneWeights[0] = bingusNoEars.BoneWeights[indice].boneWeight0;
                //        mesh.Vertices[vertIndex].BoneWeights[1] = bingusNoEars.BoneWeights[indice].boneWeight1;
                //        mesh.Vertices[vertIndex].BoneWeights[2] = bingusNoEars.BoneWeights[indice].boneWeight2;
                //        mesh.Vertices[vertIndex].BoneWeights[3] = bingusNoEars.BoneWeights[indice].boneWeight3;
                //        mesh.Vertices[vertIndex].Colors[0] = new FLVER.VertexColor(bingusNoEars.Colors[indice].w, bingusNoEars.Colors[indice].x,
                //            bingusNoEars.Colors[indice].y, bingusNoEars.Colors[indice].z);
                //        mesh.FaceSets[0].Indices.Add(vertIndex);
                //        vertIndex++;
                //    }
                //    else
                //    {
                //        mesh.FaceSets[0].Indices.Add(indicesToVertex[indice]);
                //    }
                //}

                for (int i = vertOffset; i < verticesRange + vertOffset; i++)
                {
                    //Console.WriteLine("----------");
                    //Console.WriteLine(meshIDCheck);
                    //Console.WriteLine(ArrayArrayLength(bingusNoEars.GetSubMeshIndices()) / 3); //more somehow
                    //Console.WriteLine(bingusNoEars.VertexCount);
                    //Console.WriteLine(verticesRange);
                    //Console.WriteLine(i);
                    //Console.WriteLine(vertOffset);
                    mesh.Vertices.Add(new FLVER.Vertex(template));
                    mesh.Vertices.Last().Position = new System.Numerics.Vector3((float)bingusNoEars.Vertices[i].x,
                        (float)bingusNoEars.Vertices[i].y, (float)bingusNoEars.Vertices[i].z);


                    //Console.WriteLine(bingusNoEars.Normals.Length);
                    mesh.Vertices.Last().Normal = MathV3toNumericsV3(bingusNoEars.Normals[i] * -1);

                    mesh.Vertices.Last().UVs[0] = MathV3toNumericsV3(bingusNoEars.GetUVs3D(0)[i]);

                    if (template.Tangents.Any())
                    {
                        mesh.Vertices.Last().Tangents[0] = new System.Numerics.Vector4(bingusNoEars.Tangents[i].x,
                        bingusNoEars.Tangents[i].y, bingusNoEars.Tangents[i].z, bingusNoEars.Tangents[i].w);
                        //mesh.Vertices.Last().bi
                        Vector3 cross;
                        Vector3 v3Tan = new Vector3(bingusNoEars.Tangents[i].x, bingusNoEars.Tangents[i].y, bingusNoEars.Tangents[i].z);
                        Vector3.Cross(ref bingusNoEars.Normals[i], ref v3Tan, out cross);
                        cross = cross * bingusNoEars.Tangents[i].w;
                        mesh.Vertices.Last().Bitangent = new System.Numerics.Vector4(cross.x, cross.y, cross.z, 0);
                    }
                    mesh.Vertices.Last().BoneIndices[0] = mesh.BoneIndices.IndexOf(bingusNoEars.BoneWeights[i].boneIndex0);
                    //if (!mesh.BoneIndices.Contains(bingusNoEars.BoneWeights[i].boneIndex0)) { mesh.BoneIndices.Add(bingusNoEars.BoneWeights[i].boneIndex0); }
                    mesh.Vertices.Last().BoneIndices[1] = mesh.BoneIndices.IndexOf(bingusNoEars.BoneWeights[i].boneIndex1);
                    //if (!mesh.BoneIndices.Contains(bingusNoEars.BoneWeights[i].boneIndex1)) { mesh.BoneIndices.Add(bingusNoEars.BoneWeights[i].boneIndex1); }
                    mesh.Vertices.Last().BoneIndices[2] = mesh.BoneIndices.IndexOf(bingusNoEars.BoneWeights[i].boneIndex2);
                    //if (!mesh.BoneIndices.Contains(bingusNoEars.BoneWeights[i].boneIndex2)) { mesh.BoneIndices.Add(bingusNoEars.BoneWeights[i].boneIndex2); }
                    mesh.Vertices.Last().BoneIndices[3] = mesh.BoneIndices.IndexOf(bingusNoEars.BoneWeights[i].boneIndex3);
                    //if (!mesh.BoneIndices.Contains(bingusNoEars.BoneWeights[i].boneIndex3)) { mesh.BoneIndices.Add(bingusNoEars.BoneWeights[i].boneIndex3); }
                    mesh.Vertices.Last().BoneWeights[0] = bingusNoEars.BoneWeights[i].boneWeight0;
                    mesh.Vertices.Last().BoneWeights[1] = bingusNoEars.BoneWeights[i].boneWeight1;
                    mesh.Vertices.Last().BoneWeights[2] = bingusNoEars.BoneWeights[i].boneWeight2;
                    mesh.Vertices.Last().BoneWeights[3] = bingusNoEars.BoneWeights[i].boneWeight3;
                    mesh.Vertices.Last().Colors[0] = new FLVER.VertexColor(bingusNoEars.Colors[i].w, bingusNoEars.Colors[i].x,
                            bingusNoEars.Colors[i].y, bingusNoEars.Colors[i].z);



                }

                mesh.FaceSets = new List<FLVER2.FaceSet> { new FLVER2.FaceSet() { Indices = bingusNoEars.GetSubMeshIndices()[meshIDCheck].ToList() } };
                for (int i = 0; i < mesh.FaceSets[0].Indices.Count; i++)
                {
                    mesh.FaceSets[0].Indices[i] -= vertOffset;
                }
                //mesh.BoneIndices.Sort();
                Console.WriteLine(mesh.Vertices.Count);
                Console.WriteLine(meshIDCheck);
                SolveTangentsDemonsSouls(mesh, 1);
                vertOffset += verticesRange;
                meshIDCheck++;
            }
            Console.WriteLine(bingusNoEars.Indices.Length);
            return flv;
            //flv.Write(outputPrefix + filePath);
        }

        static void DecimateFlverBND(string filePath)
        {
            //uses mesh decimator on binder if it contains a flver
            BND3 flverBND = BND3.Read(filePath);
            foreach (BinderFile file in flverBND.Files)
            {
                if (file.Name.EndsWith(".flver") || file.Name.EndsWith(".flver.dcx"))
                {
                    FLVER2 flvFile = FLVER2.Read(file.Bytes);
                    if (!flvFile.Meshes.Any() || flvFile.Meshes.Sum(nv => nv.Vertices.Count) <= 0) continue;
                    Console.WriteLine(file.Name);
                    file.Bytes = DecimateFlver(flvFile).Write();


                }
            }
            flverBND.Write(PixelSoulsGlobals.outputDirectory + "dec_" + filePath);
        }

        static System.Numerics.Vector3 MathV3toNumericsV3(MeshDecimator.Math.Vector3 v3)
        {
            //shorthand function
            return new System.Numerics.Vector3(v3.x, v3.y, v3.z);
        }

        static void SolveTangentsDemonsSouls(FLVER2.Mesh mesh, int version)
        {
            //function borrowed with permission from Shadowth117
            if (mesh.Vertices?[0].Tangents.Count > 0)
            {
                var vertexIndices = mesh.FaceSets[0].Triangulate(false);

                int vertexCount = mesh.Vertices.Count;

                System.Numerics.Vector3[] tan1 = new System.Numerics.Vector3[vertexCount];
                System.Numerics.Vector3[] tan2 = new System.Numerics.Vector3[vertexCount];

                for (int a = 0; a < vertexIndices.Count; a += 3)
                {
                    int i1 = vertexIndices[a];
                    int i2 = vertexIndices[a + 1];
                    int i3 = vertexIndices[a + 2];

                    if (i1 != i2 || i2 != i3)
                    {
                        System.Numerics.Vector3 v1 = mesh.Vertices[i1].Position;
                        System.Numerics.Vector3 v2 = mesh.Vertices[i2].Position;
                        System.Numerics.Vector3 v3 = mesh.Vertices[i3].Position;

                        System.Numerics.Vector2 w1 = new System.Numerics.Vector2(mesh.Vertices[i1].UVs[0].X, mesh.Vertices[i1].UVs[0].Y);
                        System.Numerics.Vector2 w2 = new System.Numerics.Vector2(mesh.Vertices[i2].UVs[0].X, mesh.Vertices[i2].UVs[0].Y);
                        System.Numerics.Vector2 w3 = new System.Numerics.Vector2(mesh.Vertices[i3].UVs[0].X, mesh.Vertices[i3].UVs[0].Y);

                        float x1 = v2.X - v1.X;
                        float x2 = v3.X - v1.X;
                        float y1 = v2.Y - v1.Y;
                        float y2 = v3.Y - v1.Y;
                        float z1 = v2.Z - v1.Z;
                        float z2 = v3.Z - v1.Z;

                        float s1 = w2.X - w1.X;
                        float s2 = w3.X - w1.X;
                        float t1 = w2.Y - w1.Y;
                        float t2 = w3.Y - w1.Y;

                        float r = 1.0f / (s1 * t2 - s2 * t1);

                        System.Numerics.Vector3 sdir = new System.Numerics.Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                        System.Numerics.Vector3 tdir = new System.Numerics.Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                        tan1[i1] += sdir;
                        tan1[i2] += sdir;
                        tan1[i3] += sdir;

                        tan2[i1] += tdir;
                        tan2[i2] += tdir;
                        tan2[i3] += tdir;
                    }
                }
                for (int i = 0; i < vertexCount; i++)
                {
                    System.Numerics.Vector3 n = mesh.Vertices[i].Normal;
                    System.Numerics.Vector3 t = tan1[i];
                    System.Numerics.Vector3 t2 = tan2[i];
                    System.Numerics.Vector3 t1n = System.Numerics.Vector3.Normalize(tan1[i]);
                    System.Numerics.Vector3 t2n = System.Numerics.Vector3.Normalize(tan2[i]);

                    float w = ((!(System.Numerics.Vector3.Dot(System.Numerics.Vector3.Cross(n, t), tan2[i]) < 0f)) ? 1 : (-1));
                    var tangent0 = (System.Numerics.Vector3.Normalize(t - n * System.Numerics.Vector3.Dot(n, t)));
                    System.Numerics.Vector3 finalTangent0 = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(mesh.Vertices[i].Normal,
                               new System.Numerics.Vector3(tangent0.X,
                               tangent0.Y,
                               tangent0.Z) * w));

                    mesh.Vertices[i].Tangents[0] = new System.Numerics.Vector4(finalTangent0.X, finalTangent0.Y, finalTangent0.Z, -w);

                    var ghettoBit = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Lerp(tangent0, finalTangent0, 0.46f)); //This is between these two and for w/e reason, .46 or so seems the closest approximation
                    if (mesh.Vertices[i].Tangents.Count >= 2)
                    {
                        var ghettoTan = RotatePoint(new System.Numerics.Vector3(mesh.Vertices[i].Normal.X, mesh.Vertices[i].Normal.Y, mesh.Vertices[i].Normal.Z), 0, (float)Math.PI / 2f, 0);
                        mesh.Vertices[i].Tangents[1] = new System.Numerics.Vector4(ghettoTan.X, ghettoTan.Y, ghettoTan.Z, 0);
                    }
                    if (mesh.Vertices[i].Bitangent != System.Numerics.Vector4.Zero)
                    {
                        mesh.Vertices[i].Bitangent = new System.Numerics.Vector4(ghettoBit, -w);
                    }

                }
            }

            return;
        }

        static System.Numerics.Vector3 RotatePoint(System.Numerics.Vector3 p, float pitch, float roll, float yaw)
        {
            //function borrowed with permission from Shadowth117
            System.Numerics.Vector3 ans = new System.Numerics.Vector3(0, 0, 0);


            var cosa = Math.Cos(yaw);
            var sina = Math.Sin(yaw);

            var cosb = Math.Cos(pitch);
            var sinb = Math.Sin(pitch);

            var cosc = Math.Cos(roll);
            var sinc = Math.Sin(roll);

            var Axx = cosa * cosb;
            var Axy = cosa * sinb * sinc - sina * cosc;
            var Axz = cosa * sinb * cosc + sina * sinc;

            var Ayx = sina * cosb;
            var Ayy = sina * sinb * sinc + cosa * cosc;
            var Ayz = sina * sinb * cosc - cosa * sinc;

            var Azx = -sinb;
            var Azy = cosb * sinc;
            var Azz = cosb * cosc;

            var px = p.X;
            var py = p.Y;
            var pz = p.Z;

            ans.X = (float)(Axx * px + Axy * py + Axz * pz);
            ans.Y = (float)(Ayx * px + Ayy * py + Ayz * pz);
            ans.Z = (float)(Azx * px + Azy * py + Azz * pz);


            return ans;
        }

        #endregion
    }


}
