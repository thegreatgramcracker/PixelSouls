using ImageMagick;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelSouls
{
    public static class DrawParamExtensions
    {
        public static void AdjustDrawParam(this PARAM drawparam, DrawParamColorLayout layout)
        {
            Console.WriteLine(layout.rowName);
            foreach (PARAM.Row row in drawparam.Rows)
            {
                if (layout.convertType == ColorConvertType.WhiteByLightness)
                {

                    short lightness = (short)Math.Round((new short[] { (short)row[layout.colorRCellName].Value,
                                (short)row[layout.colorGCellName].Value,
                                (short)row[layout.colorBCellName].Value}.Max() +
                        new short[] { (short)row[layout.colorRCellName].Value,
                                (short)row[layout.colorGCellName].Value,
                                (short)row[layout.colorBCellName].Value}.Min()) / 2f);
                    row[layout.colorRCellName].Value = lightness;
                    row[layout.colorGCellName].Value = lightness;
                    row[layout.colorBCellName].Value = lightness;
                }
                else if (layout.convertType == ColorConvertType.PaletteMatch)
                {
                    MagickColor inputVal = new MagickColor(Convert.ToByte(row[layout.colorRCellName].Value), Convert.ToByte(row[layout.colorGCellName].Value), Convert.ToByte(row[layout.colorBCellName].Value));
                    IMagickColor<byte> convertColor = PixelSoulsHelpers.ClosestColor(inputVal, PixelSoulsGlobals.defaultPixelSettings.colors, PixelSoulsGlobals.diffuseColorConvertMode);
                    row[layout.colorRCellName].Value = (short)convertColor.R;
                    row[layout.colorGCellName].Value = (short)convertColor.G;
                    row[layout.colorBCellName].Value = (short)convertColor.B;
                }
                else if (layout.convertType == ColorConvertType.Average)
                {
                    float average = ((float)row[layout.colorRCellName].Value + (float)row[layout.colorGCellName].Value + (float)row[layout.colorBCellName].Value) / 3f;
                    row[layout.colorRCellName].Value = average;
                    row[layout.colorGCellName].Value = average;
                    row[layout.colorBCellName].Value = average;
                }
                else if (layout.convertType == ColorConvertType.AlphaOnly)
                {
                    if (row[layout.colorRCellName].Value.GetType() == typeof(short))
                    {
                        row[layout.colorRCellName].Value = (short)0;
                        row[layout.colorGCellName].Value = (short)0;
                        row[layout.colorBCellName].Value = (short)0;
                    }
                    else
                    {
                        row[layout.colorRCellName].Value = 0f;
                        row[layout.colorGCellName].Value = 0f;
                        row[layout.colorBCellName].Value = 0f;
                    }
                }
            }
        }
    }
}
