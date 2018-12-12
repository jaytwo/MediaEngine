using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum ScriptSubField
    {
        UnknownInt16 = 16,
        UnknownInt17 = 17,
        UnknownInt18 = 18,
        UnknownByte32 = 32,
        UnknownArray33 = 33,
        UnknownArray34 = 34,
        UnknownArray35 = 35,
        UnknownArray37 = 37,
        Unknown41 = 41,
        UnknownArray48 = 48,
        UnknownString64 = 64,
    }

    class ScriptSubUnpacker : Unpacker<ScriptSubField>
    {
        protected override void Unpack(BinaryReader source, BinaryWriter destination, ScriptSubField field)
        {
            string value = null;

            switch (field)
            {
                case ScriptSubField.UnknownInt16:
                    _fieldValues[field] = source.ReadInt32();
                    if (_fieldValues[field] == 0 && source.ReadByte() != 255)
                        source.BaseStream.Position--;
                    break;

                case ScriptSubField.UnknownByte32:
                    var unknownByte32 = source.ReadByte();
                    switch (unknownByte32)
                    {
                        case 16:
                            source.BaseStream.Position--;
                            break;
                        default:
                            if (unknownByte32 != 1 || _fieldValues.ContainsKey(field))
                            {
                                while (source.ReadByte() != 48) { }
                                source.BaseStream.Position--;
                            }
                            break;
                    }
                    _fieldValues[field] = unknownByte32;
                    break;

                case ScriptSubField.UnknownArray35:
                    ReadUnknownArray35(source);
                    break;

                case ScriptSubField.UnknownArray48:
                    var unknownArray = new List<byte>();
                    unknownArray.Add(48);
                    while (true)
                    {
                        byte b1 = source.ReadByte();
                        if (b1 == 51 && unknownArray.Count == 9 && unknownArray.Last() == 255)
                            break;
                        if (b1 == 35 && unknownArray.Last() == 255)
                        {
                            b1 = source.ReadByte();
                            if (b1 == 0)
                            {
                                source.BaseStream.Position--;
                                break;
                            }
                            unknownArray.Add(35);
                        }
                        unknownArray.Add(b1);
                    }
                    source.BaseStream.Position--;
                    _fieldValues[field] = unknownArray.Count;
                    break;

                case ScriptSubField.UnknownString64:
                    value = Encoding.GetEncoding(932).GetString(source.ReadBytes(source.ReadInt32()));
                    break;

                case ScriptSubField.Unknown41:
                    _fieldValues.Add(field, source.ReadInt32());
                    source.ReadInt16();
                    break;

                case ScriptSubField.UnknownArray33:
                case ScriptSubField.UnknownArray34:
                case ScriptSubField.UnknownArray37:
                    if (source.ReadByte() != 0)
                        throw new InvalidDataException();
                    _fieldValues[field] = source.ReadInt32();
                    var unknownBytes3x = source.ReadBytes(_fieldValues[field] * (field == ScriptSubField.UnknownArray34 ? 4 : 8));
                    if (field == ScriptSubField.UnknownArray34)
                    {
                        if (source.ReadByte() == 42)
                            source.ReadBytes(6);
                        ReadUnknownArray35(source);
                        if (source.ReadByte() != 255)
                            source.BaseStream.Position--;
                    }
                    if (source.ReadByte() != 33)
                        source.BaseStream.Position--;
                    else
                    {
                        if (source.ReadByte() != 0)
                            throw new InvalidDataException();
                        var unknown11833 = new List<byte>(source.ReadBytes(source.ReadInt32()));
                        byte b3;
                        while ((b3 = source.ReadByte()) != 34)
                            unknown11833.Add(b3);
                        if (source.ReadInt16() != 0)
                            throw new InvalidDataException();
                        var unknown11834 = new List<byte>(source.ReadBytes(unknown11833.Count * 2));
                        while ((b3 = source.ReadByte()) != 255)
                            unknown11834.Add(b3);
                        while (source.ReadByte() == 255) { }
                        source.BaseStream.Position--;
                    }
                    break;

                case ScriptSubField.UnknownInt17:
                    _fieldValues[field] = source.ReadInt32();
                    if (_fieldValues[field] == 11)
                        value = Encoding.GetEncoding(932).GetString(source.ReadBytes(_fieldValues[field]));
                    break;

                default:
                    _fieldValues[field] = source.ReadInt32();
                    break;
            }

            destination.Write(Encoding.UTF8.GetBytes(string.Format("{0} = {1}\r\n", field, value ?? _fieldValues[field].ToString())));
        }

        private void ReadUnknownArray35(BinaryReader source)
        {
            if (source.ReadByte() != 0)
                throw new InvalidDataException();
            _fieldValues[ScriptSubField.UnknownArray35] = source.ReadInt32();
            var unknownArray35 = Enumerable.Range(0, _fieldValues[ScriptSubField.UnknownArray35])
                .Select(i => source.ReadInt32())
                .ToArray();
        }
    }
}
