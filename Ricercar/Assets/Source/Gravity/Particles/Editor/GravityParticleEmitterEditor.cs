using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GravityPlayground.Editor;

namespace GravityPlayground.GravityStuff.Editor
{
    [CustomEditor(typeof(GravityParticleEmitter))]
    public class GravityParticleEmitterEditor : UnityEditor.Editor
    {
        private GravityParticleEmitter m_target;
        private Transform m_transform;
        private SerializedPropertySetter m_setter;
        private SerializedProperties m_properties;

        private enum MouseEvent { Down, Drag, Up, Held, None };
        private MouseEvent m_currentMouseEvent = MouseEvent.None;
        private int m_currentID = -1;

        private bool m_isSpawnRegionSettingsOpen = true;
        private bool m_isEmissionFrequencySettingsOpen = true;
        private bool m_isParticleSettingsOpen = true;

        private const float UNSELECTED_THICKNESS = 1f;
        private const float SELECTED_THICKNESS = 2f;
        private const float HANDLE_SELECTION_MARGIN = 5f;

        private static class Labels
        {
            public static GUIContent IsRunning = new GUIContent("Is Running", "Whether this emitter is actively creating particles.");

            public static GUIContent SpawnRegionSettings = new GUIContent("Spawn Region", "These variables determine where particles will be spawned.");
            public static GUIContent InnerRadius = new GUIContent("Inner Radius", "The minimum distance away from this object at which particles are spawned.");
            public static GUIContent OuterRadius = new GUIContent("Outer Radius", "The maximum distance away from this object at which particles are spawned.");
            public static GUIContent StartAngle = new GUIContent("Start Angle", "The angle, in degrees, defining the beginning of the arc in which particles are spawned");
            public static GUIContent AngleSize = new GUIContent("Angle Size", "The size, in degrees, of the arc in which particles are spawned.");
            public static GUIContent FlipStartDirection = new GUIContent("Flip Start Direction", "If true, particle velocity will be towards the centre of the spawn region instead of away from it.");

            public static GUIContent EmissionFrequencySettings = new GUIContent("Emission Frequency", "These variables determine the frequency and amount of particles spawned.");
            public static GUIContent ParticleEmissionFrequency = new GUIContent("Particle Emission Frequency", "The time, in seconds, between each set of particle spawnings.");
            public static GUIContent MinimumParticleEmission = new GUIContent("Minimum Particle Emission", "The minimum amount of particles per spawn.");
            public static GUIContent MaximumParticleEmission = new GUIContent("Maximum Particle Emission", "The maximum amount of particles per spawn.");

            public static GUIContent ParticleSettings = new GUIContent("Particle Settings", "These variables affect individual particles.");
            public static GUIContent MinimumParticleSpawnSpeed = new GUIContent("Minimum Particle Spawn Speed", "The minimum speed of particles on spawn.");
            public static GUIContent MaximumParticleSpawnSpeed = new GUIContent("Maximum Particle Spawn Speed", "The maximum speed of particles on spawn.");
            public static GUIContent RotationType = new GUIContent("Rotation Type", "Whether particles are aligned with their velocity, rotated randomly, or not rotated at all.");
            public static GUIContent BounceChance = new GUIContent("Bounce Chance", "The chance, where 0 is 0% and 1 is 100%, to bounce on collision with a gravity body. Note that gravity bodies also have a bounciness coefficient.");
            public static GUIContent StartRadius = new GUIContent("Start Radius", "The average radius of each particle on spawn. Will lerp towards End Radius.");
            public static GUIContent EndRadius = new GUIContent("End Radius", "The average radius of each particle when it times out.");
            public static GUIContent RadiusRandomOffset = new GUIContent("Radius Random Offset", "Every particles radius is plus or minus this amount.");
            public static GUIContent MinimumParticleMass = new GUIContent("Minimum Particle Mass", "The minimum possible mass of a spawned particle. Masses near 0 will move faster.");
            public static GUIContent MaximumParticleMass = new GUIContent("Maximum Particle Mass", "The maximum possible mass of a spawned particle. Masses near 0 will move faster.");
            public static GUIContent MinimumParticleDuration = new GUIContent("Minimum Particle Duration", "The shortest time a spawned particle can live.");
            public static GUIContent MaximumParticleDuration = new GUIContent("Maximum Particle Duration", "The longest time a spawned particle can live.");
            public static GUIContent StartColourGradient = new GUIContent("Start Colour Gradient", "The range of colours a particle can have on spawn.");
            public static GUIContent EndColourGradient = new GUIContent("End Colour Gradient", "The range of colours a particle will have on death. Colours are interpolated over the particles lifetime.");
            public static GUIContent Sprites = new GUIContent("Sprites", "A flipbook of sprites the particles will cycle through over their lifetime.");
            public static GUIContent SortKey = new GUIContent("Sort Key", "This number determines particle draw order. Lower values will be drawn last and so appear in front of higher values.");
        }

