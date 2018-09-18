using Ionic.Zlib;
using System.IO;

namespace MediaEngine.Unpackers
{
    static class Inflater
    {
        public static void Inflate(string path)
        {
            long offset = 28;
            using (var stream = File.OpenRead(path))
                for (int i = 0; i < 2; i++)
                {
                    stream.Position = offset;

                    using (var output = File.Create(Path.ChangeExtension(path, null) + i + ".bin"))
                    using (var destination = new BinaryWriter(output))
                    using (var deflate = new ZlibStream(stream, CompressionMode.Decompress, true))
                    using (var source = new BinaryReader(deflate))
                        while (true)
                        {
                            var bytes = source.ReadBytes(1024);
                            if (bytes.Length == 0)
                            {
                                offset += deflate.TotalIn + 5;
                                break;
                            }

                            destination.Write(bytes);
                        }
                }
        }
    }
}
