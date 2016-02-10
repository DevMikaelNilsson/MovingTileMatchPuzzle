using UnityEngine;
using System.Collections;

namespace TileMatch.Object
{
	[RequireComponent(typeof(Rigidbody))]
	public class ObjectBase : MonoBehaviour 
	{
		public enum Status
		{
			/// <summary>
			/// The object does not move or is in any kind active.
			/// </summary>
			Idle = 0,

			/// <summary>
			/// The object is currently moving between two vector positions.
			/// </summary>
			Moving = 1,

			/// <summary>
			/// The object is activated by the player by getting shot from a canon into the game world.
			/// </summary>
			Shoot = 2,

			/// <summary>
			/// The object is not active or attached to either the object list or the path. The object is basically on its own.
			/// </summary>
			Freefall = 3,

			TranslateIntoCurve = 4,

			MovingBackward = 5
		}

		public enum Effect
		{
			Match = 0,

			Combo = 1,

			Destroy = 2
		}

		public enum ObjectColor
		{
			Blue = 0,
			Red = 1,
			LightGreen = 2,
			Green = 3,
			Orange = 4,
			Pink = 5,
			LightBlue = 6,

			/// <summary>
			/// When set as a ghost, the object will not be visible, able to collide with shooting objects. The Ghost will simply just take up a place in the internal list to act like a hole in the object chain.
			/// </summary>
			Ghost = 7,
			Bomb = 8
		}

		public LayerDropDownListHandler StandardLayer;
		public LayerDropDownListHandler FreefallLayer;

		public ObjectColor ColorType = ObjectColor.Blue;

		[Tooltip("Effect for when the object is matched with other objects.")]
		public GameObject MatchEffect = null;
		[Tooltip("Effect for when the object is matched, and the match is considered to be a combo, with other objects.")]
		public GameObject ComboMatchEffect = null;
		[Tooltip("Effect for when the the object is finished with rolling backwards and 'collides' with the object behind.")]
		public GameObject RollbackCollisionEffect = null;
		[Tooltip("A effect which will be played when the object is destroyed.")]
		public GameObject DestructionEffect = null;	

		[Tooltip("The base rotation speed the sphere object will rotate. This base speed is divided with the spheres actual translation speed during gameplay to a final dynamic rotation speed.")]
		public float RotationSpeed = 100.0f;

		public ObjectListHandler ObjectListHandlerComponent
		{
			set { m_objectListHandler = value; }
			get { return m_objectListHandler; }
		}
		
		public GameplayHandler GameplayHandlerComponent
		{
			set { m_gameplayHandler = value;}
			get { return m_gameplayHandler;}
		}
		
		public int Index
		{
			get 
			{
				m_index = m_objectListHandler.GetObjectIndexValue(this);
			 	return m_index; 
			 }
			set { m_index = value; }
		}

		protected ObjectListHandler m_objectListHandler = null;
		protected GameplayHandler m_gameplayHandler = null;
		protected Status m_sphereMovementStatus = Status.Idle;
		protected Transform m_transformComponent = null;
		protected Renderer []m_renderComponent = null;
		protected Rigidbody m_rigidbodyComponent = null;
		protected SlerpRotation m_slerpRotateComponent = null;
		protected Rotate m_rotateComponent = null;
		protected int m_index = 0;
		protected Vector3 goalPosition = Vector3.zero;
		protected Vector3 startPosition = Vector3.zero;

		protected float m_elapsedTime = 0.0f;
		protected float m_currentDuration = 0.0f;
		protected bool m_checkForMatch = false;
		protected float m_currentSpeedDiff = 1.0f;

		private float m_progressProcentage = 0.0f;
		private float m_currentPointPosition = 0.0f;
		private float m_previousPointPosition = 0.0f;

		/// <summary>
		/// Internal Unity method.
		/// This method is called whenever the object is enabled/re-enabled.
		/// Initializes all local variables which are frequently used.
		/// Always check so that the variables are valid at start, so I don't have to later.
		/// "GameObject.FindObjectOfType" method is kind of expensive and should be used as sparingly as possible.
		/// </summary>
		void OnEnable()
		{
			if(m_transformComponent == null)
				m_transformComponent = this.GetComponent<Transform>();
			if(m_rigidbodyComponent == null)
				m_rigidbodyComponent = this.GetComponent<Rigidbody>();
			if(m_slerpRotateComponent == null)
				m_slerpRotateComponent = this.GetComponent<SlerpRotation>();
			if(m_rotateComponent == null)
				m_rotateComponent = this.GetComponent<Rotate>();

			if(ColorType == ObjectColor.Ghost)
				this.GetComponent<SphereCollider>().enabled = false;
			else
				this.GetComponent<SphereCollider>().enabled = true;	

			m_index = -1;
			startPosition = m_transformComponent.position;
			goalPosition = m_transformComponent.position;
			m_transformComponent.rotation = Quaternion.identity;
			m_sphereMovementStatus = Status.Idle;
			this.gameObject.layer = StandardLayer.LayerIndex;
		}

