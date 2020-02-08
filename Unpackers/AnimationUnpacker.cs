using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    static class AnimationUnpacker
    {
        public static string Unpack(BinaryReader source, int frameCount, out string description)
        {
            var frameBits = new BitArray(source.ReadBytes((int)Math.Ceiling(frameCount / 8.0)));
            frameBits.Length = frameCount;

            var destination = new StringBuilder();
            destination.AppendLine(TrackField.FrameBits.ToString());
            destination.AppendLine($"    Bits[{frameBits.Length}]: " + string.Join(string.Empty, frameBits.Cast<bool>().Select(b => b ? "1" : "0")));

            var counts = new List<int>{ frameBits.Length };
            var firstObjRef = new int?();
            var field = (TrackField)47;
            var frame = 0;

            var sections = new List<TrackField>
            {
                TrackField.AnimatePosition,
                TrackField.AnimateRotation,
                TrackField.AnimateScale,
                TrackField.UnknownArray33,
                TrackField.UnknownArray34,
                TrackField.UnknownArray35
            };

            while (source.BaseStream.Position < source.BaseStream.Length - 1)
            {
                var frameRepeats = (int)source.ReadByte();
                if (frameRepeats == 255)
                    break;

                if (frameRepeats == (int)sections[0] || (sections[0] == TrackField.UnknownArray33 && frameRepeats == 34))
                {
                    var nextByte = source.ReadByte();
                    source.BaseStream.Position--;

                    if (firstObjRef == null || (frameRepeats <= 34 && firstObjRef == nextByte) || frame + frameRepeats > frameCount)
                    {
                        field = (TrackField)frameRepeats;
                        destination.AppendLine(field.ToString());

                        while (frameRepeats != (int)sections[0])
                            sections.RemoveAt(0);

                        counts.Add(0);
                        sections.RemoveAt(0);
                        frame = 0;

                        if (field >= TrackField.AnimatePosition)
                            frameRepeats = source.ReadByte();
                    }
                }

                if (field == TrackField.UnknownArray33)
                {
                    ReadUnknownArray(source, destination, counts, 34);
                    field = TrackField.UnknownArray34;
                    destination.AppendLine(field.ToString());
                }

                if (field == TrackField.UnknownArray34)
                {
                    ReadUnknownArray(source, destination, counts, 35);
                    field = TrackField.UnknownArray35;
                    destination.AppendLine(field.ToString());
                }

                if (field == TrackField.UnknownArray35)
                {
                    ReadUnknownArray(source, destination, counts, 255);
                    source.BaseStream.Position--;
                    break;
                }

                if (frameRepeats == 128)
                    frameRepeats = source.ReadInt32();

                if (firstObjRef == null)
                    firstObjRef = frameRepeats;

                var length = source.ReadByte();
                var content = Enumerable.Range(0, length * 3)
                    .Select(i => source.ReadSingle())
                    .ToArray();

                destination.AppendLine($"    Frames {frame}-{frame + frameRepeats}: " + string.Join(", ", content));
                counts[counts.Count - 1]++;
                frame += frameRepeats + length;
            }

            description = string.Join(" + ", counts);
            return destination.ToString();
        }

        private static void ReadUnknownArray(BinaryReader source, StringBuilder destination, List<int> counts, byte end)
        {
            var bytes = new List<byte>();
            var b = source.ReadByte();

            while (b != end)
            {
                bytes.Add(b);
                b = source.ReadByte();
            }

            destination.AppendLine("    " + string.Join(", ", bytes));
            counts[counts.Count - 1] = bytes.Count;
        }
    }
}