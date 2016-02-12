using UnityEngine;
using System.Collections;
using TileMatch.MovingLine;

namespace TileMatch.Object
{
	public class ObjectBase : MonoBehaviour 
	{
		public enum Status
		{
			/// <summary>
			/// The object does not move or is in any kind active.
			/// </summary>
			Idle = 0,
			Moving = 1,
			MovingBackwards = 2
		}

		public string CompareID = string.Empty;


		protected Status m_sphereMovementStatus = Status.Idle;
		protected Transform m_transformComponent = null;

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
			
			m_sphereMovementStatus = Status.Idle;
		}
	}
}
