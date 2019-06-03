using MediaEngine.Unpackers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaEngine.Exporters
{
    /// <summary>
    /// See http://www.martinreddy.net/gfx/3d/3DS.spec
    /// </summary>
    static class HierarchyExporter
    {
        public static MemoryStream Export(List<Group> groups)
        {
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

            return hierarchies;
        }
    }
}