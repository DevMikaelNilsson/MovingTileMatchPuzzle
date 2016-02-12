using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace TileMatch.MovingLine
{
	public class CreateObjectList : MonoBehaviour
	{
		/// <summary>
		/// The number of objects which will be added to the list.
		/// </summary>
		[Tooltip("The number of objects which will be added to the list.")]
		public int MaximumObjectCount = 10;

		/// <summary>
		/// When creating a list, the component will use the objects which are added to this list.
		/// </summary>
		[Tooltip("When creating a list, the component will use the objects which are added to this list.")]
		public GameObject []AvailableObjects = null;

		private List<GameObject> m_predefinedObjectList = new List<GameObject>();

		public List<GameObject> CreateList()
		{
			if (m_predefinedObjectList.Count < MaximumObjectCount) 
			{
				int objectCount = (MaximumObjectCount - m_predefinedObjectList.Count);
				for (int i = 0; i < objectCount; ++i) 
				{
					int randomIndex = UnityEngine.Random.Range (0, AvailableObjects.Length);
					GameObject newObject = GameObject.Instantiate (AvailableObjects [randomIndex]);
					if (newObject != null) {
						newObject.SetActive (false);
						m_predefinedObjectList.Add (newObject);
					} 
					else
						Debug.LogWarning (this + " - Could not create object from prefab (" + AvailableObjects [randomIndex] + ").");
				}
			}
		
			return new List<GameObject> (m_predefinedObjectList);
		}
	}
}
