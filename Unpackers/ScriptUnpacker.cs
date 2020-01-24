using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    class ScriptUnpacker : Unpacker<ScriptField>
    {
        protected override byte EndByte => 0;

        protected override void OnStart(ref BinaryReader source, BinaryWriter destination)
        {
            base.OnStart(ref source, destination);

            if (source.ReadByte() != 48)
                throw new InvalidDataException();

            var totalLength = source.ReadInt32();
            var scriptBytes = source.ReadBytes(source.ReadInt32());
            var destinationStream = (FileStream)destination.BaseStream;

            using (var writer = new BinaryWriter(File.Create(Path.Combine(Path.GetDirectoryName(destinationStream.Name), "Table.txt"))))
            using (var reader = new BinaryReader(new MemoryStream(source.ReadBytes(totalLength - scriptBytes.Length - 4))))
                new TableUnpacker().Unpack(reader, writer);

            source = new BinaryReader(new MemoryStream(scriptBytes));
        }

        protected override void Unpack(BinaryReader source, BinaryWriter destination, ScriptField field)
        {
            string value = null;
            bool expect255 = true;

            switch (field)
            {
                case ScriptField.Call20:
                    value = $"{field}({ReadVariants(source, false)});";
                    value = value.Replace("<16>, <70>, <48>", "FileWriteOpen");
                    value = value.Replace("<16>, <70>, <49>", "FileReadOpen");
                    break;

                case ScriptField.Call21:
                    source.BaseStream.Position--;
                    value = $"{field}({ReadVariant(source)});";
                    expect255 = false;
                    break;

                case ScriptField.Call16:
                    var arg1 = ReadVariant(source);
                    var arg2 = ReadVariant(source);
                    var op = (ScriptOperator)source.ReadByte();
                    value = $"if ({arg1} {op} {arg2})";
                    break;

                case ScriptField.Call17:
                    value = $"{field}({source.ReadByte() + ReadVariants(source)});";
                    break;

                case ScriptField.PropertySet:
                    var classType = (ClassType)source.ReadByte();
                    value = $"{classType}[{ReadVariants(source, false)}].";

                    if (source.ReadByte() != 255)
                        throw new InvalidDataException();

                    var prop = new ApiFunction(classType, source.ReadByte(), null, ApiFunctionType.Property);
                    if (ApiFunctions.All.TryGetValue(prop.Key, out var realProp))
                        prop = realProp;

                    switch (prop.FunctionType)
                    {
                        case ApiFunctionType.Property:
                            value += $"{prop.PropertyName ?? prop.PropertyNumber.ToString()} = {ReadVariants(source, false)};";
                            break;

                        case ApiFunctionType.PropertyInt:
                            value += $"{prop.PropertyName} = (int){source.ReadInt32()};";
                            break;

                        case ApiFunctionType.MethodNoParams:
                            value += $"{prop.PropertyName}();";
                            expect255 = false;
                            break;
                    }
                    break;

                case ScriptField.Call242:
                    value = $"{field}({ReadInt32(source) + ReadVariants(source)}); ";
                    break;

                case ScriptField.Call64:
                    value = $"{field}({ReadInt32(source)}); ";
                    break;

                default:
                    value = $"{field}({ReadVariants(source, false)});";
                    break;
            }

            int i = 0;
            while (source.BaseStream.Position != source.BaseStream.Length && expect255 && source.ReadByte() != 255)
                i++;

            if (value != null)
            {
                if (i != 0)
                    value += $" +{i} to {source.BaseStream.Position}";

                destination.Write(Encoding.UTF8.GetBytes(value + Environment.NewLine));
            }
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
