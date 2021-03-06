﻿using System;
using System.IO;

namespace MediaEngine.Unpackers
{
    enum MidiField
    {
        MidiData = 32
    }

    class MidiUnpacker : Unpacker<MidiField>
    {
        protected override void Unpack(BinaryReader source, BinaryWriter destination, MidiField field)
        {
            switch (field)
            {
                default:
                    _fieldValues[field] = source.ReadInt32();
                    break;

                case MidiField.MidiData:
                    ushort chunks = 2;
                    for (int i = 0; i <= chunks; i++)
                    {
                        var header = source.ReadBytes(8);
                        destination.Write(header);

                        Array.Reverse(header);
                        var dataLength = (int)BitConverter.ToUInt32(header, 0);
                        var data = source.ReadBytes(dataLength);
                        destination.Write(data);

                        if (i == 0)
                        {
                            Array.Reverse(data);
                            chunks = BitConverter.ToUInt16(data, 2);
                        }
                    }
                    break;
            }
        }
    }
}
