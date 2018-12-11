using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum ScriptField
    {
        UnknownInt16 = 16,
        UnknownString17 = 17,
        UnknownByte32 = 32,
        UnknownLong33 = 33,
        UnknownArray48 = 48
    }

    class ScriptUnpacker : Unpacker<ScriptField>
    {
        protected override void Unpack(BinaryReader source, BinaryWriter destination, ScriptField field)
        {
            switch (field)
            {
                case ScriptField.UnknownArray48:
                    _fieldValues[field] = source.ReadInt32();
                    var unknown48 = source.ReadBytes(_fieldValues[ScriptField.UnknownArray48]);

                    var destinationFile = (FileStream)destination.BaseStream;
                    var codeFile = Path.Combine(Path.GetDirectoryName(destinationFile.Name), destinationFile.Position.ToString());
                    using (var writer = new BinaryWriter(File.Create(codeFile + ".bin")))
                        writer.Write(unknown48);
                    break;

                case ScriptField.UnknownByte32:
                    _fieldValues[field] = source.ReadByte();
                    break;

                case ScriptField.UnknownString17:
                    var bytes = source.ReadBytes(source.ReadInt32());
                    var unknown = Encoding.GetEncoding(932).GetString(bytes);
                    destination.Write(Encoding.UTF8.GetBytes(string.Format("SECTION {0}\r\n\r\n", unknown)));
                    //for (int i = 0; i < _fieldValues[ScriptField.UnknownInt16]; i++)
                    while (source.ReadByte() == 32)
                    {
                        source.BaseStream.Position--;
                        new ScriptSubUnpacker().Unpack(source, destination);
                        source.BaseStream.Position--;
                        destination.Write("\r\n");
                    }
                    source.BaseStream.Position--;
                    destination.Flush();
                    break;

                case ScriptField.UnknownLong33:
                    _fieldValues[field] = source.ReadInt32();
                    source.ReadInt32();
                    break;

                default:
                    _fieldValues[field] = source.ReadInt32();
                    break;
            }
        }
    }
}
