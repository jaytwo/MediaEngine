using System;
using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum ScoreField
    {
        Index = 16,
        Name = 17,
    }

    class ScoreUnpacker : Unpacker<ScoreField>
    {
        protected override void Unpack(BinaryReader source, BinaryWriter destination, ScoreField field)
        {
            string value = null;

            switch (field)
            {
                case ScoreField.Index:
                    _fieldValues[field] = source.ReadInt32();
                    break;

                case ScoreField.Name:
                    value = Translator.ReadString(source);
                    var destinationStream = (FileStream)destination.BaseStream;
                    var path = Path.Combine(Path.GetDirectoryName(destinationStream.Name), $"{_fieldValues[ScoreField.Index]}-{value}");
                    Directory.CreateDirectory(path);

                    using (var writer = new BinaryWriter(File.Create(Path.Combine(path, "0-Score.txt"))))
                        new TrackUnpacker(path).Unpack(source, writer);
                    break;

                default:
                    _fieldValues.Clear();
                    break;
            }

            if (value == null &&  _fieldValues.TryGetValue(field, out var fieldValue))
                value = fieldValue.ToString();

            destination.Write(Encoding.UTF8.GetBytes(string.Format("{0} = {1}\r\n", field, value)));
        }

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            return source.BaseStream.Position == source.BaseStream.Length;
        }
    }
}
