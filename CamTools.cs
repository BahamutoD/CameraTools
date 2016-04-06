using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using KSP.UI.Screens;
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
		bool cameraToolActive = false;
		
		
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

		//dogfight cam
		Vessel dogfightPrevTarget;
		Vessel dogfightTarget;
		[CTPersistantField]
		float dogfightDistance = 30;
		[CTPersistantField]
		float dogfightOffsetX = 10;
		[CTPersistantField]
		float dogfightOffsetY = 4;
		float dogfightMaxOffset = 50;
		float dogfightLerp = 20;
		[CTPersistantField]
		float autoZoomMargin = 20;
		List<Vessel> loadedVessels;
		bool showingVesselList = false;
		bool dogfightLastTarget = false;
		Vector3 dogfightLastTargetPosition;
		Vector3 dogfightLastTargetVelocity;
		bool dogfightVelocityChase = false;
		//bdarmory
		bool hasBDAI = false;
		[CTPersistantField]
		public bool useBDAutoTarget = false;
		object aiComponent = null;
		FieldInfo bdAiTargetField;


		//pathing
		int selectedPathIndex = -1;
		List<CameraPath> availablePaths;
		CameraPath currentPath
		{
			get
			{
				if(selectedPathIndex >= 0 && selectedPathIndex < availablePaths.Count)
				{
					return availablePaths[selectedPathIndex];
				}
				else
				{
					return null;
				}
			}
		}
		int currentKeyframeIndex = -1;
		float currentKeyframeTime;
		string currKeyTimeString;
		bool showKeyframeEditor = false;
		float pathStartTime;
		bool isPlayingPath = false;
		float pathTime
		{
			get
			{
				return Time.time - pathStartTime;
			}
		}
		Vector2 keysScrollPos;

		void Awake()
		{
			if(fetch)
			{
				Destroy(fetch);
			}

			fetch = this;

			Load();

			guiOffsetForward = manualOffsetForward.ToString();
			guiOffsetRight = manualOffsetRight.ToString();
			guiOffsetUp = manualOffsetUp.ToString();
			guiKeyZoomSpeed = keyZoomSpeed.ToString();
			guiFreeMoveSpeed = freeMoveSpeed.ToString();
		}

		void Start()
		{
			windowRect = new Rect(Screen.width-windowWidth-40, 0, windowWidth, windowHeight);
			flightCamera = FlightCamera.fetch;
			cameraToolActive = false;
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

				CheckForBDAI(FlightGlobals.ActiveVessel);
			}
			bdAiTargetField = GetAITargetField();
			GameEvents.onVesselChange.Add(SwitchToVessel);
		}

		void OnDestroy()
		{
			GameEvents.onVesselChange.Remove(SwitchToVessel);
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
						if(!cameraToolActive)
						{
							SaveOriginalCamera();
							StartStationaryCamera();
						}
						else
						{
							//RevertCamera();
							StartStationaryCamera();
						}
					}
					else if(toolMode == ToolModes.DogfightCamera)
					{
						if(!cameraToolActive)
						{
							SaveOriginalCamera();
							StartDogfightCamera();
						}
						else
						{
							StartDogfightCamera();
						}
					}
					else if(toolMode == ToolModes.Pathing)
					{
						if(!cameraToolActive)
						{
							SaveOriginalCamera();
						}
						StartPathingCam();
						PlayPathingCam();
					}
				}


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
			}


			//stationary camera
			if(cameraToolActive)
			{
				if(toolMode == ToolModes.StationaryCamera)
				{
					UpdateStationaryCamera();
				}
				else if(toolMode == ToolModes.DogfightCamera)
				{
					UpdateDogfightCamera();
				}
				else if(toolMode == ToolModes.Pathing)
				{
					UpdatePathingCam();
				}
			}
			else
			{
				if(!autoFOV)
				{
					zoomFactor = Mathf.Exp(zoomExp)/Mathf.Exp(1);
				}
			}

			if(toolMode == ToolModes.DogfightCamera)
			{
				if(dogfightTarget && dogfightTarget.isActiveVessel)
				{
					dogfightTarget = null;
					if(cameraToolActive)
					{
						RevertCamera();
					}
				}
			}
			
			
			if(hasDied && Time.time-diedTime > 2)
			{
				RevertCamera();	
			}
		}

		void StartDogfightCamera()
		{
			if(FlightGlobals.ActiveVessel == null)
			{
				Debug.Log("No active vessel.");
				return;
			}



			if(!dogfightTarget)
			{
				dogfightVelocityChase = true;
			}
			else
			{
				dogfightVelocityChase = false;
			}

			dogfightPrevTarget = dogfightTarget;

			hasDied = false;
			vessel = FlightGlobals.ActiveVessel;
			cameraUp = -FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()).normalized;

			flightCamera.transform.parent = cameraParent.transform;
			flightCamera.setTarget(null);
			cameraParent.transform.position = vessel.transform.position+vessel.rb_velocity*Time.fixedDeltaTime;

			cameraToolActive = true;

			ResetDoppler();
			if(OnResetCTools != null)
			{
				OnResetCTools();
			}

			SetDoppler(false);
			AddAtmoAudioControllers(false);
		}

		void UpdateDogfightCamera()
		{
			if(!vessel || (!dogfightTarget && !dogfightLastTarget && !dogfightVelocityChase))
			{
				RevertCamera();
				return;
			}
				

			if(dogfightTarget)
			{
				dogfightLastTarget = true;
				dogfightLastTargetPosition = dogfightTarget.CoM;
				dogfightLastTargetVelocity = dogfightTarget.rb_velocity;
			}
			else if(dogfightLastTarget)
			{
				dogfightLastTargetPosition += dogfightLastTargetVelocity * Time.fixedDeltaTime;
			}

			cameraParent.transform.position = (vessel.CoM - (vessel.rb_velocity * Time.fixedDeltaTime));	

			if(dogfightVelocityChase)
			{
				if(vessel.srfSpeed > 1)
				{
					dogfightLastTargetPosition = vessel.CoM + (vessel.srf_velocity.normalized * 5000);
				}
				else
				{
					dogfightLastTargetPosition = vessel.CoM + (vessel.ReferenceTransform.up * 5000);
				}
			}

			Vector3 offsetDirection = Vector3.Cross(cameraUp, dogfightLastTargetPosition - vessel.CoM).normalized;
			Vector3 camPos = vessel.CoM + ((vessel.CoM - dogfightLastTargetPosition).normalized * dogfightDistance) + (dogfightOffsetX * offsetDirection) + (dogfightOffsetY * cameraUp);


			Vector3 localCamPos = cameraParent.transform.InverseTransformPoint(camPos);
			flightCamera.transform.localPosition = Vector3.Lerp(flightCamera.transform.localPosition, localCamPos, dogfightLerp * Time.fixedDeltaTime);

			//rotation
			Quaternion vesselLook = Quaternion.LookRotation(vessel.CoM-flightCamera.transform.position, cameraUp);
			Quaternion targetLook = Quaternion.LookRotation(dogfightLastTargetPosition-flightCamera.transform.position, cameraUp);
			Quaternion camRot = Quaternion.Lerp(vesselLook, targetLook, 0.5f);
			flightCamera.transform.rotation = Quaternion.Lerp(flightCamera.transform.rotation, camRot, dogfightLerp * Time.fixedDeltaTime);

			//autoFov
			if(autoFOV)
			{
				float targetFoV;
				if(dogfightVelocityChase)
				{
					targetFoV = Mathf.Clamp((7000 / (dogfightDistance + 100)) - 14 + autoZoomMargin, 2, 60);
				}
				else
				{
					float angle = Vector3.Angle((dogfightLastTargetPosition + (dogfightLastTargetVelocity * Time.fixedDeltaTime)) - flightCamera.transform.position, (vessel.CoM + (vessel.rb_velocity * Time.fixedDeltaTime)) - flightCamera.transform.position);
					targetFoV = Mathf.Clamp(angle + autoZoomMargin, 0.1f, 60f);
				}
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

			//free move
			if(enableKeypad)
			{
				if(Input.GetKey(fmUpKey))
				{
					dogfightOffsetY += freeMoveSpeed * Time.fixedDeltaTime;
					dogfightOffsetY = Mathf.Clamp(dogfightOffsetY, -dogfightMaxOffset, dogfightMaxOffset);
				}
				else if(Input.GetKey(fmDownKey))
				{
					dogfightOffsetY -= freeMoveSpeed * Time.fixedDeltaTime;	
					dogfightOffsetY = Mathf.Clamp(dogfightOffsetY, -dogfightMaxOffset, dogfightMaxOffset);
				}
				if(Input.GetKey(fmForwardKey))
				{
					dogfightDistance -= freeMoveSpeed * Time.fixedDeltaTime;
					dogfightDistance = Mathf.Clamp(dogfightDistance, 1f, 100f);
				}
				else if(Input.GetKey(fmBackKey))
				{
					dogfightDistance += freeMoveSpeed * Time.fixedDeltaTime;
					dogfightDistance = Mathf.Clamp(dogfightDistance, 1f, 100f);
				}
				if(Input.GetKey(fmLeftKey))
				{
					dogfightOffsetX -= freeMoveSpeed * Time.fixedDeltaTime;
					dogfightOffsetX = Mathf.Clamp(dogfightOffsetX, -dogfightMaxOffset, dogfightMaxOffset);
				}
				else if(Input.GetKey(fmRightKey))
				{
					dogfightOffsetX += freeMoveSpeed * Time.fixedDeltaTime;
					dogfightOffsetX = Mathf.Clamp(dogfightOffsetX, -dogfightMaxOffset, dogfightMaxOffset);
				}

				//keyZoom
				if(!autoFOV)
				{
					if(Input.GetKey(fmZoomInKey))
					{
						zoomExp = Mathf.Clamp(zoomExp + (keyZoomSpeed * Time.fixedDeltaTime), 1, 8);
					}
					else if(Input.GetKey(fmZoomOutKey))
					{
						zoomExp = Mathf.Clamp(zoomExp - (keyZoomSpeed * Time.fixedDeltaTime), 1, 8);
					}
				}
				else
				{
					if(Input.GetKey(fmZoomInKey))
					{
						autoZoomMargin = Mathf.Clamp(autoZoomMargin + (keyZoomSpeed  * 10 * Time.fixedDeltaTime), 0, 50);
					}
					else if(Input.GetKey(fmZoomOutKey))
					{
						autoZoomMargin = Mathf.Clamp(autoZoomMargin - (keyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50);
					}
				}
			}

			//vessel camera shake
			if(shakeMultiplier > 0)
			{
				foreach(var v in FlightGlobals.Vessels)
				{
					if(!v || !v.loaded || v.packed || v.isActiveVessel) continue;
					VesselCameraShake(v);
				}
			}
			UpdateCameraShake();

			if(hasBDAI && useBDAutoTarget)
			{
				Vessel newAITarget = GetAITargetedVessel();
				if(newAITarget)
				{
					dogfightTarget = newAITarget;
				}
			}

			if(dogfightTarget != dogfightPrevTarget)
			{
				//RevertCamera();
				StartDogfightCamera();
			}
		}

		void UpdateStationaryCamera()
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



			if(vessel != null)
			{
				cameraParent.transform.position = manualPosition + (vessel.findWorldCenterOfMass() - vessel.rb_velocity * Time.fixedDeltaTime);	

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
				if(!autoFOV)
				{
					if(Input.GetKey(fmZoomInKey))
					{
						zoomExp = Mathf.Clamp(zoomExp + (keyZoomSpeed * Time.fixedDeltaTime), 1, 8);
					}
					else if(Input.GetKey(fmZoomOutKey))
					{
						zoomExp = Mathf.Clamp(zoomExp - (keyZoomSpeed * Time.fixedDeltaTime), 1, 8);
					}
				}
				else
				{
					if(Input.GetKey(fmZoomInKey))
					{
						autoZoomMargin = Mathf.Clamp(autoZoomMargin + (keyZoomSpeed  * 10 * Time.fixedDeltaTime), 0, 50);
					}
					else if(Input.GetKey(fmZoomOutKey))
					{
						autoZoomMargin = Mathf.Clamp(autoZoomMargin - (keyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50);
					}
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
				float targetFoV = Mathf.Clamp((7000 / (cameraDistance + 100)) - 14 + autoZoomMargin, 2, 60);
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
			UpdateCameraShake();
		}

		
		void LateUpdate()
		{
			
			//retain pos and rot after vessel destruction
			if (cameraToolActive && flightCamera.transform.parent != cameraParent.transform)	
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

		void AddAtmoAudioControllers(bool includeActiveVessel)
		{
			if(!useAudioEffects)
			{
				return;
			}

			foreach(var vessel in FlightGlobals.Vessels)
			{
				if(!vessel || !vessel.loaded || vessel.packed || (!includeActiveVessel && vessel.isActiveVessel))
				{
					continue;
				}

				vessel.gameObject.AddComponent<CTAtmosphericAudioController>();
			}
		}
		
		void SetDoppler(bool includeActiveVessel)
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

				if(!includeActiveVessel)
				{
					Part p = audioSources[i].GetComponentInParent<Part>();
					if(p && p.vessel.isActiveVessel) continue;
				}

				audioSources[i].dopplerLevel = 1;
				audioSources[i].velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
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
					audioSources[i].velocityUpdateMode = AudioVelocityUpdateMode.Auto;
				}
			}

		

			hasSetDoppler = false;
		}

		
		void StartStationaryCamera()
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
				cameraParent.transform.position = vessel.transform.position+vessel.rb_velocity*Time.fixedDeltaTime;
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
				
				cameraToolActive = true;

				SetDoppler(true);
				AddAtmoAudioControllers(true);
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

			if(cameraToolActive)
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
			
			

			cameraToolActive = false;

			ResetDoppler();
			if(OnResetCTools != null)
			{
				OnResetCTools();
			}

			StopPlayingPath();
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
			if(cameraToolActive)	
			{
				RevertCamera();	
			}
		}

		void PostDeathRevert(GameScenes f)
		{
			if(cameraToolActive)	
			{
				RevertCamera();	
			}
		}
		
		void PostDeathRevert(Vessel v)
		{
			if(cameraToolActive)	
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

				if(showKeyframeEditor)
				{
					KeyframeEditorWindow();
				}
				if(showPathSelectorWindow)
				{
					PathSelectorWindow();
				}
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

			GUIStyle leftLabelBold = new GUIStyle(leftLabel);
			leftLabelBold.fontStyle = FontStyle.Bold;
			
			
			
			float line = 1;
			float contentWidth = (windowWidth) - (2*leftIndent);
			float contentTop = 20;
			GUIStyle titleStyle = new GUIStyle(centerLabel);
			titleStyle.fontSize = 24;
			titleStyle.alignment = TextAnchor.MiddleCenter;
			GUI.Label(new Rect(0, contentTop, windowWidth, 40), "Camera Tools", titleStyle);
			line++;
			float parseResult;

			//tool mode switcher
			GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Tool: "+toolMode.ToString(), leftLabelBold);
			line++;
			if(!cameraToolActive)
			{
				if(GUI.Button(new Rect(leftIndent, contentTop + (line * entryHeight), 25, entryHeight - 2), "<"))
				{
					CycleToolMode(false);
				}
				if(GUI.Button(new Rect(leftIndent + 25 + 4, contentTop + (line * entryHeight), 25, entryHeight - 2), ">"))
				{
					CycleToolMode(true);
				}
			}
			line++;
			line++;
			if(autoFOV)
			{
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight), "Autozoom Margin: ");
				line++;
				autoZoomMargin = GUI.HorizontalSlider(new Rect(leftIndent, contentTop + ((line) * entryHeight), contentWidth - 45, entryHeight), autoZoomMargin, 0, 50);
				GUI.Label(new Rect(leftIndent + contentWidth - 40, contentTop + ((line - 0.15f) * entryHeight), 40, entryHeight), autoZoomMargin.ToString("0.0"), leftLabel);
			}
			else
			{
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Zoom:", leftLabel);
				line++;
				zoomExp = GUI.HorizontalSlider(new Rect(leftIndent, contentTop + ((line) * entryHeight), contentWidth - 45, entryHeight), zoomExp, 1, 8);
				GUI.Label(new Rect(leftIndent + contentWidth - 40, contentTop + ((line - 0.15f) * entryHeight), 40, entryHeight), zoomFactor.ToString("0.0") + "x", leftLabel);
			}
			line++;

			if(toolMode != ToolModes.Pathing)
			{
				autoFOV = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), autoFOV, "Auto Zoom");//, leftLabel);
				line++;
			}
			line++;
			useAudioEffects = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), useAudioEffects, "Use Audio Effects");
			line++;
			GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Camera shake:");
			line++;
			shakeMultiplier = GUI.HorizontalSlider(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth - 45, entryHeight), shakeMultiplier, 0f, 10f);
			GUI.Label(new Rect(leftIndent + contentWidth - 40, contentTop + ((line - 0.25f) * entryHeight), 40, entryHeight), shakeMultiplier.ToString("0.00") + "x");
			line++;
			line++;

			//Stationary camera GUI
			if(toolMode == ToolModes.StationaryCamera)
			{
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Frame of Reference: " + referenceMode.ToString(), leftLabel);
				line++;
				if(GUI.Button(new Rect(leftIndent, contentTop + (line * entryHeight), 25, entryHeight - 2), "<"))
				{
					CycleReferenceMode(false);
				}
				if(GUI.Button(new Rect(leftIndent + 25 + 4, contentTop + (line * entryHeight), 25, entryHeight - 2), ">"))
				{
					CycleReferenceMode(true);
				}
				
				line++;
				
				if(referenceMode == ReferenceModes.Surface || referenceMode == ReferenceModes.Orbit)
				{
					GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight), "Max Rel. V: ", leftLabel);
					maxRelV = float.Parse(GUI.TextField(new Rect(leftIndent + contentWidth / 2, contentTop + (line * entryHeight), contentWidth / 2, entryHeight), maxRelV.ToString()));	
				}
				else if(referenceMode == ReferenceModes.InitialVelocity)
				{
					useOrbital = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), useOrbital, " Orbital");
				}
				line++;

				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Camera Position:", leftLabel);
				line++;
				string posButtonText = "Set Position w/ Click";
				if(setPresetOffset) posButtonText = "Clear Position";
				if(waitingForPosition) posButtonText = "Waiting...";
				if(FlightGlobals.ActiveVessel != null && GUI.Button(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight - 2), posButtonText))
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
				

				autoFlybyPosition = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), autoFlybyPosition, "Auto Flyby Position");
				if(autoFlybyPosition) manualOffset = false;
				line++;
				
				manualOffset = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), manualOffset, "Manual Flyby Position");
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
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), 60, entryHeight), "Fwd:", leftLabel);
				float textFieldWidth = 42;
				Rect fwdFieldRect = new Rect(leftIndent + contentWidth - textFieldWidth - (3 * incrButtonWidth), contentTop + (line * entryHeight), textFieldWidth, entryHeight);
				guiOffsetForward = GUI.TextField(fwdFieldRect, guiOffsetForward.ToString());
				if(float.TryParse(guiOffsetForward, out parseResult))
				{
					manualOffsetForward = parseResult;	
				}
				DrawIncrementButtons(fwdFieldRect, ref manualOffsetForward);
				guiOffsetForward = manualOffsetForward.ToString();

				line++;
				Rect rightFieldRect = new Rect(fwdFieldRect.x, contentTop + (line * entryHeight), textFieldWidth, entryHeight);
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), 60, entryHeight), "Right:", leftLabel);
				guiOffsetRight = GUI.TextField(rightFieldRect, guiOffsetRight);
				if(float.TryParse(guiOffsetRight, out parseResult))
				{
					manualOffsetRight = parseResult;	
				}
				DrawIncrementButtons(rightFieldRect, ref manualOffsetRight);
				guiOffsetRight = manualOffsetRight.ToString();
				line++;

				Rect upFieldRect = new Rect(fwdFieldRect.x, contentTop + (line * entryHeight), textFieldWidth, entryHeight);
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
				if(camTarget != null) targetText = camTarget.gameObject.name;
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Camera Target: " + targetText, leftLabel);
				line++;
				string tgtButtonText = "Set Target w/ Click";
				if(waitingForTarget) tgtButtonText = "waiting...";
				if(GUI.Button(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight - 2), tgtButtonText))
				{
					waitingForTarget = true;
					mouseUp = false;
				}
				line++;
				if(GUI.Button(new Rect(leftIndent, contentTop + (line * entryHeight), (contentWidth / 2) - 2, entryHeight - 2), "Target Self"))
				{
					camTarget = FlightGlobals.ActiveVessel.GetReferenceTransformPart();
					hasTarget = true;
				}
				if(GUI.Button(new Rect(2 + leftIndent + contentWidth / 2, contentTop + (line * entryHeight), (contentWidth / 2) - 2, entryHeight - 2), "Clear Target"))
				{
					camTarget = null;
					hasTarget = false;
				}
				line++;

				targetCoM = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight - 2), targetCoM, "Vessel Center of Mass");
			}
			else if(toolMode == ToolModes.DogfightCamera)
			{
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Secondary target:");
				line++;
				string tVesselLabel;
				if(showingVesselList)
				{
					tVesselLabel = "Clear";
				}
				else if(dogfightTarget)
				{
					tVesselLabel = dogfightTarget.vesselName;
				}
				else
				{
					tVesselLabel = "None";
				}
				if(GUI.Button(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), tVesselLabel))
				{
					if(showingVesselList)
					{
						showingVesselList = false;
						dogfightTarget = null;
					}
					else
					{
						UpdateLoadedVessels();
						showingVesselList = true;
					}
				}
				line++;

				if(showingVesselList)
				{
					foreach(var v in loadedVessels)
					{
						if(!v || !v.loaded) continue;
						if(GUI.Button(new Rect(leftIndent + 10, contentTop + (line * entryHeight), contentWidth - 10, entryHeight), v.vesselName))
						{
							dogfightTarget = v;
							showingVesselList = false;
						}
						line++;
					}
				}
				line++;

				if(hasBDAI)
				{
					useBDAutoTarget = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight - 2), useBDAutoTarget, "BDA AI Auto target");
					line++;
				}

				line++;
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight), "Distance: " + dogfightDistance.ToString("0.0"));
				line++;
				dogfightDistance = GUI.HorizontalSlider(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), dogfightDistance, 1, 100);
				line += 1.5f;

				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Offset:");
				line++;
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), 15, entryHeight), "X: ");
				dogfightOffsetX = GUI.HorizontalSlider(new Rect(leftIndent + 15, contentTop + (line * entryHeight) + 6, contentWidth - 45, entryHeight), dogfightOffsetX, -dogfightMaxOffset, dogfightMaxOffset);
				GUI.Label(new Rect(leftIndent + contentWidth - 25, contentTop + (line * entryHeight), 25, entryHeight), dogfightOffsetX.ToString("0.0"));
				line++;
				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), 15, entryHeight), "Y: ");
				dogfightOffsetY = GUI.HorizontalSlider(new Rect(leftIndent + 15, contentTop + (line * entryHeight) + 6, contentWidth - 45, entryHeight), dogfightOffsetY, -dogfightMaxOffset, dogfightMaxOffset);
				GUI.Label(new Rect(leftIndent + contentWidth - 25, contentTop + (line * entryHeight), 25, entryHeight), dogfightOffsetY.ToString("0.0"));
				line += 1.5f;
			}
			else if(toolMode == ToolModes.Pathing)
			{
				if(selectedPathIndex >= 0)
				{
					GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Path:");
					currentPath.pathName = GUI.TextField(new Rect(leftIndent + 34, contentTop + (line * entryHeight), contentWidth-34, entryHeight), currentPath.pathName);
				}
				else
				{
					GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Path: None");
				}
				line += 1.25f;
				if(GUI.Button(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Open Path"))
				{
					TogglePathList();
				}
				line++;
				if(GUI.Button(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth/2, entryHeight), "New Path"))
				{
					CreateNewPath();
				}
				if(GUI.Button(new Rect(leftIndent + (contentWidth/2), contentTop + (line * entryHeight), contentWidth / 2, entryHeight), "Delete Path"))
				{
					DeletePath(selectedPathIndex);
				}
				line ++;
				if(selectedPathIndex >= 0)
				{
					GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Interpolation rate: " + currentPath.lerpRate.ToString("0.0"));
					line++;
					currentPath.lerpRate = GUI.HorizontalSlider(new Rect(leftIndent, contentTop + (line * entryHeight) + 4, contentWidth - 50, entryHeight), currentPath.lerpRate, 1f, 15f);
					currentPath.lerpRate = Mathf.Round(currentPath.lerpRate * 10) / 10;
					line++;
					GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Path timescale " + currentPath.timeScale.ToString("0.00"));
					line++;
					currentPath.timeScale = GUI.HorizontalSlider(new Rect(leftIndent, contentTop + (line * entryHeight) + 4, contentWidth - 50, entryHeight), currentPath.timeScale, 0.05f, 4f);
					currentPath.timeScale = Mathf.Round(currentPath.timeScale * 20) / 20;
					line++;
					float viewHeight = Mathf.Max(6 * entryHeight, currentPath.keyframeCount * entryHeight);
					Rect scrollRect = new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, 6 * entryHeight);
					GUI.Box(scrollRect, string.Empty);
					float viewContentWidth = contentWidth - (2 * leftIndent);
					keysScrollPos = GUI.BeginScrollView(scrollRect, keysScrollPos, new Rect(0, 0, viewContentWidth, viewHeight));
					if(currentPath.keyframeCount > 0)
					{
						Color origGuiColor = GUI.color;
						for(int i = 0; i < currentPath.keyframeCount; i++)
						{
							if(i == currentKeyframeIndex)
							{
								GUI.color = Color.green;
							}
							else
							{
								GUI.color = origGuiColor;
							}
							string kLabel = "#" + i.ToString() + ": " + currentPath.GetKeyframe(i).time.ToString("0.00") + "s";
							if(GUI.Button(new Rect(0, (i * entryHeight), 3 * viewContentWidth / 4, entryHeight), kLabel))
							{
								SelectKeyframe(i);
							}
							if(GUI.Button(new Rect((3 * contentWidth / 4), (i * entryHeight), (viewContentWidth / 4) - 20, entryHeight), "X"))
							{
								DeleteKeyframe(i);
								break;
							}
							//line++;
						}
						GUI.color = origGuiColor;
					}
					GUI.EndScrollView();
					line += 6;
					line += 0.5f;
					if(GUI.Button(new Rect(leftIndent, contentTop + (line * entryHeight), 3 * contentWidth / 4, entryHeight), "New Key"))
					{
						CreateNewKeyframe();
					}
				}
			}

			line += 1.25f;

			enableKeypad = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), enableKeypad, "Keypad Control");
			if(enableKeypad)
			{
				line++;

				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight), "Move Speed:");
				guiFreeMoveSpeed = GUI.TextField(new Rect(leftIndent + contentWidth / 2, contentTop + (line * entryHeight), contentWidth / 2, entryHeight), guiFreeMoveSpeed);
				if(float.TryParse(guiFreeMoveSpeed, out parseResult))
				{
					freeMoveSpeed = Mathf.Abs(parseResult);
					guiFreeMoveSpeed = freeMoveSpeed.ToString();
				}

				line++;

				GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight), "Zoom Speed:");
				guiKeyZoomSpeed = GUI.TextField(new Rect(leftIndent + contentWidth / 2, contentTop + (line * entryHeight), contentWidth / 2, entryHeight), guiKeyZoomSpeed);
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
			GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Keys:", centerLabel);
			line++;

			//activate key binding
			GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Activate: ", leftLabel);
			GUI.Label(new Rect(leftIndent + 60, contentTop + (line * entryHeight), 60, entryHeight), cameraKey, leftLabel);
			if(!isRecordingInput)
			{
				if(GUI.Button(new Rect(leftIndent + 125, contentTop + (line * entryHeight), 100, entryHeight), "Bind Key"))
				{
					mouseUp = false;
					isRecordingInput = true;
					isRecordingActivate = true;
				}
			}
			else if(mouseUp && isRecordingActivate)
			{
				GUI.Label(new Rect(leftIndent + 125, contentTop + (line * entryHeight), 100, entryHeight), "Press a Key", leftLabel);

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
			GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight), "Revert: ", leftLabel);
			GUI.Label(new Rect(leftIndent + 60, contentTop + (line * entryHeight), 60, entryHeight), revertKey);
			if(!isRecordingInput)
			{
				if(GUI.Button(new Rect(leftIndent + 125, contentTop + (line * entryHeight), 100, entryHeight), "Bind Key"))
				{
					mouseUp = false;
					isRecordingInput = true;
					isRecordingRevert = true;
				}
			}
			else if(mouseUp && isRecordingRevert)
			{
				GUI.Label(new Rect(leftIndent + 125, contentTop + (line * entryHeight), 100, entryHeight), "Press a Key", leftLabel);
				string inputString = CCInputUtils.GetInputString();
				if(inputString.Length > 0)
				{
					revertKey = inputString;
					isRecordingInput = false;
					isRecordingRevert = false;
				}
			}

			line++;
			line++;
			Rect saveRect = new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight);
			if(GUI.Button(saveRect, "Save"))
			{
				Save();
			}

			Rect loadRect = new Rect(saveRect);
			loadRect.x += contentWidth / 2;
			if(GUI.Button(loadRect, "Reload"))
			{
				Load();
			}
			
			//fix length
			windowHeight = contentTop+(line*entryHeight)+entryHeight+entryHeight;
			windowRect.height = windowHeight;// = new Rect(windowRect.x, windowRect.y, windowWidth, windowHeight);
		}

		public static string pathSaveURL = "GameData/CameraTools/paths.cfg";
		void Save()
		{
			CTPersistantField.Save();

			ConfigNode pathFileNode = ConfigNode.Load(pathSaveURL);
			ConfigNode pathsNode = pathFileNode.GetNode("CAMERAPATHS");
			pathsNode.RemoveNodes("CAMERAPATH");

			foreach(var path in availablePaths)
			{
				path.Save(pathsNode);
			}
			pathFileNode.Save(pathSaveURL);
		}

		void Load()
		{
			CTPersistantField.Load();
			guiOffsetForward = manualOffsetForward.ToString();
			guiOffsetRight = manualOffsetRight.ToString();
			guiOffsetUp = manualOffsetUp.ToString();
			guiKeyZoomSpeed = keyZoomSpeed.ToString();
			guiFreeMoveSpeed = freeMoveSpeed.ToString();

			DeselectKeyframe();
			selectedPathIndex = -1;
			availablePaths = new List<CameraPath>();
			ConfigNode pathFileNode = ConfigNode.Load(pathSaveURL);
			foreach(var node in pathFileNode.GetNode("CAMERAPATHS").GetNodes("CAMERAPATH"))
			{
				availablePaths.Add(CameraPath.Load(node));
			}
		}

		void KeyframeEditorWindow()
		{
			float width = 300;
			float height = 130;
			Rect kWindowRect = new Rect(windowRect.x - width, windowRect.y + 365, width, height);
			GUI.Box(kWindowRect, string.Empty);
			GUI.BeginGroup(kWindowRect);
			GUI.Label(new Rect(5, 5, 100, 25), "Keyframe #"+currentKeyframeIndex);
			if(GUI.Button(new Rect(105, 5, 180, 25), "Revert Pos"))
			{
				ViewKeyframe(currentKeyframeIndex);
			}
			GUI.Label(new Rect(5, 35, 80, 25), "Time: ");
			currKeyTimeString = GUI.TextField(new Rect(100, 35, 195, 25), currKeyTimeString, 16);
			float parsed;
			if(float.TryParse(currKeyTimeString, out parsed))
			{
				currentKeyframeTime = parsed;
			}
			bool applied = false;
			if(GUI.Button(new Rect(100, 65, 195, 25), "Apply"))
			{
				Debug.Log("Applying keyframe at time: " + currentKeyframeTime);
				currentPath.SetTransform(currentKeyframeIndex, flightCamera.transform, zoomExp, currentKeyframeTime);
				applied = true;
			}
			if(GUI.Button(new Rect(100, 105, 195, 20), "Cancel"))
			{
				applied = true;
			}
			GUI.EndGroup();

			if(applied)
			{
				DeselectKeyframe();
			}
		}

		bool showPathSelectorWindow = false;
		Vector2 pathSelectScrollPos;
		void PathSelectorWindow()
		{
			float width = 300;
			float height = 300;
			float indent = 5;
			float scrollRectSize = width - indent - indent;
			Rect pSelectRect =  new Rect(windowRect.x - width, windowRect.y + 290, width, height);
			GUI.Box(pSelectRect, string.Empty);
			GUI.BeginGroup(pSelectRect);

			Rect scrollRect = new Rect(indent, indent, scrollRectSize, scrollRectSize);
			float scrollHeight = Mathf.Max(scrollRectSize, entryHeight * availablePaths.Count);
			Rect scrollViewRect = new Rect(0, 0, scrollRectSize-20, scrollHeight);
			pathSelectScrollPos = GUI.BeginScrollView(scrollRect, pathSelectScrollPos, scrollViewRect);
			bool selected = false;
			for(int i = 0; i < availablePaths.Count; i++)
			{
				if(GUI.Button(new Rect(0, i * entryHeight, scrollRectSize - 90, entryHeight), availablePaths[i].pathName))
				{
					SelectPath(i);
					selected = true;
				}
				if(GUI.Button(new Rect(scrollRectSize-80, i * entryHeight, 60, entryHeight), "Delete"))
				{
					DeletePath(i);
					break;
				}
			}

			GUI.EndScrollView();

			GUI.EndGroup();
			if(selected)
			{
				showPathSelectorWindow = false;
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
		
		void CycleReferenceMode(bool forward)
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

		void CycleToolMode(bool forward)
		{
			var length = System.Enum.GetValues(typeof(ToolModes)).Length;
			if(forward)
			{
				toolMode++;
				if((int)toolMode == length) toolMode = 0;
			}
			else
			{
				toolMode--;
				if((int)toolMode == -1) toolMode = (ToolModes) length-1;
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

		void UpdateLoadedVessels()
		{
			if(loadedVessels == null)
			{
				loadedVessels = new List<Vessel>();
			}
			else
			{
				loadedVessels.Clear();
			}

			foreach(var v in FlightGlobals.Vessels)
			{
				if(v.loaded && v.vesselType != VesselType.Debris && !v.isActiveVessel)
				{
					loadedVessels.Add(v);
				}
			}
		}

		private void CheckForBDAI(Vessel v)
		{
			hasBDAI = false;
			aiComponent = null;
			if(v)
			{
				foreach(Part p in v.parts)
				{
					if(p.GetComponent("BDModulePilotAI"))
					{
						hasBDAI = true;
						aiComponent = (object)p.GetComponent("BDModulePilotAI");
						return;
					}
				}
			}
		}

		private Vessel GetAITargetedVessel()
		{
			if(!hasBDAI || aiComponent==null || bdAiTargetField==null)
			{
				return null;
			}

			return (Vessel) bdAiTargetField.GetValue(aiComponent);
		}

		private Type AIModuleType()
		{
			//Debug.Log("loaded assy's: ");
			foreach(var assy in AssemblyLoader.loadedAssemblies)
			{
				//Debug.Log("- "+assy.assembly.FullName);
				if(assy.assembly.FullName.Contains("BahaTurret,"))
				{
					foreach(var t in assy.assembly.GetTypes())
					{
						if(t.Name == "BDModulePilotAI")
						{
							return t;
						}
					}
				}
			}

			return null;
		}

		private FieldInfo GetAITargetField()
		{
			Type aiModType = AIModuleType();
			if(aiModType == null) return null;

			FieldInfo[] fields = aiModType.GetFields(BindingFlags.NonPublic|BindingFlags.Instance);
			//Debug.Log("bdai fields: ");
			foreach(var f in fields)
			{
				//Debug.Log("- " + f.Name);
				if(f.Name == "targetVessel")
				{
					return f;
				}
			}

			return null;
		}


		void SwitchToVessel(Vessel v)
		{
			vessel = v;

			CheckForBDAI(v);

			if(cameraToolActive)
			{
				if(toolMode == ToolModes.DogfightCamera)
				{
					StartCoroutine(ResetDogfightCamRoutine());
				}
			}
		}

		IEnumerator ResetDogfightCamRoutine()
		{
			yield return new WaitForEndOfFrame();
			RevertCamera();
			StartDogfightCamera();
		}

		void CreateNewPath()
		{
			showKeyframeEditor = false;
			availablePaths.Add(new CameraPath());
			selectedPathIndex = availablePaths.Count - 1;
		}

		void DeletePath(int index)
		{
			if(index < 0) return;
			if(index >= availablePaths.Count) return;
			availablePaths.RemoveAt(index);
			selectedPathIndex = -1;
		}

		void SelectPath(int index)
		{
			selectedPathIndex = index;
		}

		void SelectKeyframe(int index)
		{
			if(isPlayingPath)
			{
				StopPlayingPath();
			}
			currentKeyframeIndex = index;
			UpdateCurrentValues();
			showKeyframeEditor = true;
			ViewKeyframe(currentKeyframeIndex);
		}

		void DeselectKeyframe()
		{
			currentKeyframeIndex = -1;
			showKeyframeEditor = false;
		}

		void DeleteKeyframe(int index)
		{
			currentPath.RemoveKeyframe(index);
			if(index == currentKeyframeIndex)
			{
				DeselectKeyframe();
			}
			if(currentPath.keyframeCount > 0 && currentKeyframeIndex >= 0)
			{
				SelectKeyframe(Mathf.Clamp(currentKeyframeIndex, 0, currentPath.keyframeCount - 1));
			}
		}

		void UpdateCurrentValues()
		{
			if(currentPath == null) return;
			if(currentKeyframeIndex < 0 || currentKeyframeIndex >= currentPath.keyframeCount)
			{
				return;
			}
			CameraKeyframe currentKey = currentPath.GetKeyframe(currentKeyframeIndex);
			currentKeyframeTime = currentKey.time;

			currKeyTimeString = currentKeyframeTime.ToString();
		}

		void CreateNewKeyframe()
		{
			if(!cameraToolActive)
			{
				StartPathingCam();
			}

			showPathSelectorWindow = false;

			float time = currentPath.keyframeCount > 0 ? currentPath.GetKeyframe(currentPath.keyframeCount - 1).time + 1 : 0;
			currentPath.AddTransform(flightCamera.transform, zoomExp, time);
			SelectKeyframe(currentPath.keyframeCount - 1);

			if(currentPath.keyframeCount > 6)
			{
				keysScrollPos.y += entryHeight;
			}
		}

		void ViewKeyframe(int index)
		{
			if(!cameraToolActive)
			{
				StartPathingCam();
			}
			CameraKeyframe currentKey = currentPath.GetKeyframe(index);
			flightCamera.transform.localPosition = currentKey.position;
			flightCamera.transform.localRotation = currentKey.rotation;
			zoomExp = currentKey.zoom;
		}

		void StartPathingCam()
		{
			vessel = FlightGlobals.ActiveVessel;
			cameraUp = -FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()).normalized;
			if(FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel(vessel) == FlightCamera.Modes.ORBITAL))
			{
				cameraUp = Vector3.up;
			}

			cameraParent.transform.position = vessel.transform.position+vessel.rb_velocity*Time.fixedDeltaTime;
			cameraParent.transform.rotation = vessel.transform.rotation;
			flightCamera.transform.parent = cameraParent.transform;
			flightCamera.setTarget(null);


			cameraToolActive = true;
		}

		void PlayPathingCam()
		{
			if(selectedPathIndex < 0)
			{
				RevertCamera();
				return;
			}

			if(currentPath.keyframeCount <= 0)
			{
				RevertCamera();
				return;
			}

			DeselectKeyframe();

			if(!cameraToolActive)
			{
				StartPathingCam();
			}

			CameraTransformation firstFrame = currentPath.Evaulate(0);
			flightCamera.transform.localPosition = firstFrame.position;
			flightCamera.transform.localRotation = firstFrame.rotation;
			zoomExp = firstFrame.zoom;

			isPlayingPath = true;
			pathStartTime = Time.time;
		}

		void StopPlayingPath()
		{
			isPlayingPath = false;
		}

		void TogglePathList()
		{
			showKeyframeEditor = false;
			showPathSelectorWindow = !showPathSelectorWindow;
		}

		void UpdatePathingCam()
		{
			cameraParent.transform.position = vessel.transform.position+vessel.rb_velocity*Time.fixedDeltaTime;
			cameraParent.transform.rotation = vessel.transform.rotation;

			if(isPlayingPath)
			{
				CameraTransformation tf = currentPath.Evaulate(pathTime * currentPath.timeScale);
				flightCamera.transform.localPosition = Vector3.Lerp(flightCamera.transform.localPosition, tf.position, currentPath.lerpRate*Time.fixedDeltaTime);
				flightCamera.transform.localRotation = Quaternion.Slerp(flightCamera.transform.localRotation, tf.rotation, currentPath.lerpRate*Time.fixedDeltaTime);
				zoomExp = Mathf.Lerp(zoomExp, tf.zoom, currentPath.lerpRate*Time.fixedDeltaTime);
			}
			else
			{
				//move
				//mouse panning, moving
				Vector3 forwardLevelAxis = flightCamera.transform.forward;//(Quaternion.AngleAxis(-90, cameraUp) * flightCamera.transform.right).normalized;
				Vector3 rightAxis = flightCamera.transform.right;//(Quaternion.AngleAxis(90, forwardLevelAxis) * cameraUp).normalized;
				if(enableKeypad)
				{
					if(Input.GetKey(fmUpKey))
					{
						flightCamera.transform.position += cameraUp * freeMoveSpeed * Time.fixedDeltaTime;	
					}
					else if(Input.GetKey(fmDownKey))
					{
						flightCamera.transform.position -= cameraUp * freeMoveSpeed * Time.fixedDeltaTime;	
					}
					if(Input.GetKey(fmForwardKey))
					{
						flightCamera.transform.position += forwardLevelAxis * freeMoveSpeed * Time.fixedDeltaTime;
					}
					else if(Input.GetKey(fmBackKey))
					{
						flightCamera.transform.position -= forwardLevelAxis * freeMoveSpeed * Time.fixedDeltaTime;
					}
					if(Input.GetKey(fmLeftKey))
					{
						flightCamera.transform.position -= flightCamera.transform.right * freeMoveSpeed * Time.fixedDeltaTime;
					}
					else if(Input.GetKey(fmRightKey))
					{
						flightCamera.transform.position += flightCamera.transform.right * freeMoveSpeed * Time.fixedDeltaTime;
					}

					//keyZoom
					if(!autoFOV)
					{
						if(Input.GetKey(fmZoomInKey))
						{
							zoomExp = Mathf.Clamp(zoomExp + (keyZoomSpeed * Time.fixedDeltaTime), 1, 8);
						}
						else if(Input.GetKey(fmZoomOutKey))
						{
							zoomExp = Mathf.Clamp(zoomExp - (keyZoomSpeed * Time.fixedDeltaTime), 1, 8);
						}
					}
					else
					{
						if(Input.GetKey(fmZoomInKey))
						{
							autoZoomMargin = Mathf.Clamp(autoZoomMargin + (keyZoomSpeed  * 10 * Time.fixedDeltaTime), 0, 50);
						}
						else if(Input.GetKey(fmZoomOutKey))
						{
							autoZoomMargin = Mathf.Clamp(autoZoomMargin - (keyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50);
						}
					}
				}

				if(Input.GetKey(KeyCode.Mouse1) && Input.GetKey(KeyCode.Mouse2))
				{
					flightCamera.transform.rotation = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * -1.7f, flightCamera.transform.forward) * flightCamera.transform.rotation;
				}
				else
				{
					if(Input.GetKey(KeyCode.Mouse1))
					{
						flightCamera.transform.rotation *= Quaternion.AngleAxis(Input.GetAxis("Mouse X") * 1.7f/(zoomExp*zoomExp), Vector3.up); //*(Mathf.Abs(Mouse.delta.x)/7)
						flightCamera.transform.rotation *= Quaternion.AngleAxis(-Input.GetAxis("Mouse Y") * 1.7f/(zoomExp*zoomExp), Vector3.right);
						flightCamera.transform.rotation = Quaternion.LookRotation(flightCamera.transform.forward, flightCamera.transform.up);
					}
					if(Input.GetKey(KeyCode.Mouse2))
					{
						flightCamera.transform.position += flightCamera.transform.right * Input.GetAxis("Mouse X") * 2;
						flightCamera.transform.position += forwardLevelAxis * Input.GetAxis("Mouse Y") * 2;
					}
				}
				flightCamera.transform.position += flightCamera.transform.up * 10 * Input.GetAxis("Mouse ScrollWheel");

			}

			//zoom
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
		
	}


	
	
	
	public enum ReferenceModes {InitialVelocity, Surface, Orbit}
	
	public enum ToolModes {StationaryCamera, DogfightCamera, Pathing};
}

