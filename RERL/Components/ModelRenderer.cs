using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using RCS;
using RCS.Components;
using RERL.Loaders;
using RERL.ShaderTypes;
using static RERL.RenderData;
using static RERL.RERL_Core;

namespace RERL.Components;

public class ModelRenderer : IComponent, Renderable
{
    public Entity Owner { get; set; }

    Model? _model;
    Shader? _shader;
    public Shader? GetShader() => _shader;
    
    int _vao, _ibo, _vbo, _ebo;
    int _indirectBuffer;
    int _materialSSBO;

    public bool AutoRegister { get; set; } = true;

    public ModelRenderer SetAutoRegister(bool autoRegister)
    {
        AutoRegister = autoRegister;
        return this;
    }
    
    public ModelRenderer AttachModel(Model model)
    {
        _model = model;
        return this;
    }

    public ModelRenderer AttachShader(Shader shader, bool buildModelBuffers = true)
    {
        _shader = shader;
        if (buildModelBuffers) BuildModelBuffers();
        return this;
    }

    public void OnAdd()
    {
        if (AutoRegister)
            RenderPipeline.RegisterRenderable(this);
    }

    public void BuildModelBuffers()
    {
        if (_model is not { } model) { Logger.Warning("ModelRenderer added without a model."); return; }

        if (_shader == null) { Logger.Warning("ModelRenderer added without a shader."); return; }

        DisposeBuffers();
        
        // 0. Consolidate Mesh

        var verticeCount = _model.SubMeshes.Sum(mesh => mesh.Vertices.Length);
        var indiceCount = _model.SubMeshes.Sum(mesh => mesh.Indices.Length);

        var combinedVertices = new Vertex[verticeCount];
        var combinedIndices  = new uint[indiceCount];
        
        var vertexOffset  = 0;
        var indexWritePos = 0;

        foreach (var mesh in _model.SubMeshes)
        {
            Array.Copy(mesh.Vertices, 0, combinedVertices, vertexOffset, mesh.Vertices.Length);
            foreach (var index in mesh.Indices) { combinedIndices[indexWritePos++] = index + (uint)vertexOffset; }
            vertexOffset += mesh.Vertices.Length;
        }
        
        // 1. Create VAO/IBO/VBO

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ibo = GL.GenBuffer();
        
        GL.BindVertexArray(_vao);

        // 2. Upload vertices
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer,
            combinedVertices.Length * Marshal.SizeOf<Vertex>(),
            combinedVertices,
            BufferUsageHint.StaticDraw);

        // 3. Upload indices
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ibo);
        GL.BufferData(BufferTarget.ElementArrayBuffer,
            combinedIndices.Length * sizeof(uint),
            combinedIndices,
            BufferUsageHint.StaticDraw);

        int stride = Marshal.SizeOf<Vertex>();

        // Position (location = 0)
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

        // Normal (location = 1)
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 12);

        // UV (location = 2) // I don't have these yet.
        //GL.EnableVertexAttribArray(2);
        //GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 24);

        // Tangent (location = 3) // I don't have these yet.
        //GL.EnableVertexAttribArray(3);
        //GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, stride, 32);
        
        // 4. Create indirect draw commands
        
        var commands = new DrawElementsIndirectCommand[_model.SubMeshes.Count];

        uint runningIndexOffset = 0;

        for (int i = 0; i < _model.SubMeshes.Count; i++)
        {
            var mesh = _model.SubMeshes[i];

            commands[i] = new DrawElementsIndirectCommand
            {
                Count = (uint)mesh.Indices.Length,
                InstanceCount = 1,
                FirstIndex = runningIndexOffset,
                BaseVertex = 0,
                BaseInstance = 0//(uint)i // Material index / vInstance
            };
            
            runningIndexOffset += (uint)mesh.Indices.Length;
        }
        
        _indirectBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer);

        GL.BufferData(
            BufferTarget.DrawIndirectBuffer,
            commands.Length * Marshal.SizeOf<DrawElementsIndirectCommand>(),
            commands,
            BufferUsageHint.StaticDraw
        );
        
        // 5. Create dummy material SSBO
        
        _materialSSBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _materialSSBO);

        GPUMaterial[] dummyMaterials = [ new GPUMaterial {BaseColor = new Vector4(1, 0, 0, 1)} ];
        GL.BufferData(
            BufferTarget.ShaderStorageBuffer,
            dummyMaterials.Length * Marshal.SizeOf<GPUMaterial>(),
            dummyMaterials,
            BufferUsageHint.StaticDraw
        );

        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _materialSSBO);
    }

    public void Render(int instanceCount = 1)
    {
        if (_model is not { } model) { Logger.Warning("ModelRenderer added without a model."); return; }

        if (_shader == null) { Logger.Warning("ModelRenderer added without a shader."); return; }

        _shader.Use();
        _shader.ApplyUniform("uModel", Owner.Transform.WorldMatrix, false);

        GL.BindVertexArray(_vao);

        // Bind SSBO
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _materialSSBO);

        // Bind indirect buffer
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer);
        
        // Draw all meshes in one call
        GL.MultiDrawElementsIndirect(
            PrimitiveType.Triangles,
            DrawElementsType.UnsignedInt,
            IntPtr.Zero,
            _model.SubMeshes.Count,
            0
        );
    }

    public void Load() {}
    public void Update(float deltaTime) {}

    public void DisposeBuffers()
    {
        if (_vao != 0) GL.DeleteVertexArray(_vao);
        if (_vbo != 0) GL.DeleteBuffer(_vbo);
        if (_ibo != 0) GL.DeleteBuffer(_ibo);
        if (_indirectBuffer != 0) GL.DeleteBuffer(_indirectBuffer);
        if (_materialSSBO != 0) GL.DeleteBuffer(_materialSSBO);

        _vao = _vbo = _ibo = _indirectBuffer = _materialSSBO = 0;
    }
}