using NaturalSort.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum ScriptField
    {
        Call16 = 16,
        Call17 = 17,
        File = 20,
        Call21 = 21,
        Call25 = 25,
        OpenDialog = 64,
        SaveDialog = 65,
        Call128 = 128,
        Call240 = 240,
        Call242 = 242,
    }

    enum DialogProperty
    {
        Filter = 1,
        FileExt = 2,
        Title = 3,
        FileName = 4,
    }

    enum FileProperty
    {
        Read = 48,
        Write = 49,
    }

    class ScriptUnpacker : Unpacker<ScriptField>
    {
        private readonly List<string> _values = new List<string>();

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            if (source.BaseStream.Position < 1500000)
                return false;

            /*foreach (var value in _values)
                destination.Write(Encoding.UTF8.GetBytes(value + Environment.NewLine));
            return true;*/

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
                case ScriptField.OpenDialog:
                case ScriptField.SaveDialog:
                    var dialogProperty = (DialogProperty)source.ReadByte();
                    if (source.ReadByte() == 66) // TODO: Won't be needed when we parse commands accurately
                    {
                        source.BaseStream.Position--;
                        value = $"{field}.{dialogProperty} = {ReadVariant(source)};";
                    }
                    else
                        value = $"{field}({ReadVariants(source, false)});";
                    break;

                case ScriptField.File:
                    var f1 = source.ReadInt32();
                    if (source.ReadByte() != 21)
                        break;
                    var f2 = source.ReadInt32();
                    if (source.ReadByte() != 16 || source.ReadByte() != 70)
                        break;
                    var fileProperty = (FileProperty)source.ReadByte();
                    value = $"{field}.{fileProperty}({ReadVariants(source, false)})";
                    break;

                case ScriptField.Call21:
                    var bytes = source.ReadBytes(4);
                    if (bytes.Skip(1).All(b => b == 0))
                        value = $"{field}(new[] {{ {string.Join(", ", Enumerable.Range(0, bytes[0]).Select(b => ReadInt32(source)))} }}{ReadVariants(source)});";
                    else
                        value = $"{field}({ReadVariants(source, false)});";
                    break;

                case ScriptField.Call16:
                case ScriptField.Call25:
                case ScriptField.Call128:
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
                    //value = $"{field}({ReadVariants(source, false)});";
                    break;
            }

            int i = 0;
            while (source.ReadByte() != 255) i++;
            source.BaseStream.Position--;

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
                case 64:
                    return "(uint)" + source.ReadUInt32().ToString();

                case 65:
                    return "(double)" + source.ReadDouble().ToString();

                case 66:
                    return "\"" + Translator.ReadString2(source) + "\"";

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
