using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum ScriptField
    {
        UnknownInt16 = 16,
        Type = 17,
        Index = 18,
        UnknownByte32 = 32,
        UnknownArray33 = 33,
        UnknownArray34 = 34,
        UnknownArray35 = 35,
        UnknownArray37 = 37,
        UnknownArray38 = 38,
        UnknownArray48 = 48,
        UnknownInt50 = 50,
        UnknownInt51 = 51,
        UnknownInt52 = 52,
        Name = 64,
    }

    class ScriptUnpacker : Unpacker<ScriptField>
    {
        protected override void Unpack(BinaryReader source, BinaryWriter destination, ScriptField field)
        {
            string value = null;

            switch (field)
            {
                case ScriptField.UnknownInt16:
                    _fieldValues[field] = source.ReadInt32();
                    if (_fieldValues[field] == 0 && source.ReadByte() != 255)
                        source.BaseStream.Position--;
                    break;

                case ScriptField.UnknownByte32:
                    var unknownByte32 = source.ReadByte();
                    if (unknownByte32 != 1 || _fieldValues.ContainsKey(field))
                    {
                        while (source.ReadByte() != 48) { }
                        source.BaseStream.Position--;
                    }
                    _fieldValues[field] = unknownByte32;
                    break;

                case ScriptField.UnknownArray35:
                    var unknown35 = ReadUnknownArray35(source);
                    if (unknown35.Length != 0)
                        WriteArray(destination, field, null, string.Join(", ", unknown35));
                    break;

                case ScriptField.UnknownArray48:
                    var unknown48 = new List<byte>();
                    if (_fieldValues.Count == 0)
                        unknown48 = source.ReadBytes(source.ReadInt32()).ToList();
                    else
                    {
                        unknown48.Add(48);
                        while (true)
                        {
                            try
                            {
                                byte b1 = source.ReadByte();
                                if (b1 == 50 && unknown48.Count == 4)
                                    break;
                                if (b1 == 35 && unknown48.Last() == 255)
                                {
                                    var end48 = source.ReadInt32();
                                    source.BaseStream.Position -= 4;

                                    if (end48 == 0)
                                        break;
                                }
                                unknown48.Add(b1);
                            }
                            catch (EndOfStreamException)
                            {
                                break;
                            }
                        }
                        source.BaseStream.Position--;
                    }
                    _fieldValues[field] = unknown48.Count;
                    value = unknown48.Count.ToString();
                    if (_fieldValues.TryGetValue(ScriptField.Type, out var t) && t == (int)ResourceType.Model)
                    {
                        WriteArray(destination, field, unknown48.ToArray());
                        value += " (in " + destination.BaseStream.Position + ".bin)";
                    }
                    break;

                case ScriptField.Name:
                    value = Translator.ReadString(source);
                    break;

                case ScriptField.UnknownArray33:
                case ScriptField.UnknownArray34:
                case ScriptField.UnknownArray37:
                case ScriptField.UnknownArray38:
                    if (_fieldValues.Count > 0)
                    {
                        if (source.ReadByte() != 0)
                            throw new InvalidDataException();
                        _fieldValues[field] = source.ReadInt32();
                        var unknown3x = source.ReadBytes(_fieldValues[field] * (field == ScriptField.UnknownArray34 ? 4 : 8));
                        WriteArray(destination, field, unknown3x);
                        value = _fieldValues[field].ToString() + " objects";
                        if (field == ScriptField.UnknownArray34)
                        {
                            if (source.ReadByte() == 42)
                                source.ReadBytes(6);
                            ReadUnknownArray35(source);
                            if (source.ReadByte() != 255)
                                source.BaseStream.Position--;
                        }
                        if (source.ReadByte() != 33)
                        {
                            source.BaseStream.Position--;
                            break;
                        }
                    }
                    if (source.ReadByte() != 0)
                        throw new InvalidDataException();
                    var unknown3y = new List<byte>(source.ReadBytes(source.ReadInt32()));
                    byte b3;
                    while ((b3 = source.ReadByte()) != 34)
                        unknown3y.Add(b3);
                    WriteArray(destination, field, unknown3y.ToArray());
                    value = value == null ? string.Empty : (value + " + ");
                    value += unknown3y.Count + " bytes";

                    var unknown3z = new List<byte>(source.ReadBytes(unknown3y.Count * 2));
                    while ((b3 = source.ReadByte()) != 255)
                        unknown3z.Add(b3);
                    while ((b3 = source.ReadByte()) != 32)
                        unknown3z.Add(b3);
                    source.BaseStream.Position--;
                    WriteArray(destination, field, unknown3z.ToArray());

                    _fieldValues[field] = unknown3y.Count;
                    value += " + " + unknown3z.Count + " bytes";
                    break;

                case ScriptField.Type:
                    _fieldValues[field] = source.ReadInt32();
                    value = ((ResourceType)_fieldValues[field]).ToString();
                    var nextField = source.ReadByte();
                    source.BaseStream.Position--;
                    if (nextField != 18 && nextField != 48)
                        value += ":" + Encoding.GetEncoding(932).GetString(source.ReadBytes(_fieldValues[field]));
                    break;

                default:
                    _fieldValues[field] = source.ReadInt32();
                    break;
            }

            destination.Write(Encoding.UTF8.GetBytes(string.Format("{0} = {1}\r\n", field, value ?? _fieldValues[field].ToString())));
        }

        private int[] ReadUnknownArray35(BinaryReader source)
        {
            if (source.ReadByte() != 0)
                throw new InvalidDataException();
            _fieldValues[ScriptField.UnknownArray35] = source.ReadInt32();
            return Enumerable.Range(0, _fieldValues[ScriptField.UnknownArray35])
                .Select(i => source.ReadInt32())
                .ToArray();
        }

        private void WriteArray(BinaryWriter destination, ScriptField field, byte[] array, string s = null)
        {
            var destinationStream = (FileStream)destination.BaseStream;
            var fileName = Path.Combine(Path.GetDirectoryName(destinationStream.Name), destinationStream.Position.ToString());
            using (var writer = new BinaryWriter(File.Create(fileName + "-" + field.ToString() + (array == null ? ".txt" : ".bin"))))
                writer.Write(array ?? Encoding.UTF8.GetBytes(s));
        }

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            destination.Write("\r\n");
            _fieldValues.Clear();

            if (source.ReadByte() != 32)
                source.BaseStream.Position--;

            return false;
        }
    }
}
