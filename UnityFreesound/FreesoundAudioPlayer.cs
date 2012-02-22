using UnityEngine;
using UnityEditor;
 
[RequireComponent(typeof(AudioSource))]
public class FreesoundAudioPlayer : MonoBehaviour
{
	private static FreesoundAudioPlayer instance_;
	
	static FreesoundAudioPlayer(){}
	
	private FreesoundAudioPlayer(){}
	
	public static FreesoundAudioPlayer Instance 
	{
	    get 
		{
			instance_ = (FreesoundAudioPlayer)GameObject.FindObjectOfType(typeof(FreesoundAudioPlayer));
			
			if(instance_ == null) 
			{
				instance_ = new GameObject("FreesoundAudioPlayer").AddComponent<FreesoundAudioPlayer>();
	        }
	       
	        return instance_;
	    }
	}
 
    public void StartStream (string url)
    {
		if(audio.isPlaying) audio.Stop();
		
		WWW audioStreamer = new WWW (url);
        audio.clip = audioStreamer.audioClip;
 
		EditorUtility.DisplayProgressBar("Buffering preview from Freesound.org ...", "", 0.5f);

		//Block and wait for audio buffering
		//TODO: would prefer handling it at Update()
		//so to allow different streams playing concurrently
        while(!audio.clip.isReadyToPlay) 
		{
			if(audioStreamer.error != null)
			{
				UnityEditor.EditorUtility.DisplayDialog("UnityFreesound Request ERROR!", 
                                        "Can't process the request." +
                                        "\n\nError: " + audioStreamer.error, "OK");
				EditorUtility.ClearProgressBar();
				return;
			}
			
		}
		
 		EditorUtility.ClearProgressBar();
		
        audio.Play();
    }
	
	public void StopStream()
	{
		audio.Stop();
	}
}

