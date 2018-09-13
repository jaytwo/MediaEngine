using System.IO;

namespace MediaEngine.Unpackers
{
    enum BitmapField
    {
        Width = 16,
        Height = 17,
        BitsPerPixel = 18,
        Colours = 19,
        UnknownByte22 = 22,
        UnknownByte27 = 27,
        UnknownShort31 = 31,
        BmpColourTable = 32,
        BmpPixelData = 48,
        JpegData = 64,
        UnknownArray80 = 80,
        UnknownArray99 = 99,
        UnknownByte128 = 128,
        UnknownByte129 = 129,
    }

    class BitmapUnpacker : Unpacker<BitmapField>
    {
        private byte[] _colours = new byte[0];

        protected override void Unpack(BinaryReader source, BinaryWriter destination, BitmapField field)
        {
            switch (field)
            {
                case BitmapField.JpegData:
                    var length = source.ReadInt32();
                    destination.Write(source.ReadBytes(length));
                    break;

                case BitmapField.BmpColourTable:
                    _colours = source.ReadBytes(_fieldValues[BitmapField.Colours] * 4);
                    break;

                case BitmapField.BmpPixelData:
                    // BMP header
                    var rowBits = _fieldValues[BitmapField.Width] * _fieldValues[BitmapField.BitsPerPixel];
                    if (rowBits % 32 != 0)
                        rowBits = 32 + (32 * (rowBits / 32));

                    var pixelsLength = rowBits * _fieldValues[BitmapField.Height] / 8;
                    destination.Write((byte)0x42);
                    destination.Write((byte)0x4D);
                    destination.Write(14 + 40 + _colours.Length + pixelsLength);
                    destination.Write(0);
                    destination.Write(14 + 40 + _colours.Length);

                    // DIB header
                    destination.Write(40);
                    destination.Write(_fieldValues[BitmapField.Width]);
                    destination.Write(_fieldValues[BitmapField.Height]);
                    destination.Write((short)1);
                    destination.Write((short)_fieldValues[BitmapField.BitsPerPixel]);
                    destination.Write(new byte[16]);
                    destination.Write(_fieldValues[BitmapField.Colours]);
                    destination.Write(0);

                    // Pixels
                    destination.Write(_colours);
                    destination.Write(source.ReadBytes(pixelsLength));
                    break;

                case BitmapField.BitsPerPixel:
                case BitmapField.UnknownShort31:
                    _fieldValues.Add(field, source.ReadUInt16());
                    break;

                case BitmapField.UnknownByte22:
                case BitmapField.UnknownByte27:
                case BitmapField.UnknownByte128:
                case BitmapField.UnknownByte129:
                    _fieldValues.Add(field, source.ReadByte());
                    break;

                case BitmapField.UnknownArray99:
                    _fieldValues.Add(field, source.ReadInt32());
                    source.ReadBytes(12);
                    break;

                case BitmapField.UnknownArray80:
                    var unknownLength = source.ReadInt32();
                    var unknown = source.ReadBytes(unknownLength);
                    break;

                default:
                    _fieldValues.Add(field, source.ReadInt32());
                    break;
            }
        }
    }
}
