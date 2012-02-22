using UnityEditor;

public class FreesoundTextureImporter : AssetPostprocessor
{
	void OnPreprocessTexture()
	{
		TextureImporter textureImporter = assetImporter as TextureImporter;
		
		TextureImporterSettings st = new TextureImporterSettings();
		
		textureImporter.ReadTextureSettings(st);
		st.textureFormat = TextureImporterFormat.RGB16;
		st.ApplyTextureType(TextureImporterType.GUI, true);
		textureImporter.SetTextureSettings(st);
		AssetDatabase.Refresh();
	}
}
