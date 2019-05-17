using MediaEngine.Unpackers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MediaEngine.Exporters
{
    /// <summary>
    /// 3DS files: http://www.martinreddy.net/gfx/3d/3DS.spec
    /// Materials: http://www.martinreddy.net/gfx/3d/MLI.spec
    /// </summary>
    static class ModelExporter
    {
        public static void Export(List<Group> groups, List<float[]> allVertices, List<short[]> allFaces, short[] faceGroupIds, BinaryWriter destination)
        {
            var materials = new MemoryStream();
            using (var writer = new BinaryWriter(materials, Encoding.ASCII, true))
                foreach (var group in groups)
                {
                    var name = Encoding.UTF8.GetBytes(group[ModelField.GroupName].ToString());

                    var textureId = (int)group[ModelField.Texture];
                    var textureName = textureId == -1 ? new byte[0] : Encoding.UTF8.GetBytes($"..\\Texture\\{textureId}.png");

                    writer.Write((ushort)ChunkType.MAT_ENTRY);
                    writer.Write(name.Length + textureName.Length + (textureId == -1 ? 61 : 74)); // chunk length

                    writer.Write((ushort)ChunkType.MAT_NAME);
                    writer.Write(name.Length + 7); // chunk length
                    writer.Write(name); // material name
                    writer.Write((byte)0); // name terminator

                    writer.Write((ushort)ChunkType.MAT_AMBIENT);
                    writer.Write(24); // chunk length

                    var colour = (float[])group[ModelField.MaterialAmbient];
                    writer.Write((ushort)ChunkType.COLOR_F);
                    writer.Write(18); // chunk length
                    writer.Write(colour[0]);
                    writer.Write(colour[1]);
                    writer.Write(colour[2]);

                    writer.Write((ushort)ChunkType.MAT_DIFFUSE);
                    writer.Write(24); // chunk length

                    writer.Write((ushort)ChunkType.COLOR_F);
                    writer.Write(18); // chunk length
                    writer.Write(colour[0]);
                    writer.Write(colour[1]);
                    writer.Write(colour[2]);

                    if (textureId != -1)
                    {
                        writer.Write((ushort)ChunkType.MAT_TEXMAP);
                        writer.Write(textureName.Length + 13); // chunk length

                        writer.Write((ushort)ChunkType.MAT_MAPNAME);
                        writer.Write(textureName.Length + 7); // chunk length
                        writer.Write(textureName); // texture file name
                        writer.Write((byte)0); // name terminator
                    }
                }

            var hierarchies = new MemoryStream();
            using (var writer = new BinaryWriter(hierarchies, Encoding.ASCII, true))
                foreach (var group in groups)
                {
                    var name = Encoding.UTF8.GetBytes(group[ModelField.GroupName].ToString());

                    writer.Write((ushort)ChunkType.OBJECT_NODE_TAG);
                    writer.Write(19 + name.Length); // chunk length

                    writer.Write((ushort)ChunkType.NODE_HDR);
                    writer.Write(13 + name.Length); // chunk length
                    writer.Write(name);
                    writer.Write((byte)0); // name terminator
                    writer.Write(0); // unknown 4 bytes
                    writer.Write(group == groups[0] ? ushort.MaxValue : (ushort)0); // TODO: Hierarchy of object
                }

            var faceIndex = 0;
            short triangleCount = 0;
            var triangles = new List<short>[allFaces.Count];
            var faceVertices = new List<short[]>();
            
            foreach (var face in allFaces)
            {
                // Reverse winding direction
                triangles[faceIndex] = new List<short>(new[] { triangleCount++ });
                faceVertices.Add(new[] { face[1], face[3], face[2], (short)0 });

                // Convert quads
                if (face[0] == 4)
                {
                    triangles[faceIndex].Add(triangleCount++);
                    faceVertices.Add(new[] { face[4], face[3], face[1], (short)0 });
                }

                faceIndex++;
            }

            var objects = new MemoryStream();
            var objectIndices = faceGroupIds == null ?
                Enumerable.Range(0, groups.Count) :
                faceGroupIds.Distinct().Select(b => (int)b).ToArray();

            using (var writer = new BinaryWriter(objects, Encoding.ASCII, true))
                foreach (var i in objectIndices)
                {
                    var group = groups[i];
                    var name = Encoding.UTF8.GetBytes(group[ModelField.GroupName].ToString());

                    var materialFaces = new MemoryStream();
                    var faces = faceVertices;
                    if (faceGroupIds != null)
                        using (var faceWriter = new BinaryWriter(materialFaces, Encoding.ASCII, true))
                        {
                            faces = new List<short[]>();
                            for (short f = 0; f < faceGroupIds.Length; f++)
                                if (faceGroupIds[f] == i)
                                    foreach (var triangleIndex in triangles[f])
                                        faces.Add(faceVertices[triangleIndex]);

                            faceWriter.Write((ushort)ChunkType.MSH_MAT_GROUP);
                            faceWriter.Write(faces.Count * 2 + name.Length + 9); // chunk length
                            faceWriter.Write(name); // material name
                            faceWriter.Write((byte)0); // name terminator
                            faceWriter.Write((short)faces.Count);

                            foreach (var triangleIndex in Enumerable.Range(0, faces.Count))
                                faceWriter.Write((ushort)triangleIndex);
                        }

                    var facesLength = (int)materialFaces.Length + (faces.Count * 8) + 8;
                    var meshLength = facesLength + (allVertices.Count * 20) + 22;

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

                    writer.Write((ushort)ChunkType.TEX_VERTS);
                    writer.Write(allVertices.Count * 8 + 8); // chunk length
                    writer.Write((ushort)allVertices.Count); // total vertices

                    if (faces.Count != 0)
                    {
                        var usedVertices = faces
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

                        var division = new Vector3(
                            (float)group[ModelField.TextureDivisionU],
                            (float)group[ModelField.TextureDivisionV],
                            1);

                        var position = new Vector3(
                            (float)group[ModelField.TexturePositionU],
                            (float)group[ModelField.TexturePositionV],
                            0);

                        var rotation = Matrix4x4.CreateFromYawPitchRoll(
                            -(float)group[ModelField.TextureRotateY],
                            -(float)group[ModelField.TextureRotateX],
                            -(float)group[ModelField.TextureRotateZ]);

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
