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
        public static void Export(List<Group> groups, List<float[]> allVertices, List<short[]> allFaces, short[] faceGroupIds, BinaryWriter destination)
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

            var facesByObject = TriangleExporter.Export(groups, allFaces, faceGroupIds, objectIndices);

            foreach (var i in objectIndices)
            {
                var group = groups[i];
                var name = group[ModelField.GroupName].ToString();
                var faces = facesByObject[i];

                var mesh = LIB3DS.lib3ds_mesh_new(name);
                file.meshes.Add(mesh);

                var useTexVerts = faces.Count != 0;
                LIB3DS.lib3ds_mesh_resize_vertices(mesh, (ushort)allVertices.Count, useTexVerts, false);

                for (int j = 0; j < allVertices.Count; j++)
                    LIB3DS.lib3ds_vector_copy(mesh.vertices[j], allVertices[j]);

                if (useTexVerts)
                {
                    var usedVertices = group.TextureGroup
                        .Select(g => groups.IndexOf(g))
                        .SelectMany(g => facesByObject[g])
                        .SelectMany(f => f.Take(3))
                        .Distinct()
                        .Select(v => allVertices[v])
                        .ToArray();

                    var minX = usedVertices.Min(v => v[0]);
                    var minY = usedVertices.Min(v => v[1]);
                    var minZ = usedVertices.Min(v => v[2]);
                    var maxX = usedVertices.Max(v => v[0]);
                    var maxY = usedVertices.Max(v => v[1]);
                    var maxZ = usedVertices.Max(v => v[2]);

                    var objectMin = new Vector3(minX, minY, minZ);
                    var objectScale = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);

                    if (objectScale.X < 0.001f)
                        objectScale.X = 1;
                    if (objectScale.Y < 0.001f)
                        objectScale.Y = 1;
                    if (objectScale.Z < 0.001f)
                        objectScale.Z = 1;

                    var division = new Vector3(
                        (float)group[ModelField.TextureDivisionU],
                        (float)group[ModelField.TextureDivisionV],
                        1);

                    var position = new Vector3(
                        (float)group[ModelField.TexturePositionU],
                        (float)group[ModelField.TexturePositionV],
                        0);

                    var quaternionX = Quaternion.CreateFromYawPitchRoll(0, -(float)group[ModelField.TextureRotateX], 0);
                    var quaternionY = Quaternion.CreateFromYawPitchRoll(-(float)group[ModelField.TextureRotateY], 0, 0);
                    var quaternionZ = Quaternion.CreateFromYawPitchRoll(0, 0, -(float)group[ModelField.TextureRotateZ]);

                    var quaternion = Quaternion.Multiply(quaternionX, quaternionY);
                    var rotation = Matrix4x4.CreateFromQuaternion(Quaternion.Multiply(quaternion, quaternionZ));

                    for (int v = 0; v < allVertices.Count; v++)
                    {
                        var vector = new Vector3(allVertices[v][0], allVertices[v][1], allVertices[v][2]);
                        vector -= objectMin;
                        vector /= objectScale;

                        vector = Vector3.Transform(vector, rotation);
                        vector *= division;
                        vector -= position;

                        mesh.texcos[v] = new Lib3dsTexturecoordinate(vector.X, vector.Y);
                    }
                }

                LIB3DS.lib3ds_mesh_resize_faces(mesh, (ushort)faces.Count);
                for (int face = 0; face < faces.Count; face++)
                {
                    for (int j = 0; j < 3; j++)
                        mesh.faces[face].index[j] = (ushort)faces[face][j];

                    mesh.faces[face].material = i;
                }
            }

            if (!LIB3DS.lib3ds_file_save(file, destination.BaseStream))
                throw new Exception("Saving 3ds file failed");

            LIB3DS.lib3ds_file_free(file);
        }
    }
}