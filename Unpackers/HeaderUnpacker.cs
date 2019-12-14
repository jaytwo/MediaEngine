using System;
using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum HeaderField { }

    class HeaderUnpacker : Unpacker<HeaderField>
    {
        protected override bool SkipFirstByte => false;
        protected override byte EndByte => 16;

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            source.BaseStream.Position--;
            return true;
        }

        protected override void Unpack(BinaryReader source, BinaryWriter destination, HeaderField field)
        {
            _fieldValues[field] = source.ReadInt32();
            destination.Write(Encoding.ASCII.GetBytes($"{field}: {_fieldValues[field]}\n"));
        }
    }
}
