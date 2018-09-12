using System.IO;
using System.Text;

namespace MediaEngine.Unpackers
{
    class WaveUnpacker : Unpacker
    {
        private const int WAVE_FORMAT_PCM = 0x0001;
        private const int WAVE_FORMAT_DVI_ADPCM = 0x0011;

        protected override void Unpack(BinaryReader source, BinaryWriter destination, byte fieldId)
        {
            switch (fieldId)
            {
                case 16:
                    var unknown1 = source.ReadBytes(6);
                    var format = source.ReadUInt16();
                    var channels = source.ReadUInt16();
                    var samplesPerSec = source.ReadUInt32();
                    var avgBytesPerSec = source.ReadUInt32();
                    var blockAlign = source.ReadUInt16();
                    var bitsPerSample = source.ReadUInt16();
                    var extraSize = source.ReadUInt16();

                    var unknown2 = source.ReadBytes(6);
                    var fmtLength = format == WAVE_FORMAT_PCM ? 16 : (18 + extraSize);
                    var dataLength = (int)source.ReadUInt32() - extraSize;

                    destination.Write(Encoding.ASCII.GetBytes("RIFF"));
                    destination.Write(dataLength + 20 + fmtLength);
                    destination.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                    destination.Write(fmtLength);
                    destination.Write(format);
                    destination.Write(channels);
                    destination.Write(samplesPerSec);
                    destination.Write(avgBytesPerSec);
                    destination.Write(blockAlign);
                    destination.Write(bitsPerSample);

                    if (format == WAVE_FORMAT_DVI_ADPCM)
                    {
                        // See https://icculus.org/SDL_sound/downloads/external_documentation/wavecomp.htm
                        var samplesPerBlock = source.ReadUInt16();
                        destination.Write(extraSize);
                        destination.Write(samplesPerBlock);
                        destination.Write(Encoding.ASCII.GetBytes("fact"));
                        destination.Write(4);
                        destination.Write(samplesPerBlock * dataLength / blockAlign);
                    }

                    destination.Write(Encoding.ASCII.GetBytes("data"));
                    destination.Write(dataLength);
                    destination.Write(source.ReadBytes(dataLength));
                    break;

                case 131:
                    _fieldValues.Add(fieldId, source.ReadByte());
                    break;

                default:
                    _fieldValues.Add(fieldId, source.ReadInt32());
                    break;
            }
        }
    }
}
