using System;
using System.Collections.Generic;
using System.IO;

namespace MediaEngine.Unpackers
{
    abstract class Unpacker<T> where T : Enum
    {
        protected readonly Dictionary<T, int> _fieldValues = new Dictionary<T, int>();

        protected abstract void Unpack(BinaryReader source, BinaryWriter destination, T field);

        protected virtual bool OnFinish(BinaryReader source, BinaryWriter destination) { return true; }

        protected virtual bool SkipFirstByte => true;

        protected virtual byte EndByte => 255;

        public void Unpack(BinaryReader source, BinaryWriter destination)
        {
            _fieldValues.Clear();

            if (SkipFirstByte)
                source.ReadByte();

            while (true)
            {
                var eof = source.BaseStream.Position == source.BaseStream.Length;
                var fieldByte = eof ? EndByte : source.ReadByte();
                var field = (T)Enum.ToObject(typeof(T), fieldByte);

                if (fieldByte == EndByte && OnFinish(source, destination))
                {
                    if (!eof)
                        _fieldValues[field] = source.ReadByte();
                    return;
                }
                else Unpack(source, destination, field);
            }
        }
    }
}
