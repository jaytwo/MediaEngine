using lib3ds.Net;
using MediaEngine.Unpackers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace MediaEngine.Exporters
{
    /// <summary>
    /// See http://www.martinreddy.net/gfx/3d/3DS.spec
    /// </summary>
    static class ModelExporter
    {
        public static void Export(List<Group> groups, List<Lib3dsVertex> vertices, List<ushort[]> allFaces, short[] faceGroupIds, BinaryWriter destination)
        {
            var file = LIB3DS.lib3ds_file_new();
            var textureGroups = new Dictionary<int, List<Group>>();

            foreach (var group in groups)
            {
                var groupId = (int)group[ModelField.TextureGroup];
                if (groupId == -1 || !textureGroups.TryGetValue(groupId, out var textureGroup))
                {
                    textureGroup = new List<Group>();
                    if (groupId != -1)
                        textureGroups.Add(groupId, textureGroup);
                }

                textureGroup.Add(group);
                group.TextureGroup = textureGroup;

                // TODO: Hierarchy of object
                var node = LIB3DS.lib3ds_node_new(Lib3dsNodeType.LIB3DS_NODE_MESH_INSTANCE);
                node.name = group[ModelField.GroupName].ToString();
                node.node_id = (ushort)file.nodes.Count;
                file.nodes.Add(node);
            }

            var objectIndices = (faceGroupIds == null ? Enumerable.Range(0, groups.Count) :
                faceGroupIds.Distinct().Select(b => (int)b)).ToArray();

            MaterialExporter.Export(groups, file);

            var facesByObject = TriangleExporter.Export(allFaces, faceGroupIds, objectIndices);

            foreach (var i in objectIndices)
            {
                var group = groups[i];
                var faces = facesByObject[i];

                var mesh = LIB3DS.lib3ds_mesh_new(group[ModelField.GroupName].ToString());
                mesh.vertices = vertices;
                mesh.nvertices = (ushort)vertices.Count;
                mesh.texcos = new List<Lib3dsTexturecoordinate>();
                file.meshes.Add(mesh);

                if (faces.Count != 0)
                {
                    var usedVertices = group.TextureGroup
                        .Select(g => groups.IndexOf(g))
                        .SelectMany(g => facesByObject[g])
                        .SelectMany(f => f.index)
                        .Distinct()
                        .Select(v => vertices[v])
                        .ToArray();

                    var minX = usedVertices.Min(v => v.x);
                    var minY = usedVertices.Min(v => v.y);
                    var minZ = usedVertices.Min(v => v.z);
                    var maxX = usedVertices.Max(v => v.x);
                    var maxY = usedVertices.Max(v => v.y);
                    var maxZ = usedVertices.Max(v => v.z);

                    var objectMin = new Vector3(minX, minY, minZ);
                    var objectScale = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);

                    if (objectScale.X < 0.001f)
                        objectScale.X = 1;
                    if (objectScale.Y < 0.001f)
                        objectScale.Y = 1;
                    if (objectScale.Z < 0.001f)
                        objectScale.Z = 1;

                    var quaternionX = Quaternion.CreateFromYawPitchRoll(0, -(float)group[ModelField.TextureRotateX], 0);
                    var quaternionY = Quaternion.CreateFromYawPitchRoll(-(float)group[ModelField.TextureRotateY], 0, 0);
                    var quaternionZ = Quaternion.CreateFromYawPitchRoll(0, 0, -(float)group[ModelField.TextureRotateZ]);

                    var quaternion = Quaternion.Multiply(quaternionX, quaternionY);
                    var rotation = Matrix4x4.CreateFromQuaternion(Quaternion.Multiply(quaternion, quaternionZ));

                    for (int v = 0; v < vertices.Count; v++)
                    {
                        var vector = new Vector3(vertices[v].x, vertices[v].y, vertices[v].z);
                        vector -= objectMin;
                        vector /= objectScale;
                        vector = Vector3.Transform(vector, rotation);

                        mesh.texcos.Add(new Lib3dsTexturecoordinate(vector.X, vector.Y));
                    }

                    foreach (var face in faces)
                        face.material = i;

                    mesh.faces = faces;
                    mesh.nfaces = (ushort)faces.Count;
                }
            }

            if (!LIB3DS.lib3ds_file_save(file, destination.BaseStream))
                throw new Exception("Saving 3ds file failed");
        }
    }
}