using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelSouls
{
    public static class FlverExtensions
    {
        public static void Lodify(this FLVER2 flver, int lodLevel)
        {
            foreach (FLVER2.Mesh mesh in flver.Meshes)
            {
                bool motionBlur = true;
                if (mesh.FaceSets.Count < 3)
                {
                    Console.WriteLine("ZERO SETS");
                    continue;
                }
                else if (mesh.FaceSets.Count < 6)
                {
                    Console.WriteLine("ONE SET");
                    motionBlur = false;
                }

                for (int i = lodLevel; i >= 0; i--)
                {
                    mesh.FaceSets[i] = new FLVER2.FaceSet(mesh.FaceSets[i].Flags, mesh.FaceSets[lodLevel].TriangleStrip, mesh.FaceSets[lodLevel].CullBackfaces, mesh.FaceSets[lodLevel].Unk06, mesh.FaceSets[lodLevel].Indices);
                    if (motionBlur)
                    {
                        mesh.FaceSets[i + 3] = new FLVER2.FaceSet(mesh.FaceSets[i + 3].Flags, mesh.FaceSets[lodLevel + 3].TriangleStrip, mesh.FaceSets[lodLevel + 3].CullBackfaces, mesh.FaceSets[lodLevel + 3].Unk06, mesh.FaceSets[lodLevel + 3].Indices);
                    }
                }
            }
        }
    }
}
