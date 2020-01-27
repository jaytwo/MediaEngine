using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum TableField
    {
        Unknown128 = 128,
        Unknown129 = 129,
        Unknown130 = 130,
        Unknown131 = 131,
        Unknown132 = 132
    }

    class TableUnpacker : Unpacker<TableField>
    {
        private Dictionary<int, string> _strings = new Dictionary<int, string>();

        protected override byte EndByte => 16;

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            for (int i = 0; i < 3; i++)
                if (source.ReadByte() != 0)
                    throw new InvalidDataException();

            var destinationStream = (FileStream)destination.BaseStream;

            using (var writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(destinationStream.Name), "EventHandlers.txt")))
                while (source.BaseStream.Length - source.BaseStream.Position > 3)
                {
                    var address = source.ReadInt32();
                    writer.Write(address.ToString("X8"));

                    _strings.TryGetValue(address, out var s);
                    writer.WriteLine(" " + s);
                }

            source.BaseStream.Position--;
            return true;
        }

        protected override void Unpack(BinaryReader source, BinaryWriter destination, TableField field)
        {
            var address = 0;
            var value = field.ToString() + " = ";

            switch (field)
            {
                case TableField.Unknown128:
                    var header = source.ReadBytes(2);
                    var dataLength = 0;

                    switch(header[0])
                    {
                        case 3:
                            if (header[1] == 32)
                            {
                                header = header.Concat(source.ReadBytes(12)).ToArray();
                                dataLength = header[6] * 4;
                            }
                            else dataLength = 4;
                            break;
                        case 5:
                            dataLength = header[1] == 32 ? 24 : 8;
                            break;
                        case 8:
                            dataLength = 16;
                            break;
                        case 11:
                            if (header[1] == 32)
                            {
                                header = header.Concat(source.ReadBytes(9)).ToArray();
                                dataLength = header[6];
                            }
                            else dataLength = 1;
                            break;
                    }

                    value += string.Join(", ", header.Concat(source.ReadBytes(dataLength)));
                    break;

                case TableField.Unknown129:
                    value += string.Join(", ", source.ReadBytes(10));
                    break;

                case TableField.Unknown130:
                    address = source.ReadInt32();
                    value += $"{address.ToString("X8")}, {source.ReadInt32()}, {source.ReadInt32()}, {source.ReadInt32()}";
                    break;

                case TableField.Unknown131:
                case TableField.Unknown132:
                    address = source.ReadInt32();
                    value += $"{address.ToString("X8")}, {source.ReadInt32()}, {source.ReadInt32()}";
                    break;

                default:
                    throw new NotImplementedException();
            }

            var entry = Translator.ReadString(source);
            if (address != 0)
                _strings.Add(address, entry);

            destination.Write(Encoding.UTF8.GetBytes(value + ", " + entry + Environment.NewLine));

            if (source.ReadUInt16() != 0)
                source.BaseStream.Position--;
        }
    }
}