using UnityEngine;
using System.Collections;

public class Light : MonoBehaviour {
	public string scene2load;

	float f = 0;
	bool mouseDown = false;

	void Update() {
		light.intensity = (Mathf.Cos(f) + 2) * 2;
		f = (f + Time.deltaTime * 8) % (Mathf.PI * 2);
	}

	void OnMouseEnter() {
		Tween.Value(0.4f).Target(this).OnUpdate(f => light.color = Color.Lerp(Color.white, Color.yellow, f)).Start();
	}

	void OnMouseExit() {
		Tween.Value(0.4f).Target(this).OnUpdate(f => light.color = Color.Lerp(Color.yellow, Color.white, f)).Start();
	}

	void OnMouseDown() {
		mouseDown = true;
	}

	void OnMouseUp() {
		if (mouseDown) {
			// click
			Application.LoadLevel(scene2load);
		}
		mouseDown = false;
	}
}
