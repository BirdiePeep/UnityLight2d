Shader "Bird/Light2D/Sprite Lit"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
		_NormalsTex("Normals Texture", 2D) = "white" {}
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	ENDHLSL

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
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _NormalsTex;
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};
			
			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				return OUT;
			}
			
			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, IN.texcoord);
				return c;		
			}
		ENDCG
		}
		//Normals rendering
		Pass
		{
			Tags { "LightMode" = "NormalsRendering"}
			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma vertex NormalsRenderingVertex
			#pragma fragment NormalsRenderingFragment

			struct Attributes
			{
				float3 positionOS   : POSITION;
				float4 color		: COLOR;
				float2 uv			: TEXCOORD0;
				float4 tangent      : TANGENT;
			};

			struct Varyings
			{
				float4  positionCS		: SV_POSITION;
				float4  color			: COLOR;
				float2	uv				: TEXCOORD0;
				float3  normalWS		: TEXCOORD1;
				float3  tangentWS		: TEXCOORD2;
				float3  bitangentWS		: TEXCOORD3;
			};

			sampler2D _MainTex;
			sampler2D _NormalsTex;

			float4 NormalsRenderingShared(float4 color, float3 normalTS, float3 tangent, float3 bitangent, float3 normal)
			{
				float4 normalColor;
				float3 normalWS = TransformTangentToWorld(normalTS, float3x3(tangent.xyz, bitangent.xyz, normal.xyz));
				float3 normalVS = TransformWorldToViewDir(normalWS);

				normalColor.rgb = 0.5 * ((normalVS)+1);
				normalColor.a = color.a;  // used for blending

				return normalColor;
			}

			Varyings NormalsRenderingVertex(Attributes attributes)
			{
				Varyings o = (Varyings)0;

				o.positionCS = TransformObjectToHClip(attributes.positionOS);
				o.uv = attributes.uv;
				o.uv = attributes.uv;
				o.color = attributes.color;
				o.normalWS = TransformObjectToWorldDir(float3(0, 0, -1));
				o.tangentWS = TransformObjectToWorldDir(attributes.tangent.xyz);
				o.bitangentWS = cross(o.normalWS, o.tangentWS) * attributes.tangent.w;
				return o;
			}

			float4 NormalsRenderingFragment(Varyings i) : SV_Target
			{
				float4 mainTex = i.color * tex2D(_MainTex, i.uv);
				float3 normalTS = UnpackNormal(tex2D(_NormalsTex, i.uv));
				return NormalsRenderingShared(mainTex, normalTS, i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);
			}
			ENDHLSL
		}
	}
}
