using ImageMagick;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelSouls
{
    public static class MtdExtensions
    {
        public static void AdjustMaterialColor(this MTD mat, IPixelCollection<byte> newColors)
        {
            //adjusts colors of material params to match the palette (I made it use the current global variable instance of the palette because I am big dumb)
            /*
            Blend modes:
            0, 1, 2, 3, 32, 33 = normal

            */
            MTD.Param mapColor = mat.Params.Find(p => p.Name == "g_DiffuseMapColor");
            if (mapColor != null)
            {
                float[] mapColorValue = mapColor.Value as float[];
                IMagickColor<byte> color = new MagickColor(PixelSoulsHelpers.ClampToByte(mapColorValue[0] * 255),
                    PixelSoulsHelpers.ClampToByte(mapColorValue[1] * 255),
                    PixelSoulsHelpers.ClampToByte(mapColorValue[2] * 255));
                color = PixelSoulsHelpers.ClosestColor(color, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
                mapColor.Value = new float[] { 1f, 1f, 1f }; //{ color.R / 255f, color.G / 255f, color.B / 255f };
            }
            MTD.Param specularMapColor = mat.Params.Find(p => p.Name == "g_SpecularMapColor");
            if (specularMapColor != null)
            {
                float[] specularMapColorValue = specularMapColor.Value as float[];
                IMagickColor<byte> color = new MagickColor(PixelSoulsHelpers.ClampToByte(specularMapColorValue[0] * 255),
                    PixelSoulsHelpers.ClampToByte(specularMapColorValue[1] * 255),
                    PixelSoulsHelpers.ClampToByte(specularMapColorValue[2] * 255));
                color = PixelSoulsHelpers.ClosestColor(color, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
                specularMapColor.Value = new float[] { 1f, 1f, 1f };
            }
            MTD.Param snowColor = mat.Params.Find(p => p.Name == "g_SnowColor");
            if (snowColor != null)
            {
                Console.WriteLine((snowColor.Value as float[]).Length);
                float[] snowColorValue = snowColor.Value as float[];
                IMagickColor<byte> color = new MagickColor(PixelSoulsHelpers.ClampToByte(snowColorValue[0] * 255),
                    PixelSoulsHelpers.ClampToByte(snowColorValue[1] * 255), PixelSoulsHelpers.ClampToByte(snowColorValue[2] * 255),
                    PixelSoulsHelpers.ClampToByte(snowColorValue[3] * 255));
                color = PixelSoulsHelpers.ClosestColor(color, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
                snowColor.Value = new float[] { color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f };
            }
            MTD.Param fresnelColor = mat.Params.Find(p => p.Name == "g_FresnelColor");
            if (fresnelColor != null)
            {
                float[] fresnelColorValue = fresnelColor.Value as float[];
                IMagickColor<byte> color = new MagickColor(PixelSoulsHelpers.ClampToByte(fresnelColorValue[0] * 255),
                    PixelSoulsHelpers.ClampToByte(fresnelColorValue[1] * 255),
                    PixelSoulsHelpers.ClampToByte(fresnelColorValue[2] * 255));
                color = PixelSoulsHelpers.ClosestColor(color, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
                fresnelColor.Value = new float[] { color.R / 255f, color.G / 255f, color.B / 255f };
            }
            MTD.Param waterColor = mat.Params.Find(p => p.Name == "g_WaterColor");
            if (waterColor != null)
            {
                float[] waterColorValue = waterColor.Value as float[];
                IMagickColor<byte> color = new MagickColor(PixelSoulsHelpers.ClampToByte(waterColorValue[0] * 255),
                    PixelSoulsHelpers.ClampToByte(waterColorValue[1] * 255), PixelSoulsHelpers.ClampToByte(waterColorValue[2] * 255),
                    PixelSoulsHelpers.ClampToByte(waterColorValue[3] * 255));
                color = PixelSoulsHelpers.ClosestColor(color, PixelSoulsGlobals.diffusePixelSetting.Colors, PixelSoulsGlobals.diffusePixelSetting.ColorConvertMode);
                waterColor.Value = new float[] { color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f };
            }
            //MTD.Param specularPower = mat.Params.Find(p => p.Name == "g_SpecularPower");
            //if (specularPower != null)
            //{

            //    specularPower.Value = Math.Clamp((float)specularPower.Value, 0f, 1f);
            //}
        }
    }
}
