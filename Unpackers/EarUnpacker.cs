using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    class EarUnpacker : Unpacker
    {
        private enum EarField
        {
            Distance = 16,
            Rolloff = 17,
            Doppler = 18
        }

        protected override void Unpack(BinaryReader source, BinaryWriter destination, byte fieldId)
        {
            destination.Write(Encoding.ASCII.GetBytes(string.Format("{0} = {1}\r\n",
                (EarField)fieldId,
                source.ReadSingle())));
        }
    }
}
