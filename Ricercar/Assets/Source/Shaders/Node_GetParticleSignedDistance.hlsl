#ifndef NODE_PARTICLE_SIGNED_DISTANCE
#define NODE_PARTICLE_SIGNED_DISTANCE

#include "./Gravity_Particles_Includes.hlsl"

StructuredBuffer<int> _ParticleCountData;
#define ACTIVE_PARTICLES _ParticleCountData[1]

StructuredBuffer<GravityParticle> _GravityParticles_Structured;

void GetParticleSignedDistance_float(in float2 Position, out float SignedDistance, out float4 Colour)
{
    SignedDistance = 100000.0;
    Colour = float4(0, 0, 0, 0);
    
    for (int i = 0; i < ACTIVE_PARTICLES; i++)
    {
        GravityParticle particle = _GravityParticles_Structured[_ActiveParticlesThisFrame_Structured[i]];
        
        if (particle.IsAlive == 1)
        {
            float particleSignedDistance = distance(Position, particle.Position) - lerp(particle.Radius, 0., particle.Age / particle.Duration);
            
            if (particleSignedDistance < SignedDistance)
            {
                SignedDistance = particleSignedDistance;
                Colour = particle.Colour;
            }
        }
    }
}

#endif // NODE_PARTICLE_SIGNED_DISTANCE