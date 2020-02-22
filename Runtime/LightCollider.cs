using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace Bird.Light2D
{
	[ExecuteInEditMode]
	public class LightCollider : MonoBehaviour
	{
		public bool selfShadow
		{
			get
			{
				return mSelfShadow;
			}
			set
			{
				if (mSelfShadow != value)
				{
					mSelfShadow = value;
					meshNeedsBuilt = true;
				}
			}
		}
		[SerializeField][HideInInspector]
		private bool mSelfShadow = true;
		uint shapeHash = 0;

		Mesh mesh;
		bool meshNeedsBuilt = true;

		public virtual void OnEnable()
		{
			meshNeedsBuilt = true;
		}
		public virtual void OnDisable()
		{
			SetMesh(null);
		}

		public struct Edge
		{
			public Edge(Vector3 pointA, Vector3 pointB, Vector3 normal)
			{
				this.pointA = pointA;
				this.pointB = pointB;
				this.normal = normal;
			}

			public Vector3 pointA;
			public Vector3 pointB;
			public Vector3 normal;
		}
		
		public void DirtyCollider()
		{
			meshNeedsBuilt = true;
		}
		public void UpdateMesh()
		{
			//Check shape hash
			/*var collider = GetComponent<Collider2D>();
			var hash = collider.GetShapeHash();

			//Update
			if(meshNeedsBuilt || shapeHash != hash)
			{
				meshNeedsBuilt = false;
				shapeHash = hash;
				GenerateMesh();
			}*/

			if(meshNeedsBuilt)
			{
				meshNeedsBuilt = false;
				GenerateMesh();
			}
		}
		public static List<Collider2D> colliders = new List<Collider2D>();
		public virtual void GenerateMesh()
		{
			//Generate edges for the collider
			colliders.Clear();
			GetComponents<Collider2D>(colliders);

			//Generate edges for each collider
			edgeList.Clear();
			foreach(var collider in colliders)
			{
				//BoxCollider2D
				var box = collider as BoxCollider2D;
				if (box != null)
				{
					GenerateEdges(box, edgeList);
					continue;
				}
					
				//EdgeCollider2D
				var edge = collider as EdgeCollider2D;
				if (edge != null)
				{
					GenerateEdges(edge, edgeList);
					continue;
				}
					
				//Polygon
				var poly = collider as PolygonCollider2D;
				if (poly != null)
				{
					GenerateEdges(poly, edgeList);
					continue;
				}

				//Default
				if (collider != null)
				{
					var mesh = collider.CreateMesh(false, false);

					//Get geometry
					var verts = new List<Vector3>(mesh.vertices);
					var tris = new List<int>(mesh.triangles);
					var vertsReverse = new List<Vector2>();

					//Reverse to model space, because fuck Unity
					for (int i = 0; i < verts.Count; i++)
					{
						var prev = verts[i];
						var next = collider.transform.InverseTransformPoint(prev);
						verts[i] = next;
					}

					//Generate edge list
					GenerateEdges(verts, tris, edgeList);
				}
			}

			//Generate mesh
			if (edgeList.Count > 0)
				SetMesh(GenerateMesh(edgeList, !selfShadow));
			else
				SetMesh(null);
		}
		void SetMesh(Mesh newMesh)
		{
			if(mesh != null)
				UnityEngine.Object.DestroyImmediate(mesh);
			mesh = newMesh;
		}
		public Mesh GetMesh()
		{
			return mesh;
		}

		//Helper Methods
		public static List<Edge> edgeList = new List<Edge>();
		public static void GenerateEdges(BoxCollider2D collider, List<Edge> output)
		{
			var vert1 = collider.offset + new Vector2(collider.size.x * -0.5f, collider.size.y * -0.5f);
			var vert2 = collider.offset + new Vector2(collider.size.x * -0.5f, collider.size.y * 0.5f);
			var vert3 = collider.offset + new Vector2(collider.size.x * 0.5f, collider.size.y * 0.5f);
			var vert4 = collider.offset + new Vector2(collider.size.x * 0.5f, collider.size.y * -0.5f);

			output.Add(new Edge(vert1, vert2, Vector3.left));
			output.Add(new Edge(vert2, vert3, Vector3.up));
			output.Add(new Edge(vert3, vert4, Vector3.right));
			output.Add(new Edge(vert4, vert1, Vector3.down));
		}
		public static void GenerateEdges(EdgeCollider2D collider, List<Edge> output)
		{
			//Edges
			var points = collider.points;
			for (int i = 0; i < points.Length - 1; i++)
			{
				output.Add(new Edge(points[i], points[i + 1], Vector3.zero));
			}
		}
		public static void GenerateEdges(PolygonCollider2D collider, List<Edge> output)
		{
			List<Vector2> points = new List<Vector2>();
			for(int pathIter=0; pathIter < collider.pathCount; pathIter++)
			{
				//Get path
				points.Clear();
				collider.GetPath(pathIter, points);

				//Get edges
				for (int i = 0; i < points.Count; i++)
				{
					var point1 = points[i];
					var point2 = points[(i + 1) % points.Count];
					var normal = Vector3.Cross(Vector3.back, Vector3.Normalize(point2 - point1));

					output.Add(new Edge(point1, point2, normal));
				}
			}
		}
		public static void GenerateEdges(List<Vector3> verts, List<int> tris, List<Edge> output)
		{
			int triangleCount = tris.Count / 3;
			for(int i=0; i<triangleCount; i++)
			{
				int triOffset = i * 3;

				//Edge 1
				var edge = new Edge();
				edge.pointA = verts[tris[triOffset + 0]];
				edge.pointB = verts[tris[triOffset + 1]];
				output.Add(edge);

				//Edge 2
				edge = new Edge();
				edge.pointA = verts[tris[triOffset + 1]];
				edge.pointB = verts[tris[triOffset + 2]];
				output.Add(edge);

				//Edge 3
				edge = new Edge();
				edge.pointA = verts[tris[triOffset + 2]];
				edge.pointB = verts[tris[triOffset + 0]];
				output.Add(edge);
			}
		}
		public static Mesh GenerateMesh(List<Edge> edges, bool useNormals)
		{
			//Generate new geometry
			var verts = new Vector3[edges.Count * 2];
			var vertsB = new Vector2[edges.Count * 2];
			var normals = new Vector3[edges.Count * 2];
			var lines = new int[edges.Count * 2];
			for (int i = 0; i < edges.Count; i++)
			{
				var edge = edges[i];
				int vertIndex = (i * 2);

				//We extend the edges to avoid light bleeding
				float bounds = 0.01f;
				var normal = (edge.pointB - edge.pointA).normalized;

				//Point 1
				verts[vertIndex + 0] = edge.pointA + (-normal * bounds);
				vertsB[vertIndex + 0] = edge.pointB + (normal * bounds);
				normals[vertIndex + 0] = useNormals ? edge.normal : Vector3.zero;

				//Point 2
				verts[vertIndex + 1] = edge.pointB + (normal * bounds);
				vertsB[vertIndex + 1] = edge.pointA + (-normal * bounds);
				normals[vertIndex + 1] = useNormals ? edge.normal : Vector3.zero;

				//Line
				lines[vertIndex + 0] = (vertIndex + 0);
				lines[vertIndex + 1] = (vertIndex + 1);
			}

			//Configure
			Mesh mesh = new Mesh();
			mesh.SetVertices(verts);
			mesh.SetUVs(0, vertsB);
			mesh.SetNormals(normals);
			mesh.SetIndices(lines, MeshTopology.Lines, 0);
			return mesh;
		}
		/*public static Mesh GenerateMesh(Vector2[] edgeLoop, bool closeLoop)
		{
			//Generate new geometry
			var verts = new Vector3[edgeLoop.Length];
			var vertsB = new Vector2[edgeLoop.Length];
			var lines = new int[edgeLoop.Length * 2];
			for (int i = 0; i < edgeLoop.Length; i++)
			{
				int vertA = i;
				int vertB = (i + 1) % edgeLoop.Length;

				if (i == edgeLoop.Length-1 && !closeLoop)
					vertB = i - 1;

				//Points
				verts[i + 0] = edgeLoop[vertA];
				vertsB[i + 0] = edgeLoop[vertB];

				//Line
				lines[(i*2) + 0] = vertA;
				lines[(i*2) + 1] = vertB;
			}

			//Configure
			Mesh mesh = new Mesh();
			mesh.SetVertices(verts);
			mesh.SetUVs(0, vertsB);
			mesh.SetIndices(lines, MeshTopology.Lines, 0);
			return mesh;
		}*/
	}
}
