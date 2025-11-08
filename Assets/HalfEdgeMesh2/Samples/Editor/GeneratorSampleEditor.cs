using UnityEngine;
using UnityEditor;
using HalfEdgeMesh2.Samples;

namespace HalfEdgeMesh2.Editor
{
    [CustomEditor(typeof(GeneratorSample))]
    public class GeneratorSampleEditor : UnityEditor.Editor
    {
        SerializedProperty generatorType;
        SerializedProperty normalMode;
        SerializedProperty animateSize;

        // Box
        SerializedProperty boxSize;
        SerializedProperty boxSegments;

        // Sphere
        SerializedProperty sphereRadius;
        SerializedProperty sphereSegments;

        // Cylinder
        SerializedProperty cylinderRadius;
        SerializedProperty cylinderHeight;
        SerializedProperty cylinderSegments;
        SerializedProperty cylinderCapped;

        // Plane
        SerializedProperty planeSize;
        SerializedProperty planeSegments;

        // Cone
        SerializedProperty coneRadius;
        SerializedProperty coneHeight;
        SerializedProperty coneSegments;

        // Torus
        SerializedProperty torusMajorRadius;
        SerializedProperty torusMinorRadius;
        SerializedProperty torusSegments;

        // Platonic Solids
        SerializedProperty platonicSize;

        // Icosphere
        SerializedProperty icosphereRadius;
        SerializedProperty icosphereSubdivisions;

        // Modifier properties
        SerializedProperty applySmooth;
        SerializedProperty smoothingFactor;
        SerializedProperty smoothingIterations;

        SerializedProperty applyExpand;
        SerializedProperty expandDistance;

        SerializedProperty applyStretch;
        SerializedProperty stretchScale;

        SerializedProperty applyTwist;
        SerializedProperty twistAngle;
        SerializedProperty twistAxis;

        SerializedProperty applySkew;
        SerializedProperty skewAngle;
        SerializedProperty skewDirection;

