using System.Text;
using System.Text.Json;
using OpenTK.Mathematics;
using RCS;
using static RERL.RenderData;
using static RERL.RERL_Core;

namespace RERL.Loaders;

/// <summary>
/// Provides functionality for loading mesh data from OBJ files and converting
/// them into <see cref="RenderData.Mesh"/> instances. Includes built‑in paths
/// for common primitive meshes.
/// </summary>
public static class MeshLoader
{
    public struct ModelReturn
    {
        public string Name;
        public Model? Model;
        public Transform Transform;
        public string ParrentName;
    }
    
    public const string Cube = @"./Models/Cube.obj";
    public const string Icosahedron = @"./Models/Icosahedron.obj";
    public const string UVSphere = @"./Models/UVSphere.obj";
    
    public static Mesh CubeMesh => ParseObj(Cube)[0].SubMeshes[0];
    public static Mesh IcosahedronMesh => ParseObj(Icosahedron)[0].SubMeshes[0];
    public static Mesh UVSphereMesh => ParseObj(UVSphere)[0].SubMeshes[0];

    /// <summary>
    /// Parses a mesh file based on its extension.
    /// Currently, supports only OBJ files.
    /// </summary>
    /// <param name="filename">The file path to load.</param>
    /// <returns>A new <see cref="RenderData.Mesh"/> instance.</returns>
    /// <exception cref="Exception">Thrown if the file format is unsupported.</exception>
    public static List<ModelReturn> ParseMesh(string filename)
    {
        if(filename.EndsWith(".glb") || filename.EndsWith(".gltf"))
            return ParseGltf(filename, filename.EndsWith(".glb"));

        throw new Exception($"ERR: Unsupported file format '{filename}'.");
    }

    static readonly string[] AlbedoEndings = ["_n", "_normal", "_ddn", "_nrm"];
    static readonly string[] NormalEndings = ["_albedo", "_diff", "_diffuse", "_col", "_color", "_basecolor", "_colour", "_basecolour"];
    
