using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum ModelField
    {
        VertexCount = 16,
        FaceCount = 17,
        IndexCount = 18,
        UnknownMarker19 = 19,
        UnknownArray22 = 22,
        Vertices = 32,
        FacesDouble = 33,
        UnknownMarker34 = 34,
        FacesSingle = 35,
        MaterialList = 36,
        UnknownArray113 = 113,
        UnknownArray115 = 115,
        MaterialPower = 144,
        MaterialAmbient = 145,
        MaterialEmissive = 146,
        MaterialSpecular = 147,
        UnknownShort149 = 149,
        UnknownShort150 = 150,
        UnknownShort151 = 151,
        MaterialName = 177,
    }

    /// <summary>
    /// See http://www.martinreddy.net/gfx/3d/3DS.spec
    /// </summary>
    class ModelUnpacker : Unpacker<ModelField>
    {
        private byte[] _vertices;
        private byte[] _faces;
        private byte[] _faceMaterials;
        private List<short>[] _faceTriangles;

        private readonly List<byte[]> _materials = new List<byte[]>();
        private readonly List<byte[]> _materialFaces = new List<byte[]>();
        private readonly Dictionary<ModelField, float[]> _material = new Dictionary<ModelField, float[]>();

        protected override void Unpack(BinaryReader source, BinaryWriter destination, ModelField field)
        {
            switch (field)
            {
                case ModelField.UnknownMarker19:
                case ModelField.UnknownMarker34:
                    break;

                case ModelField.UnknownArray22:
                    var unknown22 = source.ReadBytes(_fieldValues[ModelField.VertexCount]);
                    break;

                case ModelField.Vertices:
                    _vertices = source.ReadBytes(12 * _fieldValues[ModelField.VertexCount]);
                    break;

                case ModelField.FacesSingle:
                    _fieldValues[field] = source.ReadByte();
                    var indices = source.ReadBytes(_fieldValues[ModelField.IndexCount]);
                    UnpackFaces(indices.Select(b => (short)b).ToArray());
                    break;

                case ModelField.FacesDouble:
                    UnpackFaces(Enumerable.Range(0, _fieldValues[ModelField.IndexCount])
                        .Select(i => source.ReadInt16()).ToArray());

                    if (_fieldValues[ModelField.IndexCount] != 0)
                        switch (source.ReadByte())
                        {
                            case 34:
                                var unknown34 = Enumerable.Range(0, _fieldValues[ModelField.FaceCount])
                                    .Select(i => source.ReadInt16())
                                    .ToArray();
                                break;

                            default:
                                source.BaseStream.Position--;
                                break;
                        }

                    break;

                case ModelField.MaterialList:
                    var bitsPerItem = source.ReadByte();
                    var byteCount = _fieldValues[ModelField.FaceCount] * bitsPerItem / 32.0;
                    _faceMaterials = source.ReadBytes((int)Math.Ceiling(byteCount));
                    if (bitsPerItem == 16)
                        _faceMaterials = _faceMaterials.SelectMany(b => new[]
                        {
                            (byte)((b & 0xF0) >> 4),
                            (byte)(b & 0x0F)
                        })
                        .ToArray();

                    if (byteCount != Math.Ceiling(byteCount))
                        _faceMaterials = _faceMaterials.Take(_faceMaterials.Length - 1).ToArray();
                    break;

                case ModelField.UnknownArray113:
                    var unknown12 = source.ReadBytes(12);
                    break;

                case ModelField.UnknownArray115:
                    var unknown24 = Enumerable.Range(0, 6)
                        .Select(i => source.ReadUInt32())
                        .Where(i => i != uint.MaxValue)
                        .ToArray();
                    break;

                case ModelField.MaterialPower:
                case ModelField.MaterialAmbient:
                case ModelField.MaterialEmissive:
                case ModelField.MaterialSpecular:
                    _material[field] = Enumerable.Range(0, 4)
                        .Select(i => source.ReadSingle())
                        .ToArray();
                    _material[field][3] = 1;
                    break;

                case ModelField.UnknownShort149:
                case ModelField.UnknownShort150:
                case ModelField.UnknownShort151:
                    _fieldValues[field] = source.ReadInt16();
                    break;

                case ModelField.MaterialName:
                    var name = Encoding.UTF8.GetBytes(Encoding.GetEncoding(932).GetString(source.ReadBytes(source.ReadInt32())));

                    // For material format see http://www.martinreddy.net/gfx/3d/MLI.spec
                    using (var materialStream = new MemoryStream())
                    using (var materialWriter = new BinaryWriter(materialStream, Encoding.ASCII, true))
                    {
                        materialWriter.Write((ushort)0xA000); // MAT_NAME chunk
                        materialWriter.Write(31 + name.Length); // chunk length
                        materialWriter.Write(name); // material name
                        materialWriter.Write((byte)0); // name terminator

                        materialWriter.Write((ushort)0xA010); // Ambient colour chunk
                        materialWriter.Write(24); // chunk length

                        materialWriter.Write((ushort)0x0010); // RGB chunk
                        materialWriter.Write(18); // chunk length
                        materialWriter.Write(_material[ModelField.MaterialAmbient][0]);
                        materialWriter.Write(_material[ModelField.MaterialAmbient][1]);
                        materialWriter.Write(_material[ModelField.MaterialAmbient][2]);
                        
                        _materials.Add(materialStream.ToArray());
                    }

                    if (_faceMaterials != null)
                    {
                        using (var materialStream = new MemoryStream())
                        using (var materialWriter = new BinaryWriter(materialStream, Encoding.ASCII, true))
                        {
                            var triangleIndices = new List<short>();
                            for (short faceIndex = 0; faceIndex < _faceMaterials.Length; faceIndex++)
                                if (_faceMaterials[faceIndex] == _materials.Count - 1)
                                    triangleIndices.AddRange(_faceTriangles[faceIndex]);

                            materialWriter.Write((ushort)0x4130); // TRI_MATERIAL chunk
                            materialWriter.Write(triangleIndices.Count * 2 + name.Length + 9); // chunk length
                            materialWriter.Write(name); // material name
                            materialWriter.Write((byte)0); // name terminator
                            materialWriter.Write((short)triangleIndices.Count);

                            foreach (var triangleIndex in triangleIndices)
                                materialWriter.Write(triangleIndex);
                            
                            _materialFaces.Add(materialStream.ToArray());
                        }
                    }
                    break;

                default:
                    _fieldValues[field] = source.ReadInt32();
                    break;
            }
        }

        private void UnpackFaces(short[] indices)
        {
            var faces = new List<short[]>();
            var i = 0;
            while (i < indices.Length)
                faces.Add(Enumerable.Range(0, indices[i] + 1).Select(j => indices[i++]).ToArray());

            i = 0;
            short t = 0;
            _faceTriangles = new List<short>[faces.Count];

            using (var faceStream = new MemoryStream())
            {
                using (var faceWriter = new BinaryWriter(faceStream, Encoding.ASCII, true))
                    foreach (var face in faces)
                    {
                        _faceTriangles[i] = new List<short>(new[] { t++ });   
                        faceWriter.Write(face[2]);
                        faceWriter.Write(face[1]);
                        faceWriter.Write(face[3]);
                        faceWriter.Write((short)0x0007); // face info

                        // Convert quads
                        if (face[0] == 4)
                        {
                            _faceTriangles[i].Add(t++);
                            faceWriter.Write(face[3]);
                            faceWriter.Write(face[1]);
                            faceWriter.Write(face[4]);
                            faceWriter.Write((short)0x0007); // face info
                        }

                        i++;
                    }

                _faces = faceStream.ToArray();
            }
        }

        protected override bool OnFinish(BinaryWriter destination)
        {
            var materialsLength = _materials.Sum(m => m.Length) + 6;
            var facesLength = _materialFaces.Sum(m => m.Length) + _faces.Length + 8;
            var meshLength = facesLength + _vertices.Length + 14;

            destination.Write((ushort)0x4D4D); // MAIN3DS chunk
            destination.Write(meshLength + 20 + materialsLength); // chunk length

            destination.Write((ushort)0x3D3D); // EDIT3DS chunk
            destination.Write(meshLength + 14 + materialsLength); // chunk length

            destination.Write((ushort)0xAFFF); // EDIT_MATERIAL chunk
            destination.Write(materialsLength); // chunk length

            foreach (var material in _materials)
                destination.Write(material);

            destination.Write((ushort)0x4000); // EDIT_OBJECT chunk
            destination.Write(meshLength + 8); // chunk length
            destination.Write(Encoding.ASCII.GetBytes("1")); // object name
            destination.Write((byte)0); // name terminator

            destination.Write((ushort)0x4100); // OBJECT_TRIMESH chunk
            destination.Write(meshLength); // chunk length

            destination.Write((ushort)0x4110); // TRI_VERTEXL chunk
            destination.Write(_vertices.Length + 8); // chunk length
            destination.Write((ushort)(_vertices.Length / 12)); // total vertices
            destination.Write(_vertices);

            destination.Write((ushort)0x4120); // TRI_FACEL1 chunk
            destination.Write(facesLength); // chunk length
            destination.Write((ushort)(_faces.Length / 8)); // total polygons
            destination.Write(_faces);

            foreach (var materialFaces in _materialFaces)
                destination.Write(materialFaces);

            return true;
        }
    }
}
