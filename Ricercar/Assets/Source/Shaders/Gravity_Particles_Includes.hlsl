#ifndef GRAVITY_PARTICLE_INCLUDES
#define GRAVITY_PARTICLE_INCLUDES

struct ParticleSortKey
{
    uint Index;
    int SortKey;
};

struct GravityParticle
{
    int SpriteIndex;
    int SpriteCount;
    float RotateToFaceMovementDirection;
    float BounceChance;
    float2 Position;
    float2 Velocity;
    float Mass;
    float Age;
    float Duration;
    float StartRadius;
    float EndRadius;
    float RadiusRandomOffset;
    float4 StartColour;
    float4 EndColour;
    int IsAlive;
    int SortKey;
    
    float CurrentRadius()
    {
        return max(0., lerp(StartRadius, EndRadius, saturate(Age / Duration)) + RadiusRandomOffset);
    }
    
    float4 CurrentColour()
    {
        return lerp(StartColour, EndColour, saturate(Age / Duration));
    }
    
    float CurrentSpriteIndex()
    {
        return lerp(SpriteIndex, SpriteIndex + SpriteCount, saturate((Age / Duration) - 0.000001));
    }
};

StructuredBuffer<ParticleSortKey> _ActiveParticlesThisFrame_Structured;

#ifdef RW_PARTICLE_COUNT_DATA
RWStructuredBuffer<int> _ParticleIndexValues;
RWStructuredBuffer<int> _ParticleIndexKeys;
RWStructuredBuffer<int> _ParticleCountData;
#else
StructuredBuffer<int> _ParticleIndexValues;
StructuredBuffer<int> _ParticleCountData;
#endif

#define ACTIVE_PARTICLES _ParticleCountData[1]
#define MIN_PARTICLE_INDEX _ParticleCountData[5]
#define MAX_PARTICLE_INDEX _ParticleCountData[6]


#endif // GRAVITY_PARTICLE_INCLUDES