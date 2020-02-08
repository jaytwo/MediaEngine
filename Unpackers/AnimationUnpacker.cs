using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    static class AnimationUnpacker
    {
        public static string Unpack(BinaryReader source, ResourceType resourceType, int frameCount, out string description)
        {
            var field = (TrackField)47;
            var counts = new List<int>();
            var destination = new StringBuilder();
            var firstObjRef = new int?();
            var frame = 0;

            var sections = new List<TrackField>
            {
                TrackField.AnimatePosition,
                TrackField.AnimateRotation,
                TrackField.AnimateScale,
                TrackField.UnknownArray34
            };

            while (source.BaseStream.Position < source.BaseStream.Length - 1)
            {
                var frameRepeats = (int)source.ReadByte();
                if (frameRepeats == 255)
                    break;

                if (frameRepeats == (int)sections[0])
                {
                    var nextByte = source.ReadByte();
                    source.BaseStream.Position--;

                    if (firstObjRef == null || firstObjRef == nextByte || frame + frameRepeats > frameCount)
                    {
                        field = (TrackField)frameRepeats;
                        destination.AppendLine(field.ToString());
                        counts.Add(0);
                        sections.RemoveAt(0);
                        frame = 0;

                        if (field != TrackField.UnknownArray34)
                            frameRepeats = source.ReadByte();
                    }
                }

                if (field == TrackField.UnknownArray34)
                {
                    var bytes34 = new List<byte>();
                    var b = source.ReadByte();
                    while (b != 35)
                    {
                        bytes34.Add(b);
                        counts[counts.Count - 1]++;
                        b = source.ReadByte();
                    }
                    source.BaseStream.Position--;
                    destination.AppendLine("    " + string.Join(", ", bytes34));
                    break;
                }

                if (frameRepeats == 128)
                    frameRepeats = source.ReadInt32();

                if (firstObjRef == null)
                    firstObjRef = frameRepeats;

                var content = Enumerable.Range(0, source.ReadByte() * 3)
                    .Select(i => source.ReadSingle())
                    .ToArray();

                destination.AppendLine($"    Frames {frame}-{frame + frameRepeats}: " + string.Join(", ", content));
                counts[counts.Count - 1]++;
                frame += frameRepeats + 1;
            }

            description = string.Join(" + ", counts);
            return destination.ToString();
        }
    }
}