        private class SerializedProperties
        {
            public SerializedProperty Script { get; }
            public SerializedProperty IsRunning { get; }
            public SerializedProperty ParticleEmissionFrequency { get; }
            public SerializedProperty InnerRadius { get; }
            public SerializedProperty OuterRadius { get; }
            public SerializedProperty StartAngle { get; }
            public SerializedProperty AngleSize { get; }
            public SerializedProperty MinimumParticleEmission { get; }
            public SerializedProperty MaximumParticleEmission { get; }
            public SerializedProperty MinimumParticleSpawnSpeed { get; }
            public SerializedProperty MaximumParticleSpawnSpeed { get; }
            public SerializedProperty FlipStartDirection { get; }
            public SerializedProperty RotationType { get; }
            public SerializedProperty BounceChance { get; }
            public SerializedProperty StartRadius { get; }
            public SerializedProperty EndRadius { get; }
            public SerializedProperty RadiusRandomOffset { get; }
            public SerializedProperty MinimumParticleMass { get; }
            public SerializedProperty MaximumParticleMass { get; }
            public SerializedProperty MinimumParticleDuration { get; }
            public SerializedProperty MaximumParticleDuration { get; }
            public SerializedProperty StartColourGradient { get; }
            public SerializedProperty EndColourGradient { get; }
            public SerializedProperty Sprites { get; }
            public SerializedProperty SortKey { get; }

            public SerializedProperties(SerializedObject serializedObject)
            {
                Script = serializedObject.FindProperty("m_Script");
                IsRunning = serializedObject.FindProperty("m_isRunning");
                ParticleEmissionFrequency = serializedObject.FindProperty("m_particleEmissionFrequency");
                InnerRadius = serializedObject.FindProperty("m_innerRadius");
                OuterRadius = serializedObject.FindProperty("m_outerRadius");
                StartAngle = serializedObject.FindProperty("m_startAngle");
                AngleSize = serializedObject.FindProperty("m_angleSize");
                MinimumParticleEmission = serializedObject.FindProperty("m_minimumParticleEmission");
                MaximumParticleEmission = serializedObject.FindProperty("m_maximumParticleEmission");
                FlipStartDirection = serializedObject.FindProperty("m_flipStartDirection");
                MinimumParticleSpawnSpeed = serializedObject.FindProperty("m_minimumParticleSpeed");
                MaximumParticleSpawnSpeed = serializedObject.FindProperty("m_maximumParticleSpeed");
                RotationType = serializedObject.FindProperty("m_rotationType");
                BounceChance = serializedObject.FindProperty("m_bounceChance");
                StartRadius = serializedObject.FindProperty("m_startRadius");
                EndRadius = serializedObject.FindProperty("m_endRadius");
                RadiusRandomOffset = serializedObject.FindProperty("m_radiusRandomOffset");
                MinimumParticleMass = serializedObject.FindProperty("m_minimumParticleMass");
                MaximumParticleMass = serializedObject.FindProperty("m_maximumParticleMass");
                MinimumParticleDuration = serializedObject.FindProperty("m_minimumParticleDuration");
                MaximumParticleDuration = serializedObject.FindProperty("m_maximumParticleDuration");
                StartColourGradient = serializedObject.FindProperty("m_startColourGradient");
                EndColourGradient = serializedObject.FindProperty("m_endColourGradient");
                Sprites = serializedObject.FindProperty("m_sprites");
                SortKey = serializedObject.FindProperty("m_sortKey");
            }
        }

        private void OnEnable()
        {
            m_currentMouseEvent = MouseEvent.None;
            m_currentID = -1;

            m_target = (GravityParticleEmitter)target;
            m_transform = m_target.transform;
            m_setter = new SerializedPropertySetter(serializedObject);
            m_properties = new SerializedProperties(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_properties.Script);
            GUI.enabled = true;

            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_properties.IsRunning, Labels.IsRunning);

