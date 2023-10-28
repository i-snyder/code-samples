/*
	Code for a physics-based flight sim shooter by Ian Snyder
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

public class NPC : TargetableEntity {
	public bool fighter = false;
    public bool performObstacleAvoidance = false;   // Should we perform obstacle avoidance? Only use sparingly!! CPU-intensive
    public bool isBoss = false;                     // Is this a boss? Using this to automatically get the bounty when defeated
    public GameObject bossInfoPoint;                // If this is a boss, I'm spawning in a LorePoint to let the player know!
    public bool spawnedBossInfoPoint = false;
    public NewPilotSO bossPilotSO;
    public NewShipSO bossShipSO;
	public float flightSpeed = 10f;

	public float turnSpeed = 1f;

    #region Firing and attack rates
    public float fireRate = .1f;
    float fireRateCounter;
    float bulletSpeed = 1000f;
    public int ammoCount = 1000;

	public float shootRange = 40f;		// Must be within range before shooting (checked with sqrRoot so may be weird!)

    public ObscuredInt numDebris = 10;
    #endregion

    float randSeed;

    public float shotDirRandAmt = 5f;   // Max amount to rotate out of forward for bullet direction
    GameObject bulletTransformHack;     // This thing lets me rotate Quaternions lazily for randomizing shot direction!

	public GameObject target;
	public GameObject myHead;
	Quaternion headStartRot;
	public bool targetIsEscapeTarget = false;
	public AIState aiState = AIState.FlyAtTarget;
	public AIState prevAIState;
    public float breakawayLength = 5f;  // How long should we break away?
    float currBreakawayTimer;
    public float breakawayDelay = 1f;   // Delay before breaking away when we get too close
    float currBreakAwayDelay;
    public float distToBreakAway = 50f; // When we get below this distance, switch to random flight
    public float obstacleAvoidanceCheckDistance = 10f;  // Obstacle avoidance distance check -- how close something has to be before I start to avoid it

    public float turnOnColliderDelay = 2f;
    private float currTurnOnColliderDelay;
    private bool colliderOff = true;

    private bool segmentedShipHasExploded = false;

    // Fly randomly if we get hit while we're breaking away
    public float flyRandomlyLength = 2f;
    float currRandomTimer;

    public GameObject[] myTurrets;

	public AudioClip explodeSound;
	// Use this for initialization
	void OnEnable() {
        // Sometimes having weird spinning on ships spawning from the pool, think it could be angular velocity if it was killed by a crash
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        hp = maxHP;
        alive = true;
		randSeed = Random.Range (-9999, 9999);
        fireRateCounter = 0;

        if (fighter) {
            colliderOff = true;
            currTurnOnColliderDelay = turnOnColliderDelay;
            FindNewTarget();
        }
        else
        {
            foreach(GameObject t in myTurrets)
            {
                t.SetActive(true);
            }
        }

        if(isSegmentedShip)
        {
            // Undo death stuff for segmented ships
            rb.useGravity = false;

            // Enable colliders on all segments
            Collider[] childColliders = GetComponentsInChildren<Collider>();

            if (childColliders.Length > 0)
            {
                foreach (Collider c in childColliders)
                {
                    // Used to be destroyed, now I set to IsTrigger so they can be set back correctly when respawned
                    c.isTrigger = false;
                }
            }
        }

        // Reset wing tip trail renderers so we don't get "streaks" from moving to the new position
        if( wingTrails.Length > 0)
        {
            foreach(TrailRenderer tr in wingTrails)
            {
                tr.Clear();
            }
        }

        currBreakawayTimer = breakawayLength;
        currBreakAwayDelay = breakawayDelay;
        currRandomTimer = flyRandomlyLength;

        bulletTransformHack = new GameObject();
        bulletTransformHack.name = "Bullet Transform Hack (TM)";
        bulletTransformHack.transform.parent = transform;

		if (targetIsEscapeTarget && SFGameManager.instance.players[0]) {
			target = SFGameManager.instance.players [0].gameObject;
		}

		if( myHead)	headStartRot = myHead.transform.localRotation;
	}

	void FindNewTarget(){
		GameObject[] tempTargets = GameObject.FindGameObjectsWithTag("TargetableEntity");
		List<GameObject> targetsToChooseFrom = new List<GameObject> ();
		foreach (GameObject t in tempTargets)
		{
			TargetableEntity te = t.GetComponentInParent<TargetableEntity> ();
			if (te.faction != faction && te.type != EnemyType.Type.Turret && te.type != EnemyType.Type.SpawnerShipII) {
				targetsToChooseFrom.Add (t);
			}
		}

		if (targetsToChooseFrom.Count > 0) {
			target = targetsToChooseFrom [Random.Range (0, targetsToChooseFrom.Count)];
		}

	}

    void FixedUpdate()
    {
        if (!SFGameManager.instance.paused)
        {
            Transform playerTransform = SFGameManager.instance.playerRB.transform;
            Vector3 rbPos = rb.position;
            Quaternion rbRot = rb.rotation;
            Vector3 rbForward = rb.transform.forward;

            if (isBoss && !spawnedBossInfoPoint)
            {
                Instantiate(bossInfoPoint, SFGameManager.instance.playerRB.transform.position, Quaternion.identity);
                spawnedBossInfoPoint = true;
                SFGameManager.instance.bossapproachSFXAudioSource.Play();
            }

            // Segmented ships destroy after falling through ground
            if (!alive && isSegmentedShip && !segmentedShipHasExploded)
            {
                if (rbPos.y < 10f)
                {
                    segmentedShipHasExploded = true;
                    // Explode each segment
                    SegmentedShipJointFixer[] joints = GetComponentsInChildren<SegmentedShipJointFixer>();

                    if (joints.Length > 0)
                    {
                        foreach (SegmentedShipJointFixer j in joints)
                        {
                            GameObject explosionTemp;
                            if (SFGameManager.instance.vfxShipExplosionPool.TryGetNextObject(j.transform.position, Quaternion.identity, out explosionTemp))
                            {
                                //Do stuff with outObject
                                AudioSource explosionAS = explosionTemp.GetComponent<AudioSource>();
                                explosionAS.clip = explodeSound;

                                explosionAS.Play();
                            }
                        }
                    }

                    gameObject.SetActive(false);

                }
            }

            if (fighter && alive)
            {
                fireRateCounter -= Time.deltaTime;

                if (fireRateCounter <= 0f)
                {    // If we can shoot, check if we can see the target before shooting
                     // First, check if we're far enough away to bother checking if we're behind or not (should be faster than the directional check)
                    Vector3 toTarget = playerTransform.position - rbPos;
                    // Only shoot within shooting range
                    if (toTarget.sqrMagnitude < shootRange)
                    {
                        toTarget = toTarget.normalized;

                        float cosAngle = Vector3.Dot(toTarget, rbForward);
                        float forwardAngleCheck = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;

                        if (forwardAngleCheck < 15)
                        {
                            //Debug.Log("This enemy will attempt to shoot "+ gameObject.name);
                            Shoot();
                        }
                    }

                    fireRateCounter = fireRate;
                }

                Vector3 targetPos = Vector3.zero;
                if (!target)
                {
                    FindNewTarget();
                }
                else
                {
                    targetPos = target.transform.position;
                }

                // Make bosses stay high above the ground
                if (isBoss)
                {
                    if (rbPos.y < 45f)
                    {
                        aiState = AIState.BreakAway;
                    }
                }

                #region Obstacle Avoidance
                if (performObstacleAvoidance)
                {
                    var dir = transform.forward;
                    RaycastHit obstacleHit;


                    if (Physics.Raycast(transform.position, transform.forward, out obstacleHit, obstacleAvoidanceCheckDistance, SFGameManager.instance.layermaskCollidables))
                    {
                        aiState = AIState.AvoidObstacle;

                        dir += obstacleHit.normal * 20f;

                        //Debug.DrawRay(obstacleHit.point, dir, Color.red, .5f);
                    }
                    else
                    {
                        if (aiState == AIState.AvoidObstacle)
                        {
                            aiState = AIState.FlyAtTarget;
                        }
                    }


                    if (aiState == AIState.AvoidObstacle)   // AVOID OBSTACLES takes precedence over any other state
                    {
                        float distToTarget = Vector3.Distance(transform.position, target.transform.position);

                        Quaternion qTo = Quaternion.LookRotation(dir);
                        qTo = Quaternion.Slerp(transform.rotation, qTo, turnSpeed * Time.deltaTime);
                        rb.MoveRotation(qTo);

                        rb.AddForce(transform.forward * flightSpeed);


                    }
                }
                #endregion

                if (aiState == AIState.FlyAtTarget && target)
                {
                    float distToTarget = Vector3.Distance(rbPos, targetPos);

                    Quaternion qTo = Quaternion.LookRotation((targetPos + target.GetComponent<TargetableEntity>().rb.velocity) - transform.position);
                    qTo = Quaternion.Slerp(rbRot, qTo, turnSpeed * Time.deltaTime);
                    rb.MoveRotation(qTo);

                    rb.AddForce(rbForward * flightSpeed);

                    if (distToTarget < distToBreakAway)
                    {
                        aiState = AIState.FlyBy;
                    }
                }

                if (aiState == AIState.FlyBy && target)
                {
                    Quaternion qTo = Quaternion.LookRotation((targetPos - (target.GetComponent<TargetableEntity>().rb.velocity - (target.transform.forward * 10f))) - rbPos);
                    qTo = Quaternion.Slerp(rbRot, qTo, turnSpeed * Time.deltaTime);
                    rb.MoveRotation(qTo);

                    rb.AddForce(rbForward * flightSpeed);

                    currBreakAwayDelay -= Time.deltaTime;

                    if (currBreakAwayDelay <= 0f)
                    {
                        aiState = AIState.BreakAway;
                        currBreakAwayDelay = breakawayDelay;
                    }
                }

                if (aiState == AIState.BreakAway && target)
                {
                    currBreakawayTimer -= Time.deltaTime;
                    Vector3 aboveTargetPos = rbPos - targetPos;
                    aboveTargetPos.y = Mathf.Abs(aboveTargetPos.y+40f);
                    Quaternion qTo = Quaternion.LookRotation(aboveTargetPos);
                    qTo = Quaternion.Slerp(rbRot, qTo, turnSpeed * Time.deltaTime);
                    rb.MoveRotation(qTo);

                    rb.AddForce(rbForward * flightSpeed);

                    // Add some randomness when we're flying away...
                    #region Random Fight pattern
                    float rollInput = Mathf.Sin(Time.time + randSeed);
                    float yawInput = Mathf.Cos(Time.time + randSeed);
                    float pitchInput = Mathf.Abs(Mathf.Sin(Time.time + randSeed));
                    rb.AddTorque(rbForward * rollInput * turnSpeed);
                    rb.AddTorque(rb.transform.up * yawInput * turnSpeed);
                    rb.AddTorque(rb.transform.right * pitchInput * turnSpeed);
                    #endregion

                    if (currBreakawayTimer <= 0f)
                    {
                        aiState = AIState.FlyAtTarget;
                        currBreakawayTimer = breakawayLength;
                    }
                }

                if (aiState == AIState.FlyRandomly)
                {
                    currRandomTimer -= Time.deltaTime;

                    float rollInput = 30f * Mathf.Sin(Time.time + randSeed);
                    float yawInput = 30f * Mathf.Abs(Mathf.Cos(Time.time + randSeed));
                    float pitchInput = 30f * Mathf.Abs(Mathf.Sin(Time.time + randSeed));
                    rb.AddForce(rbForward * flightSpeed);
                    rb.AddTorque(rbForward * rollInput * turnSpeed);
                    rb.AddTorque(rb.transform.up * yawInput * turnSpeed);
                    rb.AddTorque(rb.transform.right * pitchInput * turnSpeed);

                    if (currRandomTimer <= 0f)
                    {
                        aiState = prevAIState;
                        currRandomTimer = flyRandomlyLength;
                    }
                }

            }


            // For Large ships
            if (!fighter && alive)
            {
                if (aiState == AIState.FlyAtTarget && target)
                {
                    Vector3 keepElevationPos = target.transform.position;
                    if (!isSegmentedShip)
                    {
                        keepElevationPos.y = rbPos.y;
                    }
                    else
                    {
                        //push segment ships up from ground
                        if(rbPos.y < 15)
                        {
                            keepElevationPos.y = 50;
                        }
                    }
                    Quaternion qTo = Quaternion.LookRotation(keepElevationPos - rbPos);
                    qTo = Quaternion.Slerp(rbRot, qTo, turnSpeed * Time.deltaTime);
                    rb.MoveRotation(qTo);
                    rb.AddForce(rbForward * flightSpeed);
                }
            }
        }
    }

    public override void FlyInFormation( GameObject formationPos ){
		// If we have a valid position, we're flying in formation, otherwise we're being set back to free fly
		if (formationPos == null) {
			target = null;
			aiState = AIState.FlyAtTarget;	// This should return us to normal behavior
		} else {
			aiState = AIState.FlyInFormation;
			target = formationPos;
		}
	}

	protected override void TakeDamageCustom( float amt)
    {
		if (alive) {
			// If we take damage while we're trying to break away or flying at the target, fly randomly to shake them off?
			if (aiState == AIState.BreakAway || aiState == AIState.FlyAtTarget )
			{
				if (fighter) {
					prevAIState = aiState;
					aiState = AIState.FlyRandomly;
					currRandomTimer = flyRandomlyLength;
				}
			}
		}
	}

	protected override void DieCustom(string dieFromWhat = "a bullet", EnemyType.Type fromType = EnemyType.Type.Turret)
    { 
        GameObject explosionTemp;
        if (fighter)
        {
            if (SFGameManager.instance.vfxShipExplosionPool.TryGetNextObject(rb.position, Quaternion.identity, out explosionTemp))
            {
                //Do stuff with outObject
                AudioSource explosionAS = explosionTemp.GetComponent<AudioSource>();
                explosionAS.clip = explodeSound;
                explosionAS.Play();
            }
        }
        else
        {
            if (SFGameManager.instance.vfxBigShipExplosionPool.TryGetNextObject(rb.position, Quaternion.identity, out explosionTemp))
            {
                //Do stuff with outObject
                AudioSource explosionAS = explosionTemp.GetComponent<AudioSource>();
                explosionAS.clip = explodeSound;
                explosionAS.Play();
            }
        }

        if (faction == Faction.Empire) {
            SFGameManager.instance.RemoveEnemy(this);

            #region Drop Item
            float randChance = Random.Range(0f, 1f);
            if(randChance > .9f)
            {
                Instantiate(Resources.Load("Items/ITEM_healing_low"), transform.position, transform.rotation);
            }

            // DROP RANDOM ITEMS
            /*float randChance = Random.Range(0f, 1f);
			if (randChance > -1f) {
				float randItem = Random.Range (0f, 1f);
				if (randItem < .15f) {
					Instantiate (Resources.Load ("Items/ITEM_wep_peach"), transform.position, transform.rotation);
				}
				if (randItem >= .15f && randItem < .30f) {
					Instantiate (Resources.Load ("Items/ITEM_wep_scatter"), transform.position, transform.rotation);
				}
				if (randItem >= .3f && randItem < .5f) {
					Instantiate (Resources.Load ("Items/ITEM_acc_scarf_strength"), transform.position, transform.rotation);
				}
				if (randItem >= .5f && randItem < .7f) {
					Instantiate (Resources.Load ("Items/ITEM_acc_scarf_defense"), transform.position, transform.rotation);
				}
				if (randItem >= .7f && randItem < .8f) {
					Instantiate (Resources.Load ("Items/ITEM_acc_scarf_reflex"), transform.position, transform.rotation);
				}
				if (randItem >= .8f ) {
					Instantiate (Resources.Load ("Items/ITEM_healing_low"), transform.position, transform.rotation);
				}
			}*/
            #endregion
        }
        if (faction == Faction.Player) {
			SFGameManager.instance.allies.Remove(gameObject);
		}


        // COINS | Spawn coins if not colliding with terrain
        if (fromType != EnemyType.Type.Terrain && !isBoss) SFGameManager.instance.SpawnCoins(numDebris, rb.position);

        // Automatically give the coins to the player if it's a boss and we shot them down
        if (fromType == EnemyType.Type.PLAYER && isBoss)
        {
            // In arcade mode for bosses
            if (DDOL_MAIN.instance.bountyMode)
            {
                SFGameManager.instance.SpawnCoins(numDebris, rb.position);
                SFGameManager.instance.DefeatedBoss("SHOT DOWN " + bossPilotSO.pilotName + "!!");
            }
            //in story mode for bosses
            if (DDOL_MAIN.instance.storyMode)
            {
                SFGameManager.instance.players[0].gold += numDebris*10;
                SFGameManager.instance.UpdateGoldTexts();
                SFGameManager.instance.shotDownBounty = true;

                // Only display confirmation if it's not the last enemy
                if (SFGameManager.instance.enemies.Count > 0)
                {
                    SFGameManager.instance.DefeatedBoss("SHOT DOWN " + bossPilotSO.pilotName + "!! Picked up their bounty of $" + (numDebris*10).ToString() + "!!");
                }
            }

            DDOL_MAIN.instance.skycadiaSaveData.SetKey(bossPilotSO.internalPilotReference + "_UNLOCKED", "true");
            DDOL_MAIN.instance.skycadiaSaveData.SetKey(bossShipSO.internalShipReference + "_UNLOCKED", "true");
        }


        if (!isSegmentedShip)
        {
            gameObject.SetActive(false);
        }
        else
        {
            // Make segmented ships drift to their oblivion
            rb.useGravity = true;

            // Disable colliders on all segments
            Collider[] childColliders = GetComponentsInChildren<Collider>();

            if (childColliders.Length > 0)
            {
                foreach (Collider c in childColliders)
                {
                    // Used to be destroyed, now I set to IsTrigger so they can be set back correctly when respawned
                    c.isTrigger = true;
                }
            }
        }
    }

    public void Shoot(){
        if ( fireRateCounter <= 0f) {
            bulletTransformHack.transform.rotation = rb.rotation;
            bulletTransformHack.transform.Rotate(new Vector3( Random.Range(-shotDirRandAmt, shotDirRandAmt), 
                Random.Range(-shotDirRandAmt, shotDirRandAmt),
                Random.Range(-shotDirRandAmt, shotDirRandAmt)));

            MuzzleFlash();

			GameObject tempBullet;
			if(SFGameManager.instance.bulletPool.TryGetNextObject(rb.position + rb.transform.forward*4,  bulletTransformHack.transform.rotation, out tempBullet))
			{
                //Do stuff with outObject
                tempBullet.GetComponent<Rigidbody> ().AddForce (tempBullet.transform.forward * bulletSpeed);
				Bullet tempBulletBullet = tempBullet.GetComponent<Bullet> ();
				tempBulletBullet.myFaction = faction;
                tempBulletBullet.ownerEnemyType = type;
                tempBulletBullet.snarkString = snarkString;
			}

            fireRateCounter = fireRate;
            // Infinite ammo!
            //ammoCount--;
        }
    }
}