    /// <summary>
    /// Parses an OBJ file into a mesh, reading vertex positions, normals,
    /// UV coordinates, and face definitions.
    /// </summary>
    /// <param name="objFilePath">Path to the OBJ file.</param>
    /// <returns>A populated <see cref="RenderData.Mesh"/> instance.</returns>
    /// <exception cref="Exception">
    /// Thrown if the OBJ file contains invalid data or cannot be read.
    /// </exception>
    [Obsolete ("Won't support, switch to glTF.")]
    public static List<Model> ParseObj(string objFilePath)
    {
        string currentModel = "";
        List<Model> models = [];
        string currentMaterial = "";
        List<Mesh> subMeshes = [];
        Dictionary<String, Material> materials = new();
        
        List<Vector3> tempVertexPositions = [];
        List<Vector3> tempVertexNormals = [];
        List<Vector2> tempVertexUVs = [];
        List<uint> indices = [];
        List<Vertex> vertices = [];

        foreach (var line in File.ReadLines(objFilePath))
        {
            if (line.StartsWith($"#") || string.IsNullOrWhiteSpace(line))
                continue;

            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (tokens[0])
            {
                case "o":
                    if (!string.IsNullOrWhiteSpace(currentModel)) {
                        AddSubMesh(ref currentMaterial);
                        models.Add(new Model([..subMeshes], [..materials.Values]));
                        subMeshes.Clear();
                        materials.Clear();
                        currentMaterial = "";
                        currentModel = "";
                    }
                    currentModel = tokens[1];
                    break;
                
                case "v":
                    tempVertexPositions.Add(new Vector3(
                        float.Parse(tokens[1]),
                        float.Parse(tokens[2]),
                        float.Parse(tokens[3])));
                    break;

                case "vn":
                    tempVertexNormals.Add(new Vector3(
                        float.Parse(tokens[1]),
                        float.Parse(tokens[2]),
                        float.Parse(tokens[3])));
                    break;

                case "vt":
                    tempVertexUVs.Add(new Vector2(
                        float.Parse(tokens[1]),
                        float.Parse(tokens[2])));
                    break;
                
                case "usemtl":
                    if (!string.IsNullOrWhiteSpace(currentMaterial)) {
                        subMeshes.Add(new Mesh(vertices.ToArray(), indices.ToArray(), materials.GetValueOrDefault(currentMaterial)));
                        vertices.Clear();
                        indices.Clear();
                    }
                    currentMaterial = tokens[1];
                    materials.TryAdd(currentMaterial, new Material(currentMaterial));
                    break;
                
                case "f":
                    for (int i = 2; i < tokens.Length; i++)
                    {
                        uint i0 = AddVertex(tokens[1]);
                        uint i1 = AddVertex(tokens[i]);
                        uint i2 = AddVertex(tokens[i + 1]);

                        indices.Add(i0);
                        indices.Add(i1);
                        indices.Add(i2);

                        if (i + 1 == tokens.Length - 1)
                            break;
                    }
                    break;
            }
        }

        AddSubMesh(ref currentMaterial);

        if (!string.IsNullOrWhiteSpace(currentModel)) {
            models.Add(new Model([..subMeshes], [..materials.Values]));
            subMeshes.Clear();
            materials.Clear();
            currentModel = "";
        }
        
        return models;

        void AddSubMesh(ref string currentMaterial)
        {
            if (string.IsNullOrEmpty(currentMaterial))
                currentMaterial = "__default";

            materials.TryAdd(currentMaterial, new Material(currentMaterial));

            subMeshes.Add(new Mesh(
                vertices.ToArray(),
                indices.ToArray(),
                materials[currentMaterial]
            ));

            vertices.Clear();
            indices.Clear();
            currentMaterial = "";
        }
        
        uint AddVertex(string faceToken)
        {
            string[] vertex = faceToken.Split('/');

            vertices.Add(new Vertex(
                position: tempVertexPositions[int.Parse(vertex[0]) - 1],
                uv: tempVertexUVs.Count > 0
                    ? tempVertexUVs[int.Parse(vertex[1]) - 1]
                    : Vector2.Zero,
                normal: tempVertexNormals.Count > 0
                    ? tempVertexNormals[int.Parse(vertex[2]) - 1]
                    : Vector3.Zero));

            return (uint)(vertices.Count - 1);
        }
    }

