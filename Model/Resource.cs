using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    public class Resource
    {
        public long Offset { get; }
        public ResourceType ResourceType { get; }
        public int Index { get; }
        public string Name { get; }
        public string Source { get; }
        public byte[] Unknown2 { get; }

        public Resource(BinaryReader source)
        {
            ResourceType = (ResourceType)source.ReadByte();

            if ((int)ResourceType == 16)
            {
                ResourceType = (ResourceType)source.ReadInt32();

                if (source.ReadByte() != 17)
                    throw new InvalidDataException();
                Index = source.ReadInt32();

                if (source.ReadByte() != 32)
                    throw new InvalidDataException();
                if (source.ReadByte() != 16)
                    throw new InvalidDataException();

                for (int i = 0; i < 5; i++)
                    if (source.ReadByte() != 0)
                        throw new InvalidDataException();

                if (source.ReadByte() != 1)
                    throw new InvalidDataException();

                var nameBytes = source.ReadBytes(source.ReadByte() + 3);
                Name = Encoding.GetEncoding(932).GetString(nameBytes).TrimStart('\0');

                if (source.ReadByte() != 2)
                    throw new InvalidDataException();
                Unknown2 = source.ReadBytes(7);

                if (source.ReadByte() != 64)
                    throw new InvalidDataException();

                var sourceChars = source.ReadBytes(source.ReadByte() + source.ReadByte());
                Source = Encoding.GetEncoding(932).GetString(sourceChars).TrimStart('.', '\\', '\0');

                if (source.ReadByte() != 255)
                    throw new InvalidDataException();
            }
            else
            {
                Name = (--source.BaseStream.Position).ToString();
            }

            if (string.IsNullOrWhiteSpace(Source))
                Source = "Untitled.txt";

            if (ResourceType == ResourceType.Model)
                Source = Path.ChangeExtension(Source, "3ds");

            Offset = source.BaseStream.Position;
        }
    }
}
