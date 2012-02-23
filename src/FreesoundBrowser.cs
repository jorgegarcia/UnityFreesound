using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.ComponentModel;

public class FreesoundBrowser : EditorWindow
{	
	private Vector2 scroll_;
	private string[] searchOptions_ = {"Duration (long first)",		//0
									   "Duration (short first)",	//1
									   "Date added (newest first)",	//2
									   "Date added (oldest first)",	//3
									   "Downloads (most first)",	//4
									   "Downloads (least first)",	//5
									   "Rating (highest first)",	//6
									   "Rating (lowest first)",		//7
									  };
	private string[] allowedFormats_ = {"aif", "mp3", "ogg", "wav"};
	private string searchQuery_ = "";
	private int sortIndex_ = 4;//Default is "Downloads (most first)" 
	private int downloadProgress_ = 0;
	private int previousDownloadProgress_ = 0;
	private float timer_ = 0.0f;
	private const float freesoundTimeout_ = 10.0f;
	private static GUIStyle waveformStyle_ = new GUIStyle();
	
	//Browser states
	private bool searchPressed_ = false;
	private bool responseProcessed_ = false;
	private bool resetScroll_ = false;
	private bool importPressed_ = false;
	private bool downloadCompleted_ = false;
	private bool downloadTimeout_ = false;
	
	//Request containers and chache
	private FreesoundLogData lastData_ = new FreesoundLogData();
	private WWW www_ = null;
	private WebClient importClient_ = null;
	private Dictionary<string, object> response_ = null;
	private List<Texture2D> soundWaveforms_ = new List<Texture2D>();
	private List<WWW> waveformWWWRequests_ = new List<WWW>();
	private List<bool> waveformsCached_ = new List<bool>();
		
	[MenuItem("Window/Freesound Browser %#x")]
	static void Init()
	{
		FreesoundBrowser window = (FreesoundBrowser)EditorWindow.GetWindow (typeof(FreesoundBrowser), 
		                                                         			true, "Freesound Browser");
		waveformStyle_.alignment = TextAnchor.MiddleCenter;
		
		window.Show();
	}
	
	void OnGUI()
	{	
		EditorGUILayout.BeginHorizontal();
		
			searchQuery_ = EditorGUILayout.TextField(searchQuery_);
			sortIndex_ = EditorGUILayout.Popup(sortIndex_, searchOptions_);
			
			if(GUILayout.Button("SEARCH") ||
			   (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return))
			{
				searchPressed_ = true;
				responseProcessed_ = false;
			}
			
			if(Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Space)
				FreesoundAudioPlayer.Instance.StopStream();
		
		EditorGUILayout.EndHorizontal();
		
		scroll_ = GUILayout.BeginScrollView(scroll_);
		
			DisplayResults();
		
		EditorGUILayout.EndScrollView();
	}
	
	void Update()
	{		
		#region SEARCH PRESSED
		if(searchPressed_)
		{
			searchPressed_ = false;
			ProcessFirstQuery(searchQuery_, sortIndex_);
		}
		
		if(www_ != null)
		{
			if(www_.isDone && www_.error == null)
			{
				try
				{
					//Thanks to https://bitbucket.org/darktable/jsonfx-for-unity3d/downloads
					Assembly jsonAssembly = Assembly.LoadFrom(Application.dataPath + "/Editor/UnityFreesound/JsonFx.Json.dll");
					Type jsonAssemblyType = jsonAssembly.GetType("JsonFx.Json.JsonReader");
					//Calling the static method "Deserialize" returning a Dictionary (default JsonReader call)
					response_ = (Dictionary<string, object>)jsonAssemblyType.InvokeMember("Deserialize", 
															BindingFlags.InvokeMethod, null, 
															Activator.CreateInstance(typeof(object)), 
															new object[]{ www_.text });
				}
				catch(Exception e)
				{
					UnityEditor.EditorUtility.DisplayDialog("UnityFreesound Request ERROR!", 
					                                        "Can't process the request. Please try again." +
					                                        "\n\nException thrown: " + e.Message, "OK");
				}
				
				//Filter here only the file types allowed in allowedFormats_
				FilterSoundsByFormat(ref response_);
				object[] sounds = (object[])response_["sounds"];
				SpawnWaveformsCache(ref sounds);
				EditorUtility.ClearProgressBar();
				responseProcessed_ = true;
				resetScroll_ = true;

				www_ = null;
			}
			else if(www_.isDone && www_.error != null)
			{
				UnityEditor.EditorUtility.DisplayDialog("UnityFreesound Request ERROR!", 
					                                     "Can't process the request." +
					                                     "\n\nError: " + www_.error, "OK");
				EditorUtility.ClearProgressBar();
				responseProcessed_ = true;
				resetScroll_ = true;
				
				www_ = null;
			}
			else
			{
				EditorUtility.DisplayProgressBar("Loading...", "Searching Freesound for ''" + searchQuery_ + "''", 0.5f);
			}
		}
		#endregion
		
		#region IMPORT PRESSED
		if(importPressed_)
		{
			EditorUtility.DisplayProgressBar("Downloading  sound...", "Completed " + downloadProgress_ + " %",
			                                 (float)downloadProgress_/100.0f);
			
			if((downloadProgress_ == previousDownloadProgress_) && !downloadTimeout_)
			{
				timer_ += Time.fixedDeltaTime;
				previousDownloadProgress_ = downloadProgress_;
				//Debug.Log("Import timeout: " + timer_.ToString());
				
				if(timer_ > freesoundTimeout_)
				{
					importClient_.CancelAsync();
					DisposeImportState();
					System.IO.File.Delete(lastData_.localPath);//Clean-up WebClient file
					
					EditorUtility.ClearProgressBar();
					UnityEditor.EditorUtility.DisplayDialog("UnityFreesound Request ERROR!", 
	                                        				"Request timeout. Sound not imported.", "OK");	
				}
			}
			else
			{
				previousDownloadProgress_ = downloadProgress_;
				timer_ = 0.0f;
			}
			
			if(downloadCompleted_ && !downloadTimeout_)
			{
				EditorUtility.ClearProgressBar();
				DisposeImportState();
				
				FreesoundHandler.Instance.AddSoundToCredits(lastData_.filename, lastData_.id, lastData_.localPath,
		                                            		lastData_.url, lastData_.user);
				EditorApplication.SaveAssets();
				AssetDatabase.Refresh();
			}
		}
		#endregion
		
		#region RESPONSE PROCESSED
		if(responseProcessed_)
		{
			object[] sounds = (object[])response_["sounds"];
		
			for(int i = 0; i < sounds.Length; i++)
			{	
				if(waveformWWWRequests_[i].isDone && !waveformsCached_[i] && waveformWWWRequests_[i].error == null)
				{
					soundWaveforms_[i] = waveformWWWRequests_[i].texture;
					waveformsCached_[i] = true;
				}
			}
			
			Repaint();
		}
		#endregion
	}
	
