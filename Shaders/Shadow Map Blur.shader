Shader "Bird/Light2D/ShadowMapBlur"
{
	Properties
	{
		//[PerRendererData] _ShadowMap ("Texture", 2D) = "white" {}
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
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Blur.hlsl"
			
			struct appdata_t
			{
				float4 vertex    : POSITION;
				float2 texcoord : TEXCOORD0; 
			};

			appdata_t vert(appdata_t IN)
			{
				return IN;
			}

			sampler2D _ShadowMap;

			fixed4 frag(appdata_t IN) : SV_Target
			{
				float4 color = float4(0, 0, 0, 0);
				color += tex2D(_ShadowMap, IN.texcoord + float2(gaussFilter[0].x*pixelToUV, 0.0f)) * gaussFilter[0].y;
				color += tex2D(_ShadowMap, IN.texcoord + float2(gaussFilter[1].x*pixelToUV, 0.0f)) * gaussFilter[1].y;
				color += tex2D(_ShadowMap, IN.texcoord + float2(gaussFilter[2].x*pixelToUV, 0.0f)) * gaussFilter[2].y;
				color += tex2D(_ShadowMap, IN.texcoord + float2(gaussFilter[3].x*pixelToUV, 0.0f)) * gaussFilter[3].y;
				color += tex2D(_ShadowMap, IN.texcoord + float2(gaussFilter[4].x*pixelToUV, 0.0f)) * gaussFilter[4].y;
				color += tex2D(_ShadowMap, IN.texcoord + float2(gaussFilter[5].x*pixelToUV, 0.0f)) * gaussFilter[5].y;
				color += tex2D(_ShadowMap, IN.texcoord + float2(gaussFilter[6].x*pixelToUV, 0.0f)) * gaussFilter[6].y;

				//color.g = color.r;
				//color.r = tex2D(_ShadowMap, IN.texcoord);

				return color;
			}


		ENDCG
		}
	}
}
