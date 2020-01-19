using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum TrackField
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
        UnknownArray42 = 42,
        UnknownArray48 = 48,
        UnknownInt50 = 50,
        UnknownInt51 = 51,
        UnknownInt52 = 52,
        Name = 64,
        UnknownInt65 = 65,
        End = 255
    }

    class TrackUnpacker : Unpacker<TrackField>
    {
        protected override void Unpack(BinaryReader source, BinaryWriter destination, TrackField field)
        {
            string value = null;

            switch (field)
            {
                case TrackField.UnknownInt16:
                    _fieldValues[field] = source.ReadInt32();
                    if (_fieldValues[field] == 0 && source.ReadByte() != 255)
                        source.BaseStream.Position--;
                    break;

                case TrackField.UnknownByte32:
                    var unknownByte32 = source.ReadByte();
                    if (unknownByte32 == 16 || unknownByte32 == 65)
                        source.BaseStream.Position--;
                    else
                    {
                        value = unknownByte32.ToString();
                        if (unknownByte32 != 1)// || _fieldValues.ContainsKey(field))
                        {
                            while ((unknownByte32 = source.ReadByte()) != 48)
                                value += ", " + unknownByte32.ToString();
                            source.BaseStream.Position--;
                        }
                    }
                    _fieldValues[field] = unknownByte32;
                    break;

                case TrackField.UnknownArray35:
                    var ints = ReadArray(source, field, s => source.ReadInt32());
                    if (!ints.EndsWith(" "))
                        WriteArray(destination, field, null, ints);
                    break;

                case TrackField.UnknownArray48:
                    List<byte> unknown48bytes = null;
                    string unknown48string = null;
                    if (_fieldValues.TryGetValue(TrackField.Type, out var resourceType) &&
                        TrackItemUnpacker.CanUnpack((ResourceType)resourceType))
                    {
                        source.BaseStream.Position--;
                        unknown48string = TrackItemUnpacker.Unpack(source, (ResourceType)resourceType);
                    }
                    else
                    {
                        unknown48bytes = new List<byte>();
                        unknown48bytes.Add(48);
                        while (true)
                        {
                            try
                            {
                                byte b1 = source.ReadByte();
                                if (b1 == 50 && unknown48bytes.Count == 4)
                                    break;
                                if (b1 == 35 && unknown48bytes.Last() == 255)
                                {
                                    var end48 = source.ReadInt32();
                                    source.BaseStream.Position -= 4;

                                    if (end48 == 0)
                                        break;
                                }
                                unknown48bytes.Add(b1);
                            }
                            catch (EndOfStreamException)
                            {
                                break;
                            }
                        }
                    }

                    _fieldValues[field] = unknown48bytes?.Count ?? unknown48string.Length;
                    value = _fieldValues[field].ToString();

                    WriteArray(destination, field, unknown48bytes?.ToArray(), unknown48string);
                    value += " (in " + destination.BaseStream.Position + ".bin)";
                    break;

                case TrackField.Name:
                    value = Translator.ReadString(source);
                    break;

                case TrackField.UnknownArray33:
                case TrackField.UnknownArray34:
                case TrackField.UnknownArray37:
                case TrackField.UnknownArray38:
                    var section = field;
                    var txt = string.Empty;

                    while (true)
                    {
                        if (!_fieldValues.ContainsKey(TrackField.Type) && section == TrackField.UnknownArray34)
                        {
                            var strings = new List<object>();
                            for (int i = 0; source.ReadByte() != 255; i++)
                            {
                                source.BaseStream.Position--;
                                strings.Add(i % 3 == 1 ? Translator.ReadString(source) : source.ReadInt32().ToString());
                            }

                            source.BaseStream.Position--;
                            txt += ReadArray(strings.ToArray(), field);
                        }
                        else txt += ReadArray(source, section, s =>
                        {
                            if (!_fieldValues.ContainsKey(TrackField.Type) && section == TrackField.UnknownArray33)
                                return source.ReadByte();
                            else if (section == TrackField.UnknownArray34 || section == TrackField.UnknownArray35)
                                return source.ReadInt32();
                            else
                                return source.ReadInt64();
                        });

                        txt += Environment.NewLine;
                        section = (TrackField)source.ReadByte();

                        if (section != TrackField.UnknownArray33 &&
                            section != TrackField.UnknownArray34 &&
                            section != TrackField.UnknownArray35 &&
                            section != TrackField.UnknownArray37 &&
                            section != TrackField.UnknownArray38 &&
                            section != TrackField.UnknownArray42)
                        {
                            source.BaseStream.Position--;
                            break;
                        }
                    }

                    WriteArray(destination, field, null, txt);
                    value = "(in " + destination.BaseStream.Position + ".txt)";

                    break;

                case TrackField.Type:
                    _fieldValues[field] = source.ReadInt32();
                    value = ((ResourceType)_fieldValues[field]).ToString();
                    var nextField = source.ReadByte();
                    source.BaseStream.Position--;
                    if (nextField != 18 && nextField != 48)
                        value += ":" + Encoding.GetEncoding(932).GetString(source.ReadBytes(_fieldValues[field]));
                    break;

                case TrackField.End:
                    _fieldValues.Clear();
                    break;

                default:
                    _fieldValues[field] = source.ReadInt32();
                    break;
            }

            if (field == TrackField.End)
                destination.Write("\r\n");
            else
                destination.Write(Encoding.UTF8.GetBytes(string.Format("{0} = {1}\r\n", field, value ?? _fieldValues[field].ToString())));
        }

        private string ReadArray(BinaryReader source, TrackField field, Func<BinaryReader, object> read)
        {
            var header = source.ReadByte();
            if (header != 0 && header != 255)
                throw new InvalidDataException();

            _fieldValues[field] = source.ReadInt32();
            if (header == 255)
                _fieldValues[field] = 0;

            var items = Enumerable.Range(0, _fieldValues[field])
                .Select(i => read(source))
                .ToArray();

            return ReadArray(items, field);
        }

        private string ReadArray(object[] items, TrackField field)
        {
            return $"Section {(int)field}" + Environment.NewLine +
                "    Objects[" + items.Length + "] = " + string.Join(", ", items);
        }

        private void WriteArray(BinaryWriter destination, TrackField field, byte[] array, string s = null)
        {
            var destinationStream = (FileStream)destination.BaseStream;
            var fileName = destinationStream.Position.ToString();
            if (_fieldValues.TryGetValue(TrackField.Type, out var t))
                fileName += "-" + ((ResourceType)t).ToString();
            fileName += "-" + field.ToString() + (array == null ? ".txt" : ".bin");

            using (var writer = new BinaryWriter(File.Create(Path.Combine(Path.GetDirectoryName(destinationStream.Name), fileName))))
                writer.Write(array ?? Encoding.UTF8.GetBytes(s));
        }

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            return false;
        }
    }
}
