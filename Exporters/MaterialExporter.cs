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

				var colour = (float[])group[ModelField.MaterialAmbient];
				material.transparency = (1.0f - colour[3]) * 100;

				material.ambient[0] = colour[0];
				material.ambient[1] = colour[1];
				material.ambient[2] = colour[2];

				material.diffuse[0] = colour[0];
				material.diffuse[1] = colour[1];
				material.diffuse[2] = colour[2];

				var textureId = (int)group[ModelField.Texture];
				if (textureId != -1)
				{
					material.texture1_map.name = $"..\\Texture\\{textureId}.png";
					material.texture1_map.percent = 1.0f;
					material.texture1_map.scale[0] = 1.0f;
					material.texture1_map.scale[1] = 1.0f;
					material.texture1_map.flags = Lib3dsTextureMapFlags.LIB3DS_TEXTURE_SUMMED_AREA;
				}
			}
		}
    }
}