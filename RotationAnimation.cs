using System;
using System.Collections.Generic;
using UnityEngine;
namespace CameraTools
{
	public class RotationAnimation
	{
		Quaternion[] rotations;
		float[] times;

		public RotationAnimation (Quaternion[] rots, float[] times)
		{
			this.rotations = rots;
			this.times = times;
		}

		public Quaternion Evaluate(float t)
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
			if(intervalTime <= 0) return rotations[nextIndex];

			float normTime = overTime/intervalTime;
			return Quaternion.Lerp(rotations[startIndex], rotations[nextIndex], normTime);
		}
	}
}

