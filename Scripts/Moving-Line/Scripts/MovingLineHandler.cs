using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TileMatch.Object;

namespace TileMatch.MovingLine
{
	public class MovingLineHandler : MonoBehaviour 
	{
		public void AddObjectToList(GameObject newObject)
		{
			newObject.SetActive(true);
		}
	}
}