                if (m_isSpawnRegionSettingsOpen = EditorGUILayout.Foldout(m_isSpawnRegionSettingsOpen, Labels.SpawnRegionSettings, true))
                {
                    using (EditorGUILayout.VerticalScope box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (EditorGUI.IndentLevelScope indent = new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(m_properties.InnerRadius, Labels.InnerRadius);
                            EditorGUILayout.PropertyField(m_properties.OuterRadius, Labels.OuterRadius);
                            EditorGUILayout.PropertyField(m_properties.StartAngle, Labels.StartAngle);
                            EditorGUILayout.PropertyField(m_properties.AngleSize, Labels.AngleSize);
                            EditorGUILayout.PropertyField(m_properties.FlipStartDirection, Labels.FlipStartDirection);
                        }
                    }
                }

                if (m_isEmissionFrequencySettingsOpen = EditorGUILayout.Foldout(m_isEmissionFrequencySettingsOpen, Labels.EmissionFrequencySettings, true))
                {
                    using (EditorGUILayout.VerticalScope box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (EditorGUI.IndentLevelScope indent = new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(m_properties.ParticleEmissionFrequency, Labels.ParticleEmissionFrequency);
                            EditorGUILayout.PropertyField(m_properties.MinimumParticleEmission, Labels.MinimumParticleEmission);
                            EditorGUILayout.PropertyField(m_properties.MaximumParticleEmission, Labels.MaximumParticleEmission);
                        }
                    }
                }

                if (m_isParticleSettingsOpen = EditorGUILayout.Foldout(m_isParticleSettingsOpen, Labels.ParticleSettings, true))
                {
                    using (EditorGUILayout.VerticalScope box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (EditorGUI.IndentLevelScope indent = new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(m_properties.SortKey, Labels.SortKey);
                            EditorGUILayout.PropertyField(m_properties.Sprites, Labels.Sprites);
                            EditorGUILayout.PropertyField(m_properties.MinimumParticleSpawnSpeed, Labels.MinimumParticleSpawnSpeed);
                            EditorGUILayout.PropertyField(m_properties.MaximumParticleSpawnSpeed, Labels.MaximumParticleSpawnSpeed);
                            EditorGUILayout.PropertyField(m_properties.RotationType, Labels.RotationType);
                            EditorGUILayout.PropertyField(m_properties.BounceChance, Labels.BounceChance);
                            EditorGUILayout.PropertyField(m_properties.StartRadius, Labels.StartRadius);
                            EditorGUILayout.PropertyField(m_properties.EndRadius, Labels.EndRadius);
                            EditorGUILayout.PropertyField(m_properties.RadiusRandomOffset, Labels.RadiusRandomOffset);
                            EditorGUILayout.PropertyField(m_properties.MinimumParticleMass, Labels.MinimumParticleMass);
                            EditorGUILayout.PropertyField(m_properties.MaximumParticleMass, Labels.MaximumParticleMass);
                            EditorGUILayout.PropertyField(m_properties.MinimumParticleDuration, Labels.MinimumParticleDuration);
                            EditorGUILayout.PropertyField(m_properties.MaximumParticleDuration, Labels.MaximumParticleDuration);
                            EditorGUILayout.PropertyField(m_properties.StartColourGradient, Labels.StartColourGradient);
                            EditorGUILayout.PropertyField(m_properties.EndColourGradient, Labels.EndColourGradient);
                        }
                    }
                }

