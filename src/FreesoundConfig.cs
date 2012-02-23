using UnityEngine;
using UnityEditor;

public class FreesoundConfig : EditorWindow
{
	private static Texture2D freesoundLogo_;
	private static GUIStyle style = new GUIStyle();
	private Vector2 scroll_;
	
	[MenuItem("Window/Freesound Config and Credits")]
	static void Init()
	{
		FreesoundConfig window = (FreesoundConfig)EditorWindow.GetWindow (typeof(FreesoundConfig), 
		                                                                  true, "Freesound Config and Credits");
		
		string logoPath = "Assets/Editor/UnityFreesound/freesound_logo_small.png";
		freesoundLogo_ = (Texture2D)AssetDatabase.LoadAssetAtPath(logoPath, typeof(Texture2D));
		
		style.alignment = TextAnchor.MiddleCenter;
		
		window.Show();		
	}

	void OnGUI()
	{	
		// Logo and link to Freesound.org
		if(GUILayout.Button(freesoundLogo_, style)) Application.OpenURL("http://www.freesound.org");
		EditorGUILayout.Separator();
		
		// API Key
		EditorGUILayout.LabelField("API KEY", FreesoundHandler.Instance.ApiKey);		
		EditorGUILayout.Separator();
		
		// Asset folder (where the downloaded Freesound sounds will be stored) 
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("AUDIO FOLDER", FreesoundHandler.Instance.FreesoundDownloads);
		if(GUILayout.Button("Change..."))
		{
			FreesoundHandler.Instance.FreesoundDownloadsPath = EditorUtility.SaveFolderPanel("Select download folder...", Application.dataPath, "");
			
			if(!System.String.IsNullOrEmpty(FreesoundHandler.Instance.FreesoundDownloadsPath))
			{
				int lastSlash = FreesoundHandler.Instance.FreesoundDownloadsPath.LastIndexOf("/");
				FreesoundHandler.Instance.FreesoundDownloads = FreesoundHandler.Instance.FreesoundDownloadsPath.Substring(lastSlash);
			}
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Separator();
		
		// Allow exporting Freesound data as CSV...
		if(FreesoundHandler.Instance.FileNames.Count > 0)
		{
			// Freesound credits
			GUILayout.Label("List of sounds imported in this project...");
			scroll_ = GUILayout.BeginScrollView(scroll_);
			GUILayout.Label(System.String.Join("\n", FreesoundHandler.Instance.GetFormattedLogData()));
			EditorGUILayout.EndScrollView();
			EditorGUILayout.Separator();
			
			if(GUILayout.Button("Export sounds full data to .csv"))
			{
				string destinationPath = EditorUtility.SaveFilePanel("Select folder to save credits...", "", "Freesound_credits", "csv");
				System.IO.File.WriteAllLines(destinationPath, FreesoundHandler.Instance.GetFormattedCSVData());
			}
			
			EditorGUILayout.Separator();
		}
	}
}