using NaturalSort.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    class ScriptUnpacker : Unpacker<ScriptField>
    {
        private readonly List<string> _values = new List<string>();

        protected override byte EndByte => 0;

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            foreach (var value in _values)
                destination.Write(Encoding.UTF8.GetBytes(value + Environment.NewLine));
            return true;

            foreach (var value in _values.GroupBy(v => v).OrderBy(v => v.Key, StringComparer.CurrentCulture.WithNaturalSort()))
            {
                var key = value.Key;
                if (value.Count() > 1)
                    key += " x" + value.Count();
                destination.Write(Encoding.UTF8.GetBytes(key + Environment.NewLine));
            }

            return true;
        }

        protected override void Unpack(BinaryReader source, BinaryWriter destination, ScriptField field)
        {
            string value = null;

            switch (field)
            {
                case ScriptField.Call20:
                    value = $"{field}({ReadVariants(source, false)});";
                    value = value.Replace("<16>, <70>, <48>", "FileWriteOpen");
                    value = value.Replace("<16>, <70>, <49>", "FileReadOpen");
                    break;

                case ScriptField.Call21:
                    source.BaseStream.Position--;
                    value = $"{field}({ReadVariants(source, false)});";
                    break;

                case ScriptField.Call16:
                case ScriptField.Call25:
                //case ScriptField.Call128:
                    value = $"{field}({ReadVariants(source, false)});";
                    break;

                case ScriptField.Call17:
                case ScriptField.Call240:
                    value = $"{field}({source.ReadByte() + ReadVariants(source)});";
                    break;

                case ScriptField.Call242:
                    value = $"{field}({ReadInt32(source) + ReadVariants(source)}); ";
                    break;

                default:
                    value = $"{field}({ReadVariants(source, false)});";
                    break;
            }

            int i = 0;
            while (source.BaseStream.Position != source.BaseStream.Length && source.ReadByte() != 255)
                i++;

            if (value != null)
                _values.Add(value + (i == 0 ? string.Empty : $" +{i} to {source.BaseStream.Position}"));
        }

        private static int ReadInt32(BinaryReader source)
        {
            var i = source.ReadUInt32();
            if (i > int.MaxValue)
            {
                i -= int.MaxValue;
                return -1 * (int)i;
            }

            return (int)i;
        }

        private static string ReadVariant(BinaryReader source)
        {
            var t = source.ReadByte();
            switch (t)
            {
                case 21:
                    var bytes = source.ReadBytes(4);
                    if (source.BaseStream.Position == source.BaseStream.Length)
                        return null;

                    if (bytes.Skip(1).All(b => b == 0))
                        return "new[] { " + string.Join(", ", Enumerable.Range(0, bytes[0]).Select(b => ReadInt32(source))) + " }";

                    source.BaseStream.Position -= 4;
                    goto default;

                case 64:
                    return "(uint)" + source.ReadUInt32().ToString();

                case 65:
                    return "(double)" + source.ReadDouble().ToString();

                case 66:
                    if (source.ReadByte() != 66)
                        source.BaseStream.Position--;
                    return "\"" + Translator.ReadString2(source) + "\"";

                case 69:
                    return ("'" + (char)source.ReadByte() + "'").Replace("\r", "\\r");

                case 71:
                case 242:
                    return "(int)" + ReadInt32(source).ToString();

                case 255:
                    source.BaseStream.Position--;
                    return null;

                default:
                    return $"<{t}>";
            }
        }

        private static string ReadVariants(BinaryReader source, bool leadingComma = true)
        {
            string value = string.Empty;
            string s;
            while (null != (s = ReadVariant(source)))
                value += ", " + s;
            if (!leadingComma)
                value = value.TrimStart(',', ' ');
            return value;
        }
    }
}
