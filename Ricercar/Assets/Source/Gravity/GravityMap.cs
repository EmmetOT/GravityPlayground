using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GravityPlayground.GravityStuff
{
    public class GravityMap : ScriptableObject
    {
        private const string DATA_ADDRESS = "Assets\\Data\\GravityMaps\\";
        private const string TEXTURE_ADDRESS = "Assets\\Data\\GravityMaps\\Textures\\";

        [SerializeField]
        private Texture m_sourceTexture;

        [SerializeField]
        private Texture2D m_texture;
        public Texture2D Texture => m_texture;

        [SerializeField]
        [HideInInspector]
        private Vector3[] m_forces;
        public Vector3[] Forces => m_forces;

        public Vector2 GetForce(Vector2Int coords) => m_forces[CellCoordinateToIndex(coords.x, coords.y)];
        public Vector2 GetForce(int index) => m_forces[index];

        public float GetSignedDistance(Vector2Int coords) => m_forces[CellCoordinateToIndex(coords.x, coords.y)].z;
        public float GetSignedDistance(int index) => m_forces[index].z;


        public Vector3 GetForceAndSignedDistance(Vector2Int coords) => m_forces[CellCoordinateToIndex(coords.x, coords.y)];
        public Vector3 GetForceAndSignedDistance(int index) => m_forces[index];


        [SerializeField]
        private Vector2 m_centreOfGravity;
        public Vector2 CentreOfGravity => m_centreOfGravity;

        [SerializeField]
        private Vector2Int m_textureSize;
        public Vector2Int TextureSize => m_textureSize;

        [SerializeField]
        private int m_padding = 0;
        public int Padding => m_padding;

        [SerializeField]
        private int m_guid;
        public int GUID => m_guid;

        [SerializeField]
        private Vector2Int m_totalSize;
        public Vector2Int TotalSize => m_totalSize;

        //[SerializeField]
        //private Vector2 m_sizeScalar;
        //public Vector2 SizeScalar => m_sizeScalar;

        public int CellCoordinateToIndex(int x, int y) => x + y * TotalSize.y;
        
#if UNITY_EDITOR
        public static GravityMap Create(Texture sourceTexture, Texture2D texture, Vector3[] forces, int padding, Vector2 centreOfGravity)
        {
            string name = sourceTexture.name;
            Vector2Int textureSize = new Vector2Int(sourceTexture.width, sourceTexture.height);

            Utils.SaveTexture(TEXTURE_ADDRESS, texture, "GravityMap_Texture_" + name);
            GravityMap map = Utils.CreateAsset<GravityMap>(DATA_ADDRESS, "GravityMap_" + name);

            map.m_guid = System.Guid.NewGuid().GetHashCode();
            map.m_sourceTexture = sourceTexture;
            map.m_texture = texture;
            map.m_forces = forces;
            map.m_centreOfGravity = centreOfGravity;
            map.m_textureSize = new Vector2Int(sourceTexture.width, sourceTexture.height);
            map.m_padding = padding;
            map.m_totalSize = new Vector2Int(textureSize.x + padding * 2, textureSize.y + padding * 2);
            //map.m_sizeScalar = new Vector2((textureSize.x + padding) / (float)textureSize.x, (textureSize.y + padding) / (float)textureSize.y);

            EditorUtility.SetDirty(map);
            return map;
        }
#endif
    }
}