#ifndef NODE_GRAVITY
#define NODE_GRAVITY

#include "./Gravity_Common.hlsl"

void GetTotalGravityForceAndSignedDistance_float(in float2 Position, in float Smoothing, in float ColourSmoothing, out float2 GravityForce, out float2 Gradient, out float SignedDistance, out float4 Colour)
{
	float3 forceAndSignedDistance = Map(Position, Smoothing, ColourSmoothing, Colour);
    
	GravityForce = forceAndSignedDistance.xy;
	SignedDistance = forceAndSignedDistance.z;
	Gradient = MapGradient(Position);
    
	if (any(isnan(GravityForce)))
		GravityForce = float2(0, 0);
}

float2 safeNormalize(float2 vec)
{
	vec = normalize(vec);
	
	if (any(isnan(vec)))
	{
		return float2(0, 0);
	}
	
	return vec;
}

void GetGravityPolarity_float(in float2 Gravity, in float2 Gradient, out float Polarity)
{
	Gravity = safeNormalize(Gravity);
	Gradient = safeNormalize(Gradient);
	
	Polarity = dot(Gravity, Gradient);
}

#endif // NODE_GRAVITY