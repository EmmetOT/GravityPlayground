using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace GravityPlayground.GravityStuff
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct GravityData
    {
        public static int Stride => sizeof(float) * 32 + sizeof(int) + sizeof(float) * 14;

        public Matrix4x4 Transform;
        public Matrix4x4 InverseTransform;
        public int Type;
        public Vector4 Data_1;
        public Vector4 Data_2;
        public float Mass;
        public Color Colour;
        public float Bounciness;
    }
}