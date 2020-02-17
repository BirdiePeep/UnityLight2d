using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace Bird.Light2D
{
	public class Light2dPass : ScriptableRenderPass
	{
		Light2dFeature feature;

		//Render data
		Material shadowMaterial;
		Material shadowOptimizedMaterial;
		Material lightMaterial;
		Material blitAdditive;

		RenderTargetHandle lightBuffer;
		MaterialPropertyBlock lightPropBlock;
		RenderTargetHandle colorBuffer;
		RenderTargetHandle shadowBuffer;
		RenderTargetHandle shadowOptBuffer;

		RenderTargetHandle normalsBuffer;
		static readonly Color NormalsClearColor = new Color(0.5f, 0.5f, 1.0f, 1.0f);
		static readonly ShaderTagId NormalsRenderingPassName = new ShaderTagId("NormalsRendering");

		static RenderTextureFormat RenderTextureFormat = RenderTextureFormat.ARGB32;

		List<LightBase> visibleLights = new List<LightBase>();

		public Light2dPass(Light2dFeature feature)
		{
			this.feature = feature;
			this.renderPassEvent = feature.renderPassEvent;
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			//Materials
			if(shadowMaterial == null)
				shadowMaterial = new Material(Shader.Find("Bird/Light2D/ShadowMap"));
			if (shadowOptimizedMaterial == null)
				shadowOptimizedMaterial = new Material(Shader.Find("Bird/Light2D/ShadowMapOptimise"));
			if (lightMaterial == null)
				lightMaterial = new Material(Shader.Find("Bird/Light2D/Light"));
			if (blitAdditive == null)
				blitAdditive = new Material(Shader.Find("Bird/Light2D/BlitAdditive"));

			//Fullscreen mesh
			if (feature.fullscreenMesh == null)
				feature.fullscreenMesh = Light2dFeature.CreateFullscreenRenderMesh();

			if (lightBuffer.id == 0)
				lightBuffer.Init("_LightMap");
			if (colorBuffer.id == 0)
				colorBuffer.Init("_CameraColorTexture");
			if (shadowBuffer.id == 0)
				shadowBuffer.Init("_ShadowMap");
			if (shadowOptBuffer.id == 0)
				shadowOptBuffer.Init("_ShadowOptMap");

			//Normals
			if (normalsBuffer.id == 0)
				normalsBuffer.Init("_NormalMap");
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			//Find current scene
			var scene = renderingData.cameraData.camera.scene;
			if (!scene.IsValid())
				scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
			var physicsScene = scene.GetPhysicsScene2D();

			//Find visible lights
			FindVisibleLights(renderingData.cameraData.camera, scene.handle, physicsScene);
			if (visibleLights.Count == 0)
				return;

			//Begin render pass
			var cmd = CommandBufferPool.Get("Light2D");
			cmd.Clear();

			//Draw normals buffer
			ref var targetDescriptor = ref renderingData.cameraData.cameraTargetDescriptor;
			{
				RenderTextureDescriptor descriptor = new RenderTextureDescriptor(targetDescriptor.width, targetDescriptor.height);
				descriptor.colorFormat = RenderTextureFormat;;
				descriptor.sRGB = false;
				descriptor.useMipMap = false;
				descriptor.autoGenerateMips = false;
				descriptor.depthBufferBits = 0;
				descriptor.msaaSamples = 1;
				descriptor.dimension = TextureDimension.Tex2D;

				//Ask for temp buffer
				cmd.GetTemporaryRT(normalsBuffer.id, descriptor, FilterMode.Bilinear);

				//Draw
				DrawNormals(cmd, context, ref renderingData);
			}

			//Setup shadow map rendering
			{
				//Ask for shadow buffer
				RenderTextureDescriptor descriptor = new RenderTextureDescriptor(feature.shadowResolution, feature.maxShadowMaps);
				descriptor.colorFormat = RenderTextureFormat.RHalf;
				descriptor.sRGB = false;
				descriptor.useMipMap = false;
				descriptor.autoGenerateMips = false;
				descriptor.depthBufferBits = 0;
				descriptor.msaaSamples = 1;
				descriptor.dimension = TextureDimension.Tex2D;

				//Ask for shadow buffers
				cmd.GetTemporaryRT(shadowBuffer.id, descriptor, FilterMode.Point);
				cmd.GetTemporaryRT(shadowOptBuffer.id, descriptor, FilterMode.Point);
			}

			//Set shadow buffer as target
			cmd.SetRenderTarget(shadowBuffer.id);
			cmd.ClearRenderTarget(true, true, Color.white);

			//Render each light's shadow texture
			for (int i=0; i< visibleLights.Count; i++)
			{
				var light = visibleLights[i];
				light.shadowMapParams = GetShadowMapParams(i);

				//cmd.SetGlobalVector("_LightPosition", new Vector4(light.transform.position.x, light.transform.position.y, light.angle * Mathf.Deg2Rad, light.spread * Mathf.Deg2Rad * 0.5f));
				cmd.SetGlobalVector("_LightPosition", new Vector4(light.transform.position.x, light.transform.position.y, light.radius, 0.0f));
				cmd.SetGlobalVector("_ShadowMapParams", light.shadowMapParams);

				//feature.shadowMaterial.SetVector("_LightPosition", new Vector4(light.transform.position.x, light.transform.position.y, light.angle * Mathf.Deg2Rad, light.spread * Mathf.Deg2Rad * 0.5f));
				//feature.shadowMaterial.SetVector("_ShadowMapParams", light.shadowMapParams);

				//Draw each collider
				foreach (var collider in light.colliders)
				{
					collider.UpdateMesh();
					var mesh = collider.GetMesh();
					if (mesh != null)
					{
						cmd.DrawMesh(mesh, collider.transform.localToWorldMatrix, shadowMaterial);
					}
				}
			}

			//Optimize the shadow texture
			{
				//feature.shadowOptimizedMaterial.SetTexture("_MainTex", shadowBuffer);
				cmd.SetRenderTarget(shadowOptBuffer.id);
				cmd.SetGlobalTexture("_MainTex", shadowBuffer.id);
				cmd.DrawMesh(feature.fullscreenMesh, Matrix4x4.identity, shadowOptimizedMaterial);
			}

			//Draw lights
			{
				//Setup render texture
				RenderTextureDescriptor descriptor = new RenderTextureDescriptor(targetDescriptor.width, targetDescriptor.height);
				descriptor.colorFormat = RenderTextureFormat.ARGBHalf;
				descriptor.sRGB = false;
				descriptor.useMipMap = false;
				descriptor.autoGenerateMips = false;
				descriptor.depthBufferBits = 0;
				descriptor.msaaSamples = 1;
				descriptor.dimension = TextureDimension.Tex2D;

				//Create & Set
				cmd.GetTemporaryRT(lightBuffer.id, descriptor, FilterMode.Bilinear);
				cmd.SetRenderTarget(lightBuffer.id);
				cmd.ClearRenderTarget(false, true, Color.black);
				cmd.SetGlobalTexture("_NormalMap", normalsBuffer.id);
				cmd.SetGlobalTexture("_ShadowTex", shadowOptBuffer.id);

				//Draw each light
				foreach (var light in visibleLights)
				{
					//Properties
					if(lightPropBlock == null)
						lightPropBlock = new MaterialPropertyBlock();

					lightPropBlock.SetColor("_Color", light.color * light.intensity);
					lightPropBlock.SetVector("_LightPosition", new Vector4(
						light.transform.position.x,
						light.transform.position.y,
						light.angle * Mathf.Deg2Rad,
						light.radius));
					lightPropBlock.SetVector("_DistFalloff", new Vector4(
						light.innerRadius * light.radius,
						Mathf.Max(0.00001f, light.falloffExponent),  //Apply max, as a pow(x,0) results in nan on some platforms
						1.0f / ((1.0f - light.innerRadius) * light.radius),
						0));
					lightPropBlock.SetVector("_AngleFalloff", new Vector4(
						light.spread * Mathf.Deg2Rad * 0.5f,
						Mathf.Max(0.00001f, light.falloffExponent),
						1.0f / Mathf.Max(0.00001f, light.innerAngle),
						0));
					lightPropBlock.SetVector("_ShadowMapParams", new Vector4(
						light.shadowMapParams.x,
						light.shadowMapParams.y,
						1.0f - light.shadowStrength,
						0f));

					//Draw
					var matrix = Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one*light.radius);
					cmd.DrawMesh(light.mesh, matrix, lightMaterial, 0, 0, lightPropBlock);
				}
			}

			//Blur for soft shadows
			/*if(feature.enableBlur)
			{
				//cmd.SetRenderTarget(feature.shadowBlurTexture);
				//cmd.SetGlobalTexture("_ShadowMap", feature.shadowOptimizedTexture);
				//cmd.DrawMesh(feature.fullscreenMesh, Matrix4x4.identity, feature.shadowBlurMaterial);
				//feature.shadowMapFinal = feature.shadowBlurTexture;

				float blurRadius = 2.0f;
				Vector4 blurParams = new Vector4(1, blurRadius / targetDescriptor.width, blurRadius / targetDescriptor.height, 0);
				feature.blitAdditive.SetVector("_BlurParams", blurParams);

				//Blur horizontal

				//Blur vertical

			}
			else
			{
				Vector4 blurParams = new Vector4(0, 0, 0, 0);
				feature.blitAdditive.SetVector("_BlurParams", blurParams);
			}*/

			//Transfer back to the color buffer
			cmd.SetRenderTarget(colorBuffer.id);
			cmd.Blit(lightBuffer.id, colorBuffer.id, blitAdditive);
			cmd.ReleaseTemporaryRT(lightBuffer.id);
			cmd.ReleaseTemporaryRT(normalsBuffer.id);
			cmd.ReleaseTemporaryRT(shadowBuffer.id);
			cmd.ReleaseTemporaryRT(shadowOptBuffer.id);

			//Render
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public void DrawNormals(CommandBuffer cmd, ScriptableRenderContext context, ref RenderingData renderingData)
		{
			//Settings
			var drawSettings = CreateDrawingSettings(NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

			FilteringSettings filterSettings = new FilteringSettings();
			filterSettings.renderQueueRange = RenderQueueRange.all;
			filterSettings.layerMask = -1;
			filterSettings.renderingLayerMask = 0xFFFFFFFF;
			filterSettings.sortingLayerRange = SortingLayerRange.all;

			//Setup normals buffer
			cmd.SetRenderTarget(normalsBuffer.id);
			cmd.ClearRenderTarget(true, true, NormalsClearColor);
			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();

			//Draw
			drawSettings.SetShaderPassName(0, NormalsRenderingPassName);
			context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
		}

		Plane[] cullPlanes = new Plane[6];
		public void FindVisibleLights(Camera camera, int sceneHandle, PhysicsScene2D scenePhysics)
		{
			GeometryUtility.CalculateFrustumPlanes(camera, cullPlanes);
			visibleLights.Clear();
			foreach(var light in Light2dFeature.lights)
			{
				if (light.gameObject.scene.handle != sceneHandle)
					continue;
				var bounds = light.GetBounds();
				if(!CullSphere(bounds))
					visibleLights.Add(light);
			}

			//Update colliders
			foreach(var light in visibleLights)
			{
				light.UpdateColliders(scenePhysics);
			}
		}
		public bool CullSphere(BoundingSphere sphere)
		{
			for(int i=0; i<6; i++)
			{
				//Check for sphere/plane collision
				Plane plane = cullPlanes[i];
				float distance = Vector3.Dot(-plane.normal, (-plane.normal * plane.distance) - sphere.position);
				if (distance < -sphere.radius)
					return true;
			}
			return false;
		}

		public Vector4 GetShadowMapParams(int slot)
		{
			float u1 = ((float)slot + 0.5f) / feature.maxShadowMaps;
			float u2 = (u1 - 0.5f) * 2.0f;

			if (   //(SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGL2) // OpenGL2 is no longer supported in Unity 5.5+
				   (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore)
				|| (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
				|| (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
				)
			{
				return new Vector4(u1, u2, 0.0f, 0.0f);
			}
			else
			{
				return new Vector4(1.0f - u1, u2, 0.0f, 0.0f);
			}
		}
	}
}

