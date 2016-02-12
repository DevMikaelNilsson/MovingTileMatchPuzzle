using UnityEngine;
using System.Collections;

namespace TileMatch.MovingLine
{
	public class CurveBase : MonoBehaviour 
	{
		public virtual Vector3 GetPoint(float pos)
		{
			Debug.LogError(this + " - This method is not overridden by the component which inherits from this component. No valid point vector is available. Returning Vector3.zero by default.");
			return Vector3.zero;
		}

		public virtual Vector3 GetDirection(float pos)
		{
			Debug.LogError(this + " - This method is not overridden by the component which inherits from this component. No valid point direction is available. Returning Vector3.zero by default.");
			return Vector3.zero;
		}
	}
}