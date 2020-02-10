#include "Core.hlsl"
#include "Blur.hlsl"

// General functions for 1D shadow mapping
//
//

inline float ToPolarAngle(float2 cartesian, float2 center)
{
	float2 d = cartesian - center;
    return atan2(d.y, d.x);
}

inline float2 ToPolar(float2 cartesian, float2 center)
{
	float2 d = cartesian - center;
    return float2(atan2(d.y, d.x), length(d));
}

// convert from (-PI to +2PI) to (-1,+1)
// the 3PI range is the normal 2PI plus another PI to deal
// with wrap around e.g if a span goes from say 350 to 10 degrees
// (20 degrees shortest path) it would require splitting the span
// into 2 parts, 350-360 and 0-10, which is not possible in a vertex
// shader (maybe a geometry shader would be fine). Instead we make the
// span go from 350-370 and then when sampling from 0-PI you must
// also sample from 2PI to 3PI and take the min to resolve the
// wraparound.
inline float PolarAngleToClipSpace(float a)
{
	a += MATH_PI;
	a *= 2.0f/(MATH_PI*3.0f);
	a -= 1.0f;			
	return a;
}
// convert from (-PI to +PI) to (0,2/3)
// The final (1/3) is the wraparound as discussed above.
// if the returned angle is < 1/3 you should sample
// again with 2/3 added on and take the min.
inline float PolarAngleToShadowTextureLookup(float a)
{
	a += MATH_PI;
	a *= 1.0f /(MATH_PI*2.0f);
	return a;
}

// Takes a single sample from the shadow texture. Actually
// somtimes two samples are done internally to handle angle wrap around.
inline float SampleShadow1TapPreOptimise(sampler2D textureSampler, float u, float v)
{
	float sample = tex2D(textureSampler, float2(min(u,2.0f/3.0f), v)).r;
	if (u < 1.0f / 3.0f) 
	{
		sample = min(sample,tex2D(textureSampler, float2(u + (2.0f / 3.0f), v)).r);
	}
	return sample;
}



// Takes a single sample from the shadow texture.
inline float SampleShadow1Tap(sampler2D textureSampler, float u, float v)
{
	float sample = tex2D(textureSampler, float2(u, v)).r;
	return sample;
}

inline float SampleShadowTexture(sampler2D textureSampler, float angle, float v)
{
	return SampleShadow1Tap(textureSampler,PolarAngleToShadowTextureLookup(angle),v);
}

inline float SampleShadowCustom(sampler2D textureSampler, float dist, float2 texcoord, float offsetU)
{
	float value = step(dist, SampleShadow1Tap(textureSampler, texcoord.x + (offsetU*pixelToUV), texcoord.y));
	if(value > 0)
		return 10;
	else
	{
		return abs(offsetU);

	}		
}
inline float SampleShadowTextureCustom(sampler2D textureSampler, float2 samplePos, float v)
{
	float u1 = PolarAngleToShadowTextureLookup(samplePos.x);
	float2 texcoord = float2(u1, v);

	//float2 moments = tex2D(textureSampler, texcoord).rg;
	//if(samplePos.y <= moments.x)
	//	return 1.0f;

	float result = 10.0f;
	result = min(result, SampleShadowCustom(textureSampler, samplePos.y, texcoord, gaussFilter[0].x));
	result = min(result, SampleShadowCustom(textureSampler, samplePos.y, texcoord, gaussFilter[1].x));
	result = min(result, SampleShadowCustom(textureSampler, samplePos.y, texcoord, gaussFilter[2].x));
	result = min(result, SampleShadowCustom(textureSampler, samplePos.y, texcoord, gaussFilter[3].x));
	result = min(result, SampleShadowCustom(textureSampler, samplePos.y, texcoord, gaussFilter[4].x));
	result = min(result, SampleShadowCustom(textureSampler, samplePos.y, texcoord, gaussFilter[5].x));
	result = min(result, SampleShadowCustom(textureSampler, samplePos.y, texcoord, gaussFilter[6].x));

	if(result > 5)
		return 1.0f;

	result = result / 4.0f;

	return result;
}

