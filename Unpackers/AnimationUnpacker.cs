using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum AnimationField
    {
        FrameBits = 32,
        PositionBits = 33,
        RotationBits = 34,
        ScaleBits = 35,
        Positions = 48,
        Rotations = 49,
        Scales = 50,
        End = 255
    }

    static class AnimationUnpacker
    {
        public static string Unpack(BinaryReader source, int frameCount, out string description)
        {
            var field = AnimationField.FrameBits;
            var counts = new List<int>();
            var destination = new StringBuilder();
            var frame = 0;

            ReadBitArray(source, destination, field, counts, frameCount);

            var sections = new List<AnimationField>
            {
                AnimationField.Positions,
                AnimationField.Rotations,
                AnimationField.Scales,
                AnimationField.PositionBits,
                AnimationField.RotationBits,
                AnimationField.ScaleBits
            };

            while (source.BaseStream.Position < source.BaseStream.Length - 1)
            {
                var frameRepeats = (int)source.ReadByte();
                if (frameRepeats == 255)
                    break;

                if (frameRepeats == (int)sections[0] || (sections[0] == AnimationField.PositionBits && frameRepeats == 34))
                {
                    var nextByte = source.ReadByte();
                    source.BaseStream.Position--;

                    if (field == AnimationField.FrameBits || frame + frameRepeats > frameCount)
                    {
                        field = (AnimationField)frameRepeats;

                        while (frameRepeats != (int)sections[0])
                            sections.RemoveAt(0);

                        sections.RemoveAt(0);
                        counts.Add(0);
                        frame = 0;

                        if (field >= AnimationField.Positions)
                        {
                            destination.AppendLine(field.ToString());
                            frameRepeats = source.ReadByte();
                        }
                    }
                }

                if (field == AnimationField.PositionBits)
                {
                    counts.RemoveAt(counts.Count - 1);

                    ReadBitArray(source, destination, field, counts, counts[1]);
                    field = (AnimationField)source.ReadByte();
                    if (field != AnimationField.RotationBits)
                        throw new InvalidDataException();
                }

                if (field == AnimationField.RotationBits)
                {
                    ReadBitArray(source, destination, field, counts, counts[2]);
                    field = (AnimationField)source.ReadByte();
                    if (field == AnimationField.End)
                        break;
                    if (field != AnimationField.ScaleBits)
                        throw new InvalidDataException();

                    ReadBitArray(source, destination, field, counts, counts[3]);
                    field = (AnimationField)source.ReadByte();
                    if (field != AnimationField.End)
                        throw new InvalidDataException();

                    break;
                }

                if (frameRepeats == 128)
                    frameRepeats = source.ReadInt32();

                var length = source.ReadByte();
                var content = Enumerable.Range(0, length * 3)
                    .Select(i => source.ReadSingle())
                    .ToArray();

                destination.AppendLine($"    Frames {frame}-{frame + frameRepeats}: " + string.Join(", ", content));
                counts[counts.Count - 1] += length;
                frame += frameRepeats + length;
            }

            description = string.Join(" + ", counts);
            return destination.ToString();
        }

        private static void ReadBitArray(BinaryReader source, StringBuilder destination, AnimationField field, List<int> counts, int count)
        {
            var bits = new BitArray(source.ReadBytes((int)Math.Ceiling(count / 8.0)));
            bits.Length = count;
            counts.Add(count);

            destination.AppendLine(field.ToString());
            destination.AppendLine($"    Bits[{bits.Length}]: " + string.Join(string.Empty, bits.Cast<bool>().Select(b => b ? "1" : "0")));
        }
    }
}