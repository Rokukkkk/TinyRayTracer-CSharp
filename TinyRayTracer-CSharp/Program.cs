using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;
using TinyRayTracer_CSharp.Libs;

namespace TinyRayTracer_CSharp
{
    internal class Program
    {
        static readonly Image<Rgba32> envmap = Image.Load<Rgba32>("./envmap.jpg");

        static void Main()
        {
            Material ivory = new(1.0f, new Vector4(0.6f, 0.3f, 0.1f, 0.0f), new Rgba32((byte)(255 * 0.4), (byte)(255 * 0.4), (byte)(255 * 0.4)), 50.0f);
            Material redRubber = new(1.0f, new Vector4(0.9f, 0.1f, 0.0f, 0.0f), new Rgba32((byte)(255 * 0.3), (byte)(255 * 0.1), (byte)(255 * 0.1)), 10.0f);
            Material mirror = new(1.0f, new Vector4(0.0f, 10.0f, 0.8f, 0.0f), Color.White, 1425.0f);
            Material glass = new(1.5f, new Vector4(0.0f, 0.5f, 0.1f, 0.8f), new Rgba32((byte)(255 * 0.2), (byte)(255 * 0.7), (byte)(255 * 0.8)), 125.0f);

            List<Sphere> spheres = new()
            {
                new Sphere(new(-3, 0, -16), 2.0f, ivory),
                new Sphere(new(-1, -1.5f, -12), 2.0f, glass),
                new Sphere(new(1.5f, -0.5f, -18), 3.0f, redRubber),
                new Sphere(new(4, -2, -13), 1.0f, mirror)
            };

            List<Light> lights = new()
            {
                new Light(new(-20, 20, 20), 1.5f),
                new Light(new(30, 50, -25), 1.8f),
                new Light(new(30, 20, 30), 1.7f)
            };

            Render(ref spheres, ref lights);
        }

