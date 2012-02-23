using UnityEngine;
using System.Collections.Generic;

struct FreesoundLogData
{
	public string filename;
	public string user;
	public string id;
	public string localPath;
	public string url;
}

public class FreesoundHandler : MonoBehaviour
{
	private static FreesoundHandler instance_;
	
	static FreesoundHandler(){}
	
	private FreesoundHandler(){}
	
	public static FreesoundHandler Instance 
	{
	    get 
		{
			instance_ = (FreesoundHandler)GameObject.FindObjectOfType(typeof(FreesoundHandler));
			
			if(instance_ == null) 
			{
				instance_ = new GameObject("FreesoundHandler").AddComponent<FreesoundHandler>();
				instance_.fileNames = new List<string>();
				instance_.freesoundIds = new List<string>();
				instance_.users = new List<string>();
				instance_.freesoundURLs = new List<string>();
				instance_.localPaths = new List<string>();
	        }
	       
	        return instance_;
	    }
	}
	
	#region Member Variables
	[SerializeField]
	private string freesoundDownloadsPath_ = "";

	[SerializeField]
	private string freesoundDownloads_ = "";
	
	//Using Lists instead of a struct in order to allow
	//serializing the data within the Unity project...
	[SerializeField]
	private List<string> fileNames;
	
	[SerializeField]
	private List<string> freesoundIds;
	
	[SerializeField]
	private List<string> users;
	
	[SerializeField]
	private List<string> localPaths;
	
	[SerializeField]
	private List<string> freesoundURLs;
	#endregion
	
	private const string apiKey_ = "8d83cf5d9b7842399ec1bae7a23b6bb5";
	
	#region Properties	
	public List<string> FileNames
	{
		get
		{
			return fileNames;
		}
	}
	
	public List<string> FreesoundIds
	{
		get
		{
			return freesoundIds;
		}
	}

	public List<string> Users
	{
		get
		{
			return users;
		}
	}	
	
	public List<string> LocalPaths
	{
		get
		{
			return localPaths;
		}
	}

	public List<string> FreesoundURLS
	{
		get
		{
			return freesoundURLs;
		}
	}

	
	public string ApiKey
	{
		get
		{
			return apiKey_;
		}
	}
	
	public string FreesoundDownloadsPath
	{
		get
		{
			return freesoundDownloadsPath_;
		}
		
		set
		{
			freesoundDownloadsPath_ = value;
		}
	}
	
	public string FreesoundDownloads
	{
		get
		{
			return freesoundDownloads_;
		}
		
		set
		{
			freesoundDownloads_ = value;
		}
	}
	#endregion
	
	#region Methods
	private void UpdateFileLog()
	{
		if(fileNames.Count > 0)
		{
			List<int> fileIndexesToRemove = new List<int>();
			
			for(int i = 0; i < fileNames.Count; i++)
			{
				if(!System.IO.File.Exists(localPaths[i]))
					fileIndexesToRemove.Add(i);
			}
			
			for(int index = fileIndexesToRemove.Count - 1; index >= 0; index--)
			{
				fileNames.RemoveAt(fileIndexesToRemove[index]);
				freesoundURLs.RemoveAt(fileIndexesToRemove[index]);
				localPaths.RemoveAt(fileIndexesToRemove[index]);
				users.RemoveAt(fileIndexesToRemove[index]);
				freesoundIds.RemoveAt(fileIndexesToRemove[index]);
			}
			
		}
	}
	
	
	public void AddSoundToCredits(string fileName, string id, string localPath, 
	                              string url, string user)
	{
		bool fileRegistered = false;
		
		if(fileNames.Count > 0)
		{
			foreach(string fileId in freesoundIds)
			{
				if(fileId == id)
				{
					fileRegistered = true;
					break;
				}
			}
		}
		
		//If the file hasn't been registered yet, then add it
		if(!fileRegistered)
		{
			freesoundURLs.Add(url);
			LocalPaths.Add(localPath);
			users.Add(user);
			fileNames.Add(fileName);
			freesoundIds.Add(id);
		}
	}
	
	public string[] GetFormattedLogData()
	{
		List<string> outBuffer = new List<string>();
		
		UpdateFileLog();
			
		for(int i = 0; i < fileNames.Count; i++)
		{
			outBuffer.Add(fileNames[i] + " by " + users[i]);
		}
		
		return outBuffer.ToArray();
	}
	
	public string[] GetFormattedCSVData()
	{
		List<string> outBuffer = new List<string>();
		
		//Add column headers first..
		outBuffer.Add("FileName;User;FreesoundID;LocalPath;FreesoundURL");
		
		for(int i = 0; i < fileNames.Count; i++)
		{
			string[] temp = { fileNames[i], users[i], freesoundIds[i], localPaths[i], freesoundURLs[i] };
			outBuffer.Add(string.Join(";", temp));
		}
		
		return outBuffer.ToArray();
	}
	#endregion
}


