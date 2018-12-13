using System.IO;

namespace MediaEngine.Unpackers
{
    enum ScriptField
    {
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

                    while (true)
                    {
                        source.BaseStream.Position--;
                        new ScriptSubUnpacker().Unpack(source, destination);
                        source.BaseStream.Position--;
                        destination.Write("\r\n");
                    }

                default:
                    throw new InvalidDataException();
            }
        }
    }
}
