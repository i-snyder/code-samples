/*
	Code for a physics-based flight sim shooter by Ian Snyder
*/

using System.Collections;
using System.Collections.Generic;
using Doozy;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using Rewired;
using UnityEditor;

public class SpaceFighterController : TargetableEntity {
    public int playerNumber = 0;    // Assigned when we spawn into the Battle scene
   
    ShipVisualHandler shipVisualHandler;
    
    #region demo hacks
    public Sprite[] enemyPortraitsForDefeat;
    public Sprite[] playerPortraits;
    Sprite myPortrait;

    public int buckShotCount = 1;
    public float buckShotDmg = 5f;
    public float buckShotDirRandAmt = 10f;
    public float buckShotFireRate = .3f;
    #endregion
    
    #region Thrower Attributes
    [Header("Flame Thrower")]
    public float throwerShotCount = 1;
    public float throwerShotDmg = .0001f;
    public float throwerShotDirRandAmt = 1f;
    public float throwerShotFireRate = .7f;
    #endregion

    #region hit and death sounds
    public AudioClip[] dmgSounds;
    public AudioClip deathSound;
    #endregion

    public float fireRate = .1f;
    float fireRateCounter;
    float bulletSpeed = 2000f;

    #region Speed
    public float pitchSpeed = 1f;
    public float yawSpeed = 1f;
    public float rollSpeed = 1f;
	public float strafeSpeed = 1f;
    public float accelSpeed = 1f;
    public float decelSpeed = 1f;
    #endregion
    
    //public int ammoCount = 1000;

	public ParticleSystem myEngineSmoke;

    public float shotDirRandAmt = 5f;   // Max amount to rotate out of forward for bullet direction
    GameObject bulletTransformHack;     // This thing lets me rotate Quaternions lazily for randomizing shot direction!

    AudioSource myAS;

    AudioSource playerShotSFXSource;

    // Charging weapon
    bool charging = false;
    private float chargeTime = 1f;
    float currChargeTime;
    bool fullyCharged = false;
    public AudioClip sfx_ChargeShot_Charging;
    public AudioClip sfx_ChargeShot_Loop;
    public AudioClip sfx_ChargeShot_ShootWeak;
    public AudioClip sfx_ChargeShot_ShootStrong;

    public AudioClip[] sfx_RapidFire_ShootSounds;
    public AudioClip[] sfx_Scatter_ShootSounds;



    public float minCamFOV = 40f;
    public float maxCamFOV = 80f;

    public int currWeapon = 1;

    public GameObject myVisual;
    public GameObject[] playerShipVisuals;
    private Animator barrelRollAnimator;

	public ShipType shipType = 0;
	public enum ShipType{
		ForwardPropulsion,
		Multidirectional
	}


	// Grapple chain stuff
	public GameObject grappleObject = null;
	public List<GameObject> chainLinks = new List<GameObject> ();

	public List<Item> myItems = new List<Item> ();
	// SKIP DRIVE
	public float skipDriveCharge = 0f;

    public List<Animator> engineAnimators = new List<Animator>();

    // Control stuff
    #region Controls
    private bool fireButtonPressed;
	private Vector4 moveVector;
	private Vector2 joystickMove;
	private float thrustButtonAmount;
	private bool skipDriveButtonPressed;
	private bool itemMenuButtonPressed;
	private bool itemMenuButtonDown;
	private bool itemMenuButtonUp;
	private bool toggleFormationButtonPressed;
    private bool changeCameraButtonPressed;
	private bool uiSubmitDown;
	private bool uiCancelDown;
    private bool barrelRollRightButtonPressed;
    private bool barrelRollLeftButtonPressed;
    private bool barrelRollRightButtonPressedAndHeld;
    private bool barrelRollLeftButtonPressedAndHeld;

    private bool cutEnginesButtonPressed;
    private bool cutEnginesButtonUp;
    public float cutEnginesTurnSpeedMultiplier = 2f;
    #endregion

    #region Barrel Roll stuff
    public ParticleSystem barrelRollVFX;
    private float barrelRollDelay = 1f;
    private float currBarrelRollDelay;
    public bool justBarrelRolled = false;
    #endregion

    // FORMATIONS
    public List<Formation> formations = new List<Formation>();
	public Formation currFormation = null;
    
    #region Audio 
	public AudioClip itemPickup;
    #endregion

    #region Stat Modifiers
    float defMod = 1f;
	float strMod = 1f;
	float agiMod = 1f;
    float spdMod = 1f;
    #endregion

    #region Post Processing
    public PostProcessLayer myPostProcessLayer;
    public PostProcessVolume myPostProcessVolume;
    public PostProcessProfile myPostProcessProfile;
    
    private SCPE.SpeedLines speedLines;
    #endregion

    #region Camera Stuff
    public Camera myCam;
    public float screenShakeOnDmgStrength = 3f;
    private float initialFOV;
    private float maxFOVchange = 10f;
    private bool isThirdPerson = true;

    public int currCamView = 0;
    public Transform[] cameraViewTransforms;
    #endregion

    public WaterSplashCollider myWaterSplashCollider;

    // Death controller vibration stuff
    bool deathControllerVibrationFading = false;

    // To detach wing trails when we die so they don't get turned off
    public TrailRenderer wingTrailRight;
    public TrailRenderer wingTrailLeft;

    public int boundariesImIn = 0;  // If this gets to 0 then we're out of bounds!
    public float outOfBoundsFor = 0;
    private float outOfBoundsLimit = .5f;
    public Transform lastBoundaryILeft = null;

