using UnityEngine;
using System.Collections;

public class Tour : MonoBehaviour {
	AudioSource audioSource;

	[System.Serializable]
	public class Location {
		public string name;
		public string location;
		public AudioClip audio;
	}

	public Location[] locations;

	public int currentIndex {
		get {
			return PlayerPrefs.GetInt("current_location", 0) % locations.Length;
		}
		set {
			PlayerPrefs.SetInt("current_location", value % locations.Length);
		}
	}

	public void Init() {
		ChangeLocation();
	}

	public void Next() {
		currentIndex++;
		ChangeLocation();
	}

	public void Prev() {
		currentIndex--;
		ChangeLocation();
	}

	public void PlayAudio(System.Action onComplete) {
		AudioClip clip = locations[currentIndex].audio;
		audioSource = SoundManager.PlaySFX(clip);
		Tween.Delay(clip.length, onComplete).Tags("play-audio").Start();
	}

	public void StopAudio() {
		if (audioSource) {
			audioSource.Stop();
			audioSource = null;
			Tween.Stop("play-audio");
		}
	}

	void ChangeLocation() {
		StreetView streetView = GetComponent<StreetView>();
		streetView.location = locations[currentIndex].location;
		streetView.Load();
	}
}
