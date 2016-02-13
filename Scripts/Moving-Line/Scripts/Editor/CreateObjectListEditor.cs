using UnityEditor;
using UnityEngine;
using System.Collections;
using TileMatch.MovingLine;

[CustomEditor(typeof(CreateObjectList))]
public class CreateObjectListEditor : Editor 
{
	private SerializedProperty m_maximumObjectCount = null;
	private SerializedProperty m_addWhenMaxCountReached = null;
	private SerializedProperty m_addCustomObject = null;
	private SerializedProperty m_availableObjects = null;

	public void OnEnable()
	{
		m_maximumObjectCount = serializedObject.FindProperty("MaximumObjectCount");
		m_addWhenMaxCountReached = serializedObject.FindProperty("AddWhenMaxCountReached");
		m_addCustomObject = serializedObject.FindProperty("AddCustomObject");
		m_availableObjects = serializedObject.FindProperty("AvailableObjects");
	}

	override public void OnInspectorGUI()
	{
		// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
		serializedObject.Update();

		CreatePropertyField(m_maximumObjectCount, "Max object count:", "The number of objects which will be added to the list.");


		CreateLabel("Use objects:", "When creating a list, the component will use the objects which are added to this list.", true);
		CreateArrayPropertyField(m_availableObjects, "Prefab:", "When creating a list, the component will use the objects which are added to this list.");

		EditorGUILayout.Space();
		CreateLabel("Add when max object count is reached:", "When the maximum object count is reached, one of the options can be used:\\n\\nNone:\\nNo GameObject will be added and the element in the object list will be empty.\\n\\nCustom Object:\\nA custom GaneObject will be added.", true);
		CreatePropertyField(m_addWhenMaxCountReached, string.Empty, string.Empty);
		if(string.Equals(CreateObjectList.AddType.CustomObject.ToString(), m_addWhenMaxCountReached.enumNames[m_addWhenMaxCountReached.enumValueIndex]) == true)
			CreatePropertyField(m_addCustomObject, "Custom object:", "If 'AddWhenMaxCountIsReached' enum is set to 'CustomObject', the component will use this prefab and add to the object list.");

		// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
		serializedObject.ApplyModifiedProperties();
	}

	private void CreatePropertyField(SerializedProperty currentProperty, string text, string tooltipText)
	{
		GUIContent content = new GUIContent(text, tooltipText);
		EditorGUILayout.PropertyField(currentProperty, content);
	}

	private void CreateArrayPropertyField(SerializedProperty arrayObject, string text, string tooltipText)
	{
		if(CreateButton("Add", "Add element to list.") == true)
			arrayObject.arraySize += 1;

		int objectCount = arrayObject.arraySize;
		for(int i = 0; i < objectCount; ++i)
		{
			EditorGUILayout.BeginHorizontal();
			CreatePropertyField(arrayObject.GetArrayElementAtIndex(i), text, tooltipText);
			bool removeButtonIsPressed = CreateButton("Remove", "Removes the current element from the list.");
			EditorGUILayout.EndHorizontal();
			if(removeButtonIsPressed == true)
			{
				arrayObject.DeleteArrayElementAtIndex(i);
				break;
			}
		}
	}

	private bool CreateButton(string text, string tooltipText)
	{
		GUIContent content = new GUIContent(text, tooltipText);
		return GUILayout.Button(content);
	}

	public void CreateLabel(string text, string tooltipText, bool boldText)
	{
		GUIContent content = new GUIContent(text, tooltipText);
		if(boldText == false)
			EditorGUILayout.LabelField(content);
		else
			EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
	}
}