	private void FilterSoundsByFormat(ref Dictionary<string, object> response)
	{
		object[] sounds = (object[])response["sounds"];
		
		List<object> filtered = new List<object>();
		
		foreach(object sound in sounds)
		{
			var soundDictionary = (Dictionary<string, object>)sound;
			
			foreach(string typeAllowed in allowedFormats_)
			{
				if(String.Equals(soundDictionary["type"], typeAllowed))
				{
					filtered.Add(sound); break;
				}
			}
		}
		
		response["sounds"] = filtered.ToArray();	
	}
	
	private void ProcessFirstQuery(string query, int sortIndex)
	{	
		UriBuilder queryBuilder = new UriBuilder("http", "www.freesound.org");
		queryBuilder.Path = "/api/sounds/search";
		string sortMode = "";
		
		switch(sortIndex)
		{
			case 0:
				sortMode = "duration_desc";
				break;
			case 1:
				sortMode = "duration_asc";
				break;
			case 2:
				sortMode = "created_desc";
				break;
			case 3:
				sortMode = "created_asc";
				break;
			case 4:
				sortMode = "downloads_desc";
				break;
			case 5:
				sortMode = "downloads_asc";
				break;
			case 6:
				sortMode = "rating_desc";
				break;
			case 7:
				sortMode = "rating_asc";
				break;
		}
		
		query = query.Replace(" ", "+");//replace spaces with AND operator (default)	
		queryBuilder.Query += "p=1" + "&q=" + query + "&s=" + sortMode + "&api_key=" + FreesoundHandler.Instance.ApiKey;
		www_ = new WWW(queryBuilder.ToString());
	}
	
