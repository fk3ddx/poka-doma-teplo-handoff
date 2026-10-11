using UnityEngine;
namespace Ataka.Scenario {
 // Temporary skin material; the falling, folded screen mesh follows section 7.4.
 public sealed class DeathVeil:MonoBehaviour {
  public Material skin;public Transform head;Mesh mesh;MeshRenderer surface;const int Columns=32,Rows=8;
  void Awake(){var g=new GameObject("Face tear screen mesh");g.transform.SetParent(head,false);g.transform.localPosition=Vector3.forward*.24f;var filter=g.AddComponent<MeshFilter>();surface=g.AddComponent<MeshRenderer>();surface.sharedMaterial=skin;surface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   mesh=new Mesh{name="Folding veil"};filter.sharedMesh=mesh;var uv=new Vector2[(Columns+1)*(Rows+1)];var triangles=new int[Columns*Rows*6];
   for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++)uv[y*(Columns+1)+x]=new Vector2((float)x/Columns,(float)y/Rows);
   int at=0;for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++){int i=y*(Columns+1)+x;triangles[at++]=i;triangles[at++]=i+Columns+1;triangles[at++]=i+1;triangles[at++]=i+1;triangles[at++]=i+Columns+1;triangles[at++]=i+Columns+2;}
   mesh.vertices=new Vector3[uv.Length];mesh.uv=uv;mesh.triangles=triangles;surface.enabled=false;
  }
  public void At(float t){surface.enabled=t>=1.5f&&t<2.5f;if(!surface.enabled)return;float progress=Mathf.Clamp01((t-1.5f)/.75f);var vertices=new Vector3[(Columns+1)*(Rows+1)];
   for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++){float u=(float)x/Columns,v=(float)y/Rows;float ragged=Mathf.Sin(x*2.3f)*.004f+Mathf.Sin(x*5.7f)*.002f;vertices[y*(Columns+1)+x]=new Vector3((u-.5f)*.58f,.17f-progress*.38f-v*.07f+ragged,Mathf.Sin(v*Mathf.PI)*.04f);}
   mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
  }
  void OnDestroy(){if(mesh)Destroy(mesh);}
 }
}
