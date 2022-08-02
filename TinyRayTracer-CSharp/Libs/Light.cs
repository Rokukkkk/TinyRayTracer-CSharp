using System.Numerics;

namespace TinyRayTracer_CSharp.Libs
{
    public struct Light
    {
        public Vector3 position;
        public float intensity;

        public Light(Vector3 p, float i)
        {
            position = p;
            intensity = i;
        }
    }
}
