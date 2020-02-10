#ifndef LIGHT2D_NORMALS_HLSL
#define LIGHT2D_NORMALS_HLSL

inline float4 CalcNormalsLighting(float3 normal, float2 lightPos, float2 fragPos, float lightRadius)
{
	float3 dirToLight;
	dirToLight.xy = lightPos - fragPos;
	dirToLight.z = lightRadius;
	dirToLight = normalize(dirToLight);
	return saturate(dot(dirToLight, normal));
}

#endif