    public static List<ModelReturn> ParseGltf(string filePath, bool isGlb)
    {
        (JsonDocument? json, Dictionary<uint, byte[]> bin) data = !isGlb ? ExtractFromGltf() : ExtractFromGlb();
        if (data.json == null) {
            Logger.Error($"Failed to extract {(!isGlb ? new string("gltf") : new string("glb"))} file {filePath}");
            return [];
        }

        var root = data.json.RootElement;
        Logger.Log(root.GetProperty("accessors").GetArrayLength().ToString());
        
        var entitiesArrayEnum = root.GetProperty("nodes").EnumerateArray();
        var entitiesElem = entitiesArrayEnum.ToArray();
        Dictionary<string, JsonElement> entities = [];
        foreach (var entity in entitiesElem) {
            entities.TryAdd(entity.GetProperty("name").GetString() ?? "", entity);
        }

        // Build parent lookup
        Dictionary<int, int?> parentOf = new();

        for (int i = 0; i < entitiesElem.Length; i++)
            parentOf[i] = null; // default: no parent (root node)

        for (int parentIndex = 0; parentIndex < entitiesElem.Length; parentIndex++)
        {
            var node = entitiesElem[parentIndex];

            if (node.TryGetProperty("children", out var childrenElem))
            {
                foreach (var child in childrenElem.EnumerateArray())
                {
                    int childIndex = child.GetInt32();
                    parentOf[childIndex] = parentIndex;
                }
            }
        }
        
        List<ModelReturn> parsedEntities = [];
        foreach (var entity in entities) {
            
            int nodeIndex = Array.FindIndex(entitiesElem, e => 
                e.GetProperty("name").GetString() == entity.Key);
            
            int? parentIndex = parentOf[nodeIndex];
            
            string parentName = parentIndex.HasValue
                ? entitiesElem[parentIndex.Value].GetProperty("name").GetString() ?? ""
                : "";
            
            #region Transform

            Transform entityTransform;
            if (entity.Value.TryGetProperty("matrix", out var mElem))
            {
                var mGltf = ReadMatrix(mElem);
                DecomposeTRS(mGltf, out var t, out var r, out var s);
                entityTransform = new Transform(t, r, s);
            } else {
                var t = entity.Value.TryGetProperty("translation", out var tElem) ? ReadVector3(tElem) : Vector3.Zero;
                var r = entity.Value.TryGetProperty("rotation", out var rElem) ? ReadQuaternion(rElem) : Quaternion.Identity;
                var s = entity.Value.TryGetProperty("scale", out var sElem) ? ReadVector3(sElem) : Vector3.One;
                entityTransform = new Transform(t, r, s);
            }
            
            #endregion
            
            if (!entity.Value.TryGetProperty("mesh", out var meshIndexElem)) {
                parsedEntities.Add(new ModelReturn{
                        Name = entity.Key,
                        Model = null,
                        Transform = entityTransform,
                        ParrentName = parentName
                    });
                continue;
            }
            
            List<Mesh> subMeshes = [];
            var modelMesh = root.GetProperty("meshes")[(int)entity.Value.GetProperty("mesh").GetUInt32()];
            
            if(entity.Key == "lionhead") Logger.Log($"lionhead Pos: {entityTransform.Position} Rot: {entityTransform.Rotation}");
            if(entity.Key == "decals_1st_floor") Logger.Log($"decals_1st_floor Pos: {entityTransform.Position} Rot: {entityTransform.Rotation}");


            foreach (var subMesh in modelMesh.GetProperty("primitives").EnumerateArray()) {
                var attributes = subMesh.GetProperty("attributes");

                var posIndex = attributes.GetProperty("POSITION").GetUInt32();
                var nrmIndex = attributes.GetProperty("NORMAL").GetUInt32();
                var indicesIndex = subMesh.GetProperty("indices").GetUInt32();

                var posAccessor = root.GetProperty("accessors")[(int)posIndex];
                var nrmAccessor = root.GetProperty("accessors")[(int)nrmIndex];
                var indicesAccessor = root.GetProperty("accessors")[(int)indicesIndex];

                var posBufferView = root.GetProperty("bufferViews")[posAccessor.GetProperty("bufferView").GetInt32()];
                var nrmBufferView = root.GetProperty("bufferViews")[nrmAccessor.GetProperty("bufferView").GetInt32()];
                var indicesBufferView =
                    root.GetProperty("bufferViews")[indicesAccessor.GetProperty("bufferView").GetInt32()];

                // Currently I assume vertex normals are always included in the mesh,
                // but later on I will make a check to calculate normals on mesh load if it doesn't find any in the file.

                var vertexPositions = ReadFromByteArray<Vector3>(
                    data.bin[posBufferView.GetProperty("buffer").GetUInt32()],
                    GetByteAreaData(posAccessor, posBufferView));
                var vertexNormals = ReadFromByteArray<Vector3>(
                    data.bin[nrmBufferView.GetProperty("buffer").GetUInt32()],
                    GetByteAreaData(nrmAccessor, nrmBufferView));

                var indicesComponentType = indicesAccessor.GetProperty("componentType").GetUInt32();

                uint[] indices = indicesComponentType switch
                {
                    5123 => // ushort
                    [
                        ..ReadFromByteArray<ushort>(data.bin[indicesBufferView.GetProperty("buffer").GetUInt32()],
                            GetByteAreaData(indicesAccessor, indicesBufferView))
                    ],

                    5125 => // uint
                        ReadFromByteArray<uint>(data.bin[indicesBufferView.GetProperty("buffer").GetUInt32()],
                            GetByteAreaData(indicesAccessor, indicesBufferView)),
                    5121 => // byte
                    [
                        ..ReadFromByteArray<byte>(data.bin[indicesBufferView.GetProperty("buffer").GetUInt32()],
                            GetByteAreaData(indicesAccessor, indicesBufferView))
                    ],

                    _ => [] // Default
                };

                subMeshes.Add(new Mesh(BuildVertices(vertexPositions, vertexNormals), [..indices],
                    new Material("__default")));
            }

            parsedEntities.Add(new ModelReturn{
                Name = entity.Key,
                Model = new Model([..subMeshes], []),
                Transform = entityTransform,
                ParrentName = parentName
            });
            continue;

            (uint offset, uint count, uint stride) GetByteAreaData(JsonElement accessor, JsonElement bufferView)
            {
                uint accessorByteOffset = accessor.TryGetProperty("byteOffset", out var aOff) ? aOff.GetUInt32() : 0;
                uint bufferViewByteOffset =
                    bufferView.TryGetProperty("byteOffset", out var bvOff) ? bvOff.GetUInt32() : 0;
                uint totalByteOffset = bufferViewByteOffset + accessorByteOffset;
                uint count = accessor.GetProperty("count").GetUInt32();
                uint stride = bufferView.TryGetProperty("byteStride", out var s) ? s.GetUInt32() : 0;
                return (totalByteOffset, count, stride);
            }
        }

        return parsedEntities;

        #region Extractions

        (JsonDocument? json, Dictionary<uint, byte[]>) ExtractFromGltf()
        {
            string jsonText = File.ReadAllText(filePath);

            var doc = JsonDocument.Parse(jsonText);
            var jsonRoot = doc.RootElement;

            Dictionary<uint, byte[]> buffers = new();
            uint currentBuffer = 0;
            foreach (var buffer in jsonRoot.GetProperty("buffers").EnumerateArray()) {
                string binUri = buffer.GetProperty("uri").GetString() ?? "";
                if (string.IsNullOrEmpty(binUri)) {
                    Logger.Error($"Missing buffer URI in glTF file: {filePath}", false);
                    return (null, []);
                }

                // ReSharper disable once NullableWarningSuppressionIsUsed
                string binPath = Path.Combine(Path.GetDirectoryName(filePath)!, binUri);
                byte[] binData = File.ReadAllBytes(binPath);
                buffers.Add(currentBuffer, binData);
                currentBuffer++;
            }

            return (doc, buffers);
        }

        (JsonDocument? json, Dictionary<uint, byte[]> bin) ExtractFromGlb()
        {
            using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));

