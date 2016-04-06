﻿using System;
using UnityEngine;

namespace CameraTools
{
	public class CTAtmosphericAudioController : MonoBehaviour
	{
		AudioSource windAudioSource;
		AudioSource windHowlAudioSource;
		AudioSource windTearAudioSource;

		AudioSource sonicBoomSource;

		Vessel vessel;

		bool playedBoom = false;

		void Awake()
		{
			vessel = GetComponent<Vessel>();
			windAudioSource = gameObject.AddComponent<AudioSource>();
			windAudioSource.minDistance = 10;
			windAudioSource.maxDistance = 10000;
			windAudioSource.dopplerLevel = .35f;
			windAudioSource.spatialBlend = 1;
			AudioClip windclip = GameDatabase.Instance.GetAudioClip("CameraTools/Sounds/windloop");
			if(!windclip)
			{
				Destroy (this);
				return;
			}
			windAudioSource.clip = windclip;

			windHowlAudioSource = gameObject.AddComponent<AudioSource>();
			windHowlAudioSource.minDistance = 10;
			windHowlAudioSource.maxDistance = 7000;
			windHowlAudioSource.dopplerLevel = .5f;
			windHowlAudioSource.pitch = 0.25f;
			windHowlAudioSource.clip = GameDatabase.Instance.GetAudioClip("CameraTools/Sounds/windhowl");
			windHowlAudioSource.spatialBlend = 1;

			windTearAudioSource = gameObject.AddComponent<AudioSource>();
			windTearAudioSource.minDistance = 10;
			windTearAudioSource.maxDistance = 5000;
			windTearAudioSource.dopplerLevel = 0.45f;
			windTearAudioSource.pitch = 0.65f;
			windTearAudioSource.clip = GameDatabase.Instance.GetAudioClip("CameraTools/Sounds/windtear");
			windTearAudioSource.spatialBlend = 1;

			sonicBoomSource = new GameObject().AddComponent<AudioSource>();
			sonicBoomSource.transform.parent = vessel.transform;
			sonicBoomSource.transform.localPosition = Vector3.zero;
			sonicBoomSource.minDistance = 50;
			sonicBoomSource.maxDistance = 20000;
			sonicBoomSource.dopplerLevel = 0;
			sonicBoomSource.clip = GameDatabase.Instance.GetAudioClip("CameraTools/Sounds/sonicBoom");
			sonicBoomSource.volume = Mathf.Clamp01(vessel.GetTotalMass()/4f);
			sonicBoomSource.Stop();
			sonicBoomSource.spatialBlend = 1;

			float angleToCam = Vector3.Angle(vessel.srf_velocity, FlightCamera.fetch.mainCamera.transform.position - vessel.transform.position);
			angleToCam = Mathf.Clamp(angleToCam, 1, 180);
			if(vessel.srfSpeed / (angleToCam) < 3.67f)
			{
				playedBoom = true;
			}

			CamTools.OnResetCTools += OnResetCTools;
		}


		void FixedUpdate()
		{
			if(!vessel)
			{
				return;
			}
			if(Time.timeScale > 0 && vessel.dynamicPressurekPa > 0)
			{
				float srfSpeed = (float)vessel.srfSpeed;
				srfSpeed = Mathf.Min(srfSpeed, 550f);
				float angleToCam = Vector3.Angle(vessel.srf_velocity, FlightCamera.fetch.mainCamera.transform.position - vessel.transform.position);
				angleToCam = Mathf.Clamp(angleToCam, 1, 180);
			

				float lagAudioFactor = (75000 / (Vector3.Distance(vessel.transform.position, FlightCamera.fetch.mainCamera.transform.position) * srfSpeed * angleToCam / 90));
				lagAudioFactor = Mathf.Clamp(lagAudioFactor * lagAudioFactor * lagAudioFactor, 0, 4);
				lagAudioFactor += srfSpeed / 230;

				float waveFrontFactor = ((3.67f * angleToCam)/srfSpeed);
				waveFrontFactor = Mathf.Clamp(waveFrontFactor * waveFrontFactor * waveFrontFactor, 0, 2);


				if(vessel.srfSpeed > CamTools.speedOfSound)
				{
					waveFrontFactor =  (srfSpeed / (angleToCam) < 3.67f) ? waveFrontFactor + ((srfSpeed/(float)CamTools.speedOfSound)*waveFrontFactor) : 0;
					if(waveFrontFactor > 0)
					{
						if(!playedBoom)
						{
							sonicBoomSource.transform.position = vessel.transform.position + (-vessel.srf_velocity);
							sonicBoomSource.PlayOneShot(sonicBoomSource.clip);
						}
						playedBoom = true;
					}
					else
					{

					}
				}
				else if(CamTools.speedOfSound / (angleToCam) < 3.67f)
				{
					playedBoom = true;
				}

				lagAudioFactor *= waveFrontFactor;

				float sqrAccel = (float)vessel.acceleration.sqrMagnitude;

				//windloop
				if(!windAudioSource.isPlaying)
				{
					windAudioSource.Play();
					//Debug.Log("vessel dynamic pressure: " + vessel.dynamicPressurekPa);
				}
				float pressureFactor = Mathf.Clamp01((float)vessel.dynamicPressurekPa / 50f);
				float massFactor = Mathf.Clamp01(vessel.GetTotalMass() / 60f);
				float gFactor = Mathf.Clamp(sqrAccel / 225, 0, 1.5f);
				windAudioSource.volume = massFactor * pressureFactor * gFactor * lagAudioFactor;


				//windhowl
				if(!windHowlAudioSource.isPlaying)
				{
					windHowlAudioSource.Play();
				}
				float pressureFactor2 = Mathf.Clamp01((float)vessel.dynamicPressurekPa / 20f);
				float massFactor2 = Mathf.Clamp01(vessel.GetTotalMass() / 30f);
				windHowlAudioSource.volume = pressureFactor2 * massFactor2 * lagAudioFactor;
				windHowlAudioSource.maxDistance = Mathf.Clamp(lagAudioFactor * 2500, windTearAudioSource.minDistance, 16000);

				//windtear
				if(!windTearAudioSource.isPlaying)
				{
					windTearAudioSource.Play();
				}
				float pressureFactor3 = Mathf.Clamp01((float)vessel.dynamicPressurekPa / 40f);
				float massFactor3 = Mathf.Clamp01(vessel.GetTotalMass() / 10f);
				//float gFactor3 = Mathf.Clamp(sqrAccel / 325, 0.25f, 1f);
				windTearAudioSource.volume = pressureFactor3 * massFactor3;

				windTearAudioSource.minDistance = lagAudioFactor * 1;
				windTearAudioSource.maxDistance = Mathf.Clamp(lagAudioFactor * 2500, windTearAudioSource.minDistance, 16000);
			
			}
			else
			{
				if(windAudioSource.isPlaying)
				{
					windAudioSource.Stop();
				}

				if(windHowlAudioSource.isPlaying)
				{
					windHowlAudioSource.Stop();
				}

				if(windTearAudioSource.isPlaying)
				{
					windTearAudioSource.Stop();
				}
			}
		}

		void OnDestroy()
		{
			if(sonicBoomSource)
			{
				Destroy(sonicBoomSource.gameObject);
			}
			CamTools.OnResetCTools -= OnResetCTools;
		}

		void OnResetCTools()
		{
			Destroy(windAudioSource);
			Destroy(windHowlAudioSource);
			Destroy(windTearAudioSource);

			Destroy(this);
		}
	}
}

