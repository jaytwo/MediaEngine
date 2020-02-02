using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    static class AnimationUnpacker
    {
        public static string Unpack(BinaryReader source, ResourceType resourceType, out string description)
        {
            var field = (TrackField)47;
            var counts = new List<int>();
            var destination = new StringBuilder();
            var firstObjRef = new int?();
            var emptyObjRef = 0;

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
                var objRef = (int)source.ReadByte();
                if (objRef == 255)
                    break;

                if (objRef == (int)field + 1)
                {
                    var nextByte = source.ReadByte();
                    source.BaseStream.Position--;

                    if (firstObjRef == null || firstObjRef == nextByte || firstObjRef == emptyObjRef)
                    {
                        field = (TrackField)objRef;
                        destination.AppendLine(field.ToString());
                        counts.Add(0);

                        if (field == TrackField.AnimateScale && firstObjRef == emptyObjRef)
                            break;

                        objRef = source.ReadByte();
                    }
                }

                if (objRef == 128)
                    objRef = source.ReadInt32();

                if (firstObjRef == null)
                    firstObjRef = objRef;

                var length = firstObjRef == emptyObjRef ? 0 : source.ReadByte();
                var content = Enumerable.Range(0, length * 3)
                    .Select(i => source.ReadSingle())
                    .ToArray();

                destination.AppendLine($"    Object {objRef}: " + string.Join(", ", content));
                counts[counts.Count - 1]++;
            }

            description = string.Join(" + ", counts);
            return destination.ToString();
        }
    }
}