        void OnEnable()
        {
            generatorType = serializedObject.FindProperty("generatorType");
            normalMode = serializedObject.FindProperty("normalMode");
            animateSize = serializedObject.FindProperty("animateSize");

            // Box
            boxSize = serializedObject.FindProperty("boxSize");
            boxSegments = serializedObject.FindProperty("boxSegments");

            // Sphere
            sphereRadius = serializedObject.FindProperty("sphereRadius");
            sphereSegments = serializedObject.FindProperty("sphereSegments");

            // Cylinder
            cylinderRadius = serializedObject.FindProperty("cylinderRadius");
            cylinderHeight = serializedObject.FindProperty("cylinderHeight");
            cylinderSegments = serializedObject.FindProperty("cylinderSegments");
            cylinderCapped = serializedObject.FindProperty("cylinderCapped");

            // Plane
            planeSize = serializedObject.FindProperty("planeSize");
            planeSegments = serializedObject.FindProperty("planeSegments");

            // Cone
            coneRadius = serializedObject.FindProperty("coneRadius");
            coneHeight = serializedObject.FindProperty("coneHeight");
            coneSegments = serializedObject.FindProperty("coneSegments");

            // Torus
            torusMajorRadius = serializedObject.FindProperty("torusMajorRadius");
            torusMinorRadius = serializedObject.FindProperty("torusMinorRadius");
            torusSegments = serializedObject.FindProperty("torusSegments");

            // Platonic Solids
            platonicSize = serializedObject.FindProperty("platonicSize");

            // Icosphere
            icosphereRadius = serializedObject.FindProperty("icosphereRadius");
            icosphereSubdivisions = serializedObject.FindProperty("icosphereSubdivisions");

            // Find modifier properties
            applySmooth = serializedObject.FindProperty("applySmooth");
            smoothingFactor = serializedObject.FindProperty("smoothingFactor");
            smoothingIterations = serializedObject.FindProperty("smoothingIterations");

            applyExpand = serializedObject.FindProperty("applyExpand");
            expandDistance = serializedObject.FindProperty("expandDistance");

            applyStretch = serializedObject.FindProperty("applyStretch");
            stretchScale = serializedObject.FindProperty("stretchScale");

            applyTwist = serializedObject.FindProperty("applyTwist");
            twistAngle = serializedObject.FindProperty("twistAngle");
            twistAxis = serializedObject.FindProperty("twistAxis");

            applySkew = serializedObject.FindProperty("applySkew");
            skewAngle = serializedObject.FindProperty("skewAngle");
            skewDirection = serializedObject.FindProperty("skewDirection");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Generator Settings
            EditorGUILayout.LabelField("Generator Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(generatorType);
            EditorGUILayout.PropertyField(normalMode);
            EditorGUILayout.Space();

            // Animation Settings
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(animateSize);
            EditorGUILayout.Space();

            // Generator-specific settings
            var selectedType = (GeneratorSample.GeneratorType)generatorType.enumValueIndex;

            switch (selectedType)
            {
                case GeneratorSample.GeneratorType.Box:
                    EditorGUILayout.LabelField("Box Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(boxSize);
                    EditorGUILayout.PropertyField(boxSegments);
                    break;

                case GeneratorSample.GeneratorType.Sphere:
                    EditorGUILayout.LabelField("Sphere Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(sphereRadius);
                    EditorGUILayout.PropertyField(sphereSegments);
                    break;

                case GeneratorSample.GeneratorType.Cylinder:
                    EditorGUILayout.LabelField("Cylinder Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(cylinderRadius);
                    EditorGUILayout.PropertyField(cylinderHeight);
                    EditorGUILayout.PropertyField(cylinderSegments);
                    EditorGUILayout.PropertyField(cylinderCapped);
                    break;

                case GeneratorSample.GeneratorType.Plane:
                    EditorGUILayout.LabelField("Plane Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(planeSize);
                    EditorGUILayout.PropertyField(planeSegments);
                    break;

                case GeneratorSample.GeneratorType.Cone:
                    EditorGUILayout.LabelField("Cone Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(coneRadius);
                    EditorGUILayout.PropertyField(coneHeight);
                    EditorGUILayout.PropertyField(coneSegments);
                    break;

                case GeneratorSample.GeneratorType.Torus:
                    EditorGUILayout.LabelField("Torus Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(torusMajorRadius);
                    EditorGUILayout.PropertyField(torusMinorRadius);
                    EditorGUILayout.PropertyField(torusSegments);
                    break;

                case GeneratorSample.GeneratorType.Tetrahedron:
                case GeneratorSample.GeneratorType.Octahedron:
                case GeneratorSample.GeneratorType.Dodecahedron:
                    EditorGUILayout.LabelField("Platonic Solid Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(platonicSize);
                    break;

                case GeneratorSample.GeneratorType.Icosphere:
                    EditorGUILayout.LabelField("Icosphere Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(icosphereRadius);
                    EditorGUILayout.PropertyField(icosphereSubdivisions);
                    break;
            }

            EditorGUILayout.Space();

            // Modifier Settings
            EditorGUILayout.LabelField("Modifiers", EditorStyles.boldLabel);

            // Smooth Vertices modifier
            EditorGUILayout.PropertyField(applySmooth, new GUIContent("Apply Smooth"));
            if (applySmooth.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(smoothingFactor);
                EditorGUILayout.PropertyField(smoothingIterations);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Expand Vertices modifier
            EditorGUILayout.PropertyField(applyExpand, new GUIContent("Apply Expand"));
            if (applyExpand.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(expandDistance);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Stretch Mesh modifier
            EditorGUILayout.PropertyField(applyStretch, new GUIContent("Apply Stretch"));
            if (applyStretch.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(stretchScale);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Twist Mesh modifier
            EditorGUILayout.PropertyField(applyTwist, new GUIContent("Apply Twist"));
            if (applyTwist.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(twistAngle);
                EditorGUILayout.PropertyField(twistAxis);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Skew Mesh modifier
            EditorGUILayout.PropertyField(applySkew, new GUIContent("Apply Skew"));
            if (applySkew.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(skewAngle);
                EditorGUILayout.PropertyField(skewDirection);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
