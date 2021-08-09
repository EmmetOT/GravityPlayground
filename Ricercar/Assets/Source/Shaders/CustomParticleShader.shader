Shader "Custom/ParticleShader"
{
	Properties
	{
	}

	SubShader
	{
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"
			#include "./Common.hlsl"
			#include "./Gravity_Particles_Includes.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 col : TEXCOORD1;
				int instanceID : TEXCOORD2;
			};

			StructuredBuffer<GravityParticle> _GravityParticles_Structured;
			UNITY_DECLARE_TEX2DARRAY(_GravityParticleSprites);

			v2f vert(appdata v, uint instanceID : SV_InstanceID)
			{
				int arrayIndex = _ParticleIndexValues[instanceID];
				GravityParticle particle = _GravityParticles_Structured[arrayIndex];

				float2 direction = normalize(particle.Velocity);
				float radius = particle.CurrentRadius();

				float2x2 lookAtRotation = LookAt2D(direction);
				float2x2 randomRotation = AngleRot(max(0, -particle.RotateToFaceMovementDirection));

				// particle.RotateToFaceMovementDirection is either a negative number from 0 to -360, representing a fixed rotation,
				// or it's 1, which means rotate to face velocity
				float2 rotated = mul(randomRotation, v.vertex.xy);
				rotated = lerp(rotated, mul(lookAtRotation, v.vertex.xy), saturate(particle.RotateToFaceMovementDirection));

				float2 position = radius * rotated + particle.Position;

				v2f o;
				o.vertex = UnityObjectToClipPos(float3(position, 0.0));
				o.col = particle.CurrentColour();
				o.uv = v.uv;
				o.instanceID = instanceID;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				int arrayIndex = _ParticleIndexValues[i.instanceID];
				GravityParticle particle = _GravityParticles_Structured[arrayIndex];

				int from = lerp(particle.SpriteIndex, particle.SpriteIndex + particle.SpriteCount, particle.Age / particle.Duration);
				int to = min(from + 1, particle.SpriteIndex + particle.SpriteCount - 1);

				float spriteDuration = particle.Duration / particle.SpriteCount;
				float t = (particle.Age % spriteDuration) / spriteDuration;

				float4 fromCol = UNITY_SAMPLE_TEX2DARRAY(_GravityParticleSprites, float3(i.uv, from));
				float4 toCol = UNITY_SAMPLE_TEX2DARRAY(_GravityParticleSprites, float3(i.uv, to));

				return lerp(fromCol, toCol, t) * i.col;
			}

			ENDCG
		}
	}
}