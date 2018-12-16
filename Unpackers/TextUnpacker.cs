using System;
using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum TextField
    {
        Colour = 16,
        BackColour = 17,
        Height = 18,
        Size = 19,
        Font = 21,
        Byte22 = 22,
        Strings = 32
    }

    class TextUnpacker : Unpacker<TextField>
    {
        protected override void Unpack(BinaryReader source, BinaryWriter destination, TextField field)
        {
            string value;

            switch (field)
            {
                case TextField.Colour:
                case TextField.BackColour:
                    value = string.Join(", ", source.ReadByte(), source.ReadByte(), source.ReadByte());
                    source.ReadByte();
                    break;

                case TextField.Font:
                    value = Translator.ReadString(source);
                    break;

                case TextField.Byte22:
                    value = source.ReadByte().ToString();
                    break;

                case TextField.Strings:
                    var count = source.ReadInt32();
                    value = string.Empty;
                    for (int i = 0; i < count; i++)
                        value += Environment.NewLine + Translator.ReadString(source);
                    break;

                default:
                    value = source.ReadInt32().ToString();
                    break;
            }

            destination.Write(Encoding.UTF8.GetBytes(string.Format("{0} = {1}\r\n", field, value)));
        }
    }
}