            #region Header

            uint magic = reader.ReadUInt32(); // should be 0x46546C67
            if (magic != 0x46546C67) {
                Logger.Error($"Wrong \"magic\" variable in file: {filePath}", throwException: false);
                return (null, []);
            }

            uint version = reader.ReadUInt32(); // should be v2.0
            if (version != 2) {
                Logger.Error($"Unsupported glb version in file: {filePath}", throwException: false);
                return (null, []);
            }

            uint length = reader.ReadUInt32(); // total file length

            #endregion

            #region Json

            uint jsonChunkLength = reader.ReadUInt32();
            uint jsonChunkType = reader.ReadUInt32(); // should be 0x4E4F534A ("JSON")
            if (jsonChunkType != 0x4E4F534A) {
                Logger.Error($"Can't find glb JSON in file: {filePath}", throwException: false);
                return (null, []);
            }

            string jsonText = Encoding.UTF8.GetString(reader.ReadBytes((int)jsonChunkLength));
            JsonDocument doc = JsonDocument.Parse(jsonText);

            #endregion

            #region Binary Buffers

            uint binChunkLength = reader.ReadUInt32();
            uint binChunkType = reader.ReadUInt32(); // should be 0x004E4942 ("BIN")
            if (binChunkType != 0x004E4942) {
                Logger.Error($"Can't find glb BIN in file: {filePath}", throwException: false);
                return (null, []);
            }

