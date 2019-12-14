using Ionic.Zlib;
using System.IO;
using System.Linq;

namespace MediaEngine.Unpackers
{
    static class Inflater
    {
        public static void Inflate(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                stream.Position = 23;

                for (int i = 0; i < 2; i++)
                {
                    var headerType = reader.ReadByte();
                    var header = reader.ReadBytes(headerType == 240 ? 4 : 0);

                    var lengthBytes = reader.ReadBytes(4).Select(b => (uint)b).ToArray();
                    var length = lengthBytes[1] | (lengthBytes[2] << 8) | (lengthBytes[3] << 16) | ((lengthBytes[0] & 0x7F) << 24);

                    using (var output = File.Create(Path.ChangeExtension(path, null) + " " + headerType + Path.GetExtension(path) + "u"))
                    using (var destination = new BinaryWriter(output))
                    using (var deflate = new ZlibStream(stream, CompressionMode.Decompress, true))
                    using (var source = new BinaryReader(deflate))
                        while (true)
                        {
                            var bytes = source.ReadBytes(1024);
                            if (bytes.Length == 0)
                            {
                                stream.Position = 28 + deflate.TotalIn;
                                break;
                            }

                            destination.Write(bytes);
                        }
                }
            }
        }
    }
}
