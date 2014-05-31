using UnityEngine;
using System.Collections;

public class Globe : MonoBehaviour {
	public Transform world;

	float speed = 0;
	Vector3 rotation = new Vector3(0, 300, 0), savedRotation, touchPosition, prevPosition;
	bool touchDown = false;

	void Update() {
		if (!touchDown)
			rotation.y = (rotation.y + speed) % 360;
		world.localEulerAngles = rotation;
	}

	void FixedUpdate() {
		speed*= 0.95f;
	}

	void OnMouseDown() {
		speed = 0;
		savedRotation = rotation;
		touchPosition = Input.mousePosition;
		prevPosition = touchPosition;
		touchDown = true;
	}

	void OnMouseUp() {
		touchDown = false;
	}

	void OnMouseDrag() {
		float dx = (touchPosition.x - Input.mousePosition.x) * 0.2f;
		speed = (prevPosition.x - Input.mousePosition.x) * 0.2f;
		rotation.y = (savedRotation.y + dx) % 360;
		prevPosition.x = Input.mousePosition.x;
	}
}
