using System;
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
        protected override byte EndByte => 16;

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            var destinationStream = (FileStream)destination.BaseStream;

            using (var file = File.Create(Path.Combine(Path.GetDirectoryName(destinationStream.Name), "Unknown.bin")))
                source.BaseStream.CopyTo(file);

            source.BaseStream.Position--;
            return true;
        }

        protected override void Unpack(BinaryReader source, BinaryWriter destination, TableField field)
        {
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
                    value += string.Join(", ", source.ReadBytes(16));
                    break;

                case TableField.Unknown131:
                case TableField.Unknown132:
                    value += string.Join(", ", source.ReadBytes(12));
                    break;

                default:
                    throw new NotImplementedException();
            }

            value += ", " + Translator.ReadString(source);
            destination.Write(Encoding.UTF8.GetBytes(value + Environment.NewLine));

            if (source.ReadUInt16() != 0)
                source.BaseStream.Position--;
        }
    }
}