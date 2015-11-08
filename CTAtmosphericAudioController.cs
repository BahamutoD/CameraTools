using System;
using UnityEngine;

namespace CameraTools
{
	public class CTAtmosphericAudioController : MonoBehaviour
	{
		AudioSource windAudioSource;
		AudioSource windHowlAudioSource;
		AudioSource windTearAudioSource;
		Vessel vessel;

		void Awake()
		{
			vessel = GetComponent<Vessel>();
			windAudioSource = gameObject.AddComponent<AudioSource>();
			windAudioSource.minDistance = 10;
			windAudioSource.maxDistance = 800;
			windAudioSource.dopplerLevel = 1f;
			AudioClip windclip = GameDatabase.Instance.GetAudioClip("CameraTools/Sounds/windloop");
			if(!windclip)
			{
				Destroy (this);
				return;
			}
			windAudioSource.clip = windclip;

			windHowlAudioSource = gameObject.AddComponent<AudioSource>();
			windHowlAudioSource.minDistance = 10;
			windHowlAudioSource.maxDistance = 800;
			windHowlAudioSource.dopplerLevel = 1f;
			windHowlAudioSource.pitch = 0.25f;
			windHowlAudioSource.clip = GameDatabase.Instance.GetAudioClip("CameraTools/Sounds/windhowl");

			windTearAudioSource = gameObject.AddComponent<AudioSource>();
			windTearAudioSource.minDistance = 1;
			windTearAudioSource.maxDistance = 5000;
			windTearAudioSource.dopplerLevel = 0.85f;
			windTearAudioSource.pitch = 0.65f;
			windTearAudioSource.clip = GameDatabase.Instance.GetAudioClip("CameraTools/Sounds/windtear");


			CamTools.OnResetCTools += OnResetCTools;


		}


		void FixedUpdate()
		{
			if(Time.timeScale > 0 && vessel.dynamicPressurekPa > 0)
			{
				float srfSpeed = (float)vessel.srfSpeed;
				float angleToCam = Vector3.Angle(vessel.srf_velocity, FlightCamera.fetch.mainCamera.transform.position - vessel.transform.position);
				angleToCam = Mathf.Clamp(angleToCam, 1, 180);

				/*
				float waveFrontFactor = (srfSpeed / (angleToCam) < 3.67f) ? 1 : 0;
				float shockwaveFactor = Mathf.Clamp01((srfSpeed/380f)*waveFrontFactor);

				float lagAudioFactor = Mathf.Clamp01(75000 / (Vector3.Distance(vessel.transform.position, FlightCamera.fetch.mainCamera.transform.position) * srfSpeed));
				if(angleToCam > 90) lagAudioFactor = 1;
				*/

				float lagAudioFactor = (75000 / (Vector3.Distance(vessel.transform.position, FlightCamera.fetch.mainCamera.transform.position) * srfSpeed * angleToCam / 90));
				lagAudioFactor = Mathf.Clamp(lagAudioFactor * lagAudioFactor * lagAudioFactor, 0, 4);
				lagAudioFactor += srfSpeed / 230;

				float waveFrontFactor = ((3.67f * angleToCam)/srfSpeed);
				waveFrontFactor = Mathf.Clamp(waveFrontFactor * waveFrontFactor * waveFrontFactor, 0, 2);
				if(vessel.srfSpeed > 330)
				{
					//waveFrontFactor =  (srfSpeed / (angleToCam) < 3.67f) ? srfSpeed/15 : 0;
					waveFrontFactor = waveFrontFactor*waveFrontFactor*waveFrontFactor;
				}

				lagAudioFactor *= waveFrontFactor;



				float sqrAccel = (float)vessel.acceleration.sqrMagnitude;

				//windloop
				if(!windAudioSource.isPlaying)
				{
					windAudioSource.Play();
					Debug.Log("vessel dynamic pressure: " + vessel.dynamicPressurekPa);
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
				windTearAudioSource.maxDistance = Mathf.Clamp(lagAudioFactor * 2500, windTearAudioSource.minDistance, 3000);
			
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

