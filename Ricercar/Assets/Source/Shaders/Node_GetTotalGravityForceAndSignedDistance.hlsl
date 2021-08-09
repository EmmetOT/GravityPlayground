#ifndef NODE_GRAVITY
#define NODE_GRAVITY

#include "./Gravity_Common.hlsl"

void GetTotalGravityForceAndSignedDistance_float(in float2 Position, in float Smoothing, in float ColourSmoothing, out float2 GravityForce, out float2 Gradient, out float SignedDistance, out float4 Colour)
{
    float3 forceAndSignedDistance = Map(Position, Smoothing, ColourSmoothing, Colour);
    
    GravityForce = forceAndSignedDistance.xy;
    SignedDistance = forceAndSignedDistance.z;
    Gradient = MapGradient(Position);
}

#endif // NODE_GRAVITY