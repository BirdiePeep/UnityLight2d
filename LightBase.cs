using UnityEngine;
using System.Collections.Generic;
using System;

namespace Bird.Light2D
{
	[ExecuteInEditMode]
	public class LightBase : MonoBehaviour
	{
		public float radius = 5.0f;
		public Color color = Color.white;

		[Range(0.0f, 1.0f)]
		public float intensity = 1.0f;

		[Range(0.0f, 360.0f)]
		public float spread = 360;

		[Range(0.0f, 1.0f)]
		public float innerRadius = 0.0f;

		[Range(0.0f, 1.0f)]
		public float innerAngle = 0.0f;

		[Range(0.0f, 10.0f)]
		public float falloffExponent = 1.0f;

		[Range(0.0f, 1.0f)]
		public float shadowStrength = 1.0f;

		public float angle
		{
			get
			{
				//float angle = (transform.localRotation.eulerAngles.z % 360.0f);
				//if (angle < 0)
				//	angle = 360f - angle;
				//return angle;
				return transform.localEulerAngles.z;
			}
		}

		//new Renderer renderer;

		public HashSet<LightCollider> colliders = new HashSet<LightCollider>();
		[NonSerialized] public Vector4 shadowMapParams;

		public BoundingSphere GetBounds()
		{
			return new BoundingSphere(transform.position, radius);
		}
		private void OnEnable()
		{
			SetMesh(GenerateMesh());
			Light2dFeature.OnLightEnable(this);
		}

		private void OnDisable()
		{
			Light2dFeature.OnLightDisable(this);
		}
		private void LateUpdate()
		{
			UpdateMesh();
		}
		protected private void OnDrawGizmos()
		{
			Gizmos.DrawIcon(transform.position, "Light2D/light.png");
		}

		//Mesh generation
		float prevSpread = 0;
		public void UpdateMesh()
		{
			//Check for change
			bool needsRebuild = false;
			if (prevSpread != spread)
			{
				needsRebuild = true;
				prevSpread = spread;
			}

			//Generate if needed
			if (needsRebuild)
				SetMesh(GenerateMesh());
		}

		static Collider2D[] overlapBuffer = new Collider2D[1024];
		public void UpdateColliders(PhysicsScene2D physics)
		{
			var bounds = GetBounds();
			int count = physics.OverlapCircle(bounds.position, radius, overlapBuffer, Light2dFeature.inst.colliderLayerMask);
			colliders.Clear();
			for (int i = 0; i < count; i++)
			{
				var comp = overlapBuffer[i].GetComponent<LightCollider>();
				if (comp != null)
					colliders.Add(comp);
			}
		}

		//Mesh
		[NonSerialized] public Mesh mesh;

		public static List<Vector3> verts = new List<Vector3>();
		public static List<int> tris = new List<int>();
		public Mesh GenerateMesh()
		{
			int maxSegments = 32;
			float segmentAngle = 360f / (float)maxSegments;
			int segmentCount = (int)Mathf.Ceil((float)maxSegments * (spread / 360f));
			verts.Clear();
			tris.Clear();

			//Verts
			verts.Add(Vector3.zero);
			for (int i = 0; i < segmentCount + 1; i++)
			{
				float angle = (float)i * segmentAngle;
				angle -= spread * 0.5f;
				Quaternion rot = Quaternion.AngleAxis(angle, Vector3.back);
				verts.Add( (rot * Vector3.right) * 1.02f);
			}

			//Tris
			for(int i=0; i<segmentCount; i++)
			{
				int vertOffset = 1 + i;

				tris.Add(0);
				tris.Add(i + 1);
				tris.Add(i + 2);
			}

			//Generate mesh
			var mesh = new Mesh();
			mesh.SetVertices(verts);
			mesh.SetTriangles(tris, 0);
			return mesh;
		}
		public void SetMesh(Mesh newMesh)
		{
			if (mesh != null)
				UnityEngine.Object.DestroyImmediate(mesh);
			mesh = newMesh;
		}
	}
}