    public ParticleSystem snowVFX;
    public ParticleSystem rainVFX;
    public ParticleSystem sandVFX;

    [SerializeField] private Transform shipVisualSocket;
    
    public List<HeartAnimation> playerUIhearts = new List<HeartAnimation>();
    private Transform heartsGroup;

    // Use this for initialization
	protected override void StartCustom () {
        DDOL_MAIN.instance.TogglePlayerInGameOrUIMaps(true);
        
        if(playerNumber > 0)
        {
            myCam.GetComponent<AudioListener>().enabled = false;
        }

        playerShotSFXSource = GameObject.Find("playerShotSFX_AudioSource").GetComponent<AudioSource>();

        fireRateCounter = 0;

        bulletTransformHack = new GameObject();
        bulletTransformHack.name = "Bullet Transform Hack (TM)";
        bulletTransformHack.transform.parent = transform;

        currChargeTime = 0f;

        barrelRollAnimator = GetComponentInChildren<Animator>();
        currBarrelRollDelay = barrelRollDelay;
		myAS = GetComponent<AudioSource> ();

		Formation[] tempFormations = GetComponentsInChildren<Formation> ();
		foreach (Formation tF in tempFormations) {
			formations.Add (tF.GetComponent<Formation>());
		}

        /*if (myItems.Count < 1) {
			GameObject rapidfireItemGO = (GameObject)Instantiate (Resources.Load ("Items/ITEM_wep_rapid"), Vector3.one*9999f, transform.rotation);
			rapidfireItemGO.GetComponent<Item> ().equipped = true;	// HACK! to have and equip rapidfire by default
			ItemPickup( rapidfireItemGO.GetComponent<Item>());
		} else {
			// SET UP ITEMS ON LOAD NEW BATTLE
			for (int i=0; i<myItems.Count;i++) {
				if (myItems[i].equipped) {
					EquipByIndex (i);
				}
			}
		}*/
		
        myPostProcessVolume = myCam.GetComponent<PostProcessVolume>();
        string pppString = "PostProcess/PostProcessProfileP"+ (playerNumber+1).ToString();
        myPostProcessProfile = (PostProcessProfile)Instantiate(Resources.Load(pppString));
        myPostProcessVolume.profile = myPostProcessProfile;
        myCam.gameObject.layer = 21 + playerNumber; // Layer 21 is player 1 (player number 0)
        myPostProcessLayer = myCam.GetComponent<PostProcessLayer>();

        int basePostProcessLayerMask = 1 << 17; // 17 is the base PPLayer
        int myCameraLayerMask = 1 << myCam.gameObject.layer;
        int finalmask = basePostProcessLayerMask | myCameraLayerMask; // Or, (1 << layer1) | (1 << layer2)
        myPostProcessLayer.volumeLayer.value = finalmask;

        speedLines = myPostProcessProfile.GetSetting<SCPE.SpeedLines>();

        initialFOV = myCam.fieldOfView;

        myWaterSplashCollider = GetComponentInChildren<WaterSplashCollider>();

        #region Pilot Effects
        int selectedPilot = DDOL_MAIN.instance.skycadiaSaveData.selectedPilot;
        if( playerNumber > 0)
        {
            selectedPilot = DDOL_MAIN.instance.mpPilotChoice[playerNumber];
        }

        UpdatePilotAndShipStats();
        SelectWeapon();

        if (playerNumber == 0) CycleView(DDOL_MAIN.instance.skycadiaSaveData.currentCameraView);
        else CycleView(0);


        #endregion

        #region Ship Effects
        int selectedShip = DDOL_MAIN.instance.skycadiaSaveData.selectedShip;
        if (playerNumber > 0)
        {
            selectedShip = DDOL_MAIN.instance.mpWeaponChoice[playerNumber];
        }

        // Get ship props.
        GameObject[] propGOs = GameObject.FindGameObjectsWithTag("Propeller");
        if(propGOs.Length > 0)
        {
            foreach(GameObject prop in propGOs)
            {
                engineAnimators.Add(prop.GetComponent<Animator>());
            }
        }
        
        #endregion
	}

    void CycleView(int camViewToSwitchTo = -1)
    {
        if ( camViewToSwitchTo == -1)
        {
            currCamView++;
            isThirdPerson = true;
            if (currCamView > cameraViewTransforms.Length-1)
            {
                isThirdPerson = false;
                currCamView = 0;
            }
            DDOL_MAIN.instance.skycadiaSaveData.currentCameraView = currCamView;
        } else
        {
            currCamView = camViewToSwitchTo;
        }

        myCam.transform.position = cameraViewTransforms[currCamView].position;
        myCam.transform.position = cameraViewTransforms[currCamView].position;
    }


    void Update(){
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Escape))
			Cursor.lockState = CursorLockMode.None;
