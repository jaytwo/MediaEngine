using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum EarField
    {
        Distance = 16,
        Rolloff = 17,
        Doppler = 18
    }

    class EarUnpacker : Unpacker<EarField>
    {
        protected override void Unpack(BinaryReader source, BinaryWriter destination, EarField field)
        {
            destination.Write(Encoding.ASCII.GetBytes(string.Format("{0} = {1}\r\n",
                field,
                source.ReadSingle())));
        }
    }
}