inline float SampleShadowTexturePCF(sampler2D textureSampler, float2 samplePos, float v)
{
	float u1 = PolarAngleToShadowTextureLookup(samplePos.x);

	float total = 0.0f;
	total += step(samplePos.y, SampleShadow1Tap(textureSampler, u1 + (gaussFilter[0].x*pixelToUV), v)) * gaussFilter[0].y;
	total += step(samplePos.y, SampleShadow1Tap(textureSampler, u1 + (gaussFilter[1].x*pixelToUV), v)) * gaussFilter[1].y;
	total += step(samplePos.y, SampleShadow1Tap(textureSampler, u1 + (gaussFilter[2].x*pixelToUV), v)) * gaussFilter[2].y;
	total += step(samplePos.y, SampleShadow1Tap(textureSampler, u1 + (gaussFilter[3].x*pixelToUV), v)) * gaussFilter[3].y;
	total += step(samplePos.y, SampleShadow1Tap(textureSampler, u1 + (gaussFilter[4].x*pixelToUV), v)) * gaussFilter[4].y;
	total += step(samplePos.y, SampleShadow1Tap(textureSampler, u1 + (gaussFilter[5].x*pixelToUV), v)) * gaussFilter[5].y;
	total += step(samplePos.y, SampleShadow1Tap(textureSampler, u1 + (gaussFilter[6].x*pixelToUV), v)) * gaussFilter[6].y;

	return total;

	/*float u1 = PolarAngleToShadowTextureLookup(samplePos.x);
		
	//float u2 = u1-2*(1.0f/1024.0f);
	float u3 = u1-1*(1.0f/1024.0f);
	float u4 = u1+1*(1.0f/1024.0f);
	//float u5 = u1+2*(1.0f/1024.0f);

	float total = 0.0f;
	total += step(samplePos.y,SampleShadow1Tap(textureSampler,u1,v) * 10);
	//total += step(samplePos.y,SampleShadow1Tap(textureSampler,u2,v) * 10);
	total += step(samplePos.y,SampleShadow1Tap(textureSampler,u3,v) * 10);
	total += step(samplePos.y,SampleShadow1Tap(textureSampler,u4,v) * 10);
	//total += step(samplePos.y,SampleShadow1Tap(textureSampler,u5,v) * 10);

	return total / 3.0f;*/
}

// Returns the shortest angle arc between a and b (all angles in radians)
inline float AngleDiff(float a, float b)
{
	float diff = fmod(abs(a-b),2 * MATH_PI);
	if (diff > MATH_PI)
	diff = 2 * MATH_PI - diff;
	return diff;
}

inline float2 ClipSpaceToUV(float2 clipSpace)
{
	#if UNITY_UV_STARTS_AT_TOP
	float4 scale = float4(0.5f,0.5f,0.5f,0.5f);
	#else
	float4 scale = float4(0.5f,-0.5f,0.5f,0.5f);
	#endif
	return clipSpace * scale.xy + scale.zw;
}

inline float SmoothWrap(float value, float range)
{
	return fmod(value-range, range*2.0f)+range;
}

float ChebyshevUpperBound(sampler2D shadowMap, float distance, float2 texcoord)
{
	float2 moments = tex2D(shadowMap, texcoord).rg;

	// Surface is fully lit. as the current fragment is before the light occluder
	if(distance < moments.x)
		return 1.0f;

	// The fragment is either in shadow or penumbra. We now use chebyshev's upperBound to check
	// How likely this pixel is to be lit (p_max)
	float variance = moments.y - (moments.x * moments.x);
	variance = max(variance, 0.00002f);

	float d = distance - moments.x;
	float p_max = variance / (variance + (d * d));

	return p_max;
}
inline float SampleShadowTextureVSM(sampler2D textureSampler, float2 samplePos, float v)
{
	float u = PolarAngleToShadowTextureLookup(samplePos.x);
	return ChebyshevUpperBound(textureSampler, samplePos.y, float2(u, v));
}