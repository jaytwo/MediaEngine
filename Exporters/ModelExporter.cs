using MediaEngine.Unpackers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MediaEngine.Exporters
{
    /// <summary>
    /// See http://www.martinreddy.net/gfx/3d/3DS.spec
    /// </summary>
    static class ModelExporter
    {
        public static void Export(List<Group> groups, List<float[]> allVertices, List<short[]> allFaces, short[] faceGroupIds, BinaryWriter destination)
        {
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
            }

            var objectIndices = (faceGroupIds == null ? Enumerable.Range(0, groups.Count) :
            faceGroupIds.Distinct().Select(b => (int)b)).ToArray();

            var materials = MaterialExporter.Export(groups);
            var hierarchies = HierarchyExporter.Export(groups);
            var facesByObject = TriangleExporter.Export(groups, allFaces, faceGroupIds, objectIndices);

            var objects = new MemoryStream();

            using (var writer = new BinaryWriter(objects, Encoding.ASCII, true))
                foreach (var i in objectIndices)
                {
                    var group = groups[i];
                    var name = Encoding.UTF8.GetBytes(group[ModelField.GroupName].ToString());
                    var materialFaces = new MemoryStream();
                    var faces = facesByObject[i];

                    if (faceGroupIds != null)
                        using (var faceWriter = new BinaryWriter(materialFaces, Encoding.ASCII, true))
                        {
                            faceWriter.Write((ushort)ChunkType.MSH_MAT_GROUP);
                            faceWriter.Write(faces.Count * 2 + name.Length + 9); // chunk length
                            faceWriter.Write(name); // material name
                            faceWriter.Write((byte)0); // name terminator
                            faceWriter.Write((short)faces.Count);

                            foreach (var triangleIndex in Enumerable.Range(0, faces.Count))
                                faceWriter.Write((ushort)triangleIndex);
                        }

                    var useTexVerts = faces.Count != 0;
                    var texVertsLength = useTexVerts ? (allVertices.Count * 8 + 8) : 0;
                    var facesLength = (int)materialFaces.Length + (faces.Count * 8) + 8;
                    var meshLength = texVertsLength + facesLength + (allVertices.Count * 12) + 14;

                    writer.Write((ushort)ChunkType.NAMED_OBJECT);
                    writer.Write(meshLength + 7 + name.Length); // chunk length
                    writer.Write(name); // object name
                    writer.Write((byte)0); // name terminator

                    writer.Write((ushort)ChunkType.N_TRI_OBJECT);
                    writer.Write(meshLength); // chunk length

                    writer.Write((ushort)ChunkType.POINT_ARRAY);
                    writer.Write(allVertices.Count * 12 + 8); // chunk length
                    writer.Write((ushort)allVertices.Count); // total vertices

                    foreach (var vertex in allVertices)
                        foreach (var offset in vertex)
                            writer.Write(offset);

                    if (useTexVerts)
                    {
                        writer.Write((ushort)ChunkType.TEX_VERTS);
                        writer.Write(texVertsLength); // chunk length
                        writer.Write((ushort)allVertices.Count); // total vertices

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

                        foreach (var v in allVertices)
                        {
                            var vector = new Vector3(v[0], v[1], v[2]);
                            vector -= objectMin;
                            vector /= objectScale;

                            vector = Vector3.Transform(vector, rotation);
                            vector *= division;
                            vector -= position;

                            writer.Write(vector.X);
                            writer.Write(vector.Y);
                        }
                    }

                    writer.Write((ushort)ChunkType.FACE_ARRAY);
                    writer.Write(facesLength); // chunk length
                    writer.Write((ushort)faces.Count); // total polygons

                    foreach (var face in faces)
                        foreach (var vertex in face)
                            writer.Write(vertex);

                    writer.Write(materialFaces.ToArray());
                }

            destination.Write((ushort)ChunkType.M3DMAGIC);
            destination.Write((int)(objects.Length + materials.Length + hierarchies.Length + 18)); // chunk length

            destination.Write((ushort)ChunkType.MDATA);
            destination.Write((int)(objects.Length + materials.Length + 6)); // chunk length
            destination.Write(materials.ToArray());

            destination.Write(objects.ToArray());

            destination.Write((ushort)ChunkType.KFDATA);
            destination.Write((int)hierarchies.Length + 6); // chunk length
            destination.Write(hierarchies.ToArray());
        }
    }
}