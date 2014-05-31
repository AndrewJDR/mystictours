// coded by Nora
// http://stereoarts.jp
using UnityEngine;
using System.Collections;

public class SkyboxMesh : MonoBehaviour {
	
	public string	ShaderName	= "Unlit/Texture";
	public float	Radius		= 800.0f;
	public int	 	Segments	= 32;
	public Material	Skybox;
	
	enum Face {
		Front,
		Back,
		Left,
		Right,
		Up,
		Down,
		Max,
	}
	
	void Awake()
	{
		Mesh mesh = _CreateMesh();
		
		_CreatePlane( mesh, Face.Front );
		GameObject left = _CreatePlane( mesh, Face.Left );
		left.transform.localRotation = Quaternion.Euler( 0.0f, 90.0f, 0.0f );
		GameObject back = _CreatePlane( mesh, Face.Back );
		back.transform.localRotation = Quaternion.Euler( 0.0f, 180.0f, 0.0f );
		GameObject right = _CreatePlane( mesh, Face.Right );
		right.transform.localRotation = Quaternion.Euler( 0.0f, 270.0f, 0.0f );
		GameObject up = _CreatePlane( mesh, Face.Up );
		up.transform.localRotation = Quaternion.Euler( -90.0f, 0.0f, 0.0f );
		GameObject down = _CreatePlane( mesh, Face.Down );
		down.transform.localRotation = Quaternion.Euler( 90.0f, 0.0f, 0.0f );
	}
	
	Mesh _CreateMesh()
	{
		Mesh mesh = new Mesh();

        int hvCount2 = this.Segments + 1;
		int hvCount2Half = hvCount2 / 2;
		int numVertices = hvCount2 * hvCount2;
		int numTriangles = this.Segments * this.Segments * 6;
		Vector3[] vertices = new Vector3[numVertices];
        Vector2[] uvs = new Vector2[numVertices];
        int[] triangles = new int[numTriangles];
		
		float scaleFactor = 2.0f / (float)this.Segments;
		float angleFactor = Mathf.Deg2Rad * 90.0f / (float)this.Segments;
		float uvFactor = 1.0f / (float)this.Segments;
		
		float py = -1.0f;
		float ty = 0.0f;
        for( int y = 0, index = 0; y < hvCount2; ++y, ty += uvFactor, py += scaleFactor ) {
			float px = -1.0f;
			float tx = 0.0f;
			for( int x = 0; x < hvCount2; ++x, ++index, tx += uvFactor, px += scaleFactor ) {
				if( x <= hvCount2Half && y <= hvCount2Half ) {
					float d = Mathf.Sqrt( px * px + py * py + 1.0f );
					float theta = Mathf.Acos( 1.0f / d );
					float phi = Mathf.Atan2( py, px );
					float sinTheta = Mathf.Sin( theta );
	                vertices[index] = new Vector3(
						sinTheta * Mathf.Cos( phi ),
						sinTheta * Mathf.Sin( phi ),
						Mathf.Cos( theta ) );
				} else if( x <= hvCount2Half ) {
					Vector3 v = vertices[(hvCount2Half - (y - hvCount2Half)) * hvCount2 + x];
					vertices[index] = new Vector3( v.x, -v.y, v.z );
				} else if( y <= hvCount2Half ) {
					Vector3 v = vertices[y * hvCount2 + (hvCount2Half - (x - hvCount2Half))];
					vertices[index] = new Vector3( -v.x, v.y, v.z );
				} else {
					Vector3 v = vertices[(hvCount2Half - (y - hvCount2Half)) * hvCount2 + (hvCount2Half - (x - hvCount2Half))];
					vertices[index] = new Vector3( -v.x, -v.y, v.z );
				}
                uvs[index] = new Vector2( tx, ty );
            }
        }

		for( int y = 0, index = 0, ofst = 0; y < this.Segments; ++y, ofst += hvCount2 ) {
			int y0 = ofst, y1 = ofst + hvCount2;
            for( int x = 0; x < this.Segments; ++x, index += 6 ) {
                triangles[index+0] = y0 + x;
                triangles[index+1] = y1 + x;
                triangles[index+2] = y0 + x + 1;
                triangles[index+3] = y1 + x;
                triangles[index+4] = y1 + x + 1;
                triangles[index+5] = y0 + x + 1;
            }
        }

		mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
		return mesh;
	}
	
	GameObject _CreatePlane( Mesh mesh, Face face )
	{
		GameObject go = new GameObject();
		go.transform.parent = this.transform;
		go.transform.localPosition = Vector3.zero;
		go.transform.localScale = new Vector3( this.Radius, this.Radius, this.Radius );
		go.transform.localRotation = Quaternion.identity;
		Material material = new Material( Shader.Find( this.ShaderName ) );
		material.mainTexture = Skybox.GetTexture( "_" + face.ToString() + "Tex" );
		MeshRenderer meshRenderer = go.AddComponent< MeshRenderer >();
		meshRenderer.material = material;
		meshRenderer.castShadows = false;
		meshRenderer.receiveShadows = false;
		MeshFilter meshFilter = go.AddComponent< MeshFilter >();
		meshFilter.mesh = mesh;
		return go;
	}
}
