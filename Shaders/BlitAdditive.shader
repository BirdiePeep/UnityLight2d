Shader "Bird/Light2D/BlitAdditive"
{
	Properties
	{
		[PerRendererData] _MainTex("Main Texture", 2D) = "white" {}
		[PerRendererData] _BlurParams("Blur Params", Vector) = (0, 0, 0, 0)
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
		CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 blur9(sampler2D image, float2 uv, float2 resolution, float2 direction)
			{
			  float4 color = float4(0, 0, 0, 0);
			  float2 off1 = float2(1.3846153846f, 1.3846153846f) * direction;
			  float2 off2 = float2(3.2307692308f, 3.2307692308f) * direction;
			  color += tex2D(image, uv) * 0.2270270270f;
			  color += tex2D(image, uv + (off1 * resolution)) * 0.3162162162f;
			  color += tex2D(image, uv - (off1 * resolution)) * 0.3162162162f;
			  color += tex2D(image, uv + (off2 * resolution)) * 0.0702702703f;
			  color += tex2D(image, uv - (off2 * resolution)) * 0.0702702703f;
			  return color;
			}

			sampler2D _MainTex;
			float4 _BlurParams;
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float2 texcoord : TEXCOORD1;
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

				/*if(_BlurParams.x == 1)
				{
					fixed4 c = blur9(_MainTex, IN.texcoord, _BlurParams.yz, float2(1, 0));

					return c;
				}
				else
				{
					fixed4 c = tex2D(_MainTex, IN.texcoord);
					return c;
				}*/			
			}
		ENDCG
		}
	}
}