		/// <summary>
		/// Makes the object rotate towards a specific position.
		/// The rotation towards the position is made smoothly over a duration of time.
		/// </summary>
		/// <param name="position">The position the object should look at.</param>
		/// <param name="Duration">The amount of time it should take to rotate the object towards the look at position.</param>
		public void SetLookAtPosition(Vector3 position, float Duration)
		{
			if(m_slerpRotateComponent != null)
				m_slerpRotateComponent.RotateTo(position);
		}

		/// <summary>
		/// Creates a effect object on the same position as the object itself.
		/// The effect object will receive the same rotation value as the prefab object has.
		/// The method will first attempt to get the prefab object from the ObjectPool. IF the prefab is not found, it will instantiate a new object automatically.
		/// </summary>
		/// <param name="typeOfEffect">Type of pre-defined effect which will be instiated.</param>
		public GameObject CreateEffect(Effect typeOfEffect)
		{
			switch(typeOfEffect)
			{
				case Effect.Match:
					return CreateEffect(MatchEffect);
				case Effect.Combo:
					return CreateEffect(ComboMatchEffect);
				case Effect.Destroy:
					return CreateEffect(DestructionEffect);
			}

			Debug.LogError(this + " - Effect for current enum (" + typeOfEffect.ToString() + ") was not found.");
			return null;
		}

		/// <summary>
		/// Creates a effect object on the same position as the object itself.
		/// The effect object will receive the same rotation value as the prefab object has.
		/// The method will first attempt to get the prefab object from the ObjectPool. IF the prefab is not found, it will instantiate a new object automatically.
		/// </summary>
		/// <param name="prefab">A valid prefab which the effect will be instantiated from.</param>
		public GameObject CreateEffect(GameObject prefab)
		{
			if(prefab != null)
			{
				GameObject effectObject = ObjectPoolManager.Instance.GetObjectFromPool(ObjectPoolManager.GetObjectByType.GameObject, prefab.name, true, string.Empty);
				if(effectObject == null)
					effectObject = GameObject.Instantiate(prefab);

				if(effectObject != null)
				{				
					Transform effectTransformComponent = effectObject.GetComponent<Transform>();
					effectTransformComponent.position = m_transformComponent.position;
					effectTransformComponent.rotation = prefab.GetComponent<Transform>().rotation;		
				}

				return effectObject;
			}

			Debug.LogError(this + " - prefab object is either missing or not valid.");
			return null;
		}

		/// <summary>
		/// Creates a sound effect object on the same position as the object itself.
		/// The effect object will receive the same rotation value as the prefab object has.
		/// The method will first attempt to get the prefab object from the ObjectPool. IF the prefab is not found, it will instantiate a new object automatically.
		/// </summary>
		/// <param name="prefab">A valid prefab which the effect will be instantiated from.</param>
		public GameObject CreateSoundEffect(GameObject prefab)
		{
			return CreateEffect(prefab);
		}

		/// <summary>
		/// Removes the object from the game.
		/// </summary>
		public void DestroyObject()
		{
			if(this.gameObject.activeSelf == true)
				StartCoroutine(DelayRemovbeObject());
		}

		/// <summary>
		/// Removes the object from the game.
		/// </summary>
		private IEnumerator DelayRemovbeObject()
		{
			yield return new WaitForEndOfFrame();

			m_index = -1;
			if(m_objectListHandler != null)
				m_objectListHandler.RemoveObject(this);

			if(m_gameplayHandler != null)
				m_gameplayHandler.DestroyObject(this.gameObject);
		}

		/// <summary>
		/// Updates the position.
		/// </summary>
		/// <param name="index">Index.</param>
		/// <param name="newPointPosition">New point position.</param>
		/// <param name="speedDiff">Speed diff.</param>
		public void UpdateIndex(int index, float newPointPosition, float speedDiff)
		{
			switch(m_sphereMovementStatus)
			{
				case Status.Freefall:
					return;
				case Status.MovingBackward:
					UpdateIndex(index);
					return;
			}

			if(m_currentSpeedDiff > speedDiff)
				m_currentSpeedDiff = speedDiff;

			if(m_objectListHandler == null)
				m_objectListHandler = m_gameplayHandler.GetObjectListHandler();

			if(m_currentPointPosition != newPointPosition)
			{
				m_sphereMovementStatus = Status.Moving;
				m_previousPointPosition = m_currentPointPosition;
				m_currentPointPosition = newPointPosition;
				m_currentDuration = (m_gameplayHandler.CreateObjectDelay * m_currentSpeedDiff);
				m_rotateComponent.RotationSpeed = (RotationSpeed / m_currentDuration);
				m_previousPos = m_transformComponent.position;
				m_elapsedTime = 0.0f;
			}

			UpdateIndex(index);
		
		}

