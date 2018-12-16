using MediaEngine.Unpackers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Exporters
{
    /// <summary>
    /// 3DS files: http://www.martinreddy.net/gfx/3d/3DS.spec
    /// Materials: http://www.martinreddy.net/gfx/3d/MLI.spec
    /// </summary>
    static class ModelExporter
    {
        public static void Export(List<SceneObject> sceneObjects, byte[] allVertices, byte[] allFaces,
            byte[] faceMaterials, List<short>[] triangles, BinaryWriter destination)
        {
            var materials = new MemoryStream();
            using (var writer = new BinaryWriter(materials, Encoding.ASCII, true))
                for (int i = 0; i < sceneObjects.Count; i++)
                {
                    var name = Encoding.UTF8.GetBytes(sceneObjects[i][ModelField.Name].ToString());

                    var textureId = (int)sceneObjects[i][ModelField.Texture];
                    var textureName = textureId == -1 ? new byte[0] : Encoding.UTF8.GetBytes($"..\\Texture\\{textureId}.bmp");

                    writer.Write((ushort)0xAFFF); // EDIT_MATERIAL chunk
                    writer.Write(name.Length + textureName.Length + (textureId == -1 ? 61 : 74)); // chunk length

                    writer.Write((ushort)0xA000); // MAT_NAME chunk
                    writer.Write(name.Length + 7); // chunk length
                    writer.Write(name); // material name
                    writer.Write((byte)0); // name terminator

                    writer.Write((ushort)0xA010); // Ambient colour chunk
                    writer.Write(24); // chunk length

                    var colour = (float[])sceneObjects[i][ModelField.MaterialAmbient];
                    writer.Write((ushort)0x0010); // RGB chunk
                    writer.Write(18); // chunk length
                    writer.Write(colour[0]);
                    writer.Write(colour[1]);
                    writer.Write(colour[2]);

                    writer.Write((ushort)0xA020); // Diffuse colour chunk
                    writer.Write(24); // chunk length

                    writer.Write((ushort)0x0010); // RGB chunk
                    writer.Write(18); // chunk length
                    writer.Write(colour[0]);
                    writer.Write(colour[1]);
                    writer.Write(colour[2]);

                    if (textureId != -1)
                    {
                        writer.Write((ushort)0xA200); // Diffuse texture channel 0 chunk
                        writer.Write(textureName.Length + 13); // chunk length

                        writer.Write((ushort)0xA300); // Texture file name
                        writer.Write(textureName.Length + 7); // chunk length
                        writer.Write(textureName); // texture file name
                        writer.Write((byte)0); // name terminator
                    }
                }

            var hierarchies = new MemoryStream();
            using (var writer = new BinaryWriter(hierarchies, Encoding.ASCII, true))
                for (int i = 0; i < sceneObjects.Count; i++)
                {
                    var name = Encoding.UTF8.GetBytes(sceneObjects[i][ModelField.Name].ToString());
                    writer.Write((ushort)0xB010); // KEYF_OBJHIERARCH chunk
                    writer.Write(13 + name.Length); // chunk length
                    writer.Write(name); // material name
                    writer.Write((byte)0); // name terminator
                    writer.Write(0); // unknown 4 bytes
                    writer.Write((ushort)(ushort.MaxValue)); // TODO: Hierarchy of object
                }

            var objects = new MemoryStream();
            var objectIndices = faceMaterials == null ?
                Enumerable.Range(0, sceneObjects.Count) :
                faceMaterials.Distinct().Select(b => (int)b).ToArray();

            using (var writer = new BinaryWriter(objects, Encoding.ASCII, true))
                foreach (var i in objectIndices)
                {
                    var name = Encoding.UTF8.GetBytes(sceneObjects[i][ModelField.Name].ToString());

                    var materialFaces = new MemoryStream();
                    var faces = allFaces;
                    if (faceMaterials != null)
                        using (var faceWriter = new BinaryWriter(materialFaces, Encoding.ASCII, true))
                        {
                            var trimmedFaces = new List<byte>();
                            for (short faceIndex = 0; faceIndex < faceMaterials.Length; faceIndex++)
                                if (faceMaterials[faceIndex] == i)
                                    foreach (var triangleIndex in triangles[faceIndex])
                                        for (int j = 0; j < 8; j++)
                                            trimmedFaces.Add(allFaces[triangleIndex * 8 + j]);

                            faceWriter.Write((ushort)0x4130); // TRI_MATERIAL chunk
                            faceWriter.Write(trimmedFaces.Count / 4 + name.Length + 9); // chunk length
                            faceWriter.Write(name); // material name
                            faceWriter.Write((byte)0); // name terminator
                            faceWriter.Write((short)trimmedFaces.Count / 8);

                            foreach (var triangleIndex in Enumerable.Range(1, trimmedFaces.Count / 8))
                                faceWriter.Write((ushort)triangleIndex);

                            faces = trimmedFaces.ToArray();
                        }
                    
                    var facesLength = (int)materialFaces.Length + faces.Length + 8;
                    var meshLength = facesLength + allVertices.Length + 14;

                    writer.Write((ushort)0x4000); // EDIT_OBJECT chunk
                    writer.Write(meshLength + 7 + name.Length); // chunk length
                    writer.Write(name); // object name
                    writer.Write((byte)0); // name terminator

                    writer.Write((ushort)0x4100); // OBJECT_TRIMESH chunk
                    writer.Write(meshLength); // chunk length

                    writer.Write((ushort)0x4110); // TRI_VERTEXL chunk
                    writer.Write(allVertices.Length + 8); // chunk length
                    writer.Write((ushort)(allVertices.Length / 12)); // total vertices
                    writer.Write(allVertices);

                    writer.Write((ushort)0x4120); // TRI_FACEL1 chunk
                    writer.Write(facesLength); // chunk length
                    writer.Write((ushort)(faces.Length / 8)); // total polygons
                    writer.Write(faces);

                    writer.Write(materialFaces.ToArray());
                }

            destination.Write((ushort)0x4D4D); // MAIN3DS chunk
            destination.Write((int)(objects.Length + materials.Length + hierarchies.Length + 24)); // chunk length

            destination.Write((ushort)0x3D3D); // EDIT3DS chunk
            destination.Write((int)(objects.Length + materials.Length + 6)); // chunk length
            destination.Write(materials.ToArray());

            destination.Write(objects.ToArray());

            destination.Write((ushort)0xB000); // KEYF3DS chunk
            destination.Write((int)hierarchies.Length + 12); // chunk length

            destination.Write((ushort)0xB002); // KEYF_OBJDES chunk
            destination.Write((int)hierarchies.Length + 6); // chunk length
            destination.Write(hierarchies.ToArray());
        }
    }
}
