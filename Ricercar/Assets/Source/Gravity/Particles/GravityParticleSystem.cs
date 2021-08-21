using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Linq;
using BufferSorter;

#if UNITY_EDITOR
using UnityEditor.Compilation;
using UnityEditor;
#endif

namespace GravityPlayground.GravityStuff
{
    public class GravityParticleSystem : MonoBehaviour
    {
        private const int MAX_PARTICLES = 1000000;
        public const int MAX_NEW_PARTICLES_PER_FRAME = 10000;
        public const int PARTICLE_SPRITE_SIZE = 128;

        private static class Properties
        {
            public static int ParticlesPool_AppendBuffer { get; private set; } = Shader.PropertyToID("_GravityParticlesPool_Append");
            public static int ParticlesPool_ConsumeBuffer { get; private set; } = Shader.PropertyToID("_GravityParticlesPool_Consume");
            public static int Particles_StructuredBuffer { get; private set; } = Shader.PropertyToID("_GravityParticles_Structured");
            public static int EmittedParticles_StructuredBuffer { get; private set; } = Shader.PropertyToID("_EmittedParticles_Structured");
            public static int Count_StructuredBuffer { get; private set; } = Shader.PropertyToID("_ParticleCountData");
            public static int ActiveParticlesThisFrame_AppendBuffer { get; private set; } = Shader.PropertyToID("_ActiveParticlesThisFrame_Append");
            public static int ActiveParticlesThisFrame_StructuredBuffer { get; private set; } = Shader.PropertyToID("_ActiveParticlesThisFrame_Structured");
            public static int GravityPartcleSprites_TextureArray { get; private set; } = Shader.PropertyToID("_GravityParticleSprites");

            public static int ParticleIndexValues_StructuredBuffer = Shader.PropertyToID("_ParticleIndexValues");
            public static int ParticleIndexKeys_StructuredBuffer = Shader.PropertyToID("_ParticleIndexKeys");

            public static int DeltaTime_Float { get; private set; } = Shader.PropertyToID("_DeltaTime");
            public static int MaxParticles_Uint { get; private set; } = Shader.PropertyToID("_MaxParticles");
        }

        public struct Kernels
        {
            public int InitializePool { get; private set; }
            public int AddEmittedParticles { get; private set; }
            public int ResetMinMax { get; private set; }
            public int UpdateParticles { get; private set; }
            public int CreateParticleSortBuffers { get; private set; }

            public Kernels(ComputeShader computeShader)
            {
                InitializePool = computeShader.FindKernel("InitializePool");
                AddEmittedParticles = computeShader.FindKernel("AddEmittedParticles");
                ResetMinMax = computeShader.FindKernel("ResetMinMax");
                UpdateParticles = computeShader.FindKernel("UpdateParticles");
                CreateParticleSortBuffers = computeShader.FindKernel("CreateParticleSortBuffers");
            }
        }

        [SerializeField]
        [HideInInspector]
        private ComputeShader m_computeShader;

        [SerializeField]
        //[HideInInspector]
        private ComputeShader m_sortComputeShader;

        [SerializeField]
        //[HideInInspector]
        private Material m_particleMaterial;

        [SerializeField]
        private bool m_drawDebugParticlesInSceneView = false;

        //[Header("Test Stuff")]
        //[SerializeField]
        //private ParticleConfig[] m_testParticles;

        private Camera m_camera;

