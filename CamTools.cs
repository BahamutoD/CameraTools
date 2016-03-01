using System;
using UnityEngine;


namespace CameraTools
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class CamTools : MonoBehaviour
	{
		public static CamTools fetch;

		GameObject cameraParent;
		Vessel vessel;
		Vector3 origPosition;
		Quaternion origRotation;
		Transform origParent;
		float origNearClip;
		FlightCamera flightCamera;
		
		Part camTarget = null;

		[CTPersistantField]
		public ReferenceModes referenceMode = ReferenceModes.Surface;
		Vector3 cameraUp = Vector3.up;

		string fmUpKey = "[7]";
		string fmDownKey = "[1]";
		string fmForwardKey = "[8]";
		string fmBackKey = "[5]";
		string fmLeftKey = "[4]";
		string fmRightKey = "[6]";
		string fmZoomInKey = "[9]";
		string fmZoomOutKey = "[3]";
		//
		
		
		//current camera setting
		bool isDefault;
		bool isStationaryCamera = false;
		
		
		//GUI
		public static bool guiEnabled = false;
		public static bool hasAddedButton = false;
		bool updateFOV = false;
		float windowWidth = 250;
		float windowHeight = 400;
		float draggableHeight = 40;
		float leftIndent = 12;
		float entryHeight = 20;
		[CTPersistantField]
		public ToolModes toolMode = ToolModes.StationaryCamera;
		Rect windowRect = new Rect(0,0,0,0);
		bool gameUIToggle = true;
		bool hasFixedWindow = false;
		float incrButtonWidth = 26;
		
		//stationary camera vars
		[CTPersistantField]
		public bool autoFlybyPosition = false;
		[CTPersistantField]
		public bool autoFOV = false;
		float manualFOV = 60;
		float currentFOV = 60;
		Vector3 manualPosition = Vector3.zero;
		[CTPersistantField]
		public float freeMoveSpeed = 10;
		string guiFreeMoveSpeed = "10";
		[CTPersistantField]
		public float keyZoomSpeed = 1;
		string guiKeyZoomSpeed = "1";
		float zoomFactor = 1;
		[CTPersistantField]
		public float zoomExp = 1;
		[CTPersistantField]
		public bool enableKeypad = false;
		[CTPersistantField]
		public float maxRelV = 2500;
		
		bool setPresetOffset = false;
		Vector3 presetOffset = Vector3.zero;
		bool hasSavedRotation = false;
		Quaternion savedRotation;
		[CTPersistantField]
		public bool manualOffset = false;
		[CTPersistantField]
		public float manualOffsetForward = 500;
		[CTPersistantField]
		public float manualOffsetRight = 50;
		[CTPersistantField]
		public float manualOffsetUp = 5;
		string guiOffsetForward = "500";
		string guiOffsetRight = "50";
		string guiOffsetUp = "5";
		
		Vector3 lastVesselPosition = Vector3.zero;
		Vector3 lastTargetPosition = Vector3.zero;
		bool hasTarget = false;

		[CTPersistantField]
		public bool useOrbital = false;

		[CTPersistantField]
		public bool targetCoM = false;

		bool hasDied = false;
		float diedTime = 0;
		//vessel reference mode
		Vector3 initialVelocity = Vector3.zero;
		Vector3 initialPosition = Vector3.zero;
		Orbit initialOrbit = null;
		double initialUT;
		
		//retaining position and rotation after vessel destruction
		Vector3 lastPosition;
		Quaternion lastRotation;
		
		
		//click waiting stuff
		bool waitingForTarget = false;
		bool waitingForPosition = false;

		bool mouseUp = false;

		//Keys
		[CTPersistantField]
		public string cameraKey = "home";
		[CTPersistantField]
		public string revertKey = "end";

		//recording input for key binding
		bool isRecordingInput = false;
		bool isRecordingActivate = false;
		bool isRecordingRevert = false;

		Vector3 resetPositionFix;//fixes position movement after setting and resetting camera
			
		//floating origin shift handler
		Vector3d lastOffset = FloatingOrigin.fetch.offset;

		AudioSource[] audioSources;
		float[] originalAudioSourceDoppler;
		bool hasSetDoppler = false;

		[CTPersistantField]
		public bool useAudioEffects = true;

		//camera shake
		Vector3 shakeOffset = Vector3.zero;
		float shakeMagnitude = 0;
		[CTPersistantField]
		public float shakeMultiplier = 1;

		public delegate void ResetCTools();
		public static event ResetCTools OnResetCTools;
		public static double speedOfSound = 330;

		void Awake()
		{
			if(fetch)
			{
				Destroy(fetch);
			}

			fetch = this;

			CTPersistantField.Load();

			guiOffsetForward = manualOffsetForward.ToString();
			guiOffsetRight = manualOffsetRight.ToString();
			guiOffsetUp = manualOffsetUp.ToString();
			guiKeyZoomSpeed = keyZoomSpeed.ToString();
			guiFreeMoveSpeed = freeMoveSpeed.ToString();
		}

		void Start()
		{
			windowRect = new Rect(Screen.width-windowWidth-5, 45, windowWidth, windowHeight);
			isDefault = true;
			flightCamera = FlightCamera.fetch;
			SaveOriginalCamera();
			
			AddToolbarButton();
			
			GameEvents.onHideUI.Add(GameUIDisable);
			GameEvents.onShowUI.Add(GameUIEnable);
			//GameEvents.onGamePause.Add (PostDeathRevert);
			GameEvents.OnVesselRecoveryRequested.Add(PostDeathRevert);
			GameEvents.onFloatingOriginShift.Add(OnFloatingOriginShift);
			GameEvents.onGameSceneLoadRequested.Add(PostDeathRevert);
			
			cameraParent = new GameObject("StationaryCameraParent");
			//cameraParent.SetActive(true);
			//cameraParent = (GameObject) Instantiate(cameraParent, Vector3.zero, Quaternion.identity);
			
			if(FlightGlobals.ActiveVessel != null)
			{
				cameraParent.transform.position = FlightGlobals.ActiveVessel.transform.position;
				vessel = FlightGlobals.ActiveVessel;
			
			}
		}
		
		void Update()
		{
			if(!isRecordingInput)
			{
				if(Input.GetKeyDown(KeyCode.KeypadDivide))
				{
					guiEnabled = !guiEnabled;	
				}
				
				if(Input.GetKeyDown(revertKey))
				{
					RevertCamera();	
				}
				else if(Input.GetKeyDown(cameraKey))
				{
					if(toolMode == ToolModes.StationaryCamera)
					{
						if(isDefault)
						{
							SaveOriginalCamera();
							StationaryCamera();
						}
						else
						{
							//RevertCamera();
							StationaryCamera();
						}
						
					}
				}
			}
			else
			{
				/*
				if(mouseUp && isRecordingActivate)
				{
					string inputString = CCInputUtils.GetInputString();
					if(inputString.Length > 0)
					{
						cameraKey = inputString;
						isRecordingInput = false;
						isRecordingActivate = false;
					}
				}
				*/
			}

			if(Input.GetMouseButtonUp(0))
			{
				mouseUp = true;
			}
			
			
			//get target transform from mouseClick
			if(waitingForTarget && mouseUp && Input.GetKeyDown(KeyCode.Mouse0))
			{
				Part tgt = GetPartFromMouse();
				if(tgt!=null)
				{
					camTarget = tgt;
					hasTarget = true;
				}
				else 
				{
					Vector3 pos = GetPosFromMouse();
					if(pos != Vector3.zero)
					{
						lastTargetPosition = pos;
						hasTarget = true;
					}
				}
				
				waitingForTarget = false;
			}
			
			//set position from mouseClick
			if(waitingForPosition && mouseUp && Input.GetKeyDown(KeyCode.Mouse0))
			{
				Vector3 pos = GetPosFromMouse();
				if(pos!=Vector3.zero)// && isStationaryCamera)
				{
					presetOffset = pos;
					setPresetOffset = true;
				}
				else Debug.Log ("No pos from mouse click");
				
				waitingForPosition = false;
			}
			
			
			
		}

		public void ShakeCamera(float magnitude)
		{
			shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
		}
		
		
		int posCounter = 0;//debug
		void FixedUpdate()
		{
			if(!FlightGlobals.ready)
			{
				return;
			}

			if(FlightGlobals.ActiveVessel != null && (vessel==null || vessel!=FlightGlobals.ActiveVessel))
			{
				vessel = FlightGlobals.ActiveVessel;
			}


			
			if(vessel != null)
			{
				lastVesselPosition = vessel.transform.position;
				cameraParent.transform.position = manualPosition + (vessel.findWorldCenterOfMass() - vessel.rigidbody.velocity * Time.fixedDeltaTime);	
			}


			//stationary camera
			if(isStationaryCamera)
			{
				if(useAudioEffects)
				{
					speedOfSound = 233 * Math.Sqrt(1 + (FlightGlobals.getExternalTemperature(vessel.GetWorldPos3D(), vessel.mainBody) / 273.15));
					//Debug.Log("speed of sound: " + speedOfSound);
				}
				
				if(posCounter < 3)
				{
					posCounter++;
					Debug.Log("flightCamera position: " + flightCamera.transform.position);
					flightCamera.transform.position = resetPositionFix;
					if(hasSavedRotation)
					{
						flightCamera.transform.rotation = savedRotation;
					}
				}
				if(flightCamera.Target != null) flightCamera.setTarget(null); //dont go to next vessel if vessel is destroyed

				if(camTarget != null)
				{
					Vector3 lookPosition = camTarget.transform.position;
					if(targetCoM)
					{
						lookPosition = camTarget.vessel.CoM;
					}

					lookPosition += 2*camTarget.vessel.rb_velocity * Time.fixedDeltaTime;
					if(targetCoM)
					{
						lookPosition += camTarget.vessel.rb_velocity * Time.fixedDeltaTime;
					}

					flightCamera.transform.rotation = Quaternion.LookRotation(lookPosition - flightCamera.transform.position, cameraUp);
					lastTargetPosition = lookPosition;
				}
				else if(hasTarget)
				{
					flightCamera.transform.rotation = Quaternion.LookRotation(lastTargetPosition - flightCamera.transform.position, cameraUp);
				}

				UpdateCameraShake();
				
				if(vessel != null)
				{
					if(referenceMode == ReferenceModes.Surface)
					{
						flightCamera.transform.position -= Time.fixedDeltaTime * Mathf.Clamp((float)vessel.srf_velocity.magnitude, 0, maxRelV) * vessel.srf_velocity.normalized;
					}
					else if(referenceMode == ReferenceModes.Orbit)
					{
						flightCamera.transform.position -= Time.fixedDeltaTime * Mathf.Clamp((float)vessel.obt_velocity.magnitude, 0, maxRelV) * vessel.obt_velocity.normalized;
					}
					else if(referenceMode == ReferenceModes.InitialVelocity)
					{
						Vector3 camVelocity = Vector3.zero;
						if(useOrbital && initialOrbit != null)
						{
							camVelocity = (initialOrbit.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()).xzy - vessel.GetObtVelocity());
						}
						else
						{
							camVelocity = (initialVelocity - vessel.srf_velocity);
						}
						flightCamera.transform.position += camVelocity * Time.fixedDeltaTime;
					}
				}
				
				
				//mouse panning, moving
				Vector3 forwardLevelAxis = (Quaternion.AngleAxis(-90, cameraUp) * flightCamera.transform.right).normalized;
				Vector3 rightAxis = (Quaternion.AngleAxis(90, forwardLevelAxis) * cameraUp).normalized;
				
				//free move
				if(enableKeypad)
				{
					if(Input.GetKey(fmUpKey))
					{
						manualPosition += cameraUp * freeMoveSpeed * Time.fixedDeltaTime;	
					}
					else if(Input.GetKey(fmDownKey))
					{
						manualPosition -= cameraUp * freeMoveSpeed * Time.fixedDeltaTime;	
					}
					if(Input.GetKey(fmForwardKey))
					{
						manualPosition += forwardLevelAxis * freeMoveSpeed * Time.fixedDeltaTime;
					}
					else if(Input.GetKey(fmBackKey))
					{
						manualPosition -= forwardLevelAxis * freeMoveSpeed * Time.fixedDeltaTime;
					}
					if(Input.GetKey(fmLeftKey))
					{
						manualPosition -= flightCamera.transform.right * freeMoveSpeed * Time.fixedDeltaTime;
					}
					else if(Input.GetKey(fmRightKey))
					{
						manualPosition += flightCamera.transform.right * freeMoveSpeed * Time.fixedDeltaTime;
					}
					
					//keyZoom
					if(Input.GetKey(fmZoomInKey))
					{
						zoomExp = Mathf.Clamp(zoomExp + (keyZoomSpeed * Time.fixedDeltaTime), 1, 8);
					}
					else if(Input.GetKey(fmZoomOutKey))
					{
						zoomExp = Mathf.Clamp(zoomExp - (keyZoomSpeed * Time.fixedDeltaTime), 1, 8);
					}
				}
				
				
				if(camTarget == null && Input.GetKey(KeyCode.Mouse1))
				{
					flightCamera.transform.rotation *= Quaternion.AngleAxis(Input.GetAxis("Mouse X") * 1.7f, Vector3.up); //*(Mathf.Abs(Mouse.delta.x)/7)
					flightCamera.transform.rotation *= Quaternion.AngleAxis(-Input.GetAxis("Mouse Y") * 1.7f, Vector3.right);
					flightCamera.transform.rotation = Quaternion.LookRotation(flightCamera.transform.forward, cameraUp);
				}
				if(Input.GetKey(KeyCode.Mouse2))
				{
					manualPosition += flightCamera.transform.right * Input.GetAxis("Mouse X") * 2;
					manualPosition += forwardLevelAxis * Input.GetAxis("Mouse Y") * 2;
				}
				manualPosition += cameraUp * 10 * Input.GetAxis("Mouse ScrollWheel");
				
				//autoFov
				if(camTarget != null && autoFOV)
				{
					float cameraDistance = Vector3.Distance(camTarget.transform.position, flightCamera.transform.position);
					float targetFoV = Mathf.Clamp((7000 / (cameraDistance + 100)) - 4, 2, 60);
					//flightCamera.SetFoV(targetFoV);	
					manualFOV = targetFoV;
				}
				//FOV
				if(!autoFOV)
				{
					zoomFactor = Mathf.Exp(zoomExp) / Mathf.Exp(1);
					manualFOV = 60 / zoomFactor;
					updateFOV = (currentFOV != manualFOV);
					if(updateFOV)
					{
						currentFOV = Mathf.Lerp(currentFOV, manualFOV, 0.1f);
						flightCamera.SetFoV(currentFOV);
						updateFOV = false;
					}
				}
				else
				{
					currentFOV = Mathf.Lerp(currentFOV, manualFOV, 0.1f);
					flightCamera.SetFoV(currentFOV);	
					zoomFactor = 60 / currentFOV;
				}
				lastPosition = flightCamera.transform.position;
				lastRotation = flightCamera.transform.rotation;



				//vessel camera shake
				if(shakeMultiplier > 0)
				{
					foreach(var v in FlightGlobals.Vessels)
					{
						if(!v || !v.loaded || v.packed) continue;
						VesselCameraShake(v);
					}
				}
			}
			else
			{
				if(!autoFOV)
				{
					zoomFactor = Mathf.Exp(zoomExp)/Mathf.Exp(1);
				}
			}
			
			
			if(hasDied && Time.time-diedTime > 2)
			{
				RevertCamera();	
			}
		}


		
		void LateUpdate()
		{
			
			//retain pos and rot after vessel destruction
			if (isStationaryCamera && flightCamera.transform.parent != cameraParent.transform)	
			{
				flightCamera.setTarget(null);
				flightCamera.transform.parent = null;
				flightCamera.transform.position = lastPosition;
				flightCamera.transform.rotation = lastRotation;
				hasDied = true;
				diedTime = Time.time;
			}
			
		}

		void UpdateCameraShake()
		{
			if(shakeMultiplier > 0)
			{
				if(shakeMagnitude > 0.1f)
				{
					Vector3 shakeAxis = UnityEngine.Random.onUnitSphere;
					shakeOffset = Mathf.Sin(shakeMagnitude * 20 * Time.time) * (shakeMagnitude / 10) * shakeAxis;
				}


				flightCamera.transform.rotation = Quaternion.AngleAxis((shakeMultiplier/2) * shakeMagnitude / 50f, Vector3.ProjectOnPlane(UnityEngine.Random.onUnitSphere, flightCamera.transform.forward)) * flightCamera.transform.rotation;
			}

			shakeMagnitude = Mathf.Lerp(shakeMagnitude, 0, 5*Time.fixedDeltaTime);
		}

		public void VesselCameraShake(Vessel vessel)
		{
			//shake
			float camDistance = Vector3.Distance(flightCamera.transform.position, vessel.findWorldCenterOfMass());

			float distanceFactor = 50f / camDistance;
			float fovFactor = 2f / zoomFactor;
			float thrustFactor = GetTotalThrust() / 1000f;

			float atmosphericFactor = (float)vessel.dynamicPressurekPa / 2f;

			float angleToCam = Vector3.Angle(vessel.srf_velocity, FlightCamera.fetch.mainCamera.transform.position - vessel.transform.position);
			angleToCam = Mathf.Clamp(angleToCam, 1, 180);

			float srfSpeed = (float)vessel.srfSpeed;

			float lagAudioFactor = (75000 / (Vector3.Distance(vessel.transform.position, FlightCamera.fetch.mainCamera.transform.position) * srfSpeed * angleToCam / 90));
			lagAudioFactor = Mathf.Clamp(lagAudioFactor * lagAudioFactor * lagAudioFactor, 0, 4);
			lagAudioFactor += srfSpeed / 230;

			float waveFrontFactor = ((3.67f * angleToCam) / srfSpeed);
			waveFrontFactor = Mathf.Clamp(waveFrontFactor * waveFrontFactor * waveFrontFactor, 0, 2);
			if(vessel.srfSpeed > 330)
			{
				waveFrontFactor = (srfSpeed / (angleToCam) < 3.67f) ? srfSpeed / 15 : 0;
			}

			lagAudioFactor *= waveFrontFactor;

			lagAudioFactor = Mathf.Clamp01(lagAudioFactor) * distanceFactor * fovFactor;

			atmosphericFactor *= lagAudioFactor;

			thrustFactor *= distanceFactor * fovFactor * lagAudioFactor;

			ShakeCamera(atmosphericFactor + thrustFactor);
		}

		float GetTotalThrust()
		{
			float total = 0;
			foreach(var engine in vessel.FindPartModulesImplementing<ModuleEngines>())
			{
				total += engine.finalThrust;
			}
			return total;
		}

		void AddAtmoAudioControllers()
		{
			if(!useAudioEffects)
			{
				return;
			}

			foreach(var vessel in FlightGlobals.Vessels)
			{
				if(!vessel || !vessel.loaded || vessel.packed)
				{
					continue;
				}

				vessel.gameObject.AddComponent<CTAtmosphericAudioController>();
			}
		}
		
		void SetDoppler()
		{
			if(hasSetDoppler)
			{
				return;
			}

			if(!useAudioEffects)
			{
				return;
			}

			audioSources = FindObjectsOfType<AudioSource>();
			originalAudioSourceDoppler = new float[audioSources.Length];

			for(int i = 0; i < audioSources.Length; i++)
			{
				originalAudioSourceDoppler[i] = audioSources[i].dopplerLevel;
				audioSources[i].dopplerLevel = 1;

				audioSources[i].bypassEffects = false;
				
				if(audioSources[i].gameObject.GetComponentInParent<Part>())
				{
					//Debug.Log("Added CTPartAudioController to :" + audioSources[i].name);
					CTPartAudioController pa = audioSources[i].gameObject.AddComponent<CTPartAudioController>();
					pa.audioSource = audioSources[i];
				}
			}

			hasSetDoppler = true;
		}

		void ResetDoppler()
		{
			if(!hasSetDoppler)
			{
				return;
			}

			for(int i = 0; i < audioSources.Length; i++)
			{
				if(audioSources[i] != null)
				{
					audioSources[i].dopplerLevel = originalAudioSourceDoppler[i];
				}
			}

		

			hasSetDoppler = false;
		}

		
		void StationaryCamera()
		{
			Debug.Log ("flightCamera position init: "+flightCamera.transform.position);
			if(FlightGlobals.ActiveVessel != null)
			{				
				hasDied = false;
				vessel = FlightGlobals.ActiveVessel;
				cameraUp = -FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()).normalized;
				if(FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel(vessel) == FlightCamera.Modes.ORBITAL))
				{
					cameraUp = Vector3.up;
				}
				
				flightCamera.transform.parent = cameraParent.transform;
				flightCamera.setTarget(null);
				cameraParent.transform.position = vessel.transform.position+vessel.rigidbody.velocity*Time.fixedDeltaTime;
				manualPosition = Vector3.zero;
				
				
				hasTarget = (camTarget != null) ? true : false;
				
				
				Vector3 rightAxis = -Vector3.Cross(vessel.srf_velocity, vessel.upAxis).normalized;
				//Vector3 upAxis = flightCamera.transform.up;
				

				if(autoFlybyPosition)
				{
					setPresetOffset = false;
					Vector3 velocity = vessel.srf_velocity;
					if(referenceMode == ReferenceModes.Orbit) velocity = vessel.obt_velocity;
					
					Vector3 clampedVelocity = Mathf.Clamp((float) vessel.srfSpeed, 0, maxRelV) * velocity.normalized;
					float clampedSpeed = clampedVelocity.magnitude;
					float sideDistance = Mathf.Clamp(20 + (clampedSpeed/10), 20, 150);
					float distanceAhead = Mathf.Clamp(4 * clampedSpeed, 30, 3500);
					
					flightCamera.transform.rotation = Quaternion.LookRotation(vessel.transform.position - flightCamera.transform.position, cameraUp);
					
					
					if(referenceMode == ReferenceModes.Surface && vessel.srfSpeed > 0)
					{
						flightCamera.transform.position = vessel.transform.position + (distanceAhead * vessel.srf_velocity.normalized);
					}
					else if(referenceMode == ReferenceModes.Orbit && vessel.obt_speed > 0)
					{
						flightCamera.transform.position = vessel.transform.position + (distanceAhead * vessel.obt_velocity.normalized);
					}
					else
					{
						flightCamera.transform.position = vessel.transform.position + (distanceAhead * vessel.vesselTransform.up);
					}
					
					
					if(flightCamera.mode == FlightCamera.Modes.FREE || FlightCamera.GetAutoModeForVessel(vessel) == FlightCamera.Modes.FREE)
					{
						flightCamera.transform.position += (sideDistance * rightAxis) + (15 * cameraUp);
					}
					else if(flightCamera.mode == FlightCamera.Modes.ORBITAL || FlightCamera.GetAutoModeForVessel(vessel) == FlightCamera.Modes.ORBITAL)
					{
						flightCamera.transform.position += (sideDistance * FlightGlobals.getUpAxis()) + (15 * Vector3.up);
					}


				}
				else if(manualOffset)
				{
					setPresetOffset = false;
					float sideDistance = manualOffsetRight;
					float distanceAhead = manualOffsetForward;
					
					
					flightCamera.transform.rotation = Quaternion.LookRotation(vessel.transform.position - flightCamera.transform.position, cameraUp);
					
					if(referenceMode == ReferenceModes.Surface && vessel.srfSpeed > 4)
					{
						flightCamera.transform.position = vessel.transform.position + (distanceAhead * vessel.srf_velocity.normalized);
					}
					else if(referenceMode == ReferenceModes.Orbit && vessel.obt_speed > 4)
					{
						flightCamera.transform.position = vessel.transform.position + (distanceAhead * vessel.obt_velocity.normalized);
					}
					else
					{
						flightCamera.transform.position = vessel.transform.position + (distanceAhead * vessel.vesselTransform.up);
					}
					
					if(flightCamera.mode == FlightCamera.Modes.FREE || FlightCamera.GetAutoModeForVessel(vessel) == FlightCamera.Modes.FREE)
					{
						flightCamera.transform.position += (sideDistance * rightAxis) + (manualOffsetUp * cameraUp);
					}
					else if(flightCamera.mode == FlightCamera.Modes.ORBITAL || FlightCamera.GetAutoModeForVessel(vessel) == FlightCamera.Modes.ORBITAL)
					{
						flightCamera.transform.position += (sideDistance * FlightGlobals.getUpAxis()) + (manualOffsetUp * Vector3.up);
					}
				}
				else if(setPresetOffset)
				{
					flightCamera.transform.position = presetOffset;
					//setPresetOffset = false;
				}
				
				initialVelocity = vessel.srf_velocity;
				initialOrbit = new Orbit();
				initialOrbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel, FlightGlobals.currentMainBody, Planetarium.GetUniversalTime());
				initialUT = Planetarium.GetUniversalTime();
				
				isStationaryCamera = true;
				isDefault = false;

				SetDoppler();
				AddAtmoAudioControllers();
			}
			else
			{
				Debug.Log ("CameraTools: Stationary Camera failed. Active Vessel is null.");	
			}
			resetPositionFix = flightCamera.transform.position;
			Debug.Log ("flightCamera position post init: "+flightCamera.transform.position);
		}
		
		void RevertCamera()
		{
			posCounter = 0;

			if(isStationaryCamera)
			{
				presetOffset = flightCamera.transform.position;
				if(camTarget==null)
				{
					savedRotation = flightCamera.transform.rotation;
					hasSavedRotation = true;
				}
				else
				{
					hasSavedRotation = false;
				}
			}
			hasDied = false;
			if(FlightGlobals.ActiveVessel!=null) flightCamera.setTarget(FlightGlobals.ActiveVessel.transform);
			flightCamera.transform.position = origPosition;
			flightCamera.transform.rotation = origRotation;
			flightCamera.transform.parent = origParent;
			flightCamera.SetFoV(60);
			currentFOV = 60;
			Camera.main.nearClipPlane = origNearClip;
			
			
			isDefault = true;
			
			isStationaryCamera = false;

			ResetDoppler();
			if(OnResetCTools != null)
			{
				OnResetCTools();
			}
		}
		
		void SaveOriginalCamera()
		{
			origPosition = flightCamera.transform.position;
			origRotation = flightCamera.transform.localRotation;
			origParent = flightCamera.transform.parent;
			origNearClip = Camera.main.nearClipPlane;	
		}
		
		Part GetPartFromMouse()
		{
			Vector3 mouseAim = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
			Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay(mouseAim);
			RaycastHit hit;
			if(Physics.Raycast(ray, out hit, 10000, 1<<0))
			{
				Part p = hit.transform.GetComponentInParent<Part>();
				return p;
			}
			else return null;
		}
		
		Vector3 GetPosFromMouse()
		{
			Vector3 mouseAim = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
			Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay(mouseAim);
			RaycastHit hit;
			if(Physics.Raycast(ray, out hit, 15000, 557057))
			{
				return 	hit.point - (10 * ray.direction);
			}
			else return Vector3.zero;
		}
		
		void PostDeathRevert()
		{
			if(isStationaryCamera)	
			{
				RevertCamera();	
			}
		}

		void PostDeathRevert(GameScenes f)
		{
			if(isStationaryCamera)	
			{
				RevertCamera();	
			}
		}
		
		void PostDeathRevert(Vessel v)
		{
			if(isStationaryCamera)	
			{
				RevertCamera();	
			}
		}
		
		//GUI
		void OnGUI()
		{
			if(guiEnabled && gameUIToggle) 
			{
				windowRect = GUI.Window(320, windowRect, GuiWindow, "");
			}
		}
				
		void GuiWindow(int windowID)
		{
			GUI.DragWindow(new Rect(0,0,windowWidth, draggableHeight));
			
			GUIStyle centerLabel = new GUIStyle();
			centerLabel.alignment = TextAnchor.UpperCenter;
			centerLabel.normal.textColor = Color.white;
			
			GUIStyle leftLabel = new GUIStyle();
			leftLabel.alignment = TextAnchor.UpperLeft;
			leftLabel.normal.textColor = Color.white;
			
			
			
			float line = 1;
			float contentWidth = (windowWidth) - (2*leftIndent);
			float contentTop = 20;
			GUIStyle titleStyle = new GUIStyle(centerLabel);
			titleStyle.fontSize = 24;
			titleStyle.alignment = TextAnchor.MiddleCenter;
			GUI.Label(new Rect(0, contentTop, windowWidth, 40), "Camera Tools", titleStyle);
			line++;
			float parseResult;
			//Stationary camera GUI
			if(toolMode == ToolModes.StationaryCamera)
			{
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Tool: Stationary Camera", leftLabel);
				line++;
				//GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "--------------------------", centerLabel);
				//line++;
				
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Frame of Reference: "+referenceMode.ToString(), leftLabel);
				line++;
				if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), 25, entryHeight-2), "<"))
				{
					CycleRefereneMode(false);
				}
				if(GUI.Button(new Rect(leftIndent+25+4, contentTop+(line*entryHeight), 25, entryHeight-2), ">"))
				{
					CycleRefereneMode(true);
				}
				
				line++;
				
				if(referenceMode == ReferenceModes.Surface || referenceMode == ReferenceModes.Orbit)
				{
					GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight),contentWidth/2, entryHeight), "Max Rel. V: ", leftLabel);
					maxRelV = float.Parse(GUI.TextField(new Rect(leftIndent + contentWidth/2, contentTop+(line*entryHeight), contentWidth/2, entryHeight), maxRelV.ToString()));	
				}
				else if(referenceMode == ReferenceModes.InitialVelocity)
				{
					useOrbital = GUI.Toggle(new Rect(leftIndent, contentTop+(line*entryHeight),contentWidth, entryHeight), useOrbital, " Orbital");
				}
				line++;
				useAudioEffects = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), useAudioEffects, "Use Audio Effects");
				line++;
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Camera shake:");
				line++;
				shakeMultiplier = GUI.HorizontalSlider(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth - 45, entryHeight), shakeMultiplier, 0f, 10f);
				GUI.Label(new Rect(leftIndent + contentWidth - 40, contentTop + ((line-0.25f) * entryHeight), 40, entryHeight), shakeMultiplier.ToString("0.00") + "x");
				line++;
				
					
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Zoom:", leftLabel);
				line++;

				if(!autoFOV)
				{
					zoomExp = GUI.HorizontalSlider(new Rect(leftIndent, contentTop + ((line) * entryHeight), contentWidth - 45, entryHeight), zoomExp, 1, 8);
				}
				
				
				GUI.Label(new Rect(leftIndent+contentWidth-40, contentTop+((line-0.15f)*entryHeight), 40, entryHeight), zoomFactor.ToString("0.0")+"x", leftLabel);
				line++;
				
				autoFOV = GUI.Toggle(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), autoFOV, "Auto Zoom");//, leftLabel);
				line++;
				line++;
				
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Camera Position:", leftLabel);
				line++;
				string posButtonText = "Set Position w/ Click";
				if(setPresetOffset) posButtonText = "Clear Position";
				if(waitingForPosition) posButtonText = "Waiting...";
				if(FlightGlobals.ActiveVessel!=null && GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight-2), posButtonText))
				{
					if(setPresetOffset)
					{
						setPresetOffset = false;
					}
					else
					{
						waitingForPosition = true;
						mouseUp = false;
					}
				}
				line++;
				

				autoFlybyPosition = GUI.Toggle(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), autoFlybyPosition, "Auto Flyby Position");
				if(autoFlybyPosition) manualOffset = false;
				line++;
				
				manualOffset = GUI.Toggle(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), manualOffset, "Manual Flyby Position");
				line++;

				Color origGuiColor = GUI.color;
				if(manualOffset)
				{
					autoFlybyPosition = false;
				}
				else
				{
					GUI.color = new Color(0.5f, 0.5f, 0.5f, origGuiColor.a);
				}
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), 60, entryHeight), "Fwd:", leftLabel);
				float textFieldWidth = 42;
				Rect fwdFieldRect = new Rect(leftIndent+contentWidth-textFieldWidth-(3*incrButtonWidth), contentTop+(line*entryHeight), textFieldWidth, entryHeight);
				guiOffsetForward = GUI.TextField(fwdFieldRect, guiOffsetForward.ToString());
				if(float.TryParse(guiOffsetForward, out parseResult))
				{
					manualOffsetForward = parseResult;	
				}
				DrawIncrementButtons(fwdFieldRect, ref manualOffsetForward);
				guiOffsetForward = manualOffsetForward.ToString();

				line++;
				Rect rightFieldRect = new Rect(fwdFieldRect.x, contentTop+(line*entryHeight), textFieldWidth, entryHeight);
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), 60, entryHeight), "Right:", leftLabel);
				guiOffsetRight = GUI.TextField(rightFieldRect, guiOffsetRight);
				if(float.TryParse(guiOffsetRight, out parseResult))
				{
					manualOffsetRight = parseResult;	
				}
				DrawIncrementButtons(rightFieldRect, ref manualOffsetRight);
				guiOffsetRight = manualOffsetRight.ToString();
				line++;

				Rect upFieldRect = new Rect(fwdFieldRect.x, contentTop+(line*entryHeight), textFieldWidth, entryHeight);
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), 60, entryHeight), "Up:", leftLabel);
				guiOffsetUp = GUI.TextField(upFieldRect, guiOffsetUp);
				if(float.TryParse(guiOffsetUp, out parseResult))
				{
					manualOffsetUp = parseResult;	
				}
				DrawIncrementButtons(upFieldRect, ref manualOffsetUp);
				guiOffsetUp = manualOffsetUp.ToString();
				GUI.color = origGuiColor;

				line++;
				line++;
				
				string targetText = "None";
				if(camTarget!=null) targetText = camTarget.gameObject.name;
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Camera Target: "+targetText, leftLabel);
				line++;
				string tgtButtonText = "Set Target w/ Click";
				if(waitingForTarget) tgtButtonText = "waiting...";
				if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight-2), tgtButtonText))
				{
					waitingForTarget = true;
					mouseUp = false;
				}
				line++;
				if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), (contentWidth/2)-2, entryHeight-2), "Target Self"))
				{
					camTarget = FlightGlobals.ActiveVessel.GetReferenceTransformPart();
					hasTarget = true;
				}
				if(GUI.Button(new Rect(2+leftIndent+contentWidth/2, contentTop+(line*entryHeight), (contentWidth/2)-2, entryHeight-2), "Clear Target"))
				{
					camTarget = null;
					hasTarget = false;
				}
				line++;

				targetCoM = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight - 2), targetCoM, "Vessel Center of Mass");

				line += 1.25f;
				
				enableKeypad = GUI.Toggle(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), enableKeypad, "Keypad Control");
				if(enableKeypad)
				{
					line++;
					
					GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth/2, entryHeight), "Move Speed:");
					guiFreeMoveSpeed = GUI.TextField(new Rect(leftIndent+contentWidth/2, contentTop+(line*entryHeight), contentWidth/2, entryHeight), guiFreeMoveSpeed);
					if(float.TryParse(guiFreeMoveSpeed, out parseResult))
					{
						freeMoveSpeed = Mathf.Abs(parseResult);
						guiFreeMoveSpeed = freeMoveSpeed.ToString();
					}
					
					line++;
					
					GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth/2, entryHeight), "Zoom Speed:");
					guiKeyZoomSpeed = GUI.TextField(new Rect(leftIndent+contentWidth/2, contentTop+(line*entryHeight), contentWidth/2, entryHeight), guiKeyZoomSpeed);
					if(float.TryParse(guiKeyZoomSpeed, out parseResult))
					{
						keyZoomSpeed = Mathf.Abs(parseResult);	
						guiKeyZoomSpeed = keyZoomSpeed.ToString();
					}
				}
				else
				{
					line++;
					line++;
				}
				line++;
				line++;
				
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Keys:", centerLabel);
				line++;

				//activate key binding
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Activate: ", leftLabel);
				GUI.Label(new Rect(leftIndent + 60, contentTop+(line*entryHeight), 60, entryHeight), cameraKey, leftLabel);
				if(!isRecordingInput)
				{
					if(GUI.Button(new Rect(leftIndent + 125, contentTop+(line*entryHeight), 100, entryHeight), "Bind Key"))
					{
						mouseUp = false;
						isRecordingInput = true;
						isRecordingActivate = true;
					}
			    }
				else if(mouseUp && isRecordingActivate)
				{
					GUI.Label(new Rect(leftIndent + 125, contentTop+(line*entryHeight), 100, entryHeight), "Press a Key", leftLabel);

					string inputString = CCInputUtils.GetInputString();
					if(inputString.Length > 0)
					{
						cameraKey = inputString;
						isRecordingInput = false;
						isRecordingActivate = false;
					}
				}

				line++;

				//revert key binding
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Revert: ", leftLabel);
				GUI.Label(new Rect(leftIndent + 60, contentTop+(line*entryHeight), 60, entryHeight), revertKey);
				if(!isRecordingInput)
				{
					if(GUI.Button(new Rect(leftIndent + 125, contentTop+(line*entryHeight), 100, entryHeight), "Bind Key"))
					{
						mouseUp = false;
						isRecordingInput = true;
						isRecordingRevert = true;
					}
				}
				else if(mouseUp && isRecordingRevert)
				{
					GUI.Label(new Rect(leftIndent + 125, contentTop+(line*entryHeight), 100, entryHeight), "Press a Key", leftLabel);
					string inputString = CCInputUtils.GetInputString();
					if(inputString.Length > 0)
					{
						revertKey = inputString;
						isRecordingInput = false;
						isRecordingRevert = false;
					}
				}
			}


			line++;
			line++;
			Rect saveRect = new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight);
			if(GUI.Button(saveRect, "Save"))
			{
				CTPersistantField.Save();
			}

			Rect loadRect = new Rect(saveRect);
			loadRect.x += contentWidth / 2;
			if(GUI.Button(loadRect, "Reload"))
			{
				CTPersistantField.Load();
				guiOffsetForward = manualOffsetForward.ToString();
				guiOffsetRight = manualOffsetRight.ToString();
				guiOffsetUp = manualOffsetUp.ToString();
				guiKeyZoomSpeed = keyZoomSpeed.ToString();
				guiFreeMoveSpeed = freeMoveSpeed.ToString();
			}
			
			if(!hasFixedWindow)
			{
				windowHeight = contentTop+(line*entryHeight)+entryHeight+entryHeight;
				windowRect = new Rect(Screen.width-windowWidth-5, 45, windowWidth, windowHeight);
				hasFixedWindow = true;
			}
		}

		void DrawIncrementButtons(Rect fieldRect, ref float val)
		{
			Rect incrButtonRect = new Rect(fieldRect.x-incrButtonWidth, fieldRect.y, incrButtonWidth, entryHeight); 
			if(GUI.Button(incrButtonRect, "-"))
			{
				val -= 5;
			}

			incrButtonRect.x -= incrButtonWidth;

			if(GUI.Button(incrButtonRect, "--"))
			{
				val -= 50;
			}

			incrButtonRect.x = fieldRect.x + fieldRect.width;

			if(GUI.Button(incrButtonRect, "+"))
			{
				val += 5;
			}

			incrButtonRect.x += incrButtonWidth;

			if(GUI.Button(incrButtonRect, "++"))
			{
				val += 50;
			}
		}
		
		//AppLauncherSetup
		void AddToolbarButton()
		{
			if(!hasAddedButton)
			{
				Texture buttonTexture = GameDatabase.Instance.GetTexture("CameraTools/Textures/icon", false);
				ApplicationLauncher.Instance.AddModApplication(EnableGui, DisableGui, Dummy, Dummy, Dummy, Dummy, ApplicationLauncher.AppScenes.FLIGHT, buttonTexture);
				CamTools.hasAddedButton = true;
			}
			
		}
		
		void EnableGui()
		{
			guiEnabled = true;
			Debug.Log ("Showing CamTools GUI");
		}
		
		void DisableGui()
		{
			guiEnabled = false;	
			Debug.Log ("Hiding CamTools GUI");
		}
			
		void Dummy()
		{}
		
		void GameUIEnable()
		{
			gameUIToggle = true;	
		}
		
		void GameUIDisable()
		{
			gameUIToggle = false;	
		}
		
		void CycleRefereneMode(bool forward)
		{
			var length = System.Enum.GetValues(typeof(ReferenceModes)).Length;
			if(forward)
			{
				referenceMode++;
				if((int)referenceMode == length) referenceMode = 0;
			}
			else
			{
				referenceMode--;
				if((int)referenceMode == -1) referenceMode = (ReferenceModes) length-1;
			}
		}
		
		void OnFloatingOriginShift(Vector3d offset)
		{
			/*
			Debug.LogWarning ("======Floating origin shifted.======");
			Debug.LogWarning ("======Passed offset: "+offset+"======");
			Debug.LogWarning ("======FloatingOrigin offset: "+FloatingOrigin.fetch.offset+"======");
			Debug.LogWarning("========Floating Origin threshold: "+FloatingOrigin.fetch.threshold+"==========");
			*/
		}
		
	}
	
	
	
	public enum ReferenceModes {InitialVelocity, Surface, Orbit}
	
	public enum ToolModes {StationaryCamera};
}

