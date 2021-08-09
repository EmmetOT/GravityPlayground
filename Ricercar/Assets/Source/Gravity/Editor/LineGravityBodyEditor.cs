using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GravityPlayground.GravityStuff.Editor
{
    [CustomEditor(typeof(LineGravityBody))]
    public class LineGravityBodyEditor : UnityEditor.Editor
    {
        private SerializedProperty m_a;
        private SerializedProperty m_b;
        private LineGravityBody m_line;

        private void OnEnable()
        {
            m_a = serializedObject.FindProperty("m_a");
            m_b = serializedObject.FindProperty("m_b");
            m_line = target as LineGravityBody;
        }

        private void OnSceneGUI()
        {
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                Handles.matrix = m_line.transform.localToWorldMatrix;
                Vector2 newA = Handles.DoPositionHandle(m_a.vector2Value, Quaternion.identity);
                Vector2 newB = Handles.DoPositionHandle(m_b.vector2Value, Quaternion.identity);

                if (changeCheck.changed)
                {
                    Undo.RecordObject(target, "Set Line Ends");

                    m_a.vector2Value = newA;
                    m_b.vector2Value = newB;

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}