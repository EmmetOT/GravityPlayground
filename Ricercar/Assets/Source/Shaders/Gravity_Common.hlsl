#ifndef GRAVITY_COMMON
#define GRAVITY_COMMON

#include "./Common.hlsl"

#define GRAVITY_BODY_TYPE_SHELL 0
#define GRAVITY_BODY_TYPE_LINE 1
#define GRAVITY_BODY_TYPE_COMPLEX 2

#define EPSILON 0.00001

struct GravityData
{
    float4x4 Transform;
    float4x4 InverseTransform;
    int Type;
    float4 Data_1;
    float4 Data_2;
    float Mass;
    float4 Colour;
    float Bounciness;
    
    float Point_Radius()
    {
        return Data_1.x;
    }
    
    float Point_SurfaceGravity()
    {
        return Data_1.y;
    }
    
    float Point_SqrRadius()
    {
        return Data_1.z;
    }
    
    float2 Complex_TotalSize()
    {
        return float2(Data_1.y, Data_1.z);
    }
    
    float2 Complex_CentreOfGravity()
    {
        return float2(Data_2.x, Data_2.y);
    }
    
    int2 Complex_CellsPerSide()
    {
        return Complex_TotalSize() - 1;
    }
    
    float Complex_Scale()
    {
        return Data_1.w;

    }
    
    float2 Line_GetA()
    {
        return float2(Data_1.x, Data_1.y);
    }
    
    float2 Line_GetB()
    {
        return float2(Data_2.x, Data_2.y);
    }
    
    float Line_Width()
    {
        return Data_1.z;
    }
};

StructuredBuffer<float3> _ComplexSamples;
StructuredBuffer<GravityData> _GravityData;
int _GravityDataCount;
float _GravitationalConstant;
float _MaxForce;

float3 SampleComplexData(int2 coords, GravityData data)
{
    return _ComplexSamples[data.Data_1.x + (coords.x + coords.y * data.Complex_TotalSize().y)];
}

float2 Clamp(float2 p, GravityData data, float bounds)
{
    float2 transformed = mul(data.InverseTransform, float4(p, 0.0, 1.0)).xy;
    float2 clamped = float2(clamp(transformed.x, -bounds, bounds), clamp(transformed.y, -bounds, bounds));
    return mul(data.Transform, float4(clamped, 0.0, 1.0)).xy;
}

// Given a point anywhere in 2D space, return true if the point is within the 'baked' area of this complex body, as well as the gravitational force to the body and the signed distance.
bool GetForceAndSignedDistanceAtClosestPointInBounds(float2 p, GravityData data, out float2 force, out float signedDistance)
{
    // transform the point into the gravity body's local space
    p = mul(data.InverseTransform, float4(p, 0, 1)).xy;
    
    // get the point clamped inside the complex region
    // normalize the point to the range [0, 1] where (-0.5, -0.5) is the bottom left corner and (0.5, 0.5) is the top right corner
    float2 normalizedPoint = float2
    (
        invLerp(-0.5, 0.5, clamp(p.x, -0.5f, 0.5f)),
        invLerp(-0.5, 0.5, clamp(p.y, -0.5f, 0.5f))
    );
    
    int2 cellsPerSide = data.Complex_CellsPerSide();
            
    int2 coords = int2(min(cellsPerSide.x - 1, floor(normalizedPoint.x * cellsPerSide.x)), min(cellsPerSide.y - 1, floor(normalizedPoint.y * cellsPerSide.y)));
    float2 fracs = float2(frac(normalizedPoint.x * cellsPerSide.x), frac(normalizedPoint.y * cellsPerSide.y));

    float3 sampleA = SampleComplexData(coords, data);
    float3 sampleB = SampleComplexData(coords + int2(1, 0), data);
    float3 sampleC = SampleComplexData(coords + int2(0, 1), data);
    float3 sampleD = SampleComplexData(coords + int2(1, 1), data);

    float3 interpolated = BilinearInterpolate(fracs, sampleA, sampleB, sampleC, sampleD);

    float scale = data.Complex_Scale();
    
    signedDistance = interpolated.z * scale;
    force = mul(data.Transform, float4(interpolated.xy, 0.0, 0.0)).xy;
    
    force *= (1.0 / (scale * scale * scale));

    return p.x >= -0.5 && p.x <= 0.5 && p.y >= -0.5 && p.y <= 0.5;
}

