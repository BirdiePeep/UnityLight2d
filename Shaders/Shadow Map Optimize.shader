Shader "Bird/Light2D/ShadowMapOptimise"
{
	Properties
	{
		//[PerRendererData] _MainTex("Main Texture", 2D) = "white" {}
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
		Blend One Zero

		Pass
		{
		HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "ShadowMap1D.hlsl"
			
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
				OUT.vertex = IN.vertex;
				OUT.texcoord = IN.texcoord;
				return OUT;
			}

			sampler2D _MainTex;

			static const float OneThird = 1.0f / 3.0f;
			static const float TwoThirds = 2.0f / 3.0f;

			float4 frag(v2f IN) : SV_Target
			{
				float u = IN.texcoord.x * TwoThirds;
				float v = IN.texcoord.y;
				float s = tex2D(_MainTex, float2(u, v)).r;
				if (u < OneThird)
				{
					s = min(s,tex2D(_MainTex, float2(u + TwoThirds, v)).r);
				}

				return float4(s,s,s,s);

				//VSM
				/*float depth = s;

				float moment1 = depth;
				float moment2 = depth * depth;

				//Adjusting moments (this is sort of bias per pixel) using derivative
				float dx = ddx(depth);
				float dy = ddx(depth);
				moment2 += 0.25*(dx*dx + dy * dy);

				return float4(moment1, moment2,s,s);*/
			}
		ENDHLSL
		}
	}
}
