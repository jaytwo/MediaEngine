using Ionic.Zlib;
using System.IO;
using System.Text;

namespace MediaEngine.Writers
{
    static class Deflater
    {
        public static void Deflate(string vendor, string outPath, params string[] inPaths)
        {
            using (var stream = File.Create(outPath))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes(vendor));
                writer.Write(new byte[8]);
                writer.Write(43);

                for (int i = 0; i < inPaths.Length; i++)
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var input = File.OpenRead(inPaths[i]))
                        using (var deflate = new ZlibStream(memoryStream, CompressionMode.Compress, true))
                            input.CopyTo(deflate);

                        memoryStream.Position = 0;
                        var length = (int)memoryStream.Length * 256 + 128 + i;

                        writer.Write((byte)(16 << i));
                        writer.Write(length);

                        memoryStream.CopyTo(stream);
                    }

                writer.Write((byte)255);
            }
        }
    }
}
