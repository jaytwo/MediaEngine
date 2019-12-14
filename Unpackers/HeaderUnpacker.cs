using System;
using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum HeaderField { }

    class HeaderUnpacker : Unpacker<HeaderField>
    {
        public override void Unpack(BinaryReader source, BinaryWriter destination)
        {
            _fieldValues.Clear();

            while (true)
            {
                var fieldByte = source.ReadByte();
                var field = (HeaderField)Enum.ToObject(typeof(HeaderField), fieldByte);

                if (fieldByte == 16)
                    return;
                
                Unpack(source, destination, field);
            }
        }

        protected override void Unpack(BinaryReader source, BinaryWriter destination, HeaderField field)
        {
            _fieldValues[field] = source.ReadInt32();
            destination.Write(Encoding.ASCII.GetBytes($"{field}: {_fieldValues[field]}\n"));
        }
    }
}
