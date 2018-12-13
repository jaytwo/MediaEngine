using System;
using System.Collections.Generic;
using System.IO;

namespace MediaEngine.Unpackers
{
    abstract class Unpacker<T> where T : Enum
    {
        protected readonly Dictionary<T, int> _fieldValues = new Dictionary<T, int>();

        protected abstract void Unpack(BinaryReader source, BinaryWriter destination, T field);

        protected virtual bool OnFinish(BinaryWriter destination) { return true; }

        public void Unpack(BinaryReader source, BinaryWriter destination)
        {
            _fieldValues.Clear();
            source.ReadByte(); // ignored

            while (true)
            {
                var fieldByte = source.ReadByte();
                var field = (T)Enum.ToObject(typeof(T), fieldByte);

                if (fieldByte == 255)
                {
                    if (OnFinish(destination))
                    {
                        _fieldValues[field] = source.ReadByte();
                        return;
                    }
                }
                else Unpack(source, destination, field);
            }
        }
    }
}
