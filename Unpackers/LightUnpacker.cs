using System;
using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum LightField
    {
        LightType = 16,
        Colour = 17,
        Channels = 21,
        CutOffAngle = 22,
        Distance = 23,
        Tag = 24,
        CutOffAnglePhi = 25,
        ChannelList = 26,
        DropOffRate = 27,
    }

    enum LightType
    {
        Parallel = 0,
        Point = 1,
        Spot = 2,
        Ambient = 3,
    }

    class LightUnpacker : Unpacker<LightField>
    {
        protected override void Unpack(BinaryReader source, BinaryWriter destination, LightField field)
        {
            object value;

            switch (field)
            {
                case LightField.LightType:
                    value = (LightType)source.ReadByte();
                    break;

                case LightField.CutOffAngle:
                case LightField.CutOffAnglePhi:
                    value = Math.Round(source.ReadSingle() * 180.0 / Math.PI);
                    break;

                case LightField.Colour:
                    value = string.Join(", ", source.ReadByte(), source.ReadByte(), source.ReadByte());
                    break;

                default:
                    value = source.ReadSingle();
                    break;
            }

            destination.Write(Encoding.ASCII.GetBytes(string.Format("{0} = {1}\r\n", field, value)));
        }
    }
}