		/// <summary>
		/// Sets the roll back position.
		/// </summary>
		/// <param name="index">Index.</param>
		/// <param name="newPointPosition">New point position.</param>
		public void UpdateRollbackPosition(int index, float newPointPosition)
		{
			m_sphereMovementStatus = Status.MovingBackward;
			m_previousPointPosition = m_currentPointPosition;
			m_currentPointPosition = newPointPosition;
			m_elapsedTime = 0.0f;
			m_currentDuration = 0.2f;
			UpdateIndex(index);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		protected void UpdateIndex(int index)
		{			
			m_index = index;
			if(m_objectListHandler.GetPosition(index, out goalPosition) == false)
				PathFinished();

			startPosition = m_transformComponent.position;
		}

		Vector3 m_previousPos = Vector3.zero;

		/// <summary>
		/// Translates the object between two positions.
		/// The object is translated by lerp calculations to maximize the stability and
		/// probability of the object. We will always know where the object is based on the lerp value.
		/// When the object is in "shoot" mode, the translation is calculated elsewhere.
		/// </summary>
		protected void TranslateSphere()
		{
			m_elapsedTime += (Time.deltaTime * Time.timeScale);
			m_progressProcentage = Mathf.Clamp((m_elapsedTime / m_currentDuration), 0.0f, 1.0f);

			switch(m_sphereMovementStatus)
			{
				case Status.Moving:
				case Status.TranslateIntoCurve:					
					m_transformComponent.position = Vector3.Lerp(startPosition, goalPosition, m_progressProcentage);
					SetLookAtPosition(goalPosition, m_currentDuration);
					break;
				case Status.MovingBackward:
					float neighbourPoint = m_objectListHandler.GetWapPointPosition((m_index+1));
					float currentRollbackPointValue = Mathf.Lerp(neighbourPoint, m_currentPointPosition, m_progressProcentage);
					m_transformComponent.position = m_objectListHandler.GetCurvePointPosition(currentRollbackPointValue);
					SetLookAtPosition(m_transformComponent.position, m_currentDuration);				
					break;
			}

			if (m_progressProcentage >= 1.0f)
			{				
				m_currentSpeedDiff = 10.0f;
				m_sphereMovementStatus = Status.Idle;
				UpdateIndex(m_index, m_objectListHandler.GetWapPointPosition(m_index), 1.0f);
				CheckForMatch();
				if(m_currentPointPosition >= m_objectListHandler.MaxPathValue)
				{
					m_sphereMovementStatus = Status.Freefall;
					PathFinished();
				}
			}
		}

			/// <summary>
		/// Activates the Object list and checks if there
		/// is a possible match with this object as base.
		/// By accessing this method, any combos will be resetted.
		/// </summary>
		public void CheckForMatch()
		{
			if (m_checkForMatch == false)
				return;
	
			m_checkForMatch = false;
			int matchValue = m_objectListHandler.CheckForMatch(this);
			m_gameplayHandler.AddMatchScoreToPlayer(matchValue, m_transformComponent.position);
		}

			/// <summary>
		/// Method to call when the object has finished following its given curve path.
		/// The object is removed from the path list and is no longer bound to the curve(s).
		/// </summary>
		public void PathFinished()
		{
			m_index = -1;
			m_objectListHandler.RemoveObject(this);
			this.gameObject.layer = FreefallLayer.LayerIndex;
			m_sphereMovementStatus = Status.Freefall;
			Rigidbody rigidbodyComponent = this.GetComponent<Rigidbody>();
			if(rigidbodyComponent != null)
			{
				rigidbodyComponent.isKinematic = false;
				rigidbodyComponent.useGravity = true;
			}
			else
				DestroyObject();
		}

		/// <summary>
		/// Set the object to be either visible or invisible.
		/// Eventhough the object is not visible, it is active.
		/// </summary>
		/// <param name="isVisible"></param>
		public void SetIsVisible(bool isVisible)
		{
			if (m_renderComponent == null)
				m_renderComponent = this.GetComponentsInChildren<Renderer>();

			if(m_renderComponent != null)
			{
				int objectCount = m_renderComponent.Length;
				for(int i = 0; i < objectCount; ++i)
					m_renderComponent[i].enabled = isVisible;
			}
		}

		/// <summary>
		/// Activates the objects "Shoot" status.
		/// </summary>
		public void ShootObject()
		{
			m_sphereMovementStatus = Status.Shoot;
		}

	}
}
