using lib3ds.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum TrackField
    {
        Frames = 16,
        Type = 17,
        Index = 18,
        Start = 32,
        UnknownArray33 = 33,
        UnknownArray34 = 34,
        UnknownArray35 = 35,
        UnknownArray37 = 37,
        UnknownArray38 = 38,
        UnknownByte41 = 41,
        UnknownArray42 = 42,
        UnknownByte48 = 48,
        UnknownByte49 = 49,
        UnknownByte50 = 50,
        UnknownInt51 = 51,
        UnknownInt52 = 52,
        Name = 64,
        UnknownInt65 = 65,
        End = 255
    }

    class TrackUnpacker : Unpacker<TrackField>
    {
        private readonly Dictionary<int, Lib3dsFile> _models;
        private readonly string _path;
        private int _depth;

        public TrackUnpacker(string path, Dictionary<int, Lib3dsFile> models)
        {
            _path = path;
            _models = models;
        }

        protected override void Unpack(BinaryReader source, BinaryWriter destination, TrackField field)
        {
            string value = null;

            switch (field)
            {
                case TrackField.Frames:
                    _fieldValues[field] = source.ReadInt32();
                    if (_fieldValues[field] == 0)
                    {
                        if (source.ReadByte() != 255)
                            source.BaseStream.Position--;
                    }
                    else if (!_fieldValues.ContainsKey(TrackField.UnknownByte41))
                    {
                        if (source.ReadByte() != 32)
                            throw new InvalidDataException();

                        var animationData = AnimationUnpacker.Unpack(source, _fieldValues[TrackField.Frames], out value);
                        value += WriteArray(destination, field, animationData);
                    }
                    break;

                case TrackField.Start:
                    if (!_fieldValues.Any())
                    {
                        // Start of track
                        if (source.ReadByte() != 16)
                            throw new InvalidDataException();

                        _fieldValues[field] = source.ReadInt32();
                        _depth++;
                        break;
                    }
                    if (source.ReadByte() != 1)
                        throw new InvalidDataException();
                    return;

                case TrackField.UnknownArray35:
                    var ints = ReadArray(source, field, s => source.ReadInt32());
                    if (!ints.EndsWith(" "))
                        value = ints.Length.ToString() + WriteArray(destination, field, ints);
                    break;

                case TrackField.UnknownByte41:
                    _fieldValues[field] = source.ReadByte();
                    break;

                case TrackField.UnknownByte48:
                case TrackField.UnknownByte49:
                    string unknown48 = string.Empty;
                    if (_fieldValues.ContainsKey(TrackField.UnknownByte41))
                    {
                        _fieldValues.TryGetValue(TrackField.Type, out var resourceType);
                        while (true)
                        {
                            var objectType = (int)source.ReadByte();
                            if (objectType == 255)
                                break;

                            if (objectType == 128)
                                objectType = source.ReadInt32();

                            var objectCount = source.ReadByte();
                            unknown48 += $"Unknown{objectType}[{objectCount}] = " + string.Join(", ",
                                Enumerable.Range(0, resourceType * objectCount).Select(e => source.ReadSingle())) + Environment.NewLine;
                        }

                        value = unknown48.Length.ToString();
                        _fieldValues[field] = unknown48.Length;
                        value += WriteArray(destination, field, unknown48);
                    }
                    else _fieldValues[field] = source.ReadByte();

                    break;

                case TrackField.Name:
                    value = Translator.ReadString(source);
                    break;

                case TrackField.UnknownArray33:
                    if (_depth > 0)
                        goto case TrackField.UnknownArray37;

                    value = ReadArray(source, field, s => source.ReadByte()) + Environment.NewLine;
                    break;

                case TrackField.UnknownArray34:
                    if (_depth > 0)
                        goto case TrackField.UnknownArray37;

                    var strings = new List<object>();
                    for (int i = 0; source.ReadUInt32() != UInt32.MaxValue; i++)
                    {
                        source.BaseStream.Position -= 4;
                        strings.Add($"    {source.ReadInt32()}, {Translator.ReadString(source)}, {source.ReadInt32()}");
                    }
                    value = strings.Count + " {\r\n" + string.Join(Environment.NewLine, strings) + Environment.NewLine + "}";
                    break;

                case TrackField.UnknownArray37:
                case TrackField.UnknownArray38:
                    var section = field;
                    var counts = new List<int>();
                    var txt = string.Empty;

                    while (true)
                    {
                        counts.Add(0);
                        txt += ReadArray(source, section, s =>
                        {
                            counts[counts.Count - 1]++;
                            return (section == TrackField.UnknownArray34 || section == TrackField.UnknownArray35) ?
                                source.ReadInt32() : source.ReadInt64();
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

                    value = string.Join(" + ", counts) + WriteArray(destination, field, txt);
                    break;

                case TrackField.Type:
                    _fieldValues[field] = source.ReadInt32();
                    value = ((ResourceType)_fieldValues[field]).ToString();
                    if (_fieldValues.ContainsKey(TrackField.UnknownByte41) && source.ReadByte() != 255)
                        source.BaseStream.Position--;
                    break;

                case TrackField.End:
                    _fieldValues.Clear();
                    break;

                default:
                    _fieldValues[field] = source.ReadInt32();
                    break;
            }

            if (field == TrackField.Start && _fieldValues.Count == 1)
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

            return $"Section {(int)field}" + Environment.NewLine +
                "    Objects[" + items.Length + "] = " + string.Join(", ", items);
        }

        private string WriteArray(BinaryWriter destination, TrackField field, string s)
        {
            var destinationStream = (FileStream)destination.BaseStream;
            var fileName = destinationStream.Position.ToString();

            if (_fieldValues.TryGetValue(TrackField.Type, out var t))
                fileName += "-" + ((ResourceType)t).ToString();

            if (_fieldValues.TryGetValue(TrackField.Index, out t))
                fileName += "-" + t.ToString();

            fileName += "-" + field.ToString() + ".txt";

            using (var writer = new BinaryWriter(File.Create(Path.Combine(_path, fileName))))
                writer.Write(Encoding.UTF8.GetBytes(s));

            return $" ({fileName})";
        }

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            return --_depth < 0;
        }

        protected override void OnStart(ref BinaryReader source, BinaryWriter destination) { }
    }
}
