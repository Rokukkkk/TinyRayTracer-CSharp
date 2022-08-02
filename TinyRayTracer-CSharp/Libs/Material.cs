using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace TinyRayTracer_CSharp.Libs
{
    public struct Material
    {
        public Vector4 albedo;
        public Rgba32 diffuseColor;
        public float specularExponent;
        public float refractionIndex;

        public Material(float r, Vector4 a, Rgba32 color, float specular)
        {
            refractionIndex = r;
            albedo = a;
            diffuseColor = color;
            specularExponent = specular;

        }

        public Material()
        {
            refractionIndex = 1;
            albedo = new(1, 0, 0, 0);
            diffuseColor = new(0, 0, 0);
            specularExponent = 0;
        }
    }
}
