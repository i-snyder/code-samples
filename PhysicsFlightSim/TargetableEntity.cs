/*
	Code for a physics-based flight sim shooter by Ian Snyder
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using VSX.Vehicles;

// Attach this to anything that's targetable
public class TargetableEntity : MonoBehaviour {
	public bool isPlayer = false;				// Assume it's not the player
    public bool isSegmentedShip = false;        // Special case stuff for segmented ships
	public Faction faction = Faction.Empire;	// Assume it's an enemy until proven quilty
	public bool flashWhenDamaged = true;		// Flash when damaged!
	private DamageFlash damageFlash = null;

	public EnemyType.Type type;

    #region Stats
    public ObscuredFloat hp = 100f;
    public ObscuredFloat dmgCausedByCollision = 10f;    // How much damage this entity causes when it's run into
    public ObscuredInt gold = 0;
    [HideInInspector]
    public ObscuredFloat maxHP;
	public bool alive = true;
	public bool escaped = false;
    #endregion

    #region What to say when I defeat the player
    public string snarkString = "REPLACE!";
    public Sprite portrait;
    #endregion

    #region Bounty Book stuff
    public string enemyTypeDisplayName = "REPLACE DISPLAY NAME";
    public string enemyBountyBookDescription = "DESCRIPTION BAD";
    #endregion

    public ParticleSystem muzzleFlashPS;
    ParticleSystem[] muzzleFlashPSChildren;

    public TrailRenderer[] wingTrails;

    public Rigidbody rb;

    public DemoTrackable myDemoTrackable;

	void Awake(){
		gameObject.tag = "TargetableEntity";

        if(!isSegmentedShip)
        {
            Rigidbody hasRigidbody = GetComponent<Rigidbody>();
            if (hasRigidbody)
            {
                rb = hasRigidbody;
            }
            else
            {
                rb = GetComponentInParent<Rigidbody>();
            }

            myDemoTrackable = GetComponent<DemoTrackable>();
        }
        else
        {
            myDemoTrackable = GetComponentInChildren<DemoTrackable>();
        }


        if (!isPlayer) maxHP = hp;

        if (flashWhenDamaged) {
			// Flash if we have the script on us
			if (!damageFlash) {
				damageFlash = gameObject.AddComponent<DamageFlash> ();
			}
		}

        if( muzzleFlashPS)
        {
            muzzleFlashPSChildren = muzzleFlashPS.GetComponentsInChildren<ParticleSystem>();
        }
    }

    void Start(){
		StartCustom ();
	}

	// Override this for specific Start stuff (for enemies, allies, turrets, etc)
	protected virtual void StartCustom(){

	}
    #region Health Management
    public void TakeDamage( float amt, string dmgFromWhat = "a bullet", EnemyType.Type fromType = EnemyType.Type.Turret){
		if (alive) {
			amt = ModDamage (amt);

			hp -= amt;


			if (flashWhenDamaged) {
				damageFlash.Flash ();
			}

			if (hp <= 0f && alive) {
				Die (dmgFromWhat, fromType);
			}
		}
			
		TakeDamageCustom (amt);
	}

	public void Heal( float amt){
		if (alive) {
			hp += amt;

			if (hp > maxHP) {
				hp = maxHP;
			}
		}

		HealCustom (amt);
	}
    #endregion

    #region Overrides
    // Override this to mod damage
    protected virtual float ModDamage(float amt){
		float moddedDmg = amt;
		return moddedDmg;
	}

	// Override this for specific damage stuff (for enemies, allies, turrets, etc)
	protected virtual void TakeDamageCustom(float amt) {}

	// Override this for specific healing stuff
	protected virtual void HealCustom(float amt){}

	// Override this for specific formation stuff (for enemies, allies, etc)
	public virtual void FlyInFormation( GameObject formationPos ){}
    #endregion

    public void Escape(){
		if (!escaped) {
			escaped = true;

			TargetableEntity[] childTEs = GetComponentsInChildren<TargetableEntity> ();

			if (childTEs.Length > 0) {
				foreach( TargetableEntity cTE in childTEs){
					cTE.Escape ();
				}
			}

			SFGameManager.instance.enemies.Remove(gameObject);
			EscapeCustom ();

			Destroy (gameObject);
		}
	}

	protected virtual void EscapeCustom(){}

    #region Death
    public void Die( string dieFromWhat="a bullet", EnemyType.Type fromType = EnemyType.Type.Turret ){
		if (alive) {
			alive = false;

            // Unregister from radar system
            myDemoTrackable.TurnOffRadarTracking();

            // For removing damage flame children
            foreach (Transform c in transform.root)
            {
                if ( c.CompareTag("Pooled") )
                {
                    c.GetComponent<ParticleSystem>().Stop();
                }
            }

            if ( deathRBExplosion)	RigidbodyExplosion ();

			TargetableEntity[] childTEs = GetComponentsInChildren<TargetableEntity> ();

			if (childTEs.Length > 0) {
				foreach( TargetableEntity cTE in childTEs){
					cTE.Die ();
				}
			}

			gameObject.GetComponentInChildren<TargetableEntity> ().Die ();

			DieCustom (dieFromWhat, fromType);
            SFGameManager.instance.CheckForClear();

            #region Update Bounty Book Save Data for a defeated enemy type
            // ONLY COUNT IF PLAYER SHOT THEM
            if(DDOL_MAIN.instance.skycadiaSaveData != null && !isPlayer)
            {
                if( fromType == EnemyType.Type.PLAYER)
                {
                    // Update number defeated by player this time
                    SFGameManager.instance.numEnemiesDefeatedThisTime++;
                    DDOL_MAIN.instance.skycadiaSaveData.totalEnemiesDefeated++;

                    // If the key doesn't exist yet, create it!
                    if (!DDOL_MAIN.instance.skycadiaSaveData.HasKey(type.ToString()))
                    {
                        DDOL_MAIN.instance.skycadiaSaveData.SetKey(type.ToString(), "1");
                    }
                    else
                    {
                        // The string does exist, so we need to add to what we already have!
                        int currScore = int.Parse(DDOL_MAIN.instance.skycadiaSaveData.GetKey(type.ToString()));
                        currScore++;
                        DDOL_MAIN.instance.skycadiaSaveData.SetKey(type.ToString(), currScore.ToString());
                    }
                }
            }
            #endregion

            #region Update Bounty Book Save Data when the player is defeated
            // We don't have turrets in-game anymore, so this is the default for dying from crashing and stuff
            if (DDOL_MAIN.instance.skycadiaSaveData != null && isPlayer && fromType != EnemyType.Type.Turret)
            {
                // If the key doesn't exist yet, create it!
                if (!DDOL_MAIN.instance.skycadiaSaveData.HasKey(fromType.ToString() + "GOTME"))
                {
                    DDOL_MAIN.instance.skycadiaSaveData.SetKey(fromType.ToString() + "GOTME", "1");
                }
                else
                {
                    // The string does exist, so we need to add to what we already have!
                    int currScore = int.Parse(DDOL_MAIN.instance.skycadiaSaveData.GetKey(fromType.ToString() + "GOTME"));
                    currScore++;
                    DDOL_MAIN.instance.skycadiaSaveData.SetKey(fromType.ToString() + "GOTME", currScore.ToString());
                }
            }
            #endregion
        }
    }

	// Override this for specific death stuff (for enemies, allies, turrets, etc)
	protected virtual void DieCustom(string dieFromWhat="a bullet", EnemyType.Type fromType = EnemyType.Type.Turret){

	}

	// Death rigidbody explosion stuff
	public bool deathRBExplosion = false;
	public float deathRBExplosionRadius = 5.0F;
	public float deathRBExplosionPower = 10.0F;

	void RigidbodyExplosion()
	{
		Vector3 explosionPos = transform.position;
		Collider[] colliders = Physics.OverlapSphere(explosionPos, deathRBExplosionRadius);
		foreach (Collider hit in colliders)
		{
			Rigidbody rb = hit.GetComponent<Rigidbody>();

			if (rb != null)
				rb.AddExplosionForce(deathRBExplosionPower * Random.Range(.3f, 1f), explosionPos, deathRBExplosionRadius, 0.0F);
		}
	}
    #endregion
    public void MuzzleFlash()
    {
        if (muzzleFlashPS)
        {
            //muzzleFlashPS.Emit(1);

            foreach (ParticleSystem ps in muzzleFlashPSChildren)
            {
                ps.Play();
            }
        }
    }

	void OnCollisionEnter( Collision col){
		Rigidbody colRB = col.gameObject.GetComponent<Rigidbody> ();
		if (colRB) {
			// NOTE Collisions to any child collider just register on the parent script, so to make the child take the damage (turret instead of the larger ship), we need to grab that specific child
			TargetableEntity te = col.gameObject.GetComponentInParent<TargetableEntity> ();
			if (te) {
                if( te != this) // Prevent centipedes from killing themselves
                {
                    te.TakeDamage(dmgCausedByCollision, snarkString, type);
                }
            }
		}

        if( col.gameObject.layer == 14) // This is a "collidable" and should just kill on impact?
        {
            TakeDamage(999999f, "That's... not gonna be cheap to fix.", EnemyType.Type.Terrain);
        }
	}
}
