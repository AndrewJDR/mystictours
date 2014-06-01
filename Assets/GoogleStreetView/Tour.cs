using UnityEngine;
using System.Collections;

public class Tour : MonoBehaviour {
	AudioSource audioSource, ambientAudioSource;
	GameObject prefabInstance;

	[System.Serializable]
	public class Location {
		public string name;
		public string location;
		public AudioClip audio, ambient;
		[Range(0, 1)]
		public float ambientVolume = 1f;
		public GameObject pointsPrefab;
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
	
	public void PlayAmbientAudio() {
		AudioClip clip = locations[currentIndex].ambient;
		if (clip) {
			float volume = locations[currentIndex].ambientVolume;
			ambientAudioSource = SoundManager.PlaySFX(clip, 0);
			ambientAudioSource.loop = true;
			Tween.Value(1).OnUpdate((f) => {
				if (ambientAudioSource)
					ambientAudioSource.volume = f * volume;
			}).Start();
		}
	}

	public void StopAmbientAudio() {
		float volume = ambientAudioSource.volume;
		Tween.Value(1).OnUpdate((f) => {
			if (ambientAudioSource)
				ambientAudioSource.volume = (1 - f) * volume;
		}).OnComplete(() => {
			if (ambientAudioSource) {
				ambientAudioSource.Stop();
				ambientAudioSource = null;
			}
		}).Start();
	}

	void ChangeLocation() {
		StreetView streetView = GetComponent<StreetView>();
		streetView.location = locations[currentIndex].location;
		streetView.Load();
		if (prefabInstance) {
			Destroy(prefabInstance);
			prefabInstance = null;
		}
		if (locations[currentIndex].pointsPrefab)
			prefabInstance = (GameObject)Instantiate(locations[currentIndex].pointsPrefab);
	}
}
