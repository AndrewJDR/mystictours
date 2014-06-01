using UnityEngine;
using System.Collections;

public class StreetView : MonoBehaviour {
	public string location = "48.857507,2.294989";
	
	public Material skybox;
	public GameObject ovrSkybox;
	public SkyboxMesh skyboxmesh;

	public Transform cam;

	IEnumerator Start() {
		object[][] directions = new object[][] {
			new object[] {  0,  0, "_FrontTex"},
			new object[] { 90,  0, "_LeftTex"},
			new object[] {180,  0, "_BackTex"},
			new object[] {270,  0, "_RightTex"},
			new object[] {  0, 90, "_UpTex"},
			new object[] {  0,-90, "_DownTex"}
		};

//		object[][] directions = new object[][] {
//			new object[] {  0,  0, "SkyMAPSsunset0000"},
//			new object[] { 90,  0, "SkyMAPSsunset0001"},
//			new object[] {180,  0, "SkyMAPSsunset0002"},
//			new object[] {270,  0, "SkyMAPSsunset0003"},
//			new object[] {  0, 90, "SkyMAPSsunset0004"},
//			new object[] {  0,-90, "SkyMAPSsunset0000"}
//		};

//		object[][] directions = new object[][] {
//			new object[] {180,  0, "_BackTex"},
//			new object[] { 90,  0, "_LeftTex"},
//			new object[] {  0, 90, "_UpTex"},
//			new object[] {  0,  0, "_FrontTex"},
//			new object[] {270,  0, "_RightTex"}
//			new object[] {  0,-90, "_DownTex"}
//		};


//		var skyboxes = ovrSkybox.renderer.materials;
//		int counter = 0;
		foreach (object[] dir in directions) {
			WWW www = new WWW(GetURL((int)dir[0], (int)dir[1]));
			yield return www;
			Texture2D tex = www.texture;
			tex.wrapMode = TextureWrapMode.Clamp;
//			skyboxes[counter].mainTexture = tex;
//			counter ++;
			skybox.SetTexture((string)dir[2], tex);
		}
		skyboxmesh.UpdateSkybox();

	}

	string GetURL(int heading, int pitch) {
		return "http://maps.googleapis.com/maps/api/streetview?size=640x640&location=" + location + "&heading=" + heading + "&pitch=" + pitch + "&sensor=false";
	}


	// Rotation
	Vector3 speed = Vector3.zero;
	Vector3 rotation = new Vector3(0, 300, 0), savedRotation, touchPosition, prevPosition;
	bool touchDown = false;

	void Update() {
		if (!touchDown) {
			rotation.x = (rotation.x + speed.x) % 360;
			rotation.y = (rotation.y + speed.y) % 360;
		}
		cam.localEulerAngles = rotation;
		if (Input.GetMouseButtonDown(0)) {
			speed = Vector3.zero;
			savedRotation = rotation;
			touchPosition = Input.mousePosition;
			prevPosition = touchPosition;
			touchDown = true;
		} else if (Input.GetMouseButtonUp(0)) {
			touchDown = false;
		} else if (touchDown) {
			float dy = (touchPosition.x - Input.mousePosition.x) * 0.05f;
			float dx = -(touchPosition.y - Input.mousePosition.y) * 0.05f;
			speed.y = (prevPosition.x - Input.mousePosition.x) * 0.05f;
			speed.x = -(prevPosition.y - Input.mousePosition.y) * 0.05f;
			rotation.x = (savedRotation.x + dx) % 360;
			rotation.y = (savedRotation.y + dy) % 360;
			prevPosition = Input.mousePosition;
		}
	}

	void FixedUpdate() {
		speed*= 0.95f;
	}
}
