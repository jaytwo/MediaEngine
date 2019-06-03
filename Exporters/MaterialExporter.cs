using MediaEngine.Unpackers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaEngine.Exporters
{
    /// <summary>
    /// See http://www.martinreddy.net/gfx/3d/MLI.spec
    /// </summary>
    static class MaterialExporter
    {
        public static MemoryStream Export(List<Group> groups)
        {
            var destination = new MemoryStream();

            using (var writer = new BinaryWriter(destination, Encoding.ASCII, true))
                for (int i = 0; i < groups.Count; i++)
                {
                    var group = groups[i];
                    var name = Encoding.UTF8.GetBytes(group[ModelField.GroupName].ToString());
                    group = group.TextureGroup[0];

                    var textureId = (int)group[ModelField.Texture];
                    var textureName = textureId == -1 ? new byte[0] : Encoding.UTF8.GetBytes($"..\\Texture\\{textureId}.png");

                    writer.Write((ushort)ChunkType.MAT_ENTRY);
                    writer.Write(name.Length + textureName.Length + 96); // chunk length

                    writer.Write((ushort)ChunkType.MAT_NAME);
                    writer.Write(name.Length + 7); // chunk length
                    writer.Write(name); // material name
                    writer.Write((byte)0); // name terminator

                    writer.Write((ushort)ChunkType.MAT_AMBIENT);
                    writer.Write(24); // chunk length

                    var colour = (float[])group[ModelField.MaterialAmbient];
                    var transparency = (1.0f - colour[3]) * 100;

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

                    writer.Write((ushort)ChunkType.MAT_TRANSPARENCY);
                    writer.Write(14); // chunk length

                    writer.Write((ushort)ChunkType.INT_PERCENTAGE);
                    writer.Write(8); // chunk length
                    writer.Write((ushort)transparency);

                    writer.Write((ushort)ChunkType.MAT_TEXMAP);
                    writer.Write(textureName.Length + 21); // chunk length

                    writer.Write((ushort)ChunkType.MAT_MAPNAME);
                    writer.Write(textureName.Length + 7); // chunk length
                    writer.Write(textureName); // texture file name
                    writer.Write((byte)0); // name terminator

                    writer.Write((ushort)ChunkType.MAT_MAP_TILING);
                    writer.Write(8); // chunk length
                    writer.Write((ushort)32); // flags
                }

            return destination;
        }
    }
}