using System;
using UnityEngine;

namespace CameraTools
{
	public class Vector3Animation
	{
		Vector3[] positions;
		float[] times;

		public Vector3Animation(Vector3[] pos, float[] times)
		{
			this.positions = pos;
			this.times = times;
		}

		public Vector3 Evaluate(float t)
		{
			int startIndex = 0;
			for(int i = 0; i < times.Length; i++)
			{
				if(t >= times[i])
				{
					startIndex = i;
				}
				else
				{
					break;
				}
			}

			int nextIndex = Mathf.RoundToInt(Mathf.Min(startIndex + 1, times.Length - 1));

			float overTime = t - times[startIndex];
			float intervalTime = times[nextIndex] - times[startIndex];
			if(intervalTime <= 0) return positions[nextIndex];

			float normTime = overTime/intervalTime;
			return Vector3.Lerp(positions[startIndex], positions[nextIndex], normTime);
		}
	}
}

