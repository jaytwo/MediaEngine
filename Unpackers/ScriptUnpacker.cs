using System;
using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum ScriptField
    {
        OpenDialog = 64,
        SaveDialog = 65,
        File = 70
    }

    enum DialogProperty
    {
        Filter = 1,
        FileExt = 2,
        Title = 3,
        FileName = 4,
    }

    class ScriptUnpacker : Unpacker<ScriptField>
    {
        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            return source.BaseStream.Position > 100000;
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
                        value = field.ToString() + "." + dialogProperty.ToString() + " = " + ReadString(source);
                    }
                    break;

                case ScriptField.File:
                    switch (source.ReadByte())
                    {
                        case 48:
                            value = "FileRead(" + ReadString(source) + ")";
                            break;
                        case 49:
                            value = "FileWrite(" + ReadString(source) + ")";
                            break;
                    }
                    break;
                default:
                    break;
            }

            if (value != null)
                destination.Write(Encoding.UTF8.GetBytes(value + Environment.NewLine));
        }

        private static string ReadString(BinaryReader source)
        {
            return source.ReadByte() == 66 ? ("\"" + Translator.ReadString2(source) + "\"") : "null";
        }
    }
}
