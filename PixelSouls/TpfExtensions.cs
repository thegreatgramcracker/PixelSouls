using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelSouls
{
    public static class TpfExtensions
    {
        public static void Pixelize(this TPF tpf, PixelArtSettings settings)
        {
            foreach (TPF.Texture tex in tpf.Textures)
            {
                tex.Bytes = PixelSoulsHelpers.Pixelize(tex, settings);
            }
        }
    }
}