                if (changeCheck.changed)
                {
                    Validate();
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        private bool IsMouseInArc(out Vector3 startDir, float margin = 0f)
        {
            Vector3 mousePos = GetMousePosInSceneView();
            float distToMouse = Vector3.Distance(m_transform.position, mousePos);
            startDir = (GetMousePosInSceneView() - m_transform.position).normalized;
            float angleToStart = CheckAngle(-Utils.Get2DClockwiseAngle(m_transform.InverseTransformDirection(startDir)) + 90f);

            return (distToMouse >= m_properties.InnerRadius.floatValue + margin &&
                distToMouse <= m_properties.OuterRadius.floatValue - margin &&
                angleToStart >= m_properties.StartAngle.floatValue + margin &&
                angleToStart <= (m_properties.StartAngle.floatValue + m_properties.AngleSize.floatValue - margin));
        }

        private void OnSceneGUI()
        {
            serializedObject.Update();

            HandleUtility.Repaint();

            // if already interacting with something, block the custom handles in this class from interacting
            if (GUIUtility.hotControl != 0)
                m_currentID = -1;

            Handles.matrix = m_transform.localToWorldMatrix;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            HandleMouse();

            bool changeCheck = false;

            changeCheck |= DrawDiscControl(0, m_properties.InnerRadius.floatValue, f => OnRadiusDrag(m_properties.InnerRadius, "Set Inner Radius", f));
            changeCheck |= DrawDiscControl(1, m_properties.OuterRadius.floatValue, f => OnRadiusDrag(m_properties.OuterRadius, "Set Outer Radius", f));
            changeCheck |= DrawAngleHandle(2, m_properties.StartAngle.floatValue, f => OnStartAngleDrag("Set Start Angle", f));
            changeCheck |= DrawAngleHandle(3, m_properties.StartAngle.floatValue + m_properties.AngleSize.floatValue, f => OnAngleSizeDrag("Set Angle Size", f));
            
            if (changeCheck)
            {
                Validate();
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        float CheckAngle(float angle)
        {
            while (angle < 0f)
                angle += 360f;

            while (angle > 360f)
                angle -= 360f;

            return angle;
        }

        private void Validate()
        {
            void CheckFloatMinMax(SerializedProperty min, SerializedProperty max, bool clampToZero = true)
            {
                if (clampToZero)
                    min.floatValue = Mathf.Clamp(min.floatValue, 0f, max.floatValue);
                else
                    min.floatValue = Mathf.Min(min.floatValue, max.floatValue);

                max.floatValue = Mathf.Max(max.floatValue, min.floatValue);
            }

            void CheckIntMinMax(SerializedProperty min, SerializedProperty max, bool clampToZero = true)
            {
                if (clampToZero)
                    min.intValue = Mathf.Clamp(min.intValue, 0, max.intValue);
                else
                    min.intValue = Mathf.Min(min.intValue, max.intValue);

                max.intValue = Mathf.Max(max.intValue, min.intValue);
            }

            CheckFloatMinMax(m_properties.InnerRadius, m_properties.OuterRadius);

            m_properties.AngleSize.floatValue = CheckAngle(m_properties.AngleSize.floatValue);
            m_properties.StartAngle.floatValue = CheckAngle(m_properties.StartAngle.floatValue);

            m_properties.ParticleEmissionFrequency.floatValue = Mathf.Max(0f, m_properties.ParticleEmissionFrequency.floatValue);

            CheckIntMinMax(m_properties.MinimumParticleEmission, m_properties.MaximumParticleEmission);

            CheckFloatMinMax(m_properties.MinimumParticleSpawnSpeed, m_properties.MaximumParticleSpawnSpeed);
            CheckFloatMinMax(m_properties.MinimumParticleMass, m_properties.MaximumParticleMass, clampToZero: false);
            CheckFloatMinMax(m_properties.MinimumParticleDuration, m_properties.MaximumParticleDuration);
        }

        private bool DrawAngleHandle(int id, float angle, System.Action<float> OnDrag)
        {
            Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f);
            Vector3 from = dir * m_properties.InnerRadius.floatValue;
            Vector3 to = dir * m_properties.OuterRadius.floatValue;

            float dist = HandleUtility.DistanceToLine(from, to);
            bool selected = GUIUtility.hotControl == 0 && dist <= HANDLE_SELECTION_MARGIN;

            float thickness = selected ? SELECTED_THICKNESS : UNSELECTED_THICKNESS;

            Handles.color = Color.green;
            Handles.DrawLine(from, to, thickness);

            bool changed = false;

            if (m_currentID == -1 && selected)
            {
                m_currentID = id;
                changed = true;
            }

            if (m_currentID == id)
            {
                if (m_currentMouseEvent == MouseEvent.Drag)
                    OnDrag?.Invoke(-Utils.Get2DClockwiseAngle(m_transform.InverseTransformDirection((GetMousePosInSceneView() - m_transform.position).normalized)) + 90f);
                else if (m_currentMouseEvent == MouseEvent.Up || m_currentMouseEvent == MouseEvent.None)
                    m_currentID = -1;

                changed = true;
            }

            return changed;
        }

        private bool DrawDiscControl(int id, float radius, System.Action<float> OnDrag)
        {
            float dist = HandleUtility.DistanceToDisc(Vector3.zero, Vector3.back, radius);
            float margin = HANDLE_SELECTION_MARGIN;

            if (radius < 2f)
                margin *= 3f;

            bool selected = GUIUtility.hotControl == 0 && dist <= margin;

            float thickness = selected ? SELECTED_THICKNESS : UNSELECTED_THICKNESS;

            float startAngle = m_properties.StartAngle.floatValue * Mathf.Deg2Rad;
            Vector3 startPos = new Vector3(Mathf.Cos(startAngle), Mathf.Sin(startAngle), 0f);

            float endAngle = (m_properties.StartAngle.floatValue + m_properties.AngleSize.floatValue) * Mathf.Deg2Rad;
            Vector3 endPos = new Vector3(Mathf.Cos(endAngle), Mathf.Sin(endAngle), 0f);

            Handles.color = Color.green;
            Handles.DrawWireArc(Vector3.zero, Vector3.back, startPos, -m_properties.AngleSize.floatValue, radius, thickness);

            Handles.color = Color.grey.SetAlpha(0.3f);
            Handles.DrawWireArc(Vector3.zero, Vector3.back, endPos, m_properties.AngleSize.floatValue - 360f, radius, thickness * 0.2f);

            bool changed = false;

            if (m_currentID == -1 && selected)
            {
                m_currentID = id;
                changed = true;
            }

            if (m_currentID == id)
            {
                if (m_currentMouseEvent == MouseEvent.Drag)
                    OnDrag?.Invoke(Vector3.Distance(GetMousePosInSceneView(), m_transform.position));
                else if (m_currentMouseEvent == MouseEvent.Up || m_currentMouseEvent == MouseEvent.None)
                    m_currentID = -1;

                changed = true;
            }

            return changed;
        }

        private void OnRadiusDrag(SerializedProperty property, string undo, float radius)
        {
            Undo.RecordObject(m_target, undo);
            property.floatValue = radius;
        }

        private void OnStartAngleDrag(string undo, float angle)
        {
            Undo.RecordObject(m_target, undo);

            float angleSize = (m_properties.AngleSize.floatValue + m_properties.StartAngle.floatValue - angle);

            while (angleSize < 0f)
                angleSize += 360f;

            while (angleSize > 360f)
                angleSize -= 360f;

            m_properties.AngleSize.floatValue = angleSize;
            m_properties.StartAngle.floatValue = angle;
        }

        private void OnAngleSizeDrag(string undo, float angle)
        {
            Undo.RecordObject(m_target, undo);

            float angleSize = angle - m_properties.StartAngle.floatValue;

            while (angleSize < 0f)
                angleSize += 360f;

            while (angleSize > 360f)
                angleSize -= 360f;

            m_properties.AngleSize.floatValue = angleSize;
        }

        private void HandleMouse()
        {
            if (m_currentMouseEvent == MouseEvent.Down)
                m_currentMouseEvent = MouseEvent.Held;
            else if (m_currentMouseEvent == MouseEvent.Up)
                m_currentMouseEvent = MouseEvent.None;

            Event e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        m_currentMouseEvent = MouseEvent.Down;

                        HandleUtility.Repaint();
                    }
                    break;
                case EventType.MouseDrag:
                    if (e.button == 0)
                    {
                        m_currentMouseEvent = MouseEvent.Drag;

                        HandleUtility.Repaint();
                    }
                    break;
                case EventType.MouseUp:
                    if (e.button == 0)
                    {
                        m_currentMouseEvent = MouseEvent.Up;

                        HandleUtility.Repaint();
                    }
                    break;
            }
        }


        #region Helper methods

        private static Vector3 GetMousePosInSceneView()
        {
            Vector3 mousePosition = Event.current.mousePosition;
            Camera sceneCamera = SceneView.currentDrawingSceneView.camera;

            mousePosition.y = sceneCamera.pixelHeight - mousePosition.y;
            mousePosition = sceneCamera.ScreenToWorldPoint(mousePosition);

            return mousePosition.SetZ(0f);
        }

        #endregion
    }
}

