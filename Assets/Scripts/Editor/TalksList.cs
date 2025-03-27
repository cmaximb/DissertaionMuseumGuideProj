using System.Collections;
using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;

//[CustomEditor(typeof(VolumetricSwitcher))]
//public class TalksList : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        VolumetricSwitcher switcher = (VolumetricSwitcher)target;

//        serializedObject.Update();

//        // Header
//        EditorGUILayout.LabelField("Volumetric Switcher Setup", EditorStyles.boldLabel);

//        // List of Depthkit-Marionette pairs
//        SerializedProperty exhibitTalks = serializedObject.FindProperty("exhibitTalks");

//        for (int i = 0; i < exhibitTalks.arraySize; i++)
//        {
//            SerializedProperty talk = exhibitTalks.GetArrayElementAtIndex(i);
//            SerializedProperty depthkitClip = talk.FindPropertyRelative("depthkitObject");
//            SerializedProperty marionetteCapture = talk.FindPropertyRelative("Marionette");

//            EditorGUILayout.BeginVertical("box");

//            EditorGUILayout.LabelField($"Talk {i + 1}", EditorStyles.boldLabel);

//            EditorGUILayout.PropertyField(depthkitClip, new GUIContent("depthkitObject"));
//            EditorGUILayout.PropertyField(marionetteCapture, new GUIContent("Marionette"));

//            if (GUILayout.Button("Remove Talk"))
//            {
//                exhibitTalks.DeleteArrayElementAtIndex(i);
//            }

//            EditorGUILayout.EndVertical();
//        }

//        // Add new pair button
//        if (GUILayout.Button("Add New Pair"))
//        {
//            exhibitTalks.InsertArrayElementAtIndex(exhibitTalks.arraySize);
//        }

//        serializedObject.ApplyModifiedProperties();
//    }
//}
