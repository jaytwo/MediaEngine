using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
        UnknownInt21 = 21,
        UnknownByte22 = 22,
        UnknownInt26 = 26,
        UnknownByte27 = 27,
        UnknownShort31 = 31,
        BmpColourTable = 32,
        BmpPixelData = 48,
        JpegData = 64,
        AlphaMap = 80,
        UnknownInt96 = 96,
        UnknownInt97 = 97,
        UnknownInt98 = 98,
        UnknownArray99 = 99,
        UnknownInt100 = 100,
        UnknownInt101 = 101,
        UnknownByte128 = 128,
        UnknownByte129 = 129,
    }

    class BitmapUnpacker : Unpacker<BitmapField>
    {
        private byte[] _colours = new byte[0];
        private byte[] _transparent = new byte[0];
        private BitArray _alphaMap;
        private MemoryStream _pixels;

        protected override void Unpack(BinaryReader source, BinaryWriter destination, BitmapField field)
        {
            switch (field)
            {
                case BitmapField.JpegData:
                    _pixels = new MemoryStream(source.ReadBytes(source.ReadInt32()));
                    break;

                case BitmapField.BmpColourTable:
                    _colours = source.ReadBytes(_fieldValues[BitmapField.Colours] * 4);
                    break;

                case BitmapField.Tranparent:
                    _transparent = source.ReadBytes(4);
                    break;

                case BitmapField.BmpPixelData:
                    _pixels = new MemoryStream();
                    using (var memoryWriter = new BinaryWriter(_pixels, Encoding.Default, true))
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

                    _pixels.Position = 0;
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

                case BitmapField.AlphaMap:
                    _fieldValues.Add(field, source.ReadInt32());
                    _alphaMap = new BitArray(source.ReadBytes(_fieldValues[field]));
                    break;

                case BitmapField.Width:
                case BitmapField.Height:
                case BitmapField.Colours:
                case BitmapField.UnknownInt21:
                case BitmapField.UnknownInt26:
                case BitmapField.UnknownInt96:
                case BitmapField.UnknownInt97:
                case BitmapField.UnknownInt98:
                case BitmapField.UnknownInt100:
                case BitmapField.UnknownInt101:
                    _fieldValues.Add(field, source.ReadInt32());
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            using (var bitmap = (Bitmap)Image.FromStream(_pixels))
            {
                if (_alphaMap != null)
                {
                    var index = 8;
                    for (int y = bitmap.Height - 1; y >= 0; y--)
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            if (!_alphaMap[--index])
                                bitmap.SetPixel(x, y, Color.Transparent);

                            if (index % 8 == 0)
                                index += 16;
                        }
                }
                else if (_transparent[3] == 0)
                {
                    bitmap.MakeTransparent(Color.FromArgb(_transparent[0], _transparent[1], _transparent[2]));
                }

                bitmap.Save(destination.BaseStream, ImageFormat.Png);
            }

            return base.OnFinish(source, destination);
        }
    }
}
