using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace CameraTools
{
	public struct CameraKeyframe 
	{
		public Vector3 position;
		public Quaternion rotation;
		public float zoom;
		public float time;

		public CameraKeyframe(Vector3 pos, Quaternion rot, float z, float t)
		{
			position = pos;
			rotation = rot;
			zoom = z;
			time = t;
		}

	}

	public class CameraKeyframeComparer : IComparer<CameraKeyframe>
	{
		public int Compare(CameraKeyframe a, CameraKeyframe b)
		{
			return a.time.CompareTo(b.time);
		}
	}
}