	private void DisplayResults()
	{
		if(responseProcessed_)
		{
			if(resetScroll_)
			{
				scroll_.x = scroll_.y = 0.0f;
				resetScroll_ = false;
			}
			
			object[] sounds = (object[])response_["sounds"];
			
			GUILayout.Label("Number of results: " + response_["num_results"].ToString());
			
			int soundCounter = 0;
			foreach(object sound in sounds)
			{
				var soundDictionary = (Dictionary<string, object>)sound;
				
				GUILayout.BeginHorizontal();
					
					GUILayout.Space(10);
				
					if(GUILayout.Button(soundWaveforms_[soundCounter], waveformStyle_))
						Application.OpenURL(soundDictionary["url"].ToString());
				
					GUILayout.BeginVertical();
						
						if(GUILayout.Button("\nPLAY\n"))
						{
							FreesoundAudioPlayer.Instance.StartStream(soundDictionary["preview-lq-ogg"].ToString());
						}
					
						GUILayout.Space(6);
						
						if(GUILayout.Button("IMPORT"))
						{
							importPressed_ = true;
					
							//TODO: refactor. Change WebClient to HttpWebRequest to handle timeouts better
							importClient_ = new WebClient();
  							importClient_.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
							importClient_.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
							Uri request = new Uri(soundDictionary["serve"] + "?api_key=" + FreesoundHandler.Instance.ApiKey);
							
							//If the user hasn't defined a default folder at Freesound configuration 
							//then create it at /Assets/Freesound/
							if(String.IsNullOrEmpty(FreesoundHandler.Instance.FreesoundDownloadsPath))
					   		{
								System.IO.Directory.CreateDirectory(Application.dataPath + "/Freesound");
								FreesoundHandler.Instance.FreesoundDownloadsPath = Application.dataPath + "/Freesound";
								FreesoundHandler.Instance.FreesoundDownloads = "/Freesound";
							}

							string lastDownloadedFilePath = FreesoundHandler.Instance.FreesoundDownloadsPath + 
															"/" + soundDictionary["original_filename"].ToString();
							
							try
							{
								importClient_.DownloadFileAsync(request, lastDownloadedFilePath);

								//Cache log data to be used in async loading completion
								Dictionary<string, object> userDataName = (Dictionary<string, object>)soundDictionary["user"];
						
								lastData_.filename = soundDictionary["original_filename"].ToString();
								lastData_.id = soundDictionary["id"].ToString();
								lastData_.url = soundDictionary["url"].ToString();
								lastData_.user = userDataName["username"].ToString();
								lastData_.localPath = lastDownloadedFilePath;
							}
							catch(WebException e)
							{
								UnityEditor.EditorUtility.DisplayDialog("UnityFreesound Request ERROR!", 
                                        								"Can't process the request." + "\n\nException thrown: " + e.Message,
						                                        		"OK");
							}
						}
					
					GUILayout.EndVertical();
				
					GUILayout.BeginVertical();
				
						EditorGUILayout.LabelField("File name", soundDictionary["original_filename"].ToString());
						int point = soundDictionary["duration"].ToString().IndexOf(".");
				
						if(point != -1)
						{
							if(soundDictionary["duration"].ToString().Length > 5)
								EditorGUILayout.LabelField("Duration (secs)", soundDictionary["duration"].ToString().Substring(0, 5));
							else
								EditorGUILayout.LabelField("Duration (secs)", soundDictionary["duration"].ToString());
						}
					    else
						{
							EditorGUILayout.LabelField("Duration (secs)", soundDictionary["duration"].ToString());
						}
				
						Dictionary<string, object> userData = (Dictionary<string, object>)soundDictionary["user"];
						EditorGUILayout.LabelField("User", userData["username"].ToString());
						
						object[] tags = (object[])soundDictionary["tags"];

						if(tags.Length > 0)
							EditorGUILayout.Popup("Tags", 0, (string[])tags);
						else
							EditorGUILayout.LabelField("Tags", "Sound untagged");
				
					GUILayout.EndVertical();
				
				GUILayout.EndHorizontal();
				
				GUILayout.Space(10);
				soundCounter++;
			}
			
			GUILayout.BeginHorizontal();
			
				GUILayout.Space(10);
				
				if(response_.ContainsKey("previous"))
			   	{
					if(GUILayout.Button("Previous results"))
					{
						string searchQuery = response_["previous"].ToString() + "&api_key=" + FreesoundHandler.Instance.ApiKey;
						responseProcessed_ = false;
						www_ = new WWW(searchQuery);
					}
				}
				if(response_.ContainsKey("next"))
			    {
					if(GUILayout.Button("Next results"))
					{
						string searchQuery = response_["next"].ToString() + "&api_key=" + FreesoundHandler.Instance.ApiKey;
						responseProcessed_ = false;
						www_ = new WWW(searchQuery);
					}
				}
				
				GUILayout.Space(10);
				
			GUILayout.EndHorizontal();
			
			GUILayout.Space(10);
		}
		else
		{
			GUILayout.Label("Search for keywords/tags in the field above to browse sounds...");
		}
	}
	
	private void SpawnWaveformsCache(ref object[] sounds)
	{	
		Texture2D placeholder = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Editor/UnityFreesound/waveform_placeholder.png",
		                                                                 typeof(Texture2D));
		soundWaveforms_.Clear();
		waveformWWWRequests_.Clear();
		waveformsCached_.Clear();
		
		foreach(object sound in sounds)
		{
			var soundDictionary = (Dictionary<string, object>)sound;
			soundWaveforms_.Add(placeholder);
			WWW www = new WWW(soundDictionary["waveform_m"].ToString());
			waveformWWWRequests_.Add(www);
			waveformsCached_.Add(false);
		}
	}
	
	private void DisposeImportState()
	{
		importClient_.Dispose();
		importPressed_ = false;
		downloadCompleted_ = false;
		downloadTimeout_ = false;
		downloadProgress_ = 0;
		timer_ = 0.0f;
	}
	
	#region WebClient Events
	private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
	{
  		downloadProgress_ = e.ProgressPercentage;
	}
	
	private void Completed(object sender, AsyncCompletedEventArgs e)
	{
		downloadCompleted_ = true;
		
  		if(e.Cancelled)
			downloadTimeout_ = true;
	}
	#endregion
}