        static void Render(ref List<Sphere> spheres, ref List<Light> lights)
        {
            int width = 1920;
            int height = 1080;
            float fov = 1.05f;
            using Image<Rgba32> frameBuffer = new(width, height);
            Vector3 origin = Vector3.Zero;

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    float dirX = (float)(i + 0.5 - width / 2.0f);
                    float dirY = (float)(j + 0.5 - height / 2.0f);
                    float dirZ = (float)(-height / (2 * Math.Tan(fov / 2.0f)));
                    Vector3 dir = Vector3.Normalize(new(dirX, dirY, dirZ));
                    frameBuffer[i, j] = CastRay(ref origin, ref dir, ref spheres, ref lights);
                }
            }
            frameBuffer.Mutate(x => x.RotateFlip(RotateMode.None, FlipMode.Vertical));
            frameBuffer.SaveAsPng(@"./out.png");
        }

        static Rgba32 CastRay(ref Vector3 origin, ref Vector3 dir, ref List<Sphere> spheres, ref List<Light> lights, int depth = 0)
        {
            Vector3 point = Vector3.Zero;
            Vector3 n = Vector3.Zero;
            Material material = new();

            // If not hit, return background color
            if (depth > 4 || !IsSceneIntersect(ref origin, ref dir, ref spheres, ref point, ref n, ref material))
            {
                int xRaw = (int)((Math.Atan2(dir.Z, dir.X) / (2 * Math.PI) + 0.5f) * envmap.Width);
                int yRaw = (int)(Math.Acos(dir.Y) / Math.PI * envmap.Height);
                int x = Math.Max(0, Math.Min(xRaw, envmap.Width));
                int y = Math.Max(0, Math.Min(yRaw, envmap.Height));
                return envmap[x, y];
            }

            Vector3 reflectDir = Vector3.Normalize(Reflect(dir, n));
            Vector3 refractDir = Vector3.Normalize(Refract(dir, n, material.refractionIndex));
            Vector3 reflectOrigin = Vector3.Dot(reflectDir, n) < 0 ? point - n * 1e-3f : point + n * 1e-3f;
            Vector3 refractOrigin = Vector3.Dot(refractDir, n) < 0 ? point - n * 1e-3f : point + n * 1e-3f;
            Rgba32 reflectColor = CastRay(ref reflectOrigin, ref reflectDir, ref spheres, ref lights, depth + 1);
            Rgba32 refractColor = CastRay(ref refractOrigin, ref refractDir, ref spheres, ref lights, depth + 1);

            float diffuseLightIntensity = 0; float specularLightIntensity = 0;
            foreach (var light in lights)
            {
                Vector3 lightDir = Vector3.Normalize(light.position - point);
                float lightDistance = Vector3.Distance(light.position, point);
                Vector3 shadowOrigin = Vector3.Dot(lightDir, n) < 0 ? point - n * (float)1e-3 : point + n * (float)1e-3;
                Vector3 shadowPoint = Vector3.Zero;
                Vector3 shadowN = Vector3.Zero; ;
                Material tmp = new();

                if (IsSceneIntersect(ref shadowOrigin, ref lightDir, ref spheres, ref shadowPoint, ref shadowN, ref tmp) && Vector3.Distance(shadowPoint, shadowOrigin) < lightDistance) continue;
                diffuseLightIntensity += light.intensity * Math.Max(0, Vector3.Dot(lightDir, n));
                specularLightIntensity += (float)Math.Pow(Math.Max(0, Vector3.Dot(-Reflect(-lightDir, n), dir)), material.specularExponent) * light.intensity;
            }

            Rgba32 rColor = new()
            {
                R = (byte)Math.Min(material.diffuseColor.R * diffuseLightIntensity * material.albedo.X + 255 * specularLightIntensity * material.albedo.Y + reflectColor.R * material.albedo.Z + refractColor.R * material.albedo.W, 255),
                G = (byte)Math.Min(material.diffuseColor.G * diffuseLightIntensity * material.albedo.X + 255 * specularLightIntensity * material.albedo.Y + reflectColor.G * material.albedo.Z + refractColor.G * material.albedo.W, 255),
                B = (byte)Math.Min(material.diffuseColor.B * diffuseLightIntensity * material.albedo.X + 255 * specularLightIntensity * material.albedo.Y + reflectColor.B * material.albedo.Z + refractColor.B * material.albedo.W, 255),
                A = 255
            };

            return rColor;
        }

        static bool IsSceneIntersect(ref Vector3 origin, ref Vector3 dir, ref List<Sphere> spheres, ref Vector3 hitPoint, ref Vector3 n, ref Material material)
        {
            float spheresDistance = float.MaxValue;
            foreach (var sphere in spheres)
            {
                float distI = float.MaxValue;
                if (sphere.IsRayIntersect(ref origin, ref dir, ref distI) && distI < spheresDistance)
                {
                    spheresDistance = distI;
                    hitPoint = origin + dir * distI;
                    n = Vector3.Normalize(hitPoint - sphere.center);
                    material = sphere.material;
                }
            }

            float checkerboardDistance = float.MaxValue;
            if (Math.Abs(dir.Y) > 1e-3f)
            {
                float d = -(origin.Y + 4) / dir.Y;
                Vector3 point = origin + dir * d;
                if (d > 0 && Math.Abs(point.X) < 10 && point.Z < -10 && point.Z > -30 && d < spheresDistance)
                {
                    checkerboardDistance = d;
                    hitPoint = point;
                    n = Vector3.UnitY;
                    material.diffuseColor = ((int)(0.5f * hitPoint.X + 1000) + (int)(0.5f * hitPoint.Z)) % 2 > 0 ? new Rgba32((byte)(255 * 0.3), (byte)(255 * 0.3), (byte)(255 * 0.3)) : new Rgba32((byte)(255 * 0.3), (byte)(255 * 0.2), (byte)(255 * 0.1));
                }
            }
            return Math.Min(spheresDistance, checkerboardDistance) < 1000;
        }

        static Vector3 Reflect(Vector3 i, Vector3 n)
        {
            return i - n * 2.0f * Vector3.Dot(i, n);
        }

        // Snell's law
        static Vector3 Refract(Vector3 i, Vector3 n, float etat, float etai = 1.0f)
        {
            float cosi = -Math.Max(-1, Math.Min(1, Vector3.Dot(i, n)));
            if (cosi < 0) return Refract(i, -n, etai, etat);
            float eta = etai / etat;
            float k = 1 - eta * eta * (1 - cosi * cosi);
            return k < 0 ? Vector3.UnitX : i * eta + n * (float)(eta * cosi - Math.Sqrt(k));
        }
    }
}