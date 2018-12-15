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
        private class Material : Dictionary<ModelField, float[]> { }

        private byte[] _vertices;
        private byte[] _faces;
        private byte[] _faceMaterials;
        private List<short>[] _faceTriangles;

        private readonly List<string> _names = new List<string>();
        private readonly List<Material> _materials = new List<Material>();

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
                    if (_materials.Count == 0 || _materials.Last().ContainsKey(field))
                        _materials.Add(new Material());
                    _materials.Last()[field] = Enumerable.Range(0, 4)
                        .Select(i => source.ReadSingle())
                        .ToArray();
                    _materials.Last()[field][3] = 1;
                    break;

                case ModelField.UnknownShort149:
                case ModelField.UnknownShort150:
                case ModelField.UnknownShort151:
                    _fieldValues[field] = source.ReadInt16();
                    break;

                case ModelField.MaterialName:
                    var nameString = Encoding.GetEncoding(932).GetString(source.ReadBytes(source.ReadInt32()));
                    _names.Add(Translator.Translate(nameString));
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
            // For material format see http://www.martinreddy.net/gfx/3d/MLI.spec
            var materials = new MemoryStream();
            using (var writer = new BinaryWriter(materials, Encoding.ASCII, true))
                for (int i = 0; i < _materials.Count; i++)
                {
                    var name = Encoding.UTF8.GetBytes(_names[i]);
                    writer.Write((ushort)0xAFFF); // EDIT_MATERIAL chunk
                    writer.Write(name.Length + 61); // chunk length

                    writer.Write((ushort)0xA000); // MAT_NAME chunk
                    writer.Write(name.Length + 7); // chunk length
                    writer.Write(name); // material name
                    writer.Write((byte)0); // name terminator

                    writer.Write((ushort)0xA010); // Ambient colour chunk
                    writer.Write(24); // chunk length

                    writer.Write((ushort)0x0010); // RGB chunk
                    writer.Write(18); // chunk length
                    writer.Write(_materials[i][ModelField.MaterialAmbient][0]);
                    writer.Write(_materials[i][ModelField.MaterialAmbient][1]);
                    writer.Write(_materials[i][ModelField.MaterialAmbient][2]);

                    writer.Write((ushort)0xA020); // Diffuse colour chunk
                    writer.Write(24); // chunk length

                    writer.Write((ushort)0x0010); // RGB chunk
                    writer.Write(18); // chunk length
                    writer.Write(_materials[i][ModelField.MaterialAmbient][0]);
                    writer.Write(_materials[i][ModelField.MaterialAmbient][1]);
                    writer.Write(_materials[i][ModelField.MaterialAmbient][2]);
                }

            var hierarchies = new MemoryStream();
            using (var writer = new BinaryWriter(hierarchies, Encoding.ASCII, true))
                for (int i = 0; i < _names.Count; i++)
                {
                    var name = Encoding.UTF8.GetBytes(_names[i]);
                    writer.Write((ushort)0xB010); // KEYF_OBJHIERARCH chunk
                    writer.Write(13 + name.Length); // chunk length
                    writer.Write(name); // material name
                    writer.Write((byte)0); // name terminator
                    writer.Write(0); // unknown 4 bytes
                    writer.Write((ushort)(ushort.MaxValue)); // TODO: Hierarchy of object
                }

            var objects = new MemoryStream();
            var objectIndices = _faceMaterials == null ?
                Enumerable.Range(0, _names.Count) :
                _faceMaterials.Distinct().Select(b => (int)b).ToArray();

            using (var writer = new BinaryWriter(objects, Encoding.ASCII, true))
                foreach (var i in objectIndices)
                {
                    var name = Encoding.UTF8.GetBytes(_names[i]);

                    var materialFaces = new MemoryStream();
                    var faces = _faces;
                    if (_faceMaterials != null)
                        using (var faceWriter = new BinaryWriter(materialFaces, Encoding.ASCII, true))
                        {
                            var trimmedFaces = new List<byte>();
                            for (short faceIndex = 0; faceIndex < _faceMaterials.Length; faceIndex++)
                                if (_faceMaterials[faceIndex] == i)
                                    foreach (var triangleIndex in _faceTriangles[faceIndex])
                                        for (int j = 0; j < 8; j++)
                                            trimmedFaces.Add(_faces[triangleIndex * 8 + j]);

                            faceWriter.Write((ushort)0x4130); // TRI_MATERIAL chunk
                            faceWriter.Write(trimmedFaces.Count / 4 + name.Length + 9); // chunk length
                            faceWriter.Write(name); // material name
                            faceWriter.Write((byte)0); // name terminator
                            faceWriter.Write((short)trimmedFaces.Count / 8);

                            foreach (var triangleIndex in Enumerable.Range(1, trimmedFaces.Count / 8))
                                faceWriter.Write((ushort)triangleIndex);

                            faces = trimmedFaces.ToArray();
                        }
                    
                    var facesLength = (int)materialFaces.Length + faces.Length + 8;
                    var meshLength = facesLength + _vertices.Length + 14;

                    writer.Write((ushort)0x4000); // EDIT_OBJECT chunk
                    writer.Write(meshLength + 7 + name.Length); // chunk length
                    writer.Write(name); // object name
                    writer.Write((byte)0); // name terminator

                    writer.Write((ushort)0x4100); // OBJECT_TRIMESH chunk
                    writer.Write(meshLength); // chunk length

                    writer.Write((ushort)0x4110); // TRI_VERTEXL chunk
                    writer.Write(_vertices.Length + 8); // chunk length
                    writer.Write((ushort)(_vertices.Length / 12)); // total vertices
                    writer.Write(_vertices);

                    writer.Write((ushort)0x4120); // TRI_FACEL1 chunk
                    writer.Write(facesLength); // chunk length
                    writer.Write((ushort)(faces.Length / 8)); // total polygons
                    writer.Write(faces);

                    writer.Write(materialFaces.ToArray());
                }

            destination.Write((ushort)0x4D4D); // MAIN3DS chunk
            destination.Write((int)(objects.Length + materials.Length + hierarchies.Length + 24)); // chunk length

            destination.Write((ushort)0x3D3D); // EDIT3DS chunk
            destination.Write((int)(objects.Length + materials.Length + 6)); // chunk length
            destination.Write(materials.ToArray());

            destination.Write(objects.ToArray());

            destination.Write((ushort)0xB000); // KEYF3DS chunk
            destination.Write((int)hierarchies.Length + 12); // chunk length

            destination.Write((ushort)0xB002); // KEYF_OBJDES chunk
            destination.Write((int)hierarchies.Length + 6); // chunk length
            destination.Write(hierarchies.ToArray());

            return true;
        }
    }
}
