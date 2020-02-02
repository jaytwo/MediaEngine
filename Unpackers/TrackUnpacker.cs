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
        UnknownByte41 = 41,
        UnknownArray42 = 42,
        AnimatePosition = 48,
        AnimateRotation = 49,
        AnimateScale = 50,
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
                case TrackField.UnknownByte32:
                    if (!_fieldValues.Any())
                    {
                        // Start of track
                        if (source.ReadByte() != 16)
                            throw new InvalidDataException();

                        _fieldValues[field] = source.ReadInt32();
                    }
                    else
                    {
                        var unknownByte32 = source.ReadByte();
                        value = unknownByte32.ToString();
                        if (unknownByte32 != 1)
                        {
                            while ((unknownByte32 = source.ReadByte()) != (byte)TrackField.AnimatePosition)
                                value += ", " + unknownByte32.ToString();
                            source.BaseStream.Position--;
                        }
                        _fieldValues[field] = unknownByte32;
                    }
                    break;

                case TrackField.UnknownArray35:
                    var ints = ReadArray(source, field, s => source.ReadInt32());
                    if (!ints.EndsWith(" "))
                        value = ints.Length.ToString() + WriteArray(destination, field, ints);
                    break;

                case TrackField.UnknownByte41:
                    _fieldValues[field] = source.ReadByte();
                    break;

                case TrackField.AnimatePosition:
                    string animationData = string.Empty;
                    _fieldValues.TryGetValue(TrackField.Type, out var resourceType);

                    if (_fieldValues.ContainsKey(TrackField.UnknownByte41))
                    {
                        while (true)
                        {
                            var objectType = source.ReadByte();
                            if (objectType == 255)
                                break;

                            var objectCount = source.ReadByte();
                            animationData += $"Unknown{objectType}[{objectCount}] = " + string.Join(", ",
                                Enumerable.Range(0, resourceType * objectCount).Select(e => source.ReadSingle())) + Environment.NewLine;
                        }

                        value = animationData.Length.ToString();
                    }
                    else
                    {
                        source.BaseStream.Position--;
                        animationData = AnimationUnpacker.Unpack(source, (ResourceType)resourceType, out value);
                    }

                    _fieldValues[field] = animationData.Length;
                    value += WriteArray(destination, field, animationData);
                    break;

                case TrackField.Name:
                    value = Translator.ReadString(source);
                    break;

                case TrackField.UnknownArray33:
                case TrackField.UnknownArray34:
                case TrackField.UnknownArray37:
                case TrackField.UnknownArray38:
                    var section = field;
                    var counts = new List<int>();
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
                            counts.Add(strings.Count);
                            txt += ReadArray(strings.ToArray(), field);
                        }
                        else
                        {
                            counts.Add(0);
                            txt += ReadArray(source, section, s =>
                            {
                                counts[counts.Count - 1]++;
                                if (!_fieldValues.ContainsKey(TrackField.Type) && section == TrackField.UnknownArray33)
                                    return source.ReadByte();
                                else if (section == TrackField.UnknownArray34 || section == TrackField.UnknownArray35)
                                    return source.ReadInt32();
                                else
                                    return source.ReadInt64();
                            });
                        }
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

                    value = string.Join(" + ", counts) + WriteArray(destination, field, txt);
                    break;

                case TrackField.Type:
                    if (_fieldValues[TrackField.UnknownByte32] < 12)
                    {
                        _fieldValues[field] = source.ReadInt32();
                        value = ((ResourceType)_fieldValues[field]).ToString();
                    }
                    else
                    {
                        value = Translator.ReadString(source) + Environment.NewLine;
                        _fieldValues.Clear();
                    }
                    break;

                case TrackField.End:
                    _fieldValues.Clear();
                    break;

                default:
                    _fieldValues[field] = source.ReadInt32();
                    break;
            }

            if (field == TrackField.UnknownByte32 && _fieldValues.Count == 1)
                destination.Write(Encoding.UTF8.GetBytes($"-- TRACK ({_fieldValues[field]}) --\r\n"));
            else if (field == TrackField.End)
                destination.Write(Encoding.UTF8.GetBytes("\r\n"));
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

        private string WriteArray(BinaryWriter destination, TrackField field, string s)
        {
            var destinationStream = (FileStream)destination.BaseStream;
            var fileName = destinationStream.Position.ToString();
            if (_fieldValues.TryGetValue(TrackField.Type, out var t))
                fileName += "-" + ((ResourceType)t).ToString();
            fileName += "-" + field.ToString() + ".txt";

            using (var writer = new BinaryWriter(File.Create(Path.Combine(Path.GetDirectoryName(destinationStream.Name), fileName))))
                writer.Write(Encoding.UTF8.GetBytes(s));

            return $" ({fileName})";
        }

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            return false;
        }
    }
}
