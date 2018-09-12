using System.Collections.Generic;
using System.IO;

namespace MediaEngine.Unpackers
{
    abstract class Unpacker
    {
        protected readonly Dictionary<byte, int> _fieldValues = new Dictionary<byte, int>();

        protected abstract void Unpack(BinaryReader source, BinaryWriter destination, byte fieldId);

        public void Unpack(BinaryReader source, BinaryWriter destination)
        {
            _fieldValues.Clear();
            source.ReadByte(); // ignored

            while (true)
            {
                var fieldId = source.ReadByte();
                if (fieldId == 255)
                {
                    _fieldValues.Add(fieldId, source.ReadByte());
                    return;
                }

                Unpack(source, destination, fieldId);
            }
        }
    }
}
