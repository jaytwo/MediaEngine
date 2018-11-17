using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum ScriptSubField
    {
        UnknownInt16 = 16,
        UnknownByte32 = 32,
        Unknown33 = 33,
        UnknownArray35 = 35,
        Unknown41 = 41,
        UnknownArray48 = 48,
        UnknownArray49 = 49,
        UnknownArray50 = 50,
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
                        throw new InvalidDataException();
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
                    if (source.ReadByte() != 0)
                        throw new InvalidDataException();
                    _fieldValues.Add(field, source.ReadInt32());
                    var unknownArray35 = Enumerable.Range(0, _fieldValues[field])
                        .Select(i => source.ReadInt32())
                        .ToArray();
                    break;

                case ScriptSubField.UnknownArray48:
                case ScriptSubField.UnknownArray49:
                case ScriptSubField.UnknownArray50:
                    var unknownArray = new List<byte>();
                    byte b1;
                    var b2 = field == ScriptSubField.UnknownArray50 ? 255 : ((byte)field + 1);
                    while ((b1 = source.ReadByte()) != b2)
                        unknownArray.Add(b1);
                    if (field != ScriptSubField.UnknownArray50)
                        source.BaseStream.Position--;
                    _fieldValues.Add(field, unknownArray.Count);
                    break;

                case ScriptSubField.UnknownString64:
                    value = Encoding.GetEncoding(932).GetString(source.ReadBytes(source.ReadInt32()));
                    break;

                case ScriptSubField.Unknown41:
                    _fieldValues.Add(field, source.ReadInt32());
                    source.ReadInt16();
                    break;

                case ScriptSubField.Unknown33:
                    break;

                default:
                    _fieldValues[field] = source.ReadInt32();
                    break;
            }

            destination.Write(Encoding.UTF8.GetBytes(string.Format("{0} = {1}\r\n", field, value ?? _fieldValues[field].ToString())));
        }
    }
}