#endif

        GetInput();

        //Debug.Log(rb.velocity.magnitude/8f);  //~ 2-8
        //if( SFGameManager.instance.enableAberrationSpeed) ca.intensity.Override(Mathf.Lerp(0.01f, .4f, rb.velocity.magnitude /8f - .24f));
        if (SFGameManager.instance.enableFOVspeed) myCam.fieldOfView = initialFOV + (rb.velocity.magnitude / 8f * maxFOVchange);
    }

	void FixedUpdate ()
    {
        CheckForOutOfBounds();
        //Debug.Log(moveVector);
		ProcessInput ();
    }

    private void GetInput() {
        // Get the input from the Rewired Player. All controllers that the Player owns will contribute, so it doesn't matter
        // whether the input is coming from a joystick, the keyboard, mouse, or a custom controller.
        #region Controller inputs
        if (!SFGameManager.instance.paused) {

			moveVector.x = Mathf.Clamp( DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetAxis("Yaw"), -1f, 1f); // get input by name or action id
			moveVector.y = Mathf.Clamp(DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetAxis("Pitch"), -1f, 1f);
			moveVector.z = DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetAxis ("Roll");

			//moveVector.w = rwPlayer.GetAxis ("Strafe");
            if(!DDOL_MAIN.instance.cruisinMode) //  Can't shoot in cruisin mode
            {
                fireButtonPressed = DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetButton("Fire");
            }
			thrustButtonAmount = DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetAxis ("Thrust");

            if (!changeCameraButtonPressed)
            {
                if(DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetButtonDown("Change Camera"))
                {
                    changeCameraButtonPressed = true;
                }
            }

            #region Barrel Roll Input Handling
            // BARREL ROLL INPUT
            if (!justBarrelRolled)
            {
                // Stationary Roll
                if (DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetButtonDoublePressDown("Roll"))
                {
                    barrelRollRightButtonPressed = true;
                }
                if (DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetNegativeButtonDoublePressDown("Roll"))
                {
                    barrelRollLeftButtonPressed = true;
                }
            }

            // Check if button up then set false
            #endregion

            cutEnginesButtonPressed = DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetButton("Cut Engines");

            if(DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetButtonUp("Cut Engines"))
            {
                cutEnginesButtonUp = true;
            }
            

            #region disabled stuff
            //skipDriveButtonPressed = rwPlayer.GetButton ("Skip Drive");
            //itemMenuButtonDown = rwPlayer.GetButtonDown ("Item Menu");
            //itemMenuButtonPressed = rwPlayer.GetButton ("Item Menu");
            //itemMenuButtonUp = rwPlayer.GetButtonUp ("Item Menu");
            //toggleFormationButtonPressed = rwPlayer.GetButtonDown ("Toggle Formation");
            #endregion


            joystickMove.x = DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetAxis ("UIHorizontal");
			joystickMove.y = DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetAxis ("UIVertical");

			uiSubmitDown = DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetButtonDown ("UISubmit");
			uiCancelDown = DDOL_MAIN.instance.rewiredPlayers[playerNumber].GetButtonDown ("UICancel");

            #endregion

        }
    }

	private void ProcessInput()
	{
		if (!SFGameManager.instance.paused) {
			if (SFGameManager.instance.atLeastOnePlayerAlive)
			{
                if (changeCameraButtonPressed)
                {
                    CycleView();
                    changeCameraButtonPressed = false;
                }

                //////// PROCESS MOVEMENT ////////////

                if (cutEnginesButtonPressed)
                {
                    rb.useGravity = true;
                    speedLines.intensity.value = 0f;

                    foreach (var wingTrails in  shipVisualHandler.wingTrails)
                    {
	                    wingTrails.emitting = false;
                    }
                    
                    foreach (ParticleSystem thrustVFX in shipVisualHandler.thrusterVFXs)
                    {
                        if (thrustVFX.isPlaying) thrustVFX.Stop();
                    }
                }

                if (cutEnginesButtonUp)
                {
                    rb.useGravity = false;
                    cutEnginesButtonUp = false;
                    foreach (var wingTrails in  shipVisualHandler.wingTrails)
                    {
	                    wingTrails.emitting = false;
                    }
                }

				// Auto-accelerate for forward propulsion ships
				if (shipType == ShipType.ForwardPropulsion && !rb.useGravity) {
                    if (thrustButtonAmount > 0)
                    {
                        // Accelerate more!
                        rb.AddForce(transform.forward * thrustButtonAmount * 4f * accelSpeed * spdMod);

                        foreach(ParticleSystem thrustVFX in shipVisualHandler.thrusterVFXs)
                        {
                            if(!thrustVFX.isPlaying) thrustVFX.Play();
                        }
                    }
                    else
                    {
                        // Auto-accelerate
                        rb.AddForce(transform.forward * 2f * accelSpeed * spdMod);
                        
                        foreach (ParticleSystem thrustVFX in shipVisualHandler.thrusterVFXs)
                        {
                            if (thrustVFX.isPlaying) thrustVFX.Stop();
                        }
                    }

                    // SPEED LINES
                    if( !cutEnginesButtonPressed) speedLines.intensity.value = Mathf.Abs(thrustButtonAmount);
				}

                #region BarrelRoll Handling
                if (barrelRollRightButtonPressed)
                {
                    //print("DO A RIGHT BARREL ROLL!!");
                    barrelRollAnimator.SetTrigger("BarrelRoll_R");
                    barrelRollRightButtonPressed = false;
                    justBarrelRolled = true;
                    if (barrelRollRightButtonPressedAndHeld) rb.AddForce(transform.right * 30000);
                    // If first person move camera to third person so you don't get sick!
                    if (currCamView == 0) CycleView(1);
                    barrelRollVFX.Play();
                }
                if (barrelRollLeftButtonPressed)
                {
                    //print("DO A LEFT BARREL ROLL!!");
                    barrelRollAnimator.SetTrigger("BarrelRoll_L");
                    barrelRollLeftButtonPressed = false;
                    justBarrelRolled = true;
                    if (barrelRollLeftButtonPressedAndHeld) rb.AddForce(-transform.right * 30000);
                    // If first person move camera to third person so you don't get sick!
                    if (currCamView == 0) CycleView(1);
                    barrelRollVFX.Play();
                }


                if (justBarrelRolled)
                {
                    currBarrelRollDelay -= Time.deltaTime;

                    if (currBarrelRollDelay < 0)
                    {
                        barrelRollVFX.Stop();
                        justBarrelRolled = false;
                        // If first person move camera back to first person so you don't get sick!
                        if (currCamView == 1 && isThirdPerson == false) CycleView(0);
                        currBarrelRollDelay = barrelRollDelay;
                    }
                }
                #endregion


                if (!itemMenuButtonPressed) {
					// Update engine animator speed
					if (engineAnimators.Count >0) {
                        float cutEngineMod = 1f;
                        if(cutEnginesButtonPressed) cutEngineMod = 0f;

                        foreach(Animator engineAnim in engineAnimators)
                        {
                            engineAnim.SetFloat("EngineSpeed", (1f + thrustButtonAmount) * cutEngineMod);
                        }
					}

                    #region Skip Driving charging
                    if (skipDriveButtonPressed) {
						skipDriveCharge += Time.deltaTime;
					}
					if (!skipDriveButtonPressed) {
						rb.AddForce(transform.forward * skipDriveCharge * 100f * accelSpeed);
						skipDriveCharge = 0f;
					}
                    #endregion
                    float cutEngineMult = 1f;
                    if (cutEnginesButtonPressed)
                    {
                        cutEngineMult = cutEnginesTurnSpeedMultiplier;
                    }
                    rb.AddTorque(transform.up * moveVector.x * yawSpeed * agiMod * cutEngineMult);
                    if(playerNumber ==0) rb.AddTorque(transform.right * moveVector.y * pitchSpeed * agiMod * DDOL_MAIN.instance.skycadiaSaveData.invertVertical * cutEngineMult);
                    else rb.AddTorque(transform.right * moveVector.y * pitchSpeed * agiMod * DDOL_MAIN.instance.mpInvertVertical[playerNumber] * cutEngineMult);
                    rb.AddTorque(transform.forward * moveVector.z * rollSpeed * agiMod * cutEngineMult);

					if (moveVector.w != 0f) {	// If we're strafing
						rb.AddForce( transform.right * (moveVector.w * strafeSpeed));
					}
                    

                    //////// END PROCESSING MOVEMENT ////////////

                    #region //////// WEAPON LOGIC //////////////
                    fireRateCounter -= Time.deltaTime;

                    #region RAPID FIRE SHOT
                    if ( currWeapon == 1 && fireButtonPressed && fireRateCounter <= 0f )
					{
                        //if (tD != null && currWeapon == 1) tD.DisableTime = 5;
                        // TODO TODO TODO HACK HACK HACK !!! Hardcoding damage amount!!! BAD BAD BAD
                        playerShotSFXSource.pitch = Random.Range(.5f, 1.5f);
                        playerShotSFXSource.clip = sfx_RapidFire_ShootSounds[Random.Range(0, sfx_RapidFire_ShootSounds.Length)];
                        playerShotSFXSource.Play();
                        Shoot(10f * strMod, 0.1f);
					}
                    #endregion

                    #region SHOTGUN
                    if (currWeapon == 2 && fireButtonPressed && fireRateCounter <= 0f )
                    {
                        playerShotSFXSource.pitch = Random.Range(.5f, 1.5f);
                        playerShotSFXSource.clip = sfx_Scatter_ShootSounds[Random.Range(0, sfx_Scatter_ShootSounds.Length)];
                        playerShotSFXSource.Play();
                        ShootScatter();
                    }
                    #endregion

                    #region CHARGE SHOT
                    if ( currWeapon == 3 )
                    {
                        // Charging for charge shot
                        if (fireButtonPressed)
                        {
                            charging = true;
                            
                            currChargeTime += Time.deltaTime;
                            SFGameManager.instance.reticles[playerNumber].transform.localScale = Vector3.one + Vector3.one * ( chargeTime - currChargeTime) * 10f;

                            if (currChargeTime >= chargeTime)
                            {
                                currChargeTime = chargeTime;
                                fullyCharged = true;
                                if( playerShotSFXSource.clip != sfx_ChargeShot_Loop)
                                {
                                    playerShotSFXSource.Stop();
                                    playerShotSFXSource.loop = true;
                                    playerShotSFXSource.clip = sfx_ChargeShot_Loop;
                                    playerShotSFXSource.Play();
                                }
                            }
                            else
                            {
                                if (playerShotSFXSource.clip != sfx_ChargeShot_Charging)
                                {
                                    playerShotSFXSource.Stop();
                                    playerShotSFXSource.clip = sfx_ChargeShot_Charging;
                                    playerShotSFXSource.Play();
                                }
                            }
                        }

                        if (charging && !fireButtonPressed)  // Shoot if we let go of charging
                        {
                            // SHOOT
                            playerShotSFXSource.Stop();
                            playerShotSFXSource.loop = false;
                            if(fullyCharged) playerShotSFXSource.clip = sfx_ChargeShot_ShootStrong;
                            else playerShotSFXSource.clip = sfx_ChargeShot_ShootWeak;

                            playerShotSFXSource.Play();
                            Shoot((currChargeTime / chargeTime) * 240f * strMod, 1f - (currChargeTime / chargeTime), .25f + currChargeTime*3f, fullyCharged);

                            charging = false;
                            currChargeTime = 0f;
                        }
                    }
                    #endregion

                    #region MISSILE BARRAGE
                    if (currWeapon == 5 && fireButtonPressed && fireRateCounter <= -.5f)
                    {
                        Shoot(20f * strMod, 1, 1, false, true);
                    }
                    #endregion

                    #region FLAMETHROWER

                    if (currWeapon == 6 && fireButtonPressed && fireRateCounter <= .7f)
                    {
                        ShootThrower();
                    }

                    #endregion

                    #endregion /// END WEAPON LOGIC ///
                }
            }
            if ( !alive && !SFGameManager.instance.atLeastOnePlayerAlive)     // When dead, go into a freespin
			{
				rb.AddTorque(transform.up * .13f * yawSpeed);
				rb.AddTorque(transform.right * .1f * pitchSpeed);
				rb.AddTorque(transform.forward * .17f * rollSpeed);

				rb.AddForce(transform.forward * .12f * accelSpeed);
			}
		}
	}

    void CheckForOutOfBounds()
    {
        if (alive && !SFGameManager.instance.paused && boundariesImIn < 1)
        {
            outOfBoundsFor += Time.deltaTime;
            SFGameManager.instance.malfunctionUIs[playerNumber].TriggerBoundary();

            if (outOfBoundsFor > outOfBoundsLimit)
            {
                // Fly towards the center of the last boundary we left
                Quaternion qTo = Quaternion.LookRotation((lastBoundaryILeft.position + Vector3.up * 20f) - transform.position);
                qTo = Quaternion.Slerp(rb.rotation, qTo, .05f);
                rb.MoveRotation(qTo);
                moveVector = Vector3.zero;
            }
        }
    }

	protected override float ModDamage(float amt){
		float moddedDmg = amt * defMod;
		return moddedDmg;
	}

	protected override void TakeDamageCustom( float amt)
    {
        int currHearts = (int)((hp / maxHP) * playerUIhearts.Count);

        for (int i=0;i< playerUIhearts.Count;i++)
        {
            if (currHearts <= i)
            {
                playerUIhearts[i].LoseHeart();
            }
        }

        if(alive && !SFGameManager.instance.enemiesCleared) // Only take damage if we haven't cleared the level
        {
            myAS.clip = dmgSounds[Random.Range(0, dmgSounds.Length)];
            myAS.Play();

            iTween.ShakePosition(myCam.gameObject, iTween.Hash("amount", Vector3.one * screenShakeOnDmgStrength * DDOL_MAIN.instance.skycadiaSaveData.CameraShake, "time", .5f, "islocal", true, "ignoretimescale", true));
            // Set vibration for a certain duration
            float randShakeDuration = Random.Range(.1f, .4f);
            float shakeStrengthFromCurrentHealthLevel = .2f;    // .2f is a good baseline vibration strength
            shakeStrengthFromCurrentHealthLevel += 1f - (hp / maxHP);

            foreach(Joystick j in DDOL_MAIN.instance.rewiredPlayers[playerNumber].controllers.Joysticks) {
                if(!j.supportsVibration) continue;
                if (j.vibrationMotorCount > 0) {
                    for(int i=0; i<j.vibrationMotorCount; i++)
                    {
                        j.SetVibration(i, shakeStrengthFromCurrentHealthLevel, randShakeDuration);
                    }
                }
            }
        }
    }

    protected override void HealCustom( float amt){
        int currHearts = (int)((hp / maxHP) * playerUIhearts.Count);

        for (int i = 0; i < currHearts; i++)
        {
            playerUIhearts[i].GainHeart();
        }
    }

    public void OnLevelCompleted()
    {
        SFGameManager.instance.enemyDefeatImage.sprite = myPortrait;
        SFGameManager.instance.enemyDefeatImage.transform.localScale *= .5f;

        #region Leaderboard and Achievements stuff
        string textToAddForHighScore = "$" + DDOL_MAIN.instance.skycadiaSaveData.highestBounty.ToString() + " was your biggest bounty!";
        if (gold > DDOL_MAIN.instance.skycadiaSaveData.highestBounty)
        {
            DDOL_MAIN.instance.skycadiaSaveData.highestBounty = gold;
            textToAddForHighScore = "BIGGEST BOUNTY!!";
        }

        DDOL_MAIN.instance.skycadiaSaveData.totalBountyCollected += gold;

        // If we're in BOUNTY or STORY mode, increase totalBountyHunts and check for highest enemies defeated
        if (DDOL_MAIN.instance.bountyMode || DDOL_MAIN.instance.storyMode)
        {
            DDOL_MAIN.instance.skycadiaSaveData.totalBountyHunts += 1;

            if (DDOL_MAIN.instance.skycadiaSaveData.mostEnemiesDefeated < SFGameManager.instance.numEnemiesDefeatedThisTime)
            {
                DDOL_MAIN.instance.skycadiaSaveData.mostEnemiesDefeated = SFGameManager.instance.numEnemiesDefeatedThisTime;
            }
        }
        #endregion

#if UNITY_STANDALONE || UNITY_EDITOR && !UNITY_XBOXONE
        if (DDOL_Steam.instance)
        {
            DDOL_Steam.instance.HighestBountyUpdate(gold);
        }
#endif
        string snarkTextTemp = "AREA " + DDOL_MAIN.instance.selectedLevel.ToString() + " CLEARED!!!";
        if( SFGameManager.instance.shotDownBounty)
        {
            snarkTextTemp += "\nShot down the bounty! I should check the tavern...";
        }
        else
        {
            snarkTextTemp += "\nThe bounty crashed instead of me shooting them down, so I didn't get their reward!";
        }

        SFGameManager.instance.enemySnarkText.text = snarkTextTemp;

        if (DDOL_MAIN.instance.skycadiaSaveData.HasKey("LevelUnlocked"))
        {
            if (int.Parse(DDOL_MAIN.instance.skycadiaSaveData.GetKey("LevelUnlocked")) > 8 && DDOL_MAIN.instance.selectedLevel == 8)
            {
                // This was the final level!
                SFGameManager.instance.enemySnarkText.text = "WE DEFEATED THE BUG ARMY!! LET'S PARTY!!";
            }
        }
            

        SFGameManager.instance.bountyText.text = "$" + gold.ToString() + " Bounty Collected\n" + textToAddForHighScore;
        if (DDOL_MAIN.instance.cruisinMode)
        {
            SFGameManager.instance.bountyText.text = "";
        }

        SaveLoadManager.Save();
    }

	protected override void DieCustom(string dieFromWhat="a bullet", EnemyType.Type fromType = EnemyType.Type.Turret)
    {
        SFGameManager.instance.PlayerDied ();

        myAS.clip = deathSound;
        myAS.Play();

        deathControllerVibrationFading = true;

        //myEngineSmoke.Play ();
        SFGameManager.instance.vfxShipExplosionPool.TryGetNextObject(transform.position, Quaternion.identity);
        myVisual.SetActive(false);
        myWaterSplashCollider.StopParticleSystem();
        barrelRollVFX.Stop();

        // Disable all colliders
        foreach (Transform t in transform)
        {
            if( t.GetComponent<Collider>())
            {
                t.GetComponent<Collider>().enabled = false;
            }
        }

        SFGameManager.instance.whatKilledThePlayer = dieFromWhat;

        #region Leaderboard and Achievements stuff
        string textToAddForHighScore = "$"+ DDOL_MAIN.instance.skycadiaSaveData.highestBounty.ToString() + " was your biggest bounty!";
        if ( gold > DDOL_MAIN.instance.skycadiaSaveData.highestBounty)
        {
            DDOL_MAIN.instance.skycadiaSaveData.highestBounty = gold;
            textToAddForHighScore = "BIGGEST BOUNTY!!";
        }

        DDOL_MAIN.instance.skycadiaSaveData.totalBountyCollected += gold;

        // If we're in BOUNTY or STORY mode, increase totalBountyHunts and check for highest enemies defeated
        if (DDOL_MAIN.instance.bountyMode || DDOL_MAIN.instance.storyMode)
        {
            DDOL_MAIN.instance.skycadiaSaveData.totalBountyHunts += 1;

            if ( DDOL_MAIN.instance.skycadiaSaveData.mostEnemiesDefeated < SFGameManager.instance.numEnemiesDefeatedThisTime)
            {
                DDOL_MAIN.instance.skycadiaSaveData.mostEnemiesDefeated = SFGameManager.instance.numEnemiesDefeatedThisTime;
            }
        }
        #endregion

#if UNITY_STANDALONE || UNITY_EDITOR && !UNITY_XBOXONE
        if (DDOL_Steam.instance)
        {
            DDOL_Steam.instance.HighestBountyUpdate(gold);
        }
#endif

        SFGameManager.instance.enemySnarkText.text = dieFromWhat;
        SFGameManager.instance.bountyText.text = "$" + gold.ToString() + " Bounty Collected\n" + textToAddForHighScore;
        if (DDOL_MAIN.instance.cruisinMode)
        {
            SFGameManager.instance.bountyText.text = "";
        }

        if ( fromType != EnemyType.Type.Turret)
        {
            if(fromType == EnemyType.Type.Terrain)
            {
                SFGameManager.instance.enemyDefeatImage.sprite = myPortrait;
            }

            foreach( TargetableEntity te in SFGameManager.instance.settingsMenuManager.bountyBookPages)
            {
                if( te.type == fromType)
                {
                    SFGameManager.instance.enemyDefeatImage.sprite = te.portrait;
                    break;
                }
            }
        }
        else
        {
            SFGameManager.instance.enemyDefeatImage.color = Color.clear;
        }

        foreach (var wingTrails in  shipVisualHandler.wingTrails)
        {
	        wingTrails.transform.parent = null;
        }

        gameObject.GetComponent<Collider>().enabled = false;
        SaveLoadManager.Save();
    }

    public void Shoot(float dmgAmt, float accuracy = 1f, float bulletScale = 1f, bool fullyChargedShot = false, bool isMissile = false, bool isThrower = false){  // Accuracy is from 0-1 of the random amount, so 0 is perfect aim
        bulletTransformHack.transform.rotation = transform.rotation;
		bulletTransformHack.transform.Rotate(new Vector3( Random.Range(-shotDirRandAmt * accuracy, shotDirRandAmt * accuracy), 
			Random.Range(-shotDirRandAmt * accuracy, shotDirRandAmt * accuracy),
			Random.Range(-shotDirRandAmt * accuracy, shotDirRandAmt * accuracy)));

        MuzzleFlash();

		GameObject tempBullet;
		if(SFGameManager.instance.bulletPool.TryGetNextObject(transform.position + transform.forward * 2f,  bulletTransformHack.transform.rotation, out tempBullet))
		{
			//Do stuff with outObject
			Bullet tempBulletBullet = tempBullet.GetComponent<Bullet> ();
            tempBulletBullet.myRB.AddForce(tempBullet.transform.forward * bulletSpeed);
            tempBulletBullet.dmgAmt = dmgAmt;
			tempBulletBullet.myFaction = faction;
            tempBulletBullet.ownerEnemyType = type;
            tempBulletBullet.fullyChargedShot = fullyChargedShot;
            tempBulletBullet.isMissile = isMissile;
            tempBulletBullet.isThrower = isThrower;
        }

        fireRateCounter = fireRate;
        //ammoCount--;
        //gold--;
        //SFGameManager.instance.playerGoldText.text = "$" + SFGameManager.instance.p1TE.gold.ToString();
        //SFGameManager.instance.UpdateAmmoText(ammoCount, rwPlayerId);
        if (SFGameManager.instance.reticles[playerNumber].transform.localScale.x > 1f)
            SFGameManager.instance.reticles[playerNumber].transform.localScale = Vector3.one;

        fullyCharged = false;
    }

    public void ShootScatter(){
        //if (ammoCount < buckshotCount) buckshotCount = ammoCount;

        for (int i = 0; i < buckShotCount; i++)
        {
            bulletTransformHack.transform.rotation = transform.rotation;
            bulletTransformHack.transform.Rotate(new Vector3( 
                Random.Range(-buckShotDirRandAmt, buckShotDirRandAmt), 
                Random.Range(-buckShotDirRandAmt, buckShotDirRandAmt),
                Random.Range(-buckShotDirRandAmt, buckShotDirRandAmt)));

            GameObject tempBullet;
            if (SFGameManager.instance.bulletPool.TryGetNextObject(transform.position + transform.forward * 2f, bulletTransformHack.transform.rotation, out tempBullet))
            {
                //Do stuff with outObject
                Bullet tempBulletBullet = tempBullet.GetComponent<Bullet>();
                tempBulletBullet.myRB.AddForce(tempBullet.transform.forward * bulletSpeed);
                tempBulletBullet.dmgAmt = buckShotDmg * strMod;
                tempBulletBullet.myFaction = faction;
                tempBulletBullet.ownerEnemyType = type;
            }
        }


        fireRateCounter = buckShotFireRate;
        //ammoCount -= 10;
		//SFGameManager.instance.UpdateAmmoText(ammoCount, rwPlayerId);
    }

    public void ShootThrower()
    {
        //if (ammoCount < buckshotCount) buckshotCount = ammoCount;

        for (int i = 0; i < throwerShotCount; i++)
        {
            bulletTransformHack.transform.rotation = transform.rotation;
            bulletTransformHack.transform.Rotate(new Vector3(
                Random.Range(-throwerShotDirRandAmt, throwerShotDirRandAmt),
                Random.Range(-throwerShotDirRandAmt, throwerShotDirRandAmt),
                Random.Range(-throwerShotDirRandAmt, throwerShotDirRandAmt)));

            GameObject tempBullet;
            if (SFGameManager.instance.bulletPool.TryGetNextObject(transform.position + transform.forward * 2f, bulletTransformHack.transform.rotation, out tempBullet))
            {
                //Do stuff with outObject
                Bullet tempBulletBullet = tempBullet.GetComponent<Bullet>();
                tempBulletBullet.myRB.AddForce(tempBullet.transform.forward * bulletSpeed);
                tempBulletBullet.dmgAmt = throwerShotDmg * strMod;
                tempBulletBullet.myFaction = faction;
                tempBulletBullet.ownerEnemyType = type;
            }
        }


        fireRateCounter = throwerShotFireRate;
        //ammoCount -= 10;
        //SFGameManager.instance.UpdateAmmoText(ammoCount, rwPlayerId);
    }

    public void SelectWeapon(){
		// 1 MG, 2 shotgun, 3 chargeshot, 4 gravity well
		
		string weaponInternalReferenceName = DDOL_MAIN.instance.skycadiaSaveData.GetKey("selectedWeapon");
		
		foreach (NewWeaponSO weaponSO in DDOL_MAIN.instance.weaponData)
		{
			if (weaponSO.internalWeaponReference == weaponInternalReferenceName)
			{
				currWeapon = weaponSO.weaponID;

                if(currWeapon == 2) SFGameManager.instance.reticles[playerNumber].GetComponent<Image>().sprite = SFGameManager.instance.reticleScatter;
                if(currWeapon == 3) SFGameManager.instance.reticles[playerNumber].GetComponent<Image>().sprite = SFGameManager.instance.reticleCharge;
                if (currWeapon == 6) SFGameManager.instance.reticles[playerNumber].GetComponent<Image>().sprite = SFGameManager.instance.reticleScatter;
            }
		}
		
		//SFGameManager.instance.UpdateWeaponText (currWeapon, playerNumber);
	}

	public void ItemPickup( Item pickedUpItem){
		bool alreadyHeld = false;

		// Don't allow pickup if we already have 8 items
		if (myItems.Count >= 8) {
			alreadyHeld = true;
		}

		// Don't give duplicates
		if (!pickedUpItem.consumable && !alreadyHeld) {
			foreach (Item i in myItems) {
				if (pickedUpItem.inGameName == i.inGameName) {
					alreadyHeld = true;
				}
			}
		}


		if (!alreadyHeld) {
			myItems.Add (pickedUpItem);
			Destroy (pickedUpItem.myGO);
			Destroy( pickedUpItem.GetComponent<SphereCollider>());
			SFGameManager.instance.p1RadialItemMenu.UpdateRadialMenu ();
		}
	}

	public void UpdatePilotAndShipStats()
    {
        string shipInternalReferenceName = DDOL_MAIN.instance.skycadiaSaveData.GetKey("selectedShip");
        string pilotInternalReferenceName = DDOL_MAIN.instance.skycadiaSaveData.GetKey("selectedPilot");
        #region Pilot and Ship Modifiers
        float tempHP=-1f;
        float tempAgi=-1f;
        float tempDef=-1f;
        float tempStr=-1f;
        float tempSpd=-1f;
        
        foreach (NewShipSO shipSO in DDOL_MAIN.instance.shipData)
        {
            if (shipSO.internalShipReference == shipInternalReferenceName)
            {
	            myVisual = (GameObject)Instantiate(Resources.Load(shipSO.shipVisualPath), shipVisualSocket.position, Quaternion.identity, shipVisualSocket);
	            
	            tempHP = shipSO.health;
                tempAgi = shipSO.agility;
                tempDef = shipSO.defense;
                tempStr = shipSO.strength;
                tempSpd = shipSO.speed;

                Transform camPos = transform.Find("CameraPos");
                Transform camViewsHolder = myVisual.transform.Find("CameraViews");

                cameraViewTransforms[0] = camViewsHolder.Find("First Person CAMERA VIEW");
                cameraViewTransforms[1] = camViewsHolder.Find("Third Person CAMERA VIEW");
                shipVisualHandler = shipVisualSocket.GetComponentInChildren<ShipVisualHandler>();
            }
        }

        foreach (NewPilotSO pilotSO in DDOL_MAIN.instance.pilotData)
        {
            if (pilotSO.internalPilotReference == pilotInternalReferenceName)
            {

	            string pilotVisualString = "VISUAL_" + pilotSO.internalPilotReference;
	            GameObject[] pilotVisuals = GameObject.FindGameObjectsWithTag("PilotVisual");

	            foreach (var pilotVisual in pilotVisuals)
	            {
		            if (pilotVisual.name != pilotVisualString)
		            {
			            pilotVisual.SetActive(false);
		            }
	            }
	            
	            //Debug.Log(pilotSO.pilot);
	            
                tempHP *= pilotSO.health;
                tempAgi *= pilotSO.agility;
                tempDef *= pilotSO.defense;
                tempStr *= pilotSO.strength;
                tempSpd *= pilotSO.speed;

                myPortrait = pilotSO.pilotProfilePicture;
                SFGameManager.instance.pilotPortraitImage.sprite = myPortrait;
                SFGameManager.instance.pilotNameText.text = pilotSO.pilotName;
            }
        }

        maxHP = tempHP;
        agiMod = tempAgi;
        defMod = tempDef;
        strMod = tempStr;
        spdMod = tempSpd;
        #endregion

        // SET hitpoints HP after setting up Pilot and Ship stuff
        hp = maxHP;

        // Set up hearts in UI
        heartsGroup = SFGameManager.instance.playerHUDs[playerNumber].transform.Find("HeartsGroup").transform;
        for(int i=0; i<maxHP/25; i++)
        {
            GameObject tempHeartUIGO = Instantiate(SFGameManager.instance.heartUIprefab, heartsGroup);
            playerUIhearts.Add(tempHeartUIGO.GetComponent<HeartAnimation>());
        }
        
        if (tempHP < 0)
        {
            Debug.LogError("tempHP didn't get set in UpdatePilotAndShipStats!!");
        }
    }

    #region OLD ITEM STUFF
    /* OLD ITEM STUFF
    public void UnequipScarf(){
		//myScarfVisual.SetActive (false);
		agiMod = 1f;
		defMod = 1f;
		strMod = 1f;
	}

	void EquipByIndex( int itemIndex){
		if (myItems.Count > itemIndex) {
			// If this is a weapon, we need to unequip all weapons, then equip it
			if (myItems [itemIndex].weapon) {
				foreach (Item i in myItems) {
					if (i.weapon) {
						i.equipped = false;
					}
				}

				myItems [itemIndex].equipped = true;

                SFGameManager.instance.UpdateWeaponText (currWeapon, playerNumber);
			} else {
				foreach (Item i in myItems) {
					if (i.accessory) {
						i.equipped = false;
					}
				}

				myItems [itemIndex].equipped = true;
				UpdatePilotAndShipStats (myItems [itemIndex].inGameName);
			}
			

			SFGameManager.instance.p1RadialItemMenu.UpdateRadialMenu ();
		}
	}

	void UseConsumableItem( int itemIndex){
		if (myItems.Count > itemIndex) {
			if (myItems [itemIndex].category == ItemCategory.ITEM_Healing) {
				AudioSource.PlayClipAtPoint (myItems[itemIndex].sfxOnUse, transform.position);
				Heal (myItems [itemIndex].damage);
				Destroy (myItems [itemIndex]);
				myItems.RemoveAt (itemIndex);
			}
		}
	}

	public void SelectItemByIndex( int itemIndex ){
		if (myItems.Count > itemIndex) {
			if (uiSubmitDown) {
				if (myItems [itemIndex].consumable) {
					UseConsumableItem (itemIndex);
				} else {
					EquipByIndex (itemIndex);
				}
			}

			if (uiCancelDown) {
				// HACK for now only drop non-weapons
				if (!myItems [itemIndex].weapon) {
					if (myItems [itemIndex].category == ItemCategory.ACC_Scarf && myItems[itemIndex].equipped) {
						UnequipScarf ();
					}

					myItems [itemIndex].equipped = false;

					GameObject droppedItem = (GameObject)Instantiate (Resources.Load ("Items/" + myItems[itemIndex].internalName), transform.position - transform.forward*4f, Quaternion.identity);
					Destroy (myItems [itemIndex]);
					myItems.RemoveAt (itemIndex);
				}
			}

			SFGameManager.instance.p1RadialItemMenu.UpdateRadialMenu ();
		}
	}
    */
    #endregion OLD ITEM STUFF
}
