using System.Numerics;

namespace TinyRayTracer_CSharp.Libs
{
    public struct Sphere
    {
        public Vector3 center;
        public float radius;
        public Material material;

        public Sphere(Vector3 c, float r, Material m)
        {
            center = c;
            radius = r;
            material = m;
        }

        public bool IsRayIntersect(ref Vector3 origin, ref Vector3 dir, ref float t0)
        {
            Vector3 L = center - origin;
            float tca = Vector3.Dot(L, dir);
            float d2 = Vector3.Dot(L, L) - tca * tca;
            if (d2 > radius * radius) return false;
            float thc = (float)Math.Sqrt(radius * radius - d2);
            t0 = tca - thc;
            float t1 = tca + thc;
            if (t0 < 0) t0 = t1;
            if (t0 < 0) return false;
            return true;
        }
    }
}
