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
            var emptyObjRef = 0;
            var frame = 0;

            switch (resourceType)
            {
                case ResourceType.Bitmap:
                    emptyObjRef = 1;
                    break;

                case ResourceType.Movie:
                    emptyObjRef = -1;
                    break;
            }

            while (source.BaseStream.Position < source.BaseStream.Length - 1)
            {
                var frameRepeats = (int)source.ReadByte();
                if (frameRepeats == 255)
                    break;

                if (frameRepeats == (int)field + 1)
                {
                    var nextByte = source.ReadByte();
                    source.BaseStream.Position--;

                    if (firstObjRef == null || firstObjRef == nextByte || firstObjRef == emptyObjRef)
                    {
                        field = (TrackField)frameRepeats;
                        destination.AppendLine(field.ToString());
                        counts.Add(0);
                        frame = 0;

                        if (field == TrackField.AnimateScale && firstObjRef == emptyObjRef)
                            break;

                        frameRepeats = source.ReadByte();
                    }
                }

                if (frameRepeats == 128)
                    frameRepeats = source.ReadInt32();

                if (firstObjRef == null)
                    firstObjRef = frameRepeats;

                var length = firstObjRef == emptyObjRef ? 0 : source.ReadByte();
                var content = Enumerable.Range(0, length * 3)
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