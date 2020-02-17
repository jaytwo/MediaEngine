using lib3ds.Net;
using MediaEngine.Exporters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaEngine.Unpackers
{
    enum ModelField
    {
        VertexCount = 16,
        FaceCount = 17,
        IndexCount = 18,
        UnknownMarker19 = 19,
        UnknownInt21 = 21,
        UnknownArray22 = 22,
        UnknownInt23 = 23,
        Vertices = 32,
        FacesDouble = 33,
        UnknownMarker34 = 34,
        FacesSingle = 35,
        FaceGroups = 36,
        UnknownInt112 = 112,
        UnknownArray113 = 113,
        UnknownInt114 = 114,
        UnknownArray115 = 115,
        Texture = 128,
        UnknownInt129 = 129,
        UnknownInt130 = 130,
        TextureGroup = 131,
        MaterialPower = 144,
        MaterialAmbient = 145,
        MaterialEmissive = 146,
        MaterialSpecular = 147,
        UnknownFloat148 = 148,
        UnknownShort149 = 149,
        UnknownShort150 = 150,
        UnknownShort151 = 151,
        UnknownInt152 = 152,
        UnknownInt154 = 154,
        TextureDivisionU = 160,
        TextureDivisionV = 161,
        TexturePositionU = 162,
        TexturePositionV = 163,
        TextureRotateX = 164,
        TextureRotateY = 165,
        TextureRotateZ = 166,
        UnknownInt176 = 176,
        GroupName = 177,
        UnknownInt178 = 178,
        UnknownInt192 = 192,
        UnknownInt193 = 193,
        UnknownInt194 = 194,
        UnknownInt195 = 195,
        UnknownFloat196 = 196,
        UnknownFloat197 = 197,
        UnknownInt198 = 198,
        UnknownInt199 = 199,
        UnknownInt200 = 200,
        UnknownInt201 = 201,
        UnknownInt202 = 202,
    }

    class Group : Dictionary<ModelField, object>
    {
        public List<Group> TextureGroup { get; set; }
    }

    class ModelUnpacker : Unpacker<ModelField>
    {
        public Lib3dsFile Model { get; private set; }

        private short[] _faceGroups;

        private readonly List<Lib3dsVertex> _vertices = new List<Lib3dsVertex>();
        private readonly List<ushort[]> _faces = new List<ushort[]>();
        private readonly List<Group> _groups = new List<Group>();

        private Group _group;

        protected override void Unpack(BinaryReader source, BinaryWriter destination, ModelField field)
        {
            switch (field)
            {
                case ModelField.UnknownMarker19:
                case ModelField.UnknownMarker34:
                    break;

                case ModelField.UnknownShort150:
                    _group.Add(field, source.ReadInt16());
                    break;

                case ModelField.UnknownArray22:
                    var unknown22 = source.ReadBytes((int)_group[ModelField.VertexCount]);
                    _group.Add(field, unknown22.Count());
                    break;

                case ModelField.Vertices:
                    _vertices.AddRange(Enumerable.Range(0, (int)_group[ModelField.VertexCount])
                        .Select(i => new Lib3dsVertex(source.ReadSingle(), source.ReadSingle(), source.ReadSingle()))
                        .ToList());
                    break;

                case ModelField.FacesSingle:
                    _group[field] = source.ReadByte();
                    var indices = source.ReadBytes((int)_group[ModelField.IndexCount]);
                    UnpackFaces(indices.Select(b => (ushort)b).ToArray());
                    break;

                case ModelField.FacesDouble:
                    UnpackFaces(Enumerable.Range(0, (int)_group[ModelField.IndexCount])
                        .Select(i => source.ReadUInt16()).ToArray());

                    if ((int)_group[ModelField.IndexCount] != 0)
                    {
                        if (source.ReadByte() == 34)
                            _faceGroups = Enumerable.Range(0, (int)_group[ModelField.FaceCount])
                                .Select(i => source.ReadInt16())
                                .ToArray();
                        else
                            source.BaseStream.Position--;
                    }
                    break;

                case ModelField.FaceGroups:
                    var bitsPerItem = source.ReadByte();
                    var byteCount = (int)_group[ModelField.FaceCount] * bitsPerItem / 32.0;
                    var faceGroups = source.ReadBytes((int)Math.Ceiling(byteCount));
                    if (bitsPerItem == 16)
                        faceGroups = faceGroups.SelectMany(b => new[]
                        {
                            (byte)((b & 0xF0) >> 4),
                            (byte)(b & 0x0F)
                        })
                        .ToArray();

                    if (byteCount != Math.Ceiling(byteCount))
                        faceGroups = faceGroups.Take(faceGroups.Length - 1).ToArray();
                    _faceGroups = faceGroups.Select(b => (short)b).ToArray();
                    break;

                case ModelField.UnknownArray113:
                    _group.Add(field, source.ReadBytes(12));
                    break;

                case ModelField.UnknownArray115:
                    _group.Add(field, Enumerable.Range(0, 6)
                        .Select(i => source.ReadUInt32())
                        .Where(i => i != uint.MaxValue)
                        .ToArray());
                    break;

                case ModelField.MaterialPower:
                case ModelField.MaterialAmbient:
                case ModelField.MaterialEmissive:
                case ModelField.MaterialSpecular:
                    _group.Add(field, Enumerable.Range(0, 4)
                        .Select(i => source.ReadSingle())
                        .ToArray());
                    break;

                case ModelField.UnknownShort149:
                case ModelField.UnknownShort151:
                    _group.Add(field, source.ReadInt16());
                    break;

                case ModelField.FaceCount:
                case ModelField.IndexCount:
                case ModelField.VertexCount:
                case ModelField.UnknownInt21:
                case ModelField.UnknownInt23:
                case ModelField.UnknownInt112:
                case ModelField.UnknownInt114:
                case ModelField.Texture:
                case ModelField.UnknownInt129:
                case ModelField.UnknownInt130:
                case ModelField.TextureGroup:
                case ModelField.UnknownInt152:
                case ModelField.UnknownInt154:
                case ModelField.UnknownInt176:
                case ModelField.UnknownInt178:
                case ModelField.UnknownInt192:
                case ModelField.UnknownInt193:
                case ModelField.UnknownInt194:
                case ModelField.UnknownInt195:
                case ModelField.UnknownInt198:
                case ModelField.UnknownInt199:
                case ModelField.UnknownInt200:
                case ModelField.UnknownInt201:
                case ModelField.UnknownInt202:
                    if (_group == null || _group.ContainsKey(field))
                        _groups.Add(_group = new Group());
                    _group.Add(field, source.ReadInt32());
                    break;

                case ModelField.UnknownFloat148:
                case ModelField.TextureDivisionU:
                case ModelField.TextureDivisionV:
                case ModelField.TexturePositionU:
                case ModelField.TexturePositionV:
                case ModelField.TextureRotateX:
                case ModelField.TextureRotateY:
                case ModelField.TextureRotateZ:
                case ModelField.UnknownFloat196:
                case ModelField.UnknownFloat197:
                    _group.Add(field, source.ReadSingle());
                    break;

                case ModelField.GroupName:
                    _group.Add(field, _groups.Count + " " + Translator.ReadString(source));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void UnpackFaces(ushort[] indices)
        {
            var i = 0;
            while (i < indices.Length)
                _faces.Add(Enumerable.Range(0, indices[i] + 1).Select(j => indices[i++]).ToArray());
        }

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            Model = ModelExporter.Export(_groups, _vertices, _faces, _faceGroups);

            if (!LIB3DS.lib3ds_file_save(Model, destination.BaseStream))
                throw new Exception("Saving 3ds file failed");
            
            return true;
        }
    }
}
