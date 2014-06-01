using UnityEngine;
using System.Collections;

public class ScreenFader : MonoBehaviour {
	public static ScreenFader instance = null;
	public enum Direction {
		In,
		Out
	}

	[HideInInspector] public Direction direction;
	[HideInInspector] public float time;
	[HideInInspector] public bool realtime = true;

	public System.Action<float> fader = null;
	public System.Action onComplete = null;
	private int GUIDepth = -2;
	private float value = 0;
	private float startTime, currentTime;
	private bool running = false;

	public static ScreenFader GetInstance() {
		if (instance == null) {
			instance = GameObject.FindObjectOfType<ScreenFader>();
			if (instance == null)
				instance = new GameObject("ScreenFader").AddComponent<ScreenFader>();
			instance.gameObject.SetActive(false);
		}
		return instance;
	}

	public void SetToState(Direction direction) {
		if (direction == Direction.In) {
			value = 1;
			gameObject.SetActive(true);
		} else {
			value = 0;
			gameObject.SetActive(false);
		}
	}

	public void Stop() {
		if (value <= 0) {
			gameObject.SetActive(false);
			fader = null;
		}
		running = false;
		if (onComplete != null) {
			onComplete();
			onComplete = null;
		}
	}

	public void Run() {
		startTime = Time.realtimeSinceStartup;
		currentTime = 0;
		running = true;
		gameObject.SetActive(true);
	}

	void Update() {
		if (!running)
			return;
		if (realtime)
			currentTime = Time.realtimeSinceStartup - startTime;
		else
			currentTime+= Time.deltaTime;
		float f = Mathf.Clamp01(currentTime / time);
		if (direction == Direction.In)
			value = f;
		else
			value = 1 - f;
		if (f >= 1)
			Stop ();
	}

	void OnGUI() {
		if (fader != null && value > 0) {
			GUI.depth = GUIDepth;
			Color guiColor = GUI.color;
			fader(value);
			GUI.color = guiColor;
		}
	}
}
