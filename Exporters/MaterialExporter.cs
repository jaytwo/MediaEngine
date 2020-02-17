using lib3ds.Net;
using MediaEngine.Unpackers;
using System.Collections.Generic;

namespace MediaEngine.Exporters
{
	/// <summary>
	/// See http://www.martinreddy.net/gfx/3d/MLI.spec
	/// </summary>
	static class MaterialExporter
    {
        public static void Export(List<Group> groups, Lib3dsFile destination)
        {
			for (int i = 0; i < groups.Count; i++)
			{
				var group = groups[i];
				var material = new Lib3dsMaterial();
				material.name = group[ModelField.GroupName].ToString();
				destination.materials.Add(material);

				group = group.TextureGroup[0];

				material.ambient = (float[])group[ModelField.MaterialAmbient];
				material.diffuse = (float[])group[ModelField.MaterialAmbient];
				material.transparency = 1.0f - material.ambient[3];

				var textureId = (int)group[ModelField.Texture];
				if (textureId != -1)
				{
					material.texture1_map.name = $"..\\Texture\\{textureId}.png";
					material.texture1_map.percent = 1.0f;
					material.texture1_map.scale[0] = (float)group[ModelField.TextureDivisionU];
					material.texture1_map.scale[1] = (float)group[ModelField.TexturePositionV];
					material.texture1_map.offset[0] = (float)group[ModelField.TexturePositionU];
					material.texture1_map.offset[1] = (float)group[ModelField.TexturePositionV];
					material.texture1_map.flags = Lib3dsTextureMapFlags.LIB3DS_TEXTURE_SUMMED_AREA;
				}
			}
		}
    }
}