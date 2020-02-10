// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Bird/Light2D/ShadowMap"
{
	Properties
	{
		//[PerRendererData] _LightPosition("LightPosition", Vector) = (0,0,0,0)
		//[PerRendererData] _ShadowMapV("ShadowMapParams", Vector) = (0,0,0,0)
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		Blend One One
		BlendOp Min

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "ShadowMap1D.hlsl"

			float4 _LightPosition;			// xy is the position, z is the angle in radians, w is half the viewcone in radians
			float4 _ShadowMapParams;		// this is the row to write to in the shadow map. x is used to write, y to read.

			float Intersect(float2 lineOneStart, float2 lineOneEnd, float2 lineTwoStart, float2 lineTwoEnd)
			{
				float2 line2Perp = float2(lineTwoEnd.y - lineTwoStart.y, lineTwoStart.x - lineTwoEnd.x);
				float line1Proj = dot(lineOneEnd - lineOneStart, line2Perp);

				if (abs(line1Proj) < 1e-4)
					return 0.0f;

				float t1 = dot(lineTwoStart-lineOneStart,line2Perp ) / line1Proj;
				return t1;
			}

			struct appdata
			{
				float3 vertex1 : POSITION;
				float2 vertex2 : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;	
				float4 edge   : TEXCOORD0;		// xy=edgeVertex1,yz=edgeVertex2
				float2 data	  : TEXCOORD1;		// x=angle, y=selfShadow
			};

			v2f vert (appdata input)
			{
				float2 vert1 = mul(unity_ObjectToWorld, float4(input.vertex1.xy, 0, 1)).xy;
				float2 vert2 = mul(unity_ObjectToWorld, float4(input.vertex2.xy, 0, 1)).xy;

				float polar1 = ToPolarAngle(vert1, _LightPosition.xy);
				float polar2 = ToPolarAngle(vert2, _LightPosition.xy);

				v2f output;
				output.edge = float4(vert1, vert2);
				output.edge = lerp(output.edge, output.edge.zwxy, step(polar1,polar2));
				
				float diff = abs(polar1-polar2);
				if (diff >= MATH_PI)
				{ 
					float maxAngle = max(polar1,polar2);
					if (polar1 == maxAngle)
					{
						polar1 = maxAngle + 2 * MATH_PI - diff;
					}
					else
					{
						polar1 = maxAngle;
					}
				}

				float2 normal = TransformObjectToWorldDir(input.normal).xy;
				float selfShadow = saturate(dot(_LightPosition.xy - vert1, normal));

				output.vertex = float4(PolarAngleToClipSpace(polar1), _ShadowMapParams.y, 0.0f, 1.0f);
				output.data = float2(polar1, selfShadow);
				return output;
			}
		
			float4 frag (v2f input) : SV_Target
			{
				float angle = input.data.x;
				//if (AngleDiff(angle, _LightPosition.z) > _LightPosition.w)
				//	return float4(0,0,0,0);
				
				float2 realEnd = _LightPosition.xy + float2(cos(angle) * _LightPosition.z, sin(angle) * _LightPosition.z);
				float t = Intersect(_LightPosition.xy, realEnd, input.edge.xy, input.edge.zw);

				//Self shadow
				t = max(input.data.y, t);
				
				//Returns the normalized distance from the light center to the intersection point, inside of the light radius
				return float4(t,t,t,t);
			}
			ENDHLSL
		}
	}
}