float2 GetVector(float2 p, GravityData data)
{
    // we need to sample the gradient slightly inside the baked area,
    // as there has to be distance information all around the sample point to get a good result
    const float smallBounds = 0.48;
    
    float2 ignore;

    float2 vecToBounds = Clamp(p, data, 0.5) - p;
    
    float2 directionInBakedArea;
    
    const float e = 0.01;
        
    float2 clampedP = Clamp(p, data, smallBounds);

    float2 xy = float2(e, -e);
    float2 yy = float2(-e, -e);
    float2 yx = float2(-e, e);
    float2 xx = float2(e, e);
    
    float xyDist;
    float yyDist;
    float yxDist;
    float xxDist;
    
    GetForceAndSignedDistanceAtClosestPointInBounds(clampedP + xy, data, ignore, xyDist);
    GetForceAndSignedDistanceAtClosestPointInBounds(clampedP + yy, data, ignore, yyDist);
    GetForceAndSignedDistanceAtClosestPointInBounds(clampedP + yx, data, ignore, yxDist);
    GetForceAndSignedDistanceAtClosestPointInBounds(clampedP + xx, data, ignore, xxDist);

    directionInBakedArea = normalize(
            xy * xyDist +
            yy * yyDist +
            yx * yxDist +
            xx * xxDist);
    
    float signedDistance;
    GetForceAndSignedDistanceAtClosestPointInBounds(p, data, ignore, signedDistance);
            
    float2 gradient = -directionInBakedArea;
    float2 vecInBounds = gradient * signedDistance;
    return vecToBounds + vecInBounds;
}

float3 GetComplexForceAndSignedDistance(float2 p, GravityData data)
{
    float2 scale = data.Complex_Scale();
    
    float2 force;
    float signedDistance;
    if (GetForceAndSignedDistanceAtClosestPointInBounds(p, data, force, signedDistance))
    {
        force *= data.Mass;
        return float3(force, signedDistance);
    }
    else
    {
        float2 centreOfGravity = data.Complex_CentreOfGravity();
        
        // adding a tiny number prevents NaN
        float2 difference = (centreOfGravity - p) + 0.00001;

        float forceMagnitude = (_GravitationalConstant * data.Mass) / dot(difference, difference);
        
        float2 dir = normalize(difference) * forceMagnitude;
        
        return float3(dir, length(GetVector(p, data)));
    }
}

float3 GetShellForceAndSignedDistance(float2 p, GravityData data)
{
    float2 shellPos = mul(data.Transform, float4(0, 0, 0, 1)).xy;
    
    // adding a tiny number prevents NaN
    float2 difference = (shellPos - p) + EPSILON;
    float sqrMagnitude = dot(difference, difference);
    float radius = data.Point_Radius();
    float sqrRadius = data.Point_SqrRadius();
    float surfaceGravity = data.Point_SurfaceGravity();
    
    float forceMagnitude;
    
    if (radius > 0.0 && sqrMagnitude < sqrRadius)
    {
        forceMagnitude = lerp(0.0, surfaceGravity, sqrt(sqrMagnitude) / radius);
    }
    else
    {
        forceMagnitude = (_GravitationalConstant * data.Mass) / sqrMagnitude;
    }

    return float3(safeNormalize(difference) * forceMagnitude, sqrt(sqrMagnitude) - radius);
}

