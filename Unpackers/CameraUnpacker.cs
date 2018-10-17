using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum CameraField
    {
        Height = 16,
        ZoomFactor = 17,
        ClipFore = 18,
        ClipBack = 19,
        FogEnabled = 32,
        FogColour = 33,
        FogFore = 34,
        FogBack = 35,
        Tag = 36,
        Unknown37 = 37,
        Angle = 38
    }

    class CameraUnpacker : Unpacker<CameraField>
    {
        protected override void Unpack(BinaryReader source, BinaryWriter destination, CameraField field)
        {
            object value;
            
            switch (field)
            {
                case CameraField.FogColour:
                    value = string.Join(", ", source.ReadByte(), source.ReadByte(), source.ReadByte());
                    source.ReadByte();
                    break;

                case CameraField.FogEnabled:
                case CameraField.Unknown37:
                    value = source.ReadInt32();
                    break;

                default:
                    value = source.ReadSingle();
                    break;
            }

            destination.Write(Encoding.ASCII.GetBytes(string.Format("{0} = {1}\r\n", field, value)));
        }
    }
}
