#ifndef LIGHT2D_BLUR_HLSL
#define LIGHT2D_BLUR_HLSL

static const float2 gaussFilter[7] =
{
	float2(-6.0, 0.015625),
	float2(-4.0, 0.09375),
	float2(-2.0, 0.234375),
	float2(0.0,	0.3125),
	float2(2.0,	0.234375),
	float2(4.0,	0.09375),
	float2(6.0,	0.015625)
};
static const float pixelToUV = 1.0f / 2048.0f;

#endif
