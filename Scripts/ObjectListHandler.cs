using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace TileMatch
{
	public class ObjectListHandler : MonoBehaviour 
	{
		public enum TypeOfEffect
		{
			GameOver = 0,
			LevelComplete = 1,
			Match = 2,
			None = 3
		}

		public enum TypeofListManagement
		{
			/// <summary>
			/// Removes a element from the list. The existing elements in the list, will automatically
			/// receive new element index values, and will direct translate to the proper index position.
			/// </summary>
			RemoveElementFromList = 0,

			/// <summary>
			/// A element is replaced with a empty element. The existing elements in the list will stay at their current 
			/// index and its position. If a existing element is going to be moved to a element position which is empty, the existing element will
			/// simply override the empty element, which will be removed.
			/// </summary>
			ReplaceElementWithEmpty = 1
		}

		[Tooltip("What kind of management the list should be populated and managed with:\n\nRemoveElementFromList\nElements will be removed and the size of the list will vary. Objects in the list will automatically be re-positioned backwards if a element is removed.\n\nReplaceElementWithEmpty\nElements which should be removed will be replaced by a empty element, making all existing objects to remain at their current position. Existing elements will simply override a ghost element when reaching a ghosts current element index.")]
		public TypeofListManagement ListManagement = TypeofListManagement.RemoveElementFromList;

		[Tooltip("Bezier Curve object which the objects which the handler generates should follow. The curve needs to have atleast 2 points in order to be valid.")]
		public BezierCurve PathCurve = null; 

		[Tooltip("A single step value for when creating a waypoint list from the PathCurve. Objects following the curve will use these waypoints for translating through the curve. The lower the value is, the more precise will the waypoints be, but it will take more time to generate.")]
		public float PathMovement = 0.0001f;
		[Tooltip("The minimum distance between two waypoints in the waypoint list from the PathCurve. Objects following the curve will use these waypoints for translating through the curve. The lower the value, the closer will the objects be each other.")]
		public float PathDistance = 0.7f;

		public float MaxPathValue = 0.95f;
		public float RollbackDuration = 0.2f;

		private List<float> m_waypointPositions = new List<float>();
		private List<Vector3> m_waypointVectorPositions = new List<Vector3>();
		private List<ObjectBase> m_pathCurveObjects = new List<ObjectBase>();
		private float m_currentProcentage = 0.0f;
		private GameplayHandler m_gameplayHandlerComponent = null;

		/// <summary>
		/// Internal Unity method.
		/// This method is called whenever the object is enabled/re-enabled.
		/// Initializes all local variables which are frequently used.
		/// </summary>
		void OnEnable()
		{
			if(m_gameplayHandlerComponent == null)
				m_gameplayHandlerComponent = this.GetComponent<GameplayHandler>();

			StartCoroutine(DelayCreateList());
		}

		/// <summary>
		/// Co-routine which creates a list of waypoint positions for the Path curve.
		/// The delay is used to make sure that all Curve points are properly initialized before
		/// attempting to access them.
		/// </summary>
		private IEnumerator DelayCreateList()
		{
			yield return new WaitForEndOfFrame();
			if (m_waypointPositions.Count == 0)
				CreateList();
		}

		/// <summary>
		/// Creates a list of waypoint positions based on the PathCurve.
		/// The method will traverse through the whole PathCurve and when the distance
		/// is big enough, a waypoint will be created and added to the internal list.
		/// </summary>
		private void CreateList()
		{
			m_currentProcentage = 0.0f;
			m_waypointPositions.Clear();

			if(PathCurve != null)
			{
				do
				{
					AddToList();
				}
				while (m_currentProcentage < 1.0f);

				m_waypointPositions[(m_waypointPositions.Count - 1)] = 1.0f;
			}
			else
				Debug.LogWarning(this + " - PathCurve object is either missing or not valid.");

		}

		/// <summary>
		/// Adds a single waypoint element to the internal waypoint list.
		/// </summary>
		/// <returns>Index value for the created element entry in the list.</returns>
		private int AddToList()
		{
			float distance = 0;
			float prevProcentage = m_currentProcentage;

			if (m_currentProcentage < 1.0f)
			{
				Vector3 currPos = Vector3.zero;
				Vector3 prevPos = Vector3.zero;
				do
				{
					prevPos = PathCurve.GetPointAt(prevProcentage);
					currPos = PathCurve.GetPointAt(m_currentProcentage);
					distance = Vector3.Distance(prevPos, currPos);
					m_currentProcentage += PathMovement;

					if(m_currentProcentage >= 1.0f)
						break;
				}
				while (distance < PathDistance);

				m_waypointVectorPositions.Add(PathCurve.GetPointAt(m_currentProcentage));
				m_waypointPositions.Add(m_currentProcentage);
				m_currentProcentage = Mathf.Clamp((m_currentProcentage + PathMovement), 0.0f, 1.0f);
			}
			else
				m_waypointPositions[(m_waypointPositions.Count - 1)] = m_currentProcentage;

			
			return (m_waypointPositions.Count - 1);
		}

		public float DestroyAllObjects(GameObject destructionEffect, bool includeGhostObject)
		{
			int objectCount = (m_pathCurveObjects.Count - 1);
			int currentObjectCount = 1;
			for(int i = objectCount; i >= 0; --i)
			{
				float delayTime = (currentObjectCount * 0.075f);
				if (includeGhostObject == false && m_pathCurveObjects[i].ColorType == ObjectBase.ObjectColor.Ghost)
					StartCoroutine(DelayRemoveObject(delayTime, m_pathCurveObjects[i], null));
				else
				{
					StartCoroutine(DelayRemoveObject(delayTime, m_pathCurveObjects[i], destructionEffect));
					currentObjectCount++;
				}
			}

			return  (currentObjectCount * 0.075f);
		}

		public IEnumerator DelayRemoveObject(float delayValue, ObjectBase objectToRemove, GameObject destructionEffect)
		{
			yield return new WaitForSeconds(delayValue);
			if(destructionEffect != null)
				objectToRemove.CreateEffect(destructionEffect);

			objectToRemove.DestroyObject();
		}

		/// <summary>
		/// Adds a object, with a SphereHandler component, to the internal list.
		/// </summary>
		/// <param name="addObjectToList">The object to add to the internal list.</param>
		/// <returns>The objects new position.</returns>
		public Vector3 AddObjectToList(ObjectBase addObjectToList)
		{
			m_pathCurveObjects.Insert(0, addObjectToList);
			addObjectToList.Index = 0;
			int normal  = 0;
			int ghostIndex = 0;

			if (addObjectToList.ColorType != ObjectBase.ObjectColor.Ghost)
				ghostIndex = GetNextGhostObjectIndex(0);
			else
			{
				normal = GetNextNonGhostObjectIndex(0);
				ghostIndex = GetNextGhostObjectIndex(normal);
			}

			if (ghostIndex != -1)
			{
				ObjectBase ghostObject = GetObjectBase(ghostIndex);
				if(ghostObject != null)
				{
					RemoveObject(ghostObject);
					ghostObject.DestroyObject();
				}
			}

			m_gameplayHandlerComponent.UpdatePlayerSphereQueue();
			RunThroughList(0, 1.0f);
			return GetPosition(0);
		}
	
		/// <summary>
		/// Updates the index value and position for all objects in the internal list.
		/// </summary>
		/// <param name="endIndex">The position in the list which should be the starting point.</param>
		/// <param name="speedDiff">Value which changes the translation time for all objects. 1.0f is normal speed. 0.5f is double the normal speed. 2.0f means half the normal speed.</param>
		public void RunThroughList(int endIndex, float speedDiff)
		{	
			int objectCount = (m_pathCurveObjects.Count - 1);
			int startIndex = Mathf.Clamp(endIndex, 0, objectCount);
			for(int i = (objectCount); i >= startIndex; --i)
			{
				if(i < m_waypointPositions.Count)
					m_pathCurveObjects[i].UpdateIndex(i, m_waypointPositions[i], speedDiff);
			}
		}

		public void RunThroughRollbackList(int endIndex, int matchCount, float duration)
		{	
			int objectCount = (m_pathCurveObjects.Count - 1);
			int startIndex = Mathf.Clamp(endIndex, 0, objectCount);
			for(int i = (objectCount); i >= startIndex; --i)
			{
				int rollbackIndex = (i - matchCount);
				if(rollbackIndex >= 0 && rollbackIndex < objectCount)
					m_pathCurveObjects[i].UpdateRollbackPosition(rollbackIndex, m_waypointPositions[rollbackIndex]);
			}
		}

		public float GetWapPointPosition(int index)
		{
			return m_waypointPositions[index];
		}

		public Vector3 GetCurvePointPosition(float curvePoint)
		{
			curvePoint = Mathf.Clamp(curvePoint, 0.0f, MaxPathValue);
			return PathCurve.GetPointAt(curvePoint);
		}

		/// <summary>
		/// Retrieves the position for a object based on the index value.
		/// </summary>
		/// <param name="index">The index value for the object.</param>
		/// <returns>Returns the position based on the given index value.</returns>
		public Vector3 GetPosition(int index)
		{
			float curvePoint = 0;
			if ((m_waypointPositions.Count - 2) > 0 && index >= (m_waypointPositions.Count - 2))
			{
				//if(index < m_pathCurveObjects.Count)
				//	m_pathCurveObjects[index].PathFinished();

				curvePoint = m_waypointPositions[m_waypointPositions.Count - 1];
			}
			else
				curvePoint = m_waypointPositions[index];

			return PathCurve.GetPointAt(curvePoint);
		}

		/// <summary>
		///	Retrieves the position for a object based on the index value.
		/// The method will set the position to the vector3 inparameter.
		/// If the index is not within the internal list, the last position will be used.
		/// </summary>
		/// <param name="index">The index value for the object.</param>
		/// <returns>Returns the position based on the given index value.</returns>
		/// <returns>True if the index value is within the internal way point list. Returns false if the value is not within the internal list.</returns>
		public bool GetPosition(int index, out Vector3 position)
		{
			bool success = true;
			float curvePoint = 0;
			if (index >= (m_waypointPositions.Count - 1))
			{
				curvePoint = m_waypointPositions[m_waypointPositions.Count - 1];
				position = PathCurve.GetPointAt(curvePoint);
				success = false;
			}
			else 
			{
				curvePoint = m_waypointPositions[index];
				position = m_waypointVectorPositions[index];
			}

			return success;
		}

		public int GetWaypointListCount()
		{
			return m_waypointPositions.Count;
		}


		/// <summary>
		/// Remove a object from the internal object list based on a object reference.
		/// The object is only removed if it is found within the internal object list.
		/// The method only removes the object from the list and does not destroy it.
		/// </summary>
		/// <param name="removeObject">Object to search for and remove if found.</param>
		public void RemoveObject(ObjectBase removeObject)
		{
			int index = GetObjectIndexValue(removeObject);
			if(index != -1)
				m_pathCurveObjects.RemoveAt(index);
		}

		/// <summary>
		/// Insert a object at a specific position in the object list.
		/// </summary>
		/// <param name="newObject">Object to add to the object list.</param>
		/// <param name="index">The specific index position in the list to add the object.</param>
		/// <param name="speeddiff">If the objects before the inserted object should have a different speed. 1.0f means normal speed. 0.5f means double the speed. 1.5f means half the speed.</param>
		public void InsertObjectAtIndex(ObjectBase newObject, int index, float speeddiff)
		{
			if(newObject == null)
				Debug.LogWarning(this + " - Object is either missing or not valid.");
			else if(index < 0)
				Debug.LogWarning(this + " - Negative index value is not allowed.");
			else
			{
				if (index >= m_pathCurveObjects.Count)
					m_pathCurveObjects.Add(newObject);
				else if(newObject.ColorType == ObjectBase.ObjectColor.Ghost)
					newObject.DestroyObject();
				else if(m_pathCurveObjects[index].ColorType == ObjectBase.ObjectColor.Ghost)
					m_pathCurveObjects[index] = newObject;
				else
				{
					RemoveGhost(index);
					m_pathCurveObjects.Insert(index, newObject);	
				}

				RunThroughList(index, speeddiff);
			}
		}

		/// <summary>
		/// Removes the first found ghost object from the internal list based on a starting index value.
		/// If a ghost object is found after this start index value, its automatically removed.
		/// </summary>
		/// <param name="startIndex">The index value where the search after a ghost object should start.</param>
		private void RemoveGhost(int startIndex)
		{
			int objectCount = m_pathCurveObjects.Count;
			for (int i = startIndex; i < objectCount; ++i)
			{
				if (m_pathCurveObjects[i].ColorType == ObjectBase.ObjectColor.Ghost)
				{
					if ((i + 1) >= m_pathCurveObjects.Count)
						break;

					if (m_pathCurveObjects[(i + 1)] != null && m_pathCurveObjects[(i + 1)].ColorType != ObjectBase.ObjectColor.Ghost)
					{
						m_pathCurveObjects[i].DestroyObject();
						m_pathCurveObjects.RemoveAt(i);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Retrieves a index value for the next ghost object in the internal object list, starting
		/// at a given index value.
		/// </summary>
		/// <param name="startIndex">Starting index value where the search should begin.</param>
		/// <returns>Index value of the first found ghost object in the list from the given start point. Returns -1 if no object is found.</returns>
		public int GetNextGhostObjectIndex(int startIndex)
		{
			
			int objectCount = (m_pathCurveObjects.Count);
			if(startIndex >= objectCount || startIndex < 0) 
				return -1;

			for (int i = startIndex; i < objectCount; ++i)
			{
				if(m_pathCurveObjects[i] == null)
					continue;
				if(m_pathCurveObjects[i].ColorType == ObjectBase.ObjectColor.Ghost)
					return i;
			}

			return -1;
		}

		public int GetLastObjectIndex(bool includeGhostObject)
		{
			if(includeGhostObject == true)
				return (m_pathCurveObjects.Count - 1);
			else
			{
				int objectCount = (m_pathCurveObjects.Count - 1);
				for (int i = objectCount; i >= 0; --i)
				{
					if (m_pathCurveObjects[i] == null)
						continue;
					if (m_pathCurveObjects[i].ColorType != ObjectBase.ObjectColor.Ghost)
						return i;
				}
			}

			return 0;
		}

		/// <summary>
		/// Retrieves a index value for the next non-ghost object in the internal object list, starting
		/// at a given index value.
		/// </summary>
		/// <param name="startIndex">Starting index value where the search should begin.</param>
		/// <returns>Index value of the first found object in the list from the given start point. Returns -1 if no object is found.</returns>
		private int GetNextNonGhostObjectIndex(int startIndex)
		{
			int objectCount = (m_pathCurveObjects.Count);
			for (int i = 0; i < objectCount; ++i)
				if (m_pathCurveObjects[i].ColorType != ObjectBase.ObjectColor.Ghost)
					return i;

			return -1;
		}

		public int GetActiveObjectCount(bool includeGhostObjects)
		{
			int activeObjects = 0;
			int objectCount = m_pathCurveObjects.Count;
			for(int i = 0; i < objectCount; ++i)
			{
				if(m_pathCurveObjects[i].ColorType == ObjectBase.ObjectColor.Ghost)
				{
					if (includeGhostObjects == true)

						activeObjects++;
				}
				else
					activeObjects++;
			}

			return activeObjects;
		}

		/// <summary>
		/// Get the current index value for a object component.
		/// </summary>
		/// <param name="currentObject">The component which the index value is needed.</param>
		/// <returns>The current valid index value. Returns -1 if the component is not found in the internal list.</returns>
		public int GetObjectIndexValue(ObjectBase currentObject)
		{
			int objectCount = (m_pathCurveObjects.Count - 1);
			for(int i = objectCount; i >= 0; --i)
			{
				if(currentObject == m_pathCurveObjects[i])
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Retrieves a ObjectBase component from the object list, based on its current index value.
		/// Do note that a objects index value may change from time to time, and one object do not have the same
		/// index value at all times.
		/// </summary>
		/// <param name="index">The index value the component should have.</param>
		/// <returns>Returns the ObjectBase component with the given index value. Returns null if a component is not found.</returns>
		public ObjectBase GetObjectBase(int index)
		{
			if(index > 0 && index < m_pathCurveObjects.Count)
				return m_pathCurveObjects[index];

			return null;
		}

		/// <summary>
		/// Checks the object list for possible matches.
		/// The match search is based on the startIndex value and goes both up and down the list.
		/// </summary>
		/// <param name="startIndex">The starting position in the object list.</param>
		public int CheckForMatch(ObjectBase currentObject)
		{
			if (currentObject == null)
				return 0;

			int startIndex = GetObjectIndexValue(currentObject);
			if(startIndex < 0)
				return 0;
			
			List<ObjectBase> matches = new List<ObjectBase>();
			matches.Add(currentObject);
			try
			{
				CheckListForMatch(startIndex, m_pathCurveObjects.Count, currentObject.ColorType, true, matches);
				CheckListForMatch(startIndex, m_pathCurveObjects.Count, currentObject.ColorType, false, matches);
			}
			catch(Exception e)
			{
				Debug.LogError(this + " - Error while checking for object matches:\n" + e.ToString());
			}

			int matchCountValue = matches.Count;

			if(matchCountValue >= 3)
			{
				matches.Sort(SortPlayerIndexAscending);
				SetLastObject(matches[(matchCountValue - 1)]);
				int firstObjectIndex = matches[(matchCountValue - 1)].Index;

				bool useComboEffect = false;
				if(m_gameplayHandlerComponent.CurrentComboCount() > 0)
					useComboEffect = true;

				for(int i = (matchCountValue - 1); i >= 0; --i)
				{
					if(useComboEffect == true)
						matches[i].CreateEffect(ObjectBase.Effect.Combo);
					else
						matches[i].CreateEffect(ObjectBase.Effect.Match);


					matches[i].DestroyObject();					
				}

				RunThroughRollbackList(firstObjectIndex, (matchCountValue), RollbackDuration);

				m_gameplayHandlerComponent.AddToComboPitch();
				m_gameplayHandlerComponent.PlayMatchSound();
			}
			else
				m_gameplayHandlerComponent.ResetComboPitch();

			m_gameplayHandlerComponent.UpdatePlayerSphereQueue();
			return matchCountValue;
		}

		private IEnumerator DelayCheckForCombo(ObjectBase currentObject)
		{
			yield return new WaitForSeconds(RollbackDuration);
			int matchValue = CheckForMatch(currentObject);
			m_gameplayHandlerComponent.AddComboMatchScoreToPlayer(matchValue, currentObject.GetComponent<Transform>().position);
		}

		/// <summary>
		/// Co-routine which locates the last object which is the last neighbour object
		/// from a batched list of matched objects. This located object will be activated to
		/// check for combo match(es) when all the matched objects are removed. 
		/// This functionality is only available through the "RemoveElementFromList" gameplay mode.
		/// </summary>
		/// <param name="currentObject">The last matched object, which will the base for searching for a valid neighbour object.</param>
		private void SetLastObject(ObjectBase currentObject)
		{
			if(ListManagement == TypeofListManagement.RemoveElementFromList)
			{
				int oldIndex = GetObjectIndexValue(currentObject);
				int index = (oldIndex  + 1);
				ObjectBase lastObject = GetObjectBase(index);
				if(lastObject != null)
				{
					StartCoroutine(DelayCheckForCombo(lastObject));
					//lastObject.ActivateCheckForComboMatch();
				}
			}
		}
		
		/// <summary>
		/// Traverse through the internal list and adds matching objects into a list.
		/// The loop check starts at a given index and either ascends or descends the list.
		/// If there is a neighbour which matches the color, its added to the list.
		/// If the neighbour object do not match the color, the loop is broken and the list is returned.
		/// </summary>
		/// <param name="startIndex">The starting point in the internal list where the color comparison should start.</param>
		/// <param name="maxCount">The maximum loops the method is allowed to make. Normally this should be the internal lists element count.</param>
		/// <param name="baseColor">The color which the objects will be compared against.</param>
		/// <param name="ascendingList">Set flag to true to ascend the list. Set to false to descend the list.</param>
		/// <param name="matchList">A list where all positive matches will be added into.</param>
		private void CheckListForMatch(int startIndex, int maxCount, ObjectBase.ObjectColor baseColor , bool ascendingList, List<ObjectBase> matchList)
		{
			int lastMatchedIndex = startIndex;
			int traverseValue = 1;
			if(ascendingList == false)
				traverseValue = -1;

			int index = startIndex;
			int currentCount = 0;
			while (index >= 0 || index < maxCount)
			{
				if (startIndex != index && index < m_pathCurveObjects.Count)
				{
					if(Mathf.Abs(index - lastMatchedIndex) > 1)
						break;
					if(m_pathCurveObjects[index] == null)
						continue;
					if (m_pathCurveObjects[index].ColorType == baseColor)
					{
						matchList.Add(m_pathCurveObjects[index]);
						lastMatchedIndex = index;
					}
					else
						break;
				}

				index += traverseValue;
				if(currentCount >= maxCount)
					break;
				else
					currentCount++;
			}
		}

		/// <summary>
		/// Internal sort method to sort a list based on the objects current index value.
		/// This method should be invoked through the lists own sort method.
		/// Ex.
		/// InternalComponentList.Sort(SortPlayerIndexAscending);
		/// </summary>
		private static int SortPlayerIndexAscending(ObjectBase x, ObjectBase y)
		{
			if (x.Index > y.Index) { return 1; }
			if (x.Index == y.Index) { return 0; }
			if (x.Index < y.Index) { return -1; }
			return 0;
		}

		/// <summary>
		/// Checks if a object within the object list has a specific color.
		/// </summary>
		/// <param name="currentColor">The color to check the list against.</param>
		/// <returns>True if a object do have the specific color. Return false otherwise.</returns>
		public bool IsColorInObjectList(ObjectBase.ObjectColor currentColor)
		{
			int objectCount = m_pathCurveObjects.Count;
			for(int i = 0; i < objectCount; ++i)
			{
				if(currentColor == m_pathCurveObjects[i].ColorType)
					return true;
			}

			return false;
		}
	}
}
