#ifndef NODE_PARTICLE_INFO
#define NODE_PARTICLE_INFO

#include "./Common.hlsl"
#include "./Gravity_Particles_Includes.hlsl"

StructuredBuffer<GravityParticle> _GravityParticles_Structured;

void GetParticleInfo_float(in int InstanceID, out float Radius, out float3 Position, out float4 Colour)
{
    int arrayIndex = _ActiveParticlesThisFrame_Structured[InstanceID];
    GravityParticle particle = _GravityParticles_Structured[arrayIndex];
    
    Radius = lerp(particle.Radius * 0.5, 0., particle.Age / particle.Duration);
    Position = float3(particle.Position, -1.0/*remap((float) MIN_PARTICLE_INDEX, (float) MAX_PARTICLE_INDEX, 10.0, 0.0, arrayIndex)*/);
    Colour = particle.Colour;
}

#endif // NODE_PARTICLE_INFO