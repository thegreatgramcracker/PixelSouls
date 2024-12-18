using DirectXTexNet;
using ImageMagick;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;

namespace PixelSouls
{
    public static class PixelSoulsHelpers
    {

        //public static unsafe byte[] PixelizeCube(byte[] tex)
        //{
        //    //pixelizes cubemaps, uses a lot of CPU power to do so.
        //    GCHandle pinnedArray = GCHandle.Alloc(tex, GCHandleType.Pinned);

        //    ScratchImage cubeImage = TexHelper.Instance.LoadFromDDSMemory(pinnedArray.AddrOfPinnedObject(), tex.Length, DDS_FLAGS.NONE);

        //    cubeImage = cubeImage.Decompress(DXGI_FORMAT.R8G8B8A8_UNORM);

        //    for (int i = 0; i < cubeImage.GetImageCount(); i++)
        //    {
        //        Console.WriteLine("Image " + i);
        //        var img = cubeImage.GetImage(i);
        //        Console.WriteLine(img.Format);
        //        Console.WriteLine(img.Width + ", " + img.Height);
        //        byte* ptr = (byte*)img.Pixels;
        //        int len = img.Width * img.Height * 4;

        //        int offset = 0;
        //        for (int j = 0; j < img.Width * img.Height; j++)
        //        {
        //            MagickColor pixelColor = new MagickColor(ptr[offset], ptr[offset + 1], ptr[offset + 2], ptr[offset + 3]);
        //            IMagickColor<byte> closestColor = ClosestColor(pixelColor, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
        //            ptr[offset] = closestColor.R;
        //            ptr[offset + 1] = closestColor.G;
        //            ptr[offset + 2] = closestColor.B;
        //            ptr[offset + 3] = closestColor.A;
        //            offset += 4;
        //        }
        //    }
        //    cubeImage = cubeImage.Compress(DXGI_FORMAT.BC6H_UF16, TEX_COMPRESS_FLAGS.PARALLEL, 0.15f);
        //    cubeImage.OverrideFormat(DXGI_FORMAT.BC6H_UF16);
        //    var stream = cubeImage.SaveToDDSMemory(DDS_FLAGS.NONE);
        //    stream.Seek(0, SeekOrigin.Begin);
        //    pinnedArray.Free();

        //    byte[] finalBytes = new byte[stream.Length];
        //    stream.Read(finalBytes);
        //    return finalBytes;
        //}

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
                case 10:
                    return DXGI_FORMAT.R8G8B8A8_UNORM;
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

        public static TextureType GetTextureType(string texName, DXGI_FORMAT texFormat)
        {
            if (texName.EndsWith("_s") || texName.EndsWith("_S")) return TextureType.Specular;
            if (texName.EndsWith("_n") || texName.EndsWith("_N")) return TextureType.Normal;
            if (texName.EndsWith("_h") || texName.Contains("HeightMap")) return TextureType.Heightmap;
            if (texName.Contains("_lit_")) return TextureType.Lightmap;
            if (texName.EndsWith("_c") 
                || texName.Contains("EnvDif") || texName.Contains("EnvSpc") 
                || texName.Contains("envdif") || texName.Contains("envspc") 
                || texName.Contains("_env_") || texName.EndsWith("_env")) return TextureType.Cube;
            if (texName.Contains("DetailBump") || texName.Contains("detailbump")) return TextureType.DetailBump;
            if (texName.EndsWith("_t") || texName.EndsWith("_M") ||
                texFormat == DXGI_FORMAT.R16G16_FLOAT )
            {
                return TextureType.Ignore;
            }

            return TextureType.Diffuse;
        }

        public static IMagickColor<byte> ClosestColor(IMagickColor<byte> inputColor, IPixelCollection<byte> palette, string mode)
        {
            //calculates the closest color on a given palette to the input color, different modes can be used to find the closest color.
            if (inputColor.A <= 0)
            {
                return inputColor;
            }
            double minDist = 9999999;
            IMagickColor<byte> convertColor = palette.GetPixel(0, 0).ToColor();

            if (mode == "Perceptual")
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
            else if (mode == "Value")
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
            else if (mode == "LinearRGB")
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

        public static byte ConvertToPS1ColorChannel(byte inputChannel)
        {
            return ClampToByte((float)Math.Round(inputChannel / 8.225806f) * 8.225806f);
        }
        public static byte ConvertToPS1AlphaChannel(byte inputChannel)
        {
            if (inputChannel > 190)
            {
                return 255;
            }
            else if (inputChannel > 96)
            {
                return 127;
            }
            else if (inputChannel > 32)
            {
                return 64;
            }
            else
            {
                return 0;
            }
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

        public static float ColorDifferenceLinearRGB(IMagickColor<byte> color1, IMagickColor<byte> color2)
        {
            //returns the linear total distance of two colors RGB values
            float dist = (Math.Abs(color1.R - color2.R) + Math.Abs(color1.G - color2.G) + Math.Abs(color1.B - color2.B)) / 765f;

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
                IMagickColor<byte> closestColor = PixelSoulsHelpers.ClosestColor(xmlColor, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
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
                IMagickColor<byte> closestColor = PixelSoulsHelpers.ClosestColor(xmlColor, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
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
                IMagickColor<byte> closestColor = PixelSoulsHelpers.ClosestColor(xmlColor, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
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
                IMagickColor<byte> closestColor = PixelSoulsHelpers.ClosestColor(xmlColor, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
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
            IMagickColor<byte> constClosestColor = PixelSoulsHelpers.ClosestColor(constXmlColor, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
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
                    IMagickColor<byte> currentClosestColor = PixelSoulsHelpers.ClosestColor(currentXmlColor, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
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

        public static void ScaleDRB(string file, string tpfPath, int scaleFactor)
        {
            DRB menuDRB = DRB.Read(file, DRB.DRBVersion.DarkSouls);
            TPF menuTPF = TPF.Read(tpfPath);
            


            foreach (DRB.Dlg dlg in menuDRB.Dlgs)
            {
                foreach (DRB.Dlgo dlgo in dlg.Dlgos)
                {
                    if (dlgo.Shape is DRB.Shape.SpriteBase)
                    {
                        Console.WriteLine("based");
                        DRB.Shape.SpriteBase? spriteBase = (dlgo.Shape as DRB.Shape.SpriteBase);
                        if (spriteBase.TextureIndex < 0) continue;
                        short texHeight = menuTPF.Textures[spriteBase.TextureIndex].Header.Height;
                        short texWidth = menuTPF.Textures[spriteBase.TextureIndex].Header.Width;

                        spriteBase.TexTopEdge = (short)QuantizeToScale(spriteBase.TexTopEdge, scaleFactor);
                        spriteBase.TexBottomEdge = (short)QuantizeToScale(spriteBase.TexBottomEdge, scaleFactor);
                        spriteBase.TexLeftEdge = (short)QuantizeToScale(spriteBase.TexLeftEdge, scaleFactor);
                        spriteBase.TexRightEdge = (short)QuantizeToScale(spriteBase.TexRightEdge, scaleFactor);
                    }

                }

            }
            menuDRB.Write("G:\\Code and Video Game Stuff\\Dark Souls Modding\\Pixel Souls Files\\DeS\\_out\\menu.drb");
        }

        public static void PS1Dither(MagickImage image)
        {
            int[][] matrix = new int[4][]
            {
                new int[] { -4, 0, -3, 1},
                new int[] { 2, -2, 3, -1},
                new int[] { -3, 1, -4, 0},
                new int[] { 3, -1, 2, -2}
            };
            for (int x = 0; x < image.Width; x++)
            {
                for(int y = 0; y < image.Height; y++)
                {
                    IPixel<byte> pixel = image.GetPixels().GetPixel(x, y);
                    image.GetPixels().SetPixel(x, y, 
                        new byte[]
                        {
                            ClampToByte(pixel.GetChannel(0) + matrix[x % 4][y % 4]),
                            ClampToByte(pixel.GetChannel(1) + matrix[x % 4][y % 4]),
                            ClampToByte(pixel.GetChannel(2) + matrix[x % 4][y % 4]),
                            pixel.GetChannel(3)
                        }
                        );
                }
            }
        }


        public static int QuantizeToScale(int value, int scaleFactor)
        {
            float ratio = (float)value / scaleFactor;

            return (int)MathF.Round(ratio) * scaleFactor;
        }



        #region Experimental



        public static void DumpPNGTexturesFromBNDFolder(string dir, string outputDir)
        {
            string[] files = Directory.GetFiles(dir);
            foreach (string file in files)
            {
                if (file.EndsWith("_l.partsbnd.dcx"))
                {
                    BND3 binder = BND3.Read(file);
                    foreach (BinderFile binderFile in binder.Files)
                    {
                        if (binderFile.Name.EndsWith(".tpf"))
                        {
                            TPF tpf = TPF.Read(binderFile.Bytes);
                            foreach (TPF.Texture texture in tpf.Textures)
                            {
                                if (!texture.Name.EndsWith("_n") && !texture.Name.EndsWith("_s") && !texture.Name.EndsWith("_N") && !texture.Name.EndsWith("_S"))
                                {
                                    MagickImage magickImage = new MagickImage(texture.Headerize());
                                    magickImage.Format = MagickFormat.Png;
                                    magickImage.Write(outputDir + "\\" + texture.Name + ".png");
                                }
                            }
                        }
                    }
                }
            }
        }

        public static unsafe void ImportPNGTexturesFromFolder(string fromDir, string toDir)
        {
            foreach (string file in Directory.GetFiles(toDir))
            {
                if (file.EndsWith(".partsbnd") || file.EndsWith(".partsbnd.dcx"))
                {
                    Console.WriteLine(file);
                    BND3 bnd = BND3.Read(file);
                    foreach (BinderFile binderFile in bnd.Files)
                    {
                        if (binderFile.Name.EndsWith(".tpf") || binderFile.Name.EndsWith(".tpf.dcx"))
                        {
                            TPF tpf = TPF.Read(binderFile.Bytes);
                            foreach (TPF.Texture tex in tpf.Textures)
                            {
                                Console.WriteLine(tex.Name);
                                if (Directory.GetFiles(fromDir).Contains(fromDir + "\\" + tex.Name + ".png"))
                                {
                                    Console.WriteLine("writing " + tex.Name);
                                    MagickImage importImage = new MagickImage(fromDir + "\\" + tex.Name + ".png");
                                    if (tex.Header.Unk1 == 120) return;

                                    DXGI_FORMAT format = GetFormat(tex.Format);

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

                                    magickImage.ImportPixels(importImage.GetPixels().ToByteArray("RGBA"), new PixelImportSettings(magickImage.Width, magickImage.Height, StorageType.Char, "RGBA"));

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
                            }
                            binderFile.Bytes = tpf.Write();
                        }
                    }
                    bnd.Write(file);
                }
            }
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
