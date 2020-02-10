// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Bird/Light2D/Light"
{
	Properties
	{
		//[PerRendererData] _ShadowTex ("Shadow Texture", 2D) = "white" {}
		[PerRendererData] _Color ("Color", Color) = (1,1,1,1)
		[PerRendererData] _LightPosition("LightPosition", Vector) = (0,0,1,0)
		[PerRendererData] _ShadowMapParams("ShadowMapParams", Vector) = (0,0,0,0)
		[PerRendererData] _DistFalloff("DistFalloff", Vector) = (0,0,0,0)
		[PerRendererData] _AngleFalloff("AngleFalloff", Vector) = (0,0,0,0)
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One One

		Pass
		{
		HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "ShadowMap1D.hlsl"
			#include "Normals.hlsl"

			sampler2D 	_ShadowTex;
			sampler2D 	_NormalMap;
			float4 		_LightPosition;
			float4 		_ShadowMapParams;
			float4 		_DistFalloff;
			float4		_AngleFalloff;
			float4 		_Color;

			//_LightPosition
			//x = x position
			//y = y position
			//z = angle
			//w = radius
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float2 screenUV : TEXCOORD1;
				float2 worldPos : TEXCOORD2;
			};
			
			v2f vert(appdata_t IN)
			{
				v2f output;
				output.vertex = TransformObjectToHClip(IN.vertex);
				output.worldPos = TransformObjectToWorld(IN.vertex).xy;

				float4 clipVertex = output.vertex / output.vertex.w;
				output.screenUV = (clipVertex.xy + 1.0f) * 0.5f;
				output.screenUV.y = 1.0f-output.screenUV.y;

				return output;
			}
			
			float4 frag(v2f IN) : SV_Target
			{
				float4 c = _Color;

				float2 polar = ToPolar(IN.worldPos.xy, _LightPosition.xy);
				polar.y = polar.y / _LightPosition.w;

				//Shadow
				float shadow = SampleShadowTexturePCF(_ShadowTex, polar, _ShadowMapParams.x);
				//float shadow = SampleShadowTextureVSM(_ShadowTex, polar, _ShadowMapParams.x);
				//float shadow = SampleShadowTextureCustom(_ShadowTex, polar, _ShadowMapParams.x);
				shadow = max(shadow, _ShadowMapParams.z); //Shadow intensity
				/*if (shadow < 0.5f)
				{
					clip( -1.0 );
					return c;
				}*/

				//Apply normals
				float3 normal = UnpackNormal(tex2D(_NormalMap, IN.screenUV.xy));
				c *= CalcNormalsLighting(normal, _LightPosition.xy, IN.worldPos, _LightPosition.w);
				
				//Distance falloff
				float distFalloff = max(0.0f, length(IN.worldPos.xy-_LightPosition.xy) - _DistFalloff.x) * _DistFalloff.z;
				distFalloff = clamp(distFalloff, 0.0f, 1.0f);
				distFalloff = 1.0f-distFalloff;

				//Angle falloff
				float angleFalloff = abs(SmoothWrap(polar.x - _LightPosition.z, MATH_PI)); //Apply the angle
				angleFalloff = 1.0f-min(angleFalloff / _AngleFalloff.x, 1.0f); //Apply the spread
				angleFalloff = min(1.0f, angleFalloff * _AngleFalloff.z); //Apply inner angle

				//Apply
				c.rgb *= pow(distFalloff * angleFalloff, _DistFalloff.y) * shadow;
				return c;
			}
			ENDHLSL
		}
	}
}
