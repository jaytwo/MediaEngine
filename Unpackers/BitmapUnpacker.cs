using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum BitmapField
    {
        Width = 16,
        Height = 17,
        BitsPerPixel = 18,
        Colours = 19,
        Tranparent = 20,
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
        private byte[] _transparent = new byte[0];

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

                case BitmapField.Tranparent:
                    _transparent = source.ReadBytes(4);
                    break;

                case BitmapField.BmpPixelData:
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var memoryWriter = new BinaryWriter(memoryStream, Encoding.Default, true))
                        {
                            // BMP header
                            var rowBits = _fieldValues[BitmapField.Width] * _fieldValues[BitmapField.BitsPerPixel];
                            if (rowBits % 32 != 0)
                                rowBits = 32 + (32 * (rowBits / 32));

                            var pixelsLength = rowBits * _fieldValues[BitmapField.Height] / 8;
                            memoryWriter.Write((byte)0x42);
                            memoryWriter.Write((byte)0x4D);
                            memoryWriter.Write(14 + 40 + _colours.Length + pixelsLength);
                            memoryWriter.Write(0);
                            memoryWriter.Write(14 + 40 + _colours.Length);

                            // DIB header
                            memoryWriter.Write(40);
                            memoryWriter.Write(_fieldValues[BitmapField.Width]);
                            memoryWriter.Write(_fieldValues[BitmapField.Height]);
                            memoryWriter.Write((short)1);
                            memoryWriter.Write((short)_fieldValues[BitmapField.BitsPerPixel]);
                            memoryWriter.Write(new byte[16]);
                            memoryWriter.Write(_fieldValues[BitmapField.Colours]);
                            memoryWriter.Write(0);

                            // Pixels
                            memoryWriter.Write(_colours);
                            memoryWriter.Write(source.ReadBytes(pixelsLength));
                        }

                        memoryStream.Position = 0;
                        using (var bitmap = (Bitmap)Image.FromStream(memoryStream))
                        {
                            if (_transparent.Length == 4)
                                bitmap.MakeTransparent(Color.FromArgb(_transparent[0], _transparent[1], _transparent[2]));

                            bitmap.Save(destination.BaseStream, ImageFormat.Png);
                        }
                    }
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
