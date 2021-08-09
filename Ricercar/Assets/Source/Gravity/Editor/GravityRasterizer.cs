using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace GravityPlayground.GravityStuff
{
    public class GravityRasterizer : EditorWindow
    {
        private const string COMPUTE_SHADER_PATH = "Shaders/GravityRasterizerShader";

        private const string MASS_DISTRIBUTION_KERNEL = "CalculateMassDistribution";
        private const string GENERATE_GRAVITY_MAP_KERNEL = "GenerateGravityMap";

        private const string INPUT_TEXTURE = "_InputTexture";
        private const string INPUT_TEXTURE_SIZE = "_InputTextureSize";

        private const string OUTPUT_TEXTURE = "_OutputTexture";
        private const string OUTPUT_TEXTURE_SIZE = "_OutputTextureSize";

        private const string OUTPUT_FORCES = "_OutputForces";
        private const string PADDING = "_Padding";
        
        private const string EDGE_PARTICLES_APPEND_BUFFER = "_EdgeParticles_AppendBuffer";
        private const string EDGE_PARTICLES_STRUCTURED_BUFFER = "_EdgeParticles_StructuredBuffer";

        private const string GRAVITY_PARTICLES_APPEND_BUFFER = "_GravityParticles_AppendBuffer";
        private const string GRAVITY_PARTICLES_STRUCTURED_BUFFER = "_GravityParticles_StructuredBuffer";
        private const string COUNT_BUFFER = "_CountBuffer";

        private const float MULTIPLICATION_FACTOR = 10000f;

        [SerializeField]
        private ComputeShader m_computeShader;

        [SerializeField]
        private ComputeShader m_computeShaderInstance;

        private Vector3[] m_forces;

        private Texture2D m_inputTexture;
        private Texture2D m_outputTexture;
        
        private int m_padding = 0;

        private Vector2 m_centreOfGravity;

        private int m_massDistributionKernel;
        private int m_generateGravityMapKernel;

        private ComputeBuffer m_outputForcesBuffer;
        private ComputeBuffer m_countBuffer;
        private ComputeBuffer m_gravityParticlesAppendBuffer;
        private ComputeBuffer m_edgeParticlesAppendBuffer;

        private static GravityRasterizer m_window;

        private static readonly int[] m_outputData = new int[5];
        private static readonly int[] m_blankData = new int[] { 0, 0, 0, 0, 0 };

        [MenuItem("Tools/Gravity Rasterizer")]
        public static void ShowWindow()
        {
            m_window = (GravityRasterizer)GetWindow(typeof(GravityRasterizer));
            m_window.Initialize();
        }

        private void Initialize()
        {
            ReleaseBuffers();

            m_centreOfGravity = default;
            m_computeShader = Resources.Load<ComputeShader>(COMPUTE_SHADER_PATH);

            m_massDistributionKernel = m_computeShader.FindKernel(MASS_DISTRIBUTION_KERNEL);
            m_generateGravityMapKernel = m_computeShader.FindKernel(GENERATE_GRAVITY_MAP_KERNEL);
        }

        public void OnGUI()
        {
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Compute Shader", m_computeShader, typeof(ComputeShader), false);
            GUI.enabled = true;

            EditorGUILayout.HelpBox("Add a texture here. It's recommended to have no compression of any kind on this texture, especially check that 'Non Power of 2' under 'Advanced' is set to 'None.", MessageType.Info);

            m_inputTexture = (Texture2D)EditorGUILayout.ObjectField("Input Texture", m_inputTexture, typeof(Texture2D), false);
            
            m_padding = Mathf.Max(0, EditorGUILayout.IntField("Padding", m_padding));

            GUI.enabled = m_inputTexture != null;
            if (GUILayout.Button("Generate Gravity Texture"))
            {
                if (m_computeShader == null)
                {
                    m_computeShader = Resources.Load<ComputeShader>(COMPUTE_SHADER_PATH);

                    if (m_computeShader == null)
                    {
                        Debug.LogError("Couldn't find a Compute Shader at: " + COMPUTE_SHADER_PATH);
                        GUI.enabled = true;
                        return;
                    }
                }

                CreateBuffers();

                RenderTexture renderTexture = Utils.CreateTempRenderTexture(m_inputTexture.width + m_padding * 2, m_inputTexture.height + m_padding * 2, Color.black, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat);
                GenerateGravityTexture(m_inputTexture, renderTexture, out m_centreOfGravity);

                m_outputTexture = renderTexture.ToTexture2D();
                m_outputTexture.alphaIsTransparency = true;
                m_outputTexture.wrapMode = TextureWrapMode.Clamp;
                m_outputTexture.filterMode = FilterMode.Point;

                ReleaseBuffers();
                renderTexture.Release();
            }
            GUI.enabled = true;

            if (m_outputTexture != null)
            {
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Output Texture", m_outputTexture, typeof(Texture2D), false);
                EditorGUILayout.Vector2Field("Centre Of Gravity", m_centreOfGravity);
                GUI.enabled = true;

                if (GUILayout.Button("Save as Asset"))
                {
                    Debug.Log("Creating from " + m_inputTexture.name, m_inputTexture);
                    GravityMap map = GravityMap.Create(m_inputTexture, m_outputTexture, m_forces, m_padding, m_centreOfGravity);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Saved as " + map.name + "!", map);
                }
            }
        }

        public void GenerateGravityTexture(Texture2D input, RenderTexture output, out Vector2 centreOfGravity)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            m_computeShaderInstance.SetTexture(m_massDistributionKernel, INPUT_TEXTURE, input);
            m_computeShaderInstance.SetTexture(m_generateGravityMapKernel, INPUT_TEXTURE, input);

            m_computeShaderInstance.SetTexture(m_generateGravityMapKernel, OUTPUT_TEXTURE, output);
            
            m_computeShaderInstance.SetVector(INPUT_TEXTURE_SIZE, new Vector2(input.width, input.height));
            m_computeShaderInstance.SetVector(OUTPUT_TEXTURE_SIZE, new Vector2(input.width + m_padding * 2, input.height + m_padding * 2));
            m_computeShaderInstance.SetFloat(PADDING, m_padding);

            // this kernel finds each texel which has mass, and sums the overall mass
            m_computeShaderInstance.GetKernelThreadGroupSizes(m_massDistributionKernel, out uint x, out uint y, out _);
            m_computeShaderInstance.Dispatch(m_massDistributionKernel, Mathf.CeilToInt((float)input.width / x), Mathf.CeilToInt((float)input.height / y), 1);
            
            // finally, do a gravity calculation from each texel to every other texel with mass
            // (this is why sparser textures will get faster results)
            m_computeShaderInstance.GetKernelThreadGroupSizes(m_generateGravityMapKernel, out x, out y, out _);
            m_computeShaderInstance.Dispatch(m_generateGravityMapKernel, Mathf.CeilToInt((float)output.width / x), Mathf.CeilToInt((float)output.height / y), 1);

            // get the data for the centre of gravity. can't be avoided unfortunately :(
            m_countBuffer.GetData(m_outputData);
            
            // since atomic operations can only be on ints, we compute it as ints and then divide it by a large value
            float xCentre = (m_outputData[1] / MULTIPLICATION_FACTOR);
            float yCentre = (m_outputData[2] / MULTIPLICATION_FACTOR);

            float totalMass = m_outputData[0] / MULTIPLICATION_FACTOR;
            
            centreOfGravity = new Vector2(xCentre, yCentre) / totalMass;

            m_forces = new Vector3[(m_inputTexture.width + m_padding * 2) * (m_inputTexture.height + m_padding * 2)];
            m_outputForcesBuffer.GetData(m_forces);
            
            //Test(input.GetPixels(), input.width);

            stopwatch.Stop();
            Debug.Log("Took " + stopwatch.ElapsedMilliseconds + " milliseconds");


            //Vector2Int IndexToCellCoordinate(int index, int size)
            //{
            //    int _y = index / size;
            //    int _x = index % size;

            //    return new Vector2Int(_x, _y);
            //}


            //for (int i = 0; i < Mathf.Min(m_forces.Length, 1000); i++)
            //{
            //    Vector2Int coordinate = IndexToCellCoordinate(i, m_inputTexture.width);

            //    Debug.Log(i + ") " + coordinate + ", " + m_forces[i].ToString("F4"));
            //}
        }

        //private void Test(Color[] colours, int size)
        //{
        //    Debug.Log("Colours length = " + colours.Length);

        //    Vector2Int IndexToCellCoordinate(int index)
        //    {
        //        int y = index / size;
        //        int x = index % size;

        //        return new Vector2Int(x, y);
        //    }


        //    int particles = 0;

        //    for (int i = 0; i < colours.Length; i++)
        //    {
        //        Color col = colours[i];

        //        if (col.a > 0f)
        //        {
        //            Debug.Log("Pixel " + i + " = " + col.ToString("F4") + " at " + (IndexToCellCoordinate(i)));

        //            ++particles;
        //        }
        //    }

        //    Debug.Log("Found " + particles + " particles.");
        //}

        private void CreateBuffers()
        {
            if (m_computeShaderInstance)
                DestroyImmediate(m_computeShaderInstance);

            m_computeShaderInstance = Instantiate(m_computeShader);

            // the number of points at which gravity will be sampled
            int pointCount = (m_inputTexture.width + m_padding * 2) * (m_inputTexture.height + m_padding * 2);

            m_countBuffer = new ComputeBuffer(m_outputData.Length, sizeof(int));
            m_outputForcesBuffer = new ComputeBuffer(pointCount, sizeof(float) * 3);
            m_gravityParticlesAppendBuffer = new ComputeBuffer(pointCount, sizeof(float) * 2, ComputeBufferType.Append);
            m_edgeParticlesAppendBuffer = new ComputeBuffer(pointCount, sizeof(float) * 2, ComputeBufferType.Append);

            m_computeShaderInstance.SetBuffer(m_massDistributionKernel, COUNT_BUFFER, m_countBuffer);
            m_computeShaderInstance.SetBuffer(m_generateGravityMapKernel, COUNT_BUFFER, m_countBuffer);

            m_computeShaderInstance.SetBuffer(m_massDistributionKernel, GRAVITY_PARTICLES_APPEND_BUFFER, m_gravityParticlesAppendBuffer);
            m_computeShaderInstance.SetBuffer(m_generateGravityMapKernel, GRAVITY_PARTICLES_STRUCTURED_BUFFER, m_gravityParticlesAppendBuffer);

            m_computeShaderInstance.SetBuffer(m_massDistributionKernel, EDGE_PARTICLES_APPEND_BUFFER, m_edgeParticlesAppendBuffer);
            m_computeShaderInstance.SetBuffer(m_generateGravityMapKernel, EDGE_PARTICLES_STRUCTURED_BUFFER, m_edgeParticlesAppendBuffer);

            m_computeShaderInstance.SetBuffer(m_generateGravityMapKernel, OUTPUT_FORCES, m_outputForcesBuffer);

            m_countBuffer.SetData(m_blankData);

            m_countBuffer.SetCounterValue(0);
            m_gravityParticlesAppendBuffer.SetCounterValue(0);
            m_edgeParticlesAppendBuffer.SetCounterValue(0);
        }

        private void ReleaseBuffers()
        {
            DestroyImmediate(m_computeShaderInstance);
            m_computeShaderInstance = null;

            m_countBuffer?.Release();
            m_countBuffer = null;

            m_gravityParticlesAppendBuffer?.Release();
            m_gravityParticlesAppendBuffer = null;

            m_edgeParticlesAppendBuffer?.Release();
            m_edgeParticlesAppendBuffer = null;

            m_outputForcesBuffer?.Release();
            m_outputForcesBuffer = null;
        }

        private void OnDestroy()
        {
            m_outputTexture = null;

            ReleaseBuffers();
        }
    }
}