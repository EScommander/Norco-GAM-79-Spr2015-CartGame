﻿using UnityEngine;
using System.Collections;

public class CartController : MonoBehaviour 
{
	public static float cartGravity = 200.0f;

	public float handling = 10.0f;
	public float acceleration = 30.0f;
	public float topSpeed = 100.0f;

	public float cartStabilizationThreshold = 0.0f;
	public float cartStabilizationDampener = 0.1f;
	public float cartStabilizationUpThrust = 2.0f;
	public float cartStabilizationSusp = 1.0f;

	public float traction = 1.0f;
	public float rotationalTraction = 2.0f;

	public float suspension = 0.1f;

	public float Speedometer = 0.0f;

	public ParticleSystem driftFX;

	public GameObject Glaive;

	public ParticleSystem[] thrustFX;

	public PitchShiftTest speedSource;
	public PitchShiftTest accelSource;

	private bool hasTraction = true;
	private float forwardAcceleration = 30.0f;
	private float steerHandling = 10.0f;

	//Jer//
	public GameObject[] turnableWheels;
	private float maxTurnDeg = 75;
	private float steeringInput = 0;
	private float accelInput = 0;
	//Jer//

	public bool respawning = false;
	private GameObject lastNode;
	
	public bool drifting = false;
	float prevShot = 0.0f;
	float shotCoolDown = 0.2f;

	Vector3 cameraAttachPos = new Vector3(0,1.6f,-3.4f);
	Quaternion cameraRot = Quaternion.Euler(15.0f,0.0f,0.0f);

	Vector3 cameraFlipPos = new Vector3(0,1.6f,3.4f);
	Quaternion cameraFlipRot = Quaternion.Euler(15.0f,180.0f,0.0f);


	Camera sceneCamera;

	public GameObject cartChar = null;
	Animator charAnim;

	NetworkView myView;

	public float turnInput = 0.0f;

	public Powerup activePowerup;

	public Transform topConnectionPoint;
	public Transform leftConnectionPoint;
	public Transform rightConnectionPoint;
	public Transform backConnectionPoint;

	void Start()
	{
		sceneCamera = Camera.main;
		charAnim = cartChar.transform.GetComponent<Animator> ();

		myView = GetComponent<NetworkView> ();

		//sceneCamera.transform.parent = transform;
		//sceneCamera.transform.localPosition = cameraAttachPos;
		//sceneCamera.transform.localRotation = cameraRot;

		foreach(ParticleSystem system in this.thrustFX)
		{
			if(system != null)
				system.enableEmission = false;
		}

		GameManager.SceneInstance.activeCarts.Add (this);
	}

	public IEnumerator ResetCart(float delay)
	{
		this.respawning = true;

		yield return new WaitForSeconds (delay);

		this.respawning = false;
		GetComponent<Rigidbody>().velocity = Vector3.zero;
		this.transform.position = this.lastNode.transform.position;
		this.transform.rotation = this.lastNode.transform.rotation;
		GetComponent<Rigidbody>().velocity = Vector3.zero;
	}

	// Update is called once per frame
	void FixedUpdate () 
	{
		if (myView.isMine) 
		{
			sceneCamera.transform.position = Vector3.Lerp (sceneCamera.transform.position, transform.TransformPoint (cameraAttachPos), 0.25f);
			sceneCamera.transform.rotation = Quaternion.Slerp (sceneCamera.transform.rotation, transform.rotation * cameraRot, 0.25f);

			if (drifting) {
				if (this.driftFX != null) {
					this.driftFX.enableEmission = true;
				}
			} else {
				if (this.driftFX != null) {
					this.driftFX.enableEmission = false;
				}
			}


			if ((GetComponent<NetworkView> ().isMine && NetworkManager.gameStarted) || NetworkManager.GetInstance ().isOnline == false) {
				Debug.DrawRay (transform.position, transform.forward * 10.0f);

				if (this.GetComponent<Rigidbody> () == null)
					return;

				if (sceneCamera.transform.IsChildOf (transform)) {
					if (Input.GetButton ("CameraFlip")) {
						sceneCamera.transform.localPosition = transform.TransformPoint (cameraFlipPos);
						sceneCamera.transform.localRotation = cameraFlipRot;
					}
					//				else
					//				{
					//					sceneCamera.transform.localPosition = cameraAttachPos;
					//					sceneCamera.transform.localRotation = cameraRot;
					//				}
				}


				if (this.Glaive != null) {
					prevShot += Time.deltaTime;
					if (prevShot > shotCoolDown && Input.GetKey (KeyCode.LeftShift)) {
						GameObject glaiveObj = (GameObject)Network.Instantiate (this.Glaive, transform.position + (this.transform.forward * 5.5f), this.transform.rotation, 0);

						prevShot = 0.0f;

						/*SimpleMovement moveScript = glaiveObj.GetComponent<SimpleMovement>();

						if(moveScript != null)
						{
							moveScript.movementVector = this.transform.forward;
						}*/
					}
				}

				if (Input.GetButton ("Drift")) {
					this.GetComponent<Rigidbody> ().drag = this.traction / 1.5f;
					this.forwardAcceleration = acceleration / 2.0f;
					this.steerHandling = this.handling * 2.0f;
					drifting = true;


				} else {
					this.GetComponent<Rigidbody> ().drag = this.traction;
					this.forwardAcceleration = acceleration;
					this.steerHandling = this.handling;
					drifting = false;


				}

				this.GetComponent<Rigidbody> ().angularDrag = this.rotationalTraction;

				if (hasTraction) {
					//JER//

					steeringInput = 0;
					steeringInput = Input.GetAxis ("JoyX0");
					accelInput = Input.GetAxis ("R_Trigger");

					//JER//

					if (Input.GetKey (KeyCode.W)) {
						accelInput = 1;
						//this.rigidbody.AddForce (transform.forward * forwardAcceleration);
					}
	

					if (Input.GetKey (KeyCode.A)) {
						steeringInput = -1;
						//this.rigidbody.AddTorque(-transform.up * handling);
					}

					if (Input.GetKey (KeyCode.D)) {
						steeringInput = 1;
						//this.rigidbody.AddTorque(transform.up * handling);
					}

					if (Input.GetKey (KeyCode.S)) {
						accelInput = -1;
						steeringInput = -steeringInput;
						//this.rigidbody.AddForce (-transform.forward * forwardAcceleration);
					}

					if(accelInput > 0)
					{
						foreach(ParticleSystem system in this.thrustFX)
						{
							if(system != null)
								system.enableEmission = true;
						}
					}
					else
					{
						foreach(ParticleSystem system in this.thrustFX)
						{
							if(system != null)
								system.enableEmission = false;
						}
					}

					if(this.respawning)
					{
						accelInput = 0;
						steeringInput = 0;
					}

					if(speedSource != null)
					{
						speedSource.pitch = Mathf.Max(0.0f,Mathf.Min((this.GetComponent<Rigidbody>().velocity.magnitude)/this.topSpeed,1.0f));
					}

					if(accelSource != null)
					{
						if(accelInput > 0)
						{
							accelSource.stepUp = 0.5f;
						}
						else
						{
							accelSource.stepUp = -0.5f;
						}
					}

					//****Jer's Code**//
					this.GetComponent<Rigidbody> ().AddForce ((transform.forward * accelInput * forwardAcceleration));
					this.GetComponent<Rigidbody> ().AddTorque (transform.up * steeringInput * steerHandling * Time.deltaTime);

					//Potential 4-wheel handling, WIP
					if (!drifting) {
						this.GetComponent<Rigidbody> ().velocity = Vector3.RotateTowards (this.GetComponent<Rigidbody> ().velocity, transform.forward, this.traction * 0.3f * Time.deltaTime, 0.0f);
					}
	 	
					if (turnableWheels.Length > 0) {
						for (int i = 0; i < turnableWheels.Length; i++) {
							turnableWheels [i].transform.localEulerAngles = new Vector3 (0, steeringInput * maxTurnDeg, 0);
						}
					}
					//****Jer's Code END**//
				}

				RaycastHit hit;

				if (Physics.Raycast (new Ray (transform.position, -transform.up), out hit, 20.0f)) {
					if (hit.distance > suspension) {
						this.GetComponent<Rigidbody> ().AddForce (-transform.up * cartGravity * Mathf.Min (0.7f, (hit.distance - suspension)));
					} else if (hit.distance < suspension) {
						this.GetComponent<Rigidbody> ().AddForce (transform.up * cartGravity * Mathf.Min (1, (suspension - hit.distance)));
					}

					lastNode = TrackManager.SceneInstance.nearestNode (transform.position);
				} else {
					this.GetComponent<Rigidbody> ().AddForce (Vector3.down * cartGravity);
				}

				if (Physics.Raycast (new Ray (transform.position + transform.right, -transform.up), out hit)) {
					if (hit.distance < suspension) {
						this.GetComponent<Rigidbody> ().AddTorque (transform.forward * Mathf.Min (1, (suspension - hit.distance)));
					}
				}

				if (Physics.Raycast (new Ray (transform.position - transform.right, -transform.up), out hit)) {
					if (hit.distance < suspension) {
						this.GetComponent<Rigidbody> ().AddTorque (-transform.forward * Mathf.Min (1, (suspension - hit.distance)));
					}
				}

				if (Physics.Raycast (new Ray (transform.position + (transform.forward * 2), -transform.up), out hit)) {
					if (hit.distance < suspension) {
						this.GetComponent<Rigidbody> ().AddTorque (-transform.right * Mathf.Min (1, ((suspension - hit.distance) * this.cartStabilizationSusp)) * (cartGravity * this.cartStabilizationSusp));
					}

					if (this.GetComponent<Rigidbody> ().velocity.magnitude >= this.cartStabilizationThreshold) {
						Vector3 relativeForward = this.transform.forward + (hit.normal.normalized - this.transform.up);

						this.transform.rotation = Quaternion.LookRotation (Vector3.Lerp (this.transform.forward.normalized, relativeForward.normalized, cartStabilizationDampener), Vector3.Lerp (this.transform.up.normalized, hit.normal.normalized, cartStabilizationDampener));

						if (Vector3.Angle (this.transform.forward, relativeForward) >= 2.0f && Vector3.Angle (this.transform.forward, relativeForward) <= 10.0f) {
							this.GetComponent<Rigidbody> ().AddTorque (-transform.right * this.cartStabilizationUpThrust * (Vector3.Angle (this.transform.forward, relativeForward)));
						}
					}
				}

				if (Physics.Raycast (new Ray (transform.position - (transform.forward * 2), -transform.up), out hit)) {
					if (hit.distance < suspension) {
						this.GetComponent<Rigidbody> ().AddTorque (transform.right * Mathf.Min (1, ((suspension - hit.distance) * this.cartStabilizationSusp)) * (cartGravity * this.cartStabilizationSusp));
					}
				}

				/*if(rigidbody.velocity.x >= this.topSpeed)
				{
					rigidbody.velocity = new Vector3(this.topSpeed, rigidbody.velocity.y, rigidbody.velocity.z);
				}*/
			
				this.Speedometer = GetComponent<Rigidbody> ().velocity.magnitude;
			}
		}
	}

	void Update()
	{
		turnInput = Input.GetAxis ("Horizontal");
		if(myView.isMine)
		{
			UpdateTurnAnim(turnInput);
		}
	}

	public void UpdateTurnAnim( float turnInput)
	{
		charAnim.SetFloat ("TurnInput", turnInput);
	}

	public void Damage()
	{
		this.StartCoroutine (ResetCart(1.0f));
	}

	public void PowerupInit()
	{
		if(this.activePowerup != null)
		{
			GameObject go = (GameObject)Instantiate(this.activePowerup.gameObject, this.GetConnectionPoint(this.activePowerup.connectionType).position, this.activePowerup.gameObject.transform.rotation);

			Powerup powerup = go.GetComponent<Powerup>();

			if(powerup != null)
			{
				this.activePowerup = powerup;
			}

			go.transform.parent = this.GetConnectionPoint(this.activePowerup.connectionType);
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = Vector3.one;
		}
	}

	private Transform GetConnectionPoint(Powerup.e_connectionTypes connectionType)
	{
		switch(connectionType)
		{
			case Powerup.e_connectionTypes.TOP:
				return this.topConnectionPoint;

			case Powerup.e_connectionTypes.LEFT:
				return this.leftConnectionPoint;

			case Powerup.e_connectionTypes.RIGHT:
				return this.rightConnectionPoint;

			case Powerup.e_connectionTypes.BACK:
				return this.backConnectionPoint;

			default:
				return this.transform;
		}
	}
}
