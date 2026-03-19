using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace RERL;

public static class RenderData
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUMaterial
    {
        public Vector4 BaseColor;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct DrawElementsIndirectCommand
    {
        public uint Count;
        public uint InstanceCount;
        public uint FirstIndex;
        public uint BaseVertex;
        public uint BaseInstance;
    }

}