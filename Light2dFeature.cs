using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Experimental.Rendering.LWRP;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System;

namespace Bird.Light2D
{
	public class Light2dFeature : ScriptableRendererFeature
	{
		public static Light2dFeature inst;

		//Data
		[NonSerialized] public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
		[NonSerialized] public Mesh fullscreenMesh;
		[NonSerialized] public RenderTexture shadowMapFinal;

		public LayerMask colliderLayerMask = 1;

		public int shadowResolution = 2048;
		public int maxShadowMaps = 64;
		//public bool enableBlur = false;

		//Meta
		Light2dPass lightingPass;

		public Light2dFeature()
		{
			inst = this;
		}
		public override void Create()
		{
			if (lightingPass == null)
				lightingPass = new Light2dPass(this);	
		}
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			renderer.EnqueuePass(lightingPass);
		}

		public static List<LightBase> lights = new List<LightBase>();
		public static void OnLightEnable(LightBase light)
		{
			lights.Add(light);
		}
		public static void OnLightDisable(LightBase light)
		{
			lights.Remove(light);
		}
		
		public static Mesh CreateFullscreenRenderMesh()
		{
			var verts = new Vector3[4];
			var uvs0 = new Vector2[4];
			var indices = new int[6];

			verts[0] = new Vector3(-1.0f, +1.0f, 0.0f);
			verts[1] = new Vector3(+1.0f, +1.0f, 0.0f);
			verts[2] = new Vector3(+1.0f, -1.0f, 0.0f);
			verts[3] = new Vector3(-1.0f, -1.0f, 0.0f);

			uvs0[0] = new Vector2(0.0f, 0.0f);
			uvs0[1] = new Vector2(1.0f, 0.0f);
			uvs0[2] = new Vector2(1.0f, 1.0f);
			uvs0[3] = new Vector2(0.0f, 1.0f);

			indices[0] = 0;
			indices[1] = 1;
			indices[2] = 2;
			indices[3] = 0;
			indices[4] = 2;
			indices[5] = 3;

			Mesh mesh = new Mesh();
			mesh.SetVertices(verts);
			mesh.SetUVs(0, uvs0);
			mesh.SetIndices(indices, MeshTopology.Triangles, 0);
			return mesh;
		}
	}
}
