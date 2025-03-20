using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static System.TimeZoneInfo;

[CustomEditor(typeof(VolumetricSwitcher))]
public class EnsureNextSequenceAdded : Editor
{
    private SerializedProperty steps;

    private void OnEnable()
    {
        steps = serializedObject.FindProperty("steps");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        VolumetricSwitcher switcher = FindObjectOfType<VolumetricSwitcher>();
        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty element = steps.GetArrayElementAtIndex(i);
            if (element.managedReferenceValue is BaseActionTransition step)
            {
                step.Init(switcher.GetRiggedModel());
            }
        }

        DrawPropertiesExcluding(serializedObject, "steps");

        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Steps", EditorStyles.boldLabel);

        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty element = steps.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(element, new GUIContent($"Step {i + 1}"), true);
        }

        EditorGUILayout.Space();
        

        if (GUILayout.Button("Add Next Step"))
        {
            bool lastIsAction = steps.arraySize == 0 || steps.GetArrayElementAtIndex(steps.arraySize - 1).managedReferenceValue is Action;

            steps.InsertArrayElementAtIndex(steps.arraySize);
            SerializedProperty newElement = steps.GetArrayElementAtIndex(steps.arraySize - 1);
            if (lastIsAction)
            {
                newElement.managedReferenceValue = new Transition();
                
            }
            else
                newElement.managedReferenceValue = new Action();
        }

        if (steps.arraySize > 1 && GUILayout.Button("Remove Last Step"))
        {
            steps.DeleteArrayElementAtIndex(steps.arraySize - 1);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
