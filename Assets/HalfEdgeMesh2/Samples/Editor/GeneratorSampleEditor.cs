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

        // Conway operator properties
        SerializedProperty applyConwayKis;
        SerializedProperty kisHeight;

        SerializedProperty applyConwayDual;

        SerializedProperty applyConwayAmbo;

        SerializedProperty applyConwayGyro;
        SerializedProperty gyroSpinRatio;

        SerializedProperty applyConwayZip;
        SerializedProperty zipHeight;

        SerializedProperty applyConwayExpand;

        SerializedProperty applyConwayBevel;
        SerializedProperty bevelRatio;

        SerializedProperty applyConwayOrtho;

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

            // Conway operators
            applyConwayKis = serializedObject.FindProperty("applyConwayKis");
            kisHeight = serializedObject.FindProperty("kisHeight");

            applyConwayDual = serializedObject.FindProperty("applyConwayDual");

            applyConwayAmbo = serializedObject.FindProperty("applyConwayAmbo");

            applyConwayGyro = serializedObject.FindProperty("applyConwayGyro");
            gyroSpinRatio = serializedObject.FindProperty("gyroSpinRatio");

            applyConwayZip = serializedObject.FindProperty("applyConwayZip");
            zipHeight = serializedObject.FindProperty("zipHeight");

            applyConwayExpand = serializedObject.FindProperty("applyConwayExpand");

            applyConwayBevel = serializedObject.FindProperty("applyConwayBevel");
            bevelRatio = serializedObject.FindProperty("bevelRatio");

            applyConwayOrtho = serializedObject.FindProperty("applyConwayOrtho");
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

            EditorGUILayout.Space();

            // Conway Operators
            EditorGUILayout.LabelField("Conway Operators", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Conway operators create new topology. They are applied first, before vertex modifiers.", MessageType.Info);

            // Kis operator
            EditorGUILayout.PropertyField(applyConwayKis, new GUIContent("Apply Kis", "Subdivides each face into triangles from a raised center point"));
            if (applyConwayKis.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(kisHeight, new GUIContent("Height", "Height offset for center vertices"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Dual operator
            EditorGUILayout.PropertyField(applyConwayDual, new GUIContent("Apply Dual", "Swaps faces and vertices (face centroids become vertices)"));

            EditorGUILayout.Space(5);

            // Ambo operator
            EditorGUILayout.PropertyField(applyConwayAmbo, new GUIContent("Apply Ambo", "Places vertices at edge midpoints"));

            EditorGUILayout.Space(5);

            // Gyro operator
            EditorGUILayout.PropertyField(applyConwayGyro, new GUIContent("Apply Gyro", "Rotates and subdivides faces"));
            if (applyConwayGyro.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(gyroSpinRatio, new GUIContent("Spin Ratio", "How much to rotate faces (0-1)"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Zip operator
            EditorGUILayout.PropertyField(applyConwayZip, new GUIContent("Apply Zip", "Dual of Kis - creates pyramids at vertices"));
            if (applyConwayZip.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(zipHeight, new GUIContent("Height", "Height offset for raised vertices"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Expand operator
            EditorGUILayout.PropertyField(applyConwayExpand, new GUIContent("Apply Expand", "Moves faces apart and fills gaps"));

            EditorGUILayout.Space(5);

            // Bevel operator
            EditorGUILayout.PropertyField(applyConwayBevel, new GUIContent("Apply Bevel", "Truncates edges creating rectangular faces"));
            if (applyConwayBevel.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(bevelRatio, new GUIContent("Ratio", "How far along edges to place vertices (0.1-0.4)"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Ortho operator
            EditorGUILayout.PropertyField(applyConwayOrtho, new GUIContent("Apply Ortho", "Creates all-quad mesh (medial)"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
