using UnityEngine;
using System.Collections;
using System.IO;

public class StreetView : MonoBehaviour {
	public string location = "48.857507,2.294989";
	
	public Material skybox;
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

		foreach (object[] dir in directions) {
			var texFolder = "Resources\\" + location.Replace(",", "_");
			var texFile = texFolder + "\\" + dir[2] + ".png";

			if( System.IO.File.Exists(texFile) ) {
				// "Empty" texture. Will be replaced by LoadImage
				Texture2D tex = new Texture2D(4,4);
				FileStream fs = new FileStream(texFile, FileMode.Open, FileAccess.Read);
				byte[] imageData = new byte[fs.Length];
				fs.Read(imageData, 0, (int) fs.Length);
				tex.LoadImage(imageData);
				tex.wrapMode = TextureWrapMode.Clamp;
				skybox.SetTexture((string)dir[2], tex);
				Debug.Log("Using cached texture " + imageData);
			} else {
				WWW www = new WWW(GetURL((int)dir[0], (int)dir[1]));
				yield return www;
				Texture2D tex = www.texture;
				tex.wrapMode = TextureWrapMode.Clamp;
				skybox.SetTexture((string)dir[2], tex);

				System.IO.Directory.CreateDirectory(texFolder);
				var bytes = tex.EncodeToPNG();
				File.WriteAllBytes( texFile, bytes );
				Debug.Log("Using downloaded texture for " + (string)dir[2]);
			}
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