        private static readonly Matrix4x4[] m_matrices = new Matrix4x4[MAX_PARTICLES];
        private static readonly int[] m_meshTriangles = { 0, 2, 1, 2, 0, 3 };
        private static readonly Vector3[] m_meshVertices = { -Vector2.one, new Vector2(1f, -1f), Vector2.one, new Vector2(-1f, 1f) };
        private static readonly Vector2[] m_meshUVs = { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        private Mesh m_particleMesh;

        private ComputeBuffer m_particlePoolBuffer;
        private ComputeBuffer m_particleSpritesBuffer;
        private ComputeBuffer m_particlesBuffer;
        private ComputeBuffer m_emittedParticlesBuffer;
        private ComputeBuffer m_argsBuffer;
        private ComputeBuffer m_activeParticlesThisFrameBuffer;
        private ComputeBuffer m_particleIndexValues;
        private ComputeBuffer m_particleIndexKeys;

        private Kernels m_kernels;

        private readonly List<Particle> m_emittedParticles = new List<Particle>();

        private bool m_initialized = false;

        private static readonly uint[] m_argsBufferInit = new uint[] { 6, 0, 0, 0, 0, 0, 0 };

        [SerializeField]
        private List<Texture2D> m_particleSpriteSequences = new();
        //private readonly Dictionary<Texture2D, List<int>> m_particleSpriteIndices = new Dictionary<Texture2D, int>();

        private void OnEnable()
        {
#if UNITY_EDITOR
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            //SceneView.duringSceneGui += OnScene;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            //SceneView.duringSceneGui -= OnScene;
#endif
            m_particleSpritesBuffer?.Dispose();
            m_particlePoolBuffer?.Dispose();
            m_particlesBuffer?.Dispose();
            m_emittedParticlesBuffer?.Dispose();
            m_argsBuffer?.Dispose();
            m_activeParticlesThisFrameBuffer?.Dispose();
            m_particleIndexValues?.Dispose();
            m_particleIndexKeys?.Dispose();

            m_particleSpriteSequences.Clear();
            //m_particleSpriteIndices.Clear();
        }

        public int ActiveParticleCount { get; private set; } = 0;

        private void FixedUpdate()
        {
            if (!m_initialized)
                Init();

            m_activeParticlesThisFrameBuffer.SetCounterValue(0);

            SpawnNewEmittedParticles();

            m_computeShader.Dispatch(m_kernels.ResetMinMax, 1, 1, 1);

            m_computeShader.GetKernelThreadGroupSizes(m_kernels.UpdateParticles, out uint x, out _, out _);
            m_computeShader.Dispatch(m_kernels.UpdateParticles, Mathf.CeilToInt((float)MAX_PARTICLES / x), 1, 1);

            // note: until i have a way to use the sorter with indirect args, this is the best i can do
            m_argsBuffer.GetData(m_argsBufferInit);

            ActiveParticleCount = (int)m_argsBufferInit[1];

            if (ActiveParticleCount > 1)
            {
                m_computeShader.GetKernelThreadGroupSizes(m_kernels.CreateParticleSortBuffers, out x, out _, out _);
                m_computeShader.Dispatch(m_kernels.CreateParticleSortBuffers, Mathf.CeilToInt((float)ActiveParticleCount / x), 1, 1);

                using Sorter sorter = new(m_sortComputeShader);
                sorter.Sort(m_particleIndexValues, m_particleIndexKeys, reverse: true, ActiveParticleCount);
            }
        }

        private void Update()
        {
            if (!m_particleMaterial)
                return;

            Graphics.DrawMeshInstancedIndirect(m_particleMesh, 0, m_particleMaterial, new Bounds(Vector3.zero, BOUNDS_EXTENTS), m_argsBuffer);
        }

        private static readonly Vector3 BOUNDS_EXTENTS = new(100000f, 100000f, 100000f);

        private void Init()
        {
            Gravity.Instance.ForceUpdate();

            m_initialized = true;

            for (int i = 0; i < MAX_PARTICLES; i++)
                m_matrices[i] = Matrix4x4.identity;

            if (m_particleMesh == null)
            {
                m_particleMesh = new Mesh();
                m_particleMesh.SetVertices(m_meshVertices);
                m_particleMesh.SetTriangles(m_meshTriangles, 0);
                m_particleMesh.SetUVs(0, m_meshUVs);
                m_particleMesh.bounds = new Bounds(Vector3.zero, BOUNDS_EXTENTS);
            }

            // initialize the append/consume pool buffer
            m_particlePoolBuffer?.Dispose();
            m_particlePoolBuffer = new ComputeBuffer(MAX_PARTICLES, sizeof(uint), ComputeBufferType.Append);
            m_particlePoolBuffer.SetCounterValue(0);

            // initialize the buffer of all particles
            m_particlesBuffer?.Dispose();
            m_particlesBuffer = new ComputeBuffer(MAX_PARTICLES, Particle.Stride);

            m_emittedParticlesBuffer?.Dispose();
            m_emittedParticlesBuffer = new ComputeBuffer(MAX_NEW_PARTICLES_PER_FRAME, Particle.Stride);

            m_argsBuffer?.Dispose();
            m_argsBuffer = new ComputeBuffer(m_argsBufferInit.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
            m_argsBuffer.SetData(m_argsBufferInit);

            m_activeParticlesThisFrameBuffer?.Dispose();
            m_activeParticlesThisFrameBuffer = new ComputeBuffer(MAX_PARTICLES, ParticleSortKey.Stride, ComputeBufferType.Append);
            m_activeParticlesThisFrameBuffer.SetCounterValue(0);

            m_particleIndexValues?.Dispose();
            m_particleIndexValues = new ComputeBuffer(MAX_PARTICLES, sizeof(int));

            m_particleIndexKeys?.Dispose();
            m_particleIndexKeys = new ComputeBuffer(MAX_PARTICLES, sizeof(int));
        
            m_kernels = new Kernels(m_computeShader);

            Shader.SetGlobalBuffer(Properties.ParticlesPool_AppendBuffer, m_particlePoolBuffer);
            Shader.SetGlobalBuffer(Properties.ParticlesPool_ConsumeBuffer, m_particlePoolBuffer);
            Shader.SetGlobalBuffer(Properties.Particles_StructuredBuffer, m_particlesBuffer);
            Shader.SetGlobalBuffer(Properties.EmittedParticles_StructuredBuffer, m_emittedParticlesBuffer);
            Shader.SetGlobalBuffer(Properties.Count_StructuredBuffer, m_argsBuffer);
            Shader.SetGlobalBuffer(Properties.ActiveParticlesThisFrame_AppendBuffer, m_activeParticlesThisFrameBuffer);
            Shader.SetGlobalBuffer(Properties.ActiveParticlesThisFrame_StructuredBuffer, m_activeParticlesThisFrameBuffer);
            Shader.SetGlobalBuffer(Properties.ParticleIndexValues_StructuredBuffer, m_particleIndexValues);
            Shader.SetGlobalBuffer(Properties.ParticleIndexKeys_StructuredBuffer, m_particleIndexKeys);

            m_computeShader.SetFloat(Properties.DeltaTime_Float, Time.fixedDeltaTime);
            m_computeShader.SetInt(Properties.MaxParticles_Uint, MAX_PARTICLES);

            m_computeShader.GetKernelThreadGroupSizes(m_kernels.InitializePool, out uint x, out _, out _);
            m_computeShader.Dispatch(m_kernels.InitializePool, Mathf.CeilToInt((float)MAX_PARTICLES / x), 1, 1);
        }

        /// <summary>
        /// Given an ordered sequence of sprites, either find the index where that sequence starts, or add the sequence to the list and return the start index.
        /// 
        /// Recommended to cache this value at runtime.
        /// </summary>
        public int GetSpriteSequenceStartIndex(IList<Sprite> spriteSequence)
        {
            void SendSpritesToGPU()
            {
                Texture2DArray texture2Darray = new Texture2DArray(PARTICLE_SPRITE_SIZE, PARTICLE_SPRITE_SIZE, m_particleSpriteSequences.Count, TextureFormat.DXT5, true);

                for (int i = 0; i < m_particleSpriteSequences.Count; i++)
                    Graphics.CopyTexture(m_particleSpriteSequences[i], 0, texture2Darray, i);

                Shader.SetGlobalTexture(Properties.GravityPartcleSprites_TextureArray, texture2Darray);
            }
            
            if (spriteSequence.IsNullOrEmpty())
                return 0;

            // iterate over the current ordered list
            for (int i = 0; i < m_particleSpriteSequences.Count; i++)
            {
                bool isMatch = true;
                for (int j = 0; j < spriteSequence.Count; j++)
                {
                    // we've reached the end of the main sequence, 
                    // meaning the end of the sequence is a partial match
                    // to the given sequence. this means we can just add the remainder on to the end
                    if (i + j >= m_particleSpriteSequences.Count)
                    {
                        for (int k = j; k < spriteSequence.Count; k++)
                            m_particleSpriteSequences.Add(spriteSequence[k].texture);

                        SendSpritesToGPU();
                        return i;
                    }
                    
                    // sequence does not match
                    if (m_particleSpriteSequences[i + j] != spriteSequence[j])
                    {
                        isMatch = false;
                        break;
                    }
                }

                // if we finish the given sequence without finding a mismatch, the sequence is present and starts at i
                if (isMatch)
                    return i;
            }

            // we havent found the given sequence and the end of the main sequence doesn't end in a partial match, so we need to add the whole thing.
            int startIndex = m_particleSpriteSequences.Count;
            for (int i = 0; i < spriteSequence.Count; i++)
                m_particleSpriteSequences.Add(spriteSequence[i].texture);

            SendSpritesToGPU();
            return startIndex;
        }

        private void SpawnNewEmittedParticles()
        {
            if (m_emittedParticles.Count <= 0)
                return;

            m_emittedParticlesBuffer.SetCounterValue(0);
            m_emittedParticlesBuffer.SetData(m_emittedParticles);
            m_computeShader.Dispatch(m_kernels.AddEmittedParticles, m_emittedParticles.Count, 1, 1);
            m_emittedParticles.Clear();
        }

        public void SpawnParticle(Particle gravityParticle)
        {
            Debug.Assert(m_emittedParticles.Count < MAX_NEW_PARTICLES_PER_FRAME, "Can't add more than " + MAX_NEW_PARTICLES_PER_FRAME + " particles per frame.");

            m_emittedParticles.Add(gravityParticle);
        }

        public void SpawnParticles(IList<Particle> gravityParticles)
        {
            Debug.Assert(m_emittedParticles.Count < MAX_NEW_PARTICLES_PER_FRAME, "Can't add more than " + MAX_NEW_PARTICLES_PER_FRAME + " particles per frame.");

            m_emittedParticles.AddRange(gravityParticles);
        }

        private void OnCompilationStarted(object param)
        {
            m_particlePoolBuffer?.Dispose();
            m_particlesBuffer?.Dispose();
            m_emittedParticlesBuffer?.Dispose();
            m_argsBuffer?.Dispose();
            m_activeParticlesThisFrameBuffer?.Dispose();
        }

        [System.Serializable]
        [StructLayout(LayoutKind.Sequential)]
        private struct ParticleSortKey
        {
            public static readonly int Stride = sizeof(uint) + sizeof(int);

            public uint Index;
            public int SortKey;
        };

        /// <summary>
        /// Represents a single particle to be sent to the GPU.
        /// </summary>
        [System.Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct Particle
        {
            public static readonly int Stride = sizeof(float) * 20 + sizeof(int) * 4;

            public int SpriteIndex;
            public int SpriteCount;
            public float RotateToFaceMovementDirection;
            public float BounceChance;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Mass;
            public float Age;
            public float Duration;
            public float StartRadius;
            public float EndRadius;
            public float RadiusRandomOffset;
            public Color StartColour;
            public Color EndColour;
            public int IsAlive;
            public int SortKey;

            public override string ToString() => Position.ToString("F4");
        }
    }
}
