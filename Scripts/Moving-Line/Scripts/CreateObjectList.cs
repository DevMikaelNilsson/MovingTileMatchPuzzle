using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace TileMatch.MovingLine
{
	public class CreateObjectList : MonoBehaviour
	{
		public enum AddType
		{
			None = 0,
			CustomObject = 1
		}

		/// <summary>
		/// The number of objects which will be added to the list.
		/// </summary>
		[Tooltip("The number of objects which will be added to the list.")]
		public int MaximumObjectCount = 10;

		/// <summary>
		/// When the maximum object count is reached, one of the options can be used:
		/// None:
		/// No GameObject will be added and the element in the object list will be empty.
		/// Custom Object:
		/// A custom GaneObject will be added.
		/// </summary>
		[Tooltip("When the maximum object count is reached, one of the options can be used:\\n\\nNone:\\nNo GameObject will be added and the element in the object list will be empty.\\n\\nCustom Object:\\nA custom GaneObject will be added.")]
		public AddType AddWhenMaxCountReached = AddType.None;

		/// <summary>
		/// If 'AddWhenMaxCountIsReached' enum is set to 'CustomObject', the component will use this prefab and add to the object list.
		/// </summary>
		[Tooltip("If 'AddWhenMaxCountIsReached' enum is set to 'CustomObject', the component will use this prefab and add to the object list.")]
		public GameObject AddCustomObject = null;

		/// <summary>
		/// When creating a list, the component will use the objects which are added to this list.
		/// </summary>
		[Tooltip("When creating a list, the component will use the objects which are added to this list.")]
		public GameObject []AvailableObjects = null;

		/// <summary>
		/// This list contains all pre-defined objects in a single list.
		/// </summary>
		protected List<GameObject> m_predefinedObjectList = new List<GameObject>();

		/// <summary>
		/// Returns the current object list.
		/// If the list is empty, or have less objects than the MaximumObjectCount value, a new list will be generated and returned.
		/// </summary>
		/// <returns>A list with all objects.</returns>
		public List<GameObject> GetList()
		{	
			if (m_predefinedObjectList.Count < MaximumObjectCount)
				return CreateList();

			return new List<GameObject> (m_predefinedObjectList);
		}

		/// <summary>
		/// Creates a list with objects.
		/// The method will clear the existing list (if any) and add new objects to the list.
		/// </summary>
		/// <returns>The list.</returns>
		public List<GameObject> CreateList()
		{
			m_predefinedObjectList.Clear();
			int objectCount = (MaximumObjectCount);
			for (int i = 0; i < objectCount; ++i) 
			{
				int randomIndex = UnityEngine.Random.Range (0, AvailableObjects.Length);
				if (AvailableObjects[randomIndex] != null)
					m_predefinedObjectList.Add(AvailableObjects[randomIndex]);
				else
					Debug.LogWarning (this + " - Could not create object from prefab (" + AvailableObjects [randomIndex] + ").");
			}
		
			return new List<GameObject> (m_predefinedObjectList);
		}

		/// <summary>
		/// Gets the custom object prefab.
		/// Do note that the flag 'AddWhenMaxCountReached' must be set to 'CustomObject' in order to return the prefab object.
		/// If the flag is NOT set to 'CustomObject', or if the CustomObject is null, this method will return null by default.
		/// </summary>
		/// <returns>A custom object prefab.</returns>
		public GameObject GetCustomObject()
		{
			switch(AddWhenMaxCountReached)
			{
				case AddType.CustomObject:
					return AddCustomObject;
			}

			return null;
		}
	}
}