            byte[] binData = reader.ReadBytes((int)binChunkLength);

            #endregion

            Dictionary<uint, byte[]> buffer = [];
            buffer.Add(0, binData);
            return (doc, buffer);
        }

        #endregion
    }

    #region JSON Readers

    static Matrix4 ReadMatrix(JsonElement matrixElement)
    {
        Span<float> m = stackalloc float[16];
        int i = 0;
        
        foreach (var v in matrixElement.EnumerateArray())
            m[i++] = v.GetSingle();
        
        return new Matrix4(
            m[0],  m[1],  m[2],  m[3],
            m[4],  m[5],  m[6],  m[7],
            m[8],  m[9],  m[10], m[11],
            m[12], m[13], m[14], m[15]
        );
    }

    
    static Vector3 ReadVector3(JsonElement vector3Element)
    {
        Span<float> vec3 = stackalloc float[3];
        var i = 0;

        foreach (var v in vector3Element.EnumerateArray())
            vec3[i++] = v.GetSingle();

        return new Vector3(vec3[0], vec3[1], vec3[2]);
    }
    
    static Quaternion ReadQuaternion(JsonElement e)
    {
        var x = e[0].GetSingle();
        var y = e[1].GetSingle();
        var z = e[2].GetSingle();
        var w = e[3].GetSingle();
        return new Quaternion(x, y, z, w);
    }

    #endregion

    static unsafe T[] ReadFromByteArray<T>(byte[] buffer, (uint offset, uint count, uint stride) byteArea) where T : unmanaged
    {
        int elementSize = sizeof(T);
        T[] result = new T[byteArea.count];
        
        fixed (byte* src = &buffer[byteArea.offset]) // Freezes the position of the buffer's memory, then sets src to the first byte at offset.
        fixed (T* dst = result) {                    // Freezes the position of the result's memory, points to the first element of the T[] we are filling.
            
            if (byteArea.stride <= elementSize) {
                // This should copy from offset to offset + count * elementSize;
                Buffer.MemoryCopy(src, dst, byteArea.count * elementSize, byteArea.count * elementSize);
            }
            else {
                // This should copy from offset to offset + count * elementSize, each element starts with custom 'stride' bytes apart.
                // Coping each element individually, skipping padding.
                for (uint i = 0; i < byteArea.count; i++) {
                    byte* elementPtr = src + i * byteArea.stride; //Get location (pointer) of the first byte of the section we want to copy.
                    Buffer.MemoryCopy(elementPtr, dst + i, elementSize, elementSize);
                }
            }
        }

        return result;
    }
    
    public static Vertex[] BuildVertices(Vector3[] positions, Vector3[] normals)
    {
        int count = positions.Length;
        Vertex[] verts = new Vertex[count];

        for (int i = 0; i < count; i++)
        {
            verts[i] = new Vertex(
                positions[i],
                normals != null && i < normals.Length ? normals[i] : Vector3.Zero,
                Vector2.Zero // UVs later
            );
        }

        return verts;
    }
    
    public static void DecomposeTRS(Matrix4 m, out Vector3 translation, out Quaternion rotation, out Vector3 scale)
    {
        translation = new Vector3(m.M41, m.M42, m.M43);

        var x = new Vector3(m.M11, m.M12, m.M13);
        var y = new Vector3(m.M21, m.M22, m.M23);
        var z = new Vector3(m.M31, m.M32, m.M33);

        var sx = x.Length;
        var sy = y.Length;
        var sz = z.Length;

        var det = Vector3.Dot(x, Vector3.Cross(y, z));
        if (det < 0)
            sx = -sx;

        scale = new Vector3(sx, sy, sz);

        var rotMat = new Matrix3(
            x.X / sx, x.Y / sx, x.Z / sx,
            y.X / sy, y.Y / sy, y.Z / sy,
            z.X / sz, z.Y / sz, z.Z / sz
        );

        rotation = Quaternion.FromMatrix(rotMat);
    }
}
