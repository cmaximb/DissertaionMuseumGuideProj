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

        if (FindObjectOfType<VolumetricSwitcher>() == null ) { return; }

        VolumetricSwitcher switcher = FindObjectOfType<VolumetricSwitcher>();
        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty element = steps.GetArrayElementAtIndex(i);
            if ((element.managedReferenceValue is BaseActionTransition step) && switcher.GetRiggedModel() != null)
            {
                GameObject riggedModel = switcher.GetRiggedModel();
                riggedModel.AddComponent<Animator>();
                step.Init(switcher.GetRiggedModel(), switcher);
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
                
                BaseActionTransition newStep = new Transition();
                newStep.Init(switcher.GetRiggedModel(), switcher);
                newElement.managedReferenceValue = newStep;

            }
            else
            {
                BaseActionTransition newStep = new Action();
                newStep.Init(switcher.GetRiggedModel(), switcher);
                newElement.managedReferenceValue = newStep;
            }
        }

        if (steps.arraySize > 1 && GUILayout.Button("Remove Last Step"))
        {
            steps.DeleteArrayElementAtIndex(steps.arraySize - 1);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