float3 GetLineForceAndSignedDistance(float2 p, GravityData data)
{
    float2 a = mul(data.Transform, float4(data.Line_GetA(), 0, 1)).xy;
    float2 b = mul(data.Transform, float4(data.Line_GetB(), 0, 1)).xy;
    
    // adding a tiny number prevents NaN
    float2 difference = ProjectPointOnLineSegment(a, b, p) - p + 0.00001;
    float2 directionToLineSegment = normalize(difference);
    difference = directionToLineSegment * (length(difference) - data.Line_Width());

    // https://iquilezles.org/www/articles/distgradfunctions2d/distgradfunctions2d.htm
    float2 pa = p - a, ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    float2 q = pa - h * ba;
    float d = length(q);
    
    float forceMagnitude = (_GravitationalConstant * data.Mass) / dot(difference, difference);

    float2 dir = normalize(difference) * forceMagnitude;
    
    return float3(dir, d - data.Line_Width());
}

float3 GetForceAndSignedDistance(float2 p, GravityData data)
{
    float3 forceAndSignedDistance = float3(0, 0, 0);
    
    switch (data.Type)
    {
        case GRAVITY_BODY_TYPE_SHELL:
            forceAndSignedDistance = GetShellForceAndSignedDistance(p, data);
            break;
        case GRAVITY_BODY_TYPE_LINE:
            forceAndSignedDistance = GetLineForceAndSignedDistance(p, data);
            break;
        case GRAVITY_BODY_TYPE_COMPLEX:
            forceAndSignedDistance = GetComplexForceAndSignedDistance(p, data);
            break;
    }
    
    float2 force = forceAndSignedDistance.xy;
    forceAndSignedDistance.xy = safeNormalize(force) * min(length(force), _MaxForce);
    
    return forceAndSignedDistance;
}


float3 Map(float2 p, float smoothing, float colourSmoothing, out float4 colour)
{
    colour = float4(0, 0, 0, 0);
    float2 force = float2(0, 0);
    float minDist = 1000000.;
    
    if (_GravityDataCount <= 0)
        return float3(force, minDist);
    
    GravityData data = _GravityData[0];
    float3 forceAndSignedDistance = GetForceAndSignedDistance(p, data);
    force = forceAndSignedDistance.xy;
    colour = data.Colour;
    minDist = forceAndSignedDistance.z;
    
    for (int i = 1; i < _GravityDataCount; i++)
    {
        GravityData data = _GravityData[i];
        
        float3 forceAndSignedDistance = GetForceAndSignedDistance(p, data);
        force += forceAndSignedDistance.xy;
        minDist = SmoothMin(minDist, forceAndSignedDistance.z, colour, data.Colour, smoothing, colourSmoothing, colour);
    }
    
    colour = saturate(colour);

    return float3(force, minDist);
}

float3 Map(float2 p, out GravityData closestBody)
{
    float2 force = float2(0, 0);
    
    float minDist = 100000.0;
    float smoothMinDist = 100000.0;

    const float smoothness = 0.4;
    
    for (int i = 0; i < _GravityDataCount; i++)
    {
        GravityData data = _GravityData[i];
        
        float3 forceAndSignedDistance = GetForceAndSignedDistance(p, data);
        
        force += forceAndSignedDistance.xy;
        
        if (forceAndSignedDistance.z < minDist)
        {
            minDist = forceAndSignedDistance.z;
            closestBody = data;
        }
        
        smoothMinDist = SmoothMin(smoothMinDist, forceAndSignedDistance.z, smoothness);
    }

    return float3(force, smoothMinDist);
}

float3 Map(float2 p)
{
    GravityData ignore;
    return Map(p, ignore);
}

float2 MapGradient(float2 p)
{
	if (_GravityDataCount <= 0)
		return float2(0, 0);
    
    const float2 h = float2(0.1, 0);
    return -normalize(float2(Map(p + h.xy).z - Map(p - h.xy).z,
                           Map(p + h.yx).z - Map(p - h.yx).z));
}

//float3 GetTotalGravityForceAndSignedDistance(float2 p, out float2 gradient)
//{
//    gradient = MapGradient(p);
//    return Map(p);
//}

#endif // GRAVITY_COMMON