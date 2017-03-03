using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBufferIntermediates
{
    /// <summary>
    /// Intermediate pre-flatbuffer material
    /// </summary>
    internal class Material
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float[] Color { get; set; } 
        public float ShinyPercent { get; set; }
        public float Alpha { get; set; }
        public Texture DiffuseTexture { get; set; }

        /*
         * name = matHash.ToString(),
                        id = matHash.ToString(),
                        ambient = new[] { material.RGBA.R, material.RGBA.G, material.RGBA.B },
                        diffuse = new[] { material.RGBA.R, material.RGBA.G, material.RGBA.B },
                        specular = new[] { material.RGBA.R * shinyPercent, material.RGBA.G * shinyPercent, material.RGBA.B * shinyPercent },
                        specularPower = 50,
                        emissive = new[] { 0.01f, 0.01f, 0.01f },
                        alpha = material.RGBA.A,
                        backFaceCulling = true,
                        wireframe = false,
                        diffuseTexture = hasTexture ? texture : null,
                        useLightmapAsShadowmap = false,
                        checkReadOnlyOnce = true
                        */
    }
}
