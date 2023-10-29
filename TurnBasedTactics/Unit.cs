/*
	Code for a turn-based tactics game by Ian Snyder
*/

using UnityEngine;
using UnityEngine.UI;
using Random=UnityEngine.Random;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

public class Unit : MonoBehaviour {
	public bool isAI = false;
	
	public int movementPerDecisionPoint = 5;
	public int totalHP = 5;
	public int meleeSkill = 50;
	public int ballisticSkill = 50;
	public int agility = 50;

	public int xp = 0;
	public int level = 1;

	public int xpForThisBattle = 0;

	// ABILITIES
	public List<string> abilityStrings = new List<string>();
	public List<Ability> abilities = new List<Ability>();
	public List<int> abilityPoints = new List<int> ();	// How many Ability Points each ability has accumulated

	// Equipped abilities
	// 0, 1= Active	NOTE these are actually Job Ability Categories for the Active slot, which contains mutiple Abilities
	// 2 = Reactive
	// 3 = Passive
	public List<string> equippedAbilityStrings = new List<string>();
	public List<Ability> equippedAbilities = new List<Ability> ();
	public List<AbilityCategory> equippedAbilCats = new List<AbilityCategory> ();

	// Equipped Equipment!
	public List<string> equipmentStrings = new List<string>();
	public List<GameObject> equipmentGOs = new List<GameObject> ();

	public GameObject shield;

	public float jumpHeight = 2;

	public string myClanName;
	public Clan myClan;

	public List<string> jobsLearned = new List<string> (); // Each job that this unit knows
	public string currJobString;
	public Job currJob;
	public GameObject currJobGO;

	public string visualPrefabString;
	private GameObject visualPrefab;
	public Sprite portrait;

	// Not to be modified in the editor!!

	public int DONT_MODIFY_BELOW_THIS;
	public Weapon equippedWeapon;
	public Weapon setAsideWeapon = null;	// When we attack with an ability "weapon", set aside the currently equipped weapon here, then set it back once the ability attack is over

	public bool shieldActive = false;

	public List<Weapon> weapons = new List<Weapon> ();

	public GameObject aoeTrigger;

	public List<string> itemStrings = new List<string>();
	public List<Item> items = new List<Item>();
	public Item currItem = null;

    // COUNTERING
    public bool startedCounter = false;

	public bool mainPhase = false;
	public bool selectingWeapon = false;
	private bool selectingMagic = false;
	public bool selectingItems = false;
	public bool selectingFacing = false;

	public int coverModifier = 0;

	public bool pinned = false;
	public bool down = false;
	public bool outOfAction = false;

	public bool selectingTarget = false;
	public bool aoeAttack = false;
	public bool moving = false;
	public bool attacking = false;
	public bool meleeAttacking = false;
	public bool running = false;
	public bool healing = false;
	public bool recruiting = false;	// Are we attempting to recruit an enemy unit?
	public float maxMeleeElevationDifference = 1f;

	public bool confirmingAction = false;

	public bool carryingIntel = false;

	public int teamID = 0;
	public int currHP;

	
	public int decisionPoints = 2;

	public Transform spriteRef;
	private bool flipped = false;
	private bool tweenRunning = false;

	public bool losTooltipActive = false;
	
	//public int numOfHeals = 1;
	private int healAmount = 3;

	public AI myAI;
	public float moveAnimationSpeed = 0.5f;	// the time in seconds the tweens will take to complete (to each block)
	
	public Vector2 gridPosition = Vector2.zero;
	
	public Vector3 moveDestination;

	//movement animation
	public List<Vector3> positionQueue = new List<Vector3>();
	//

	public Animator spriteAnimator;

	public int diceNumSides = 100;

	public bool choseToWait = false;

	private SpriteRenderer weaponSprite;
	public GameObject progressIcon;

	public int weaponRangeModifier;
    public bool completedActionDelayRunning = false;
	
	void Awake () {
		moveDestination = transform.position;
	}
	
	// Use this for initialization
	void Start () {
		if( !isAI)
			myClan = GameObject.Find ("CLAN_" + myClanName).GetComponent<Clan> ();

		currHP = totalHP;

		if (GameManager.instance) {
			GameManager.instance.AddUnit (this);

			progressIcon = (GameObject)Instantiate (Resources.Load ("Helpers/ProgressCircle", typeof(GameObject)));
			progressIcon.transform.position = this.transform.position - Vector3.up * .49f;
			progressIcon.transform.parent = this.transform;
		}
		
		tweenRunning = false;

		// Add our box collider
		BoxCollider b = gameObject.AddComponent<BoxCollider> ();
		if(b) {
			b.size = new Vector3(.4f, 1.0f, .4f);
			b.center = new Vector3(0f, 0f, 0f);
		}

		// Add an AI component if this is an AI
		if( isAI ) {
			gameObject.AddComponent<AI>();

			myAI = gameObject.GetComponent<AI>();
		}

		// Initialize weapons
		UpdateWeapons ();

		// Initialize armor and accessory equipment slots
		UpdateArmorAndAccessories();

		UpdateAbilsFromEquipment ();

		// SHIELD
		if( shield != null ){	// Give this unit a shield
			shield = (GameObject)Instantiate(Resources.Load("Equipment/Shield", typeof(GameObject)));
			shield.transform.position = this.transform.position;
			shield.transform.rotation = this.transform.rotation;
			shield.transform.parent = this.transform;
		}

		// Initialize items
		UpdateItemsFromStrings();

		// Initialize Equipped Abilities
		UpdateAbilitiesFromStrings ();
		UpdateEquippedAbils ();

		// VISUAL PREFAB LOADING
		GameObject chessPiece = (GameObject)Instantiate(Resources.Load("Puppets/" + visualPrefabString), transform.position , transform.rotation );

		chessPiece.transform.parent = this.gameObject.transform;

		spriteRef = chessPiece.transform;

		spriteAnimator = spriteRef.GetComponent<Animator>();

		// Attach SortingLayerFixing to each sprite of the visual object
		Transform[] children = gameObject.GetComponentsInChildren<Transform>();
		foreach(Transform child in children){
			if (child.GetComponent<Renderer>() != null)
				child.gameObject.AddComponent<SortingLayerFixing>();
		}

		// Set values for this unit based on their current job
		SetValsFromJob();
	}
	
	// Update is called once per frame
	void Update () {
		spriteRef.rotation = Camera.main.transform.rotation;
        spriteRef.position = this.transform.position - (Vector3.up * .4f);

		// flipped?
		if  ( Vector3.Angle(this.transform.right, Camera.main.transform.forward) > 90f) {
			if( flipped){
				//Debug.Log("where");
				spriteRef.transform.localScale = new Vector3(spriteRef.transform.localScale.x, spriteRef.transform.localScale.y, 1);
				flipped = false;
			}

		} else {
			if( !flipped){
				spriteRef.transform.localScale = new Vector3(spriteRef.transform.localScale.x, spriteRef.transform.localScale.y, -1);
				spriteRef.transform.Rotate( new Vector3( 0, 180, 0));
				flipped = true;
			} else {
				spriteRef.transform.Rotate( new Vector3( 0, 180, 0));
				spriteRef.transform.localScale = new Vector3(spriteRef.transform.localScale.x, spriteRef.transform.localScale.y, -1);
			}
		}
	}

	public void TurnStart(){
		// Turn on the command panel for player units
		if( !isAI){
			if( !GameManager.instance.inbattleMenuManager.commandPanelOn)
				GameManager.instance.inbattleMenuManager.ToggleCommandPanel();
		}
		if( !GameManager.instance.inbattleMenuManager.currUnitInfoPanelOn)
			GameManager.instance.inbattleMenuManager.ToggleCurrUnitInfoPanel();

		choseToWait = false;
		moving = false;
		attacking = false;
		meleeAttacking = false;
		running = false;
		AttackingWithAoE( false );
		selectingTarget = false;
		//spriteAnimator.SetBool("Aiming", false);
		selectingWeapon = false;
		selectingMagic = false;
		selectingItems = false;
		healing = false;
		currItem = null;
		GameManager.instance.targetedTile = null;

		GameManager.instance.selectionMarker.transform.localScale = Vector3.zero;
		GameManager.instance.selectionMarker.transform.position = this.gameObject.transform.position + 1.5f * Vector3.up;
		GameManager.instance.selectionMarker.transform.parent = this.gameObject.transform;

		iTween.ScaleTo( GameManager.instance.selectionMarker, Vector3.one - .8f * Vector3.one + .3f * Vector3.up, .3f );

		// Look at the unit that just started it's turn
		GameManager.instance.FocusOnPos( transform.position, 1f);

		if( pinned ) {
			decisionPoints--;
			pinned = false;

			if(!down)
				UnpinAnimation();
		}

		if( down ) {
			decisionPoints = 1;
		}

		if (shield != null)
			shield.SetActive (false);

		// If we're AI, start workin'
		if( isAI){
			myAI.startDelayTimerIsRunning = true;
		}

		mainPhase = true;
	}
	
	public virtual void TurnUpdate () {
		if( decisionPoints <= 0 ){
			if( !selectingFacing ){
				SelectingFacing( true );
			}

			// If we're AI, stop workin'
			if( isAI){
				myAI.startDelayTimerIsRunning = false;
				myAI.showingDelayTimerIsRunning = false;
				myAI.decisionDelayTimerIsRunning = false;

				myAI.aiState = 0;
			}
		}


		if (positionQueue.Count > 0) {
			if(!tweenRunning){
				float elevationDiff =  positionQueue[0].y - transform.position.y ;

				if ( elevationDiff < -jumpHeight ) {
					tweenComplete (true);

				} else {
					iTween.MoveTo(this.gameObject, iTween.Hash("position", positionQueue[0], 
						"easeType", "easeOutQuart", 
						"time", moveAnimationSpeed,
						"orienttopath", true,
						"axis", "y",
						"oncomplete", "tweenComplete",
						"oncompleteparams", false));
					tweenRunning = true;
				}
			}			
		}

		if( isAI && !outOfAction){
			myAI.AIUpdate();
		}
	}

	public void tweenComplete( bool hopped ) {
		//Debug.Log ("completed");
		if( !hopped)
			transform.position = positionQueue[0];
		positionQueue.RemoveAt(0);
		tweenRunning = false;

		if( carryingIntel && !hopped)
			Intel.instance.SetPos( transform.position + Vector3.up * 2 );

		if (positionQueue.Count == 0 ) {	// Finished moving
			SetOccupiedToTeamID();

			// If we stopped on the tile where the intel was dropped, pick it up!
			if( gridPosition.x > -9000 && GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].intelDroppedHere ){
				carryingIntel = true;
				Intel.instance.SetPos( transform.position + Vector3.up * 2 );
				GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].intelDroppedHere = false;

				GameObject intelPickupParticle = (GameObject)Instantiate(Resources.Load("Helpers/IntelPickupParticle", typeof(GameObject)));
				intelPickupParticle.transform.position = this.transform.position;
				Destroy( intelPickupParticle, 5);
			}

			if( !GameManager.instance.somebodyWon ){

				// If we ran to this position, remove an extra DP
				if( GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].runDestination ){
					decisionPoints--;
				}

				if ( decisionPoints > 0) {
					GameManager.instance.FocusOnPos( transform.position, 1);
					if( isAI ){
						myAI.startDelayTimerIsRunning = true;
					}
				}

                StartCoroutine(CompletedAction());
			}

		}
	}

    public IEnumerator CompletedAction(){
		GameManager.instance.targetUnit = null;
        completedActionDelayRunning = true;

		if (GameManager.instance.counteringUnits.Count > 0) {
			for( int i=0; i<GameManager.instance.counteringUnits.Count;i++) {
				Unit counteringUnit = GameManager.instance.counteringUnits [i];
				if ( counteringUnit != GameManager.instance.currUnit) {	// Don't let the attacking unit counter a counter
					if (!counteringUnit.outOfAction) {	// Don't allow a killed unit to counter!
						bool needToCounter = true;
						// Give time for counter-attacking unit to counter
						while(needToCounter){
							if( !startedCounter){
								startedCounter = true;
								yield return new WaitForSeconds(1.0f);
								GameManager.instance.targetUnit = this;
								Debug.Log ("Counter attack!");
								Debug.Log (counteringUnit.name);
								GameManager.instance.ShowMessageInWorld( "COUNTER!", counteringUnit.transform.position, Color.yellow);
								counteringUnit.FaceTarget ();
								BeingAttacked( counteringUnit );
								needToCounter = false;
							}
							yield return null;
						}
					}
				}
			}
		}

        yield return new WaitForSeconds(1.0f);

        completedActionDelayRunning = false;
		decisionPoints--;
		moving = false;
		attacking = false;
		meleeAttacking = false;
		selectingTarget = false;
		selectingWeapon = false;
		selectingMagic = false;
		selectingItems = false;
		AttackingWithAoE( false );
		GameManager.instance.counteringUnits.Clear ();

		if (setAsideWeapon) {	// If we set aside our equipped weapon to use an ability weapon, re-equip it!
			equippedWeapon = setAsideWeapon;
			setAsideWeapon = null;
		}

		// If nobody's won, keep goin'
		if (!GameManager.instance.somebodyWon) {
			if (!outOfAction) {
				GameManager.instance.inbattleMenuManager.UpdateCurrUnitInfoPanel ();

				if (!isAI && decisionPoints > 0) {
					if (!GameManager.instance.inbattleMenuManager.commandPanelOn)
						GameManager.instance.inbattleMenuManager.ToggleCommandPanel ();
				}
			}
		}
	}

	public void FaceTarget(){
		// Rotate to face target
		Vector3 target = GameManager.instance.targetUnit.transform.position;
		Vector3 correctedTarget = new Vector3 (target.x, transform.position.y, target.z);

		transform.LookAt( correctedTarget );
	}

	public void SetTeam(int TeamID) {
		teamID = TeamID;
	}

	public bool CanCounter(){
		if (equippedAbilityStrings [2] == "ABL_Counter")
			return true;
		return false;
	}

	public void HitFX(){
		iTween.ShakePosition(this.gameObject,new Vector3(.2f,0.2f,0.2f),.4f);
	}

	public virtual void MeleeHitFX() {
		iTween.PunchRotation(this.gameObject, iTween.Hash("x", 90f, "time", 2.5f ) );
		iTween.ShakePosition(Camera.main.gameObject,new Vector3(.4f,0f,0f),.4f);

	}

	public void MercyKillFX(){
		iTween.ShakePosition(Camera.main.gameObject,new Vector3(.4f,0f,0f),.4f);

		GameObject mercyParticle = (GameObject)Instantiate(Resources.Load("Helpers/MercyKillParticle", typeof(GameObject)));
		mercyParticle.transform.position = this.transform.position;
		Destroy( mercyParticle, 5);
	}

	public void DownAnimation() {
		down = true;
		//Debug.Log ("downanimation");

		iTween.ShakePosition(Camera.main.gameObject,new Vector3(.4f,0f,0f),.4f);

		GameObject downParticle = (GameObject)Instantiate(Resources.Load("Helpers/DownParticle", typeof(GameObject)));
		downParticle.transform.position = this.transform.position;
		Destroy( downParticle, 5);


		BoxCollider b = this.GetComponent<Collider>() as BoxCollider;

		if(b) {
			b.size = new Vector3(.4f, .5f, .4f);
			b.center = new Vector3(0f, -.25f, 0f);
		}

	}

	public void PinAnimation() {
		//Debug.Log ("PinAnimation");
		iTween.ShakePosition(Camera.main.gameObject,new Vector3(.2f,0f,0f),.2f);

		GameObject pinnedParticle = (GameObject)Instantiate(Resources.Load("Helpers/PinnedParticle", typeof(GameObject)));
		pinnedParticle.transform.position = this.transform.position;
		Destroy( pinnedParticle, 5);


		BoxCollider b = this.GetComponent<Collider>() as BoxCollider;
		
		if(b) {
			b.size = new Vector3(.4f, .5f, .4f);
			b.center = new Vector3(0f, -.25f, 0f);
		}
	}

	public void UnpinAnimation() {
		pinned = false;

		GameObject unpinnedParticle = (GameObject)Instantiate(Resources.Load("Helpers/RecoverParticle", typeof(GameObject)));
		unpinnedParticle.transform.position = this.transform.position;
		Destroy( unpinnedParticle, 5);

		BoxCollider b = this.GetComponent<Collider>() as BoxCollider;
		
		if(b) {
			b.size = new Vector3(.4f, 1.0f, .4f);
			b.center = new Vector3(0f, 0f, 0f);
		}
	}

	public virtual void SetOccupiedToTeamID() {
		if( !outOfAction ) {
			GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].occupied = teamID;
			GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].occupyingUnit = this;
			GameManager.instance.targetedTile = null;
		}
	}

	public void KillFX(){
		iTween.ShakePosition(Camera.main.gameObject,new Vector3(.4f,0f,0f),.4f);

		GameObject killedParticle = (GameObject)Instantiate(Resources.Load("Helpers/KilledParticle", typeof(GameObject)));
		killedParticle.transform.position = this.transform.position;
		Destroy( killedParticle, 5);
	}

	public virtual void OutOfAction() {
		if( !outOfAction) {
			// Add our equipment to the player's clan stores if we're an enemy
			if (isAI) {
				foreach (string s in itemStrings) {
					if( !string.IsNullOrEmpty(s))
						GameManager.instance.itemsAndEquipmentCollected.Add (s);
				}
				foreach (string s2 in equipmentStrings) {
					if( !string.IsNullOrEmpty(s2))
						GameManager.instance.itemsAndEquipmentCollected.Add (s2);
				}
			}

			//If we have the intel, drop it on this tile
			if( carryingIntel ){
				GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].intelDroppedHere = true;
				Intel.instance.SetPos( GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].transform.position + Vector3.up * 2 );
				carryingIntel = false;
			}

			// If a unit is killed on it's own turn, end it's turn :P
			if (GameManager.instance.currUnit == this) {
				EndTurn ();
			}

			outOfAction = true;

			iTween.FadeTo( spriteRef.gameObject, 0, 2 );
			Invoke( "MoveForDeath", 2);

			GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].occupied = -1;
			GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].occupyingUnit = null;
			gridPosition = new Vector2(-9999f, -9999f);
			
			GameManager.instance.CheckForWinner();
		}
	}

	void MoveForDeath(){
		transform.position = new Vector3( -9999, -9999, -9999);
	}

	public virtual void EndTurn() {
		// Re-activate our shield if it was de-activated this turn
		if (shield != null)
			shield.SetActive (true);

		GameManager.instance.removeTileHighlights();

		// Tell the progress timer how much DP we used this turn before resetting
		progressIcon.GetComponent<TestProgress>().pastTurnDPRemaining = decisionPoints;

		decisionPoints = 2;
		//movePoints = 1;
		//attackPoints = 1;
		moving = false;
		attacking = false;
		meleeAttacking = false;
		GameManager.instance.counteringUnits.Clear ();

		GameManager.instance.targetUnit = null;

		GameManager.instance.endTurnDelayTimerIsRunning = true;
		mainPhase = false;
		GameManager.instance.targetedTile = null;
		currItem = null;
		if( GameManager.instance.inbattleMenuManager.currUnitInfoPanelOn)
			GameManager.instance.inbattleMenuManager.ToggleCurrUnitInfoPanel();
	}

	void Reload( Weapon weapon ) {
		Debug.Log("Reloaded!");
		AudioSource.PlayClipAtPoint( GameManager.instance.audioReload , Camera.main.transform.position);

		for( int i = 0; i < weapon.clipSize; i++ ) {
			if( weapon.totalAmmoRemaining > 0 ) {
				weapon.ammoInClip++;
				weapon.totalAmmoRemaining--;
			}
		}

		GameObject reloadParticle = (GameObject)Instantiate(Resources.Load("Helpers/ReloadParticle", typeof(GameObject)));
		reloadParticle.transform.position = this.transform.position;
		Destroy( reloadParticle, 5);

		CancelAction();

        StartCoroutine(CompletedAction());
	}

	public virtual void AttackingWithAoE( bool casting ) {
		if( casting ){
			GameManager.instance.aoeTrigger = equippedWeapon.aoeTrigger;
			aoeAttack = true;
			if( equippedWeapon.areaOfEffect)
				equippedWeapon.aoeTrigger.SetActive( true );
		} else {
			aoeAttack = false;
			if (equippedWeapon) {
				if( equippedWeapon.areaOfEffect)
					equippedWeapon.aoeTrigger.SetActive( false );
			}

			GameManager.instance.aoeTrigger = null;
		}
	}

    public void BeingAttacked( Unit attackingUnit ) {
		if (!outOfAction) {
			// If we're being melee'd and we're down, MERCY KILL US!
			if( attackingUnit.equippedWeapon.melee && down ){
				OutOfAction();
				MercyKillFX();
			} else {	// Otherwise do all the attack logic :P
				StartCoroutine( IndividualAttackLogic( attackingUnit));
			}

			GameManager.instance.SetUnitOccupyingSpaces();
			if( startedCounter ){
				startedCounter = false;
			}
		}
	}

	IEnumerator IndividualAttackLogic( Unit attackingUnit){
		int shotsToFire = attackingUnit.equippedWeapon.shotsPerAP;
		if (attackingUnit.equippedWeapon.ammoInClip < shotsToFire && !attackingUnit.equippedWeapon.melee)
			shotsToFire = attackingUnit.equippedWeapon.ammoInClip;

		for (int i = 0; i < shotsToFire; i++) {
			if (outOfAction)
				break;

			if( CanCounter()){	// Check if we have the Counter ability
				// Check if we're in melee range
				Tile myTile = GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y];
				bool inMeleeRange = false;
				foreach (Tile t in myTile.neighbors) {
					if (t.occupied > -1) {
						if (t.occupyingUnit == attackingUnit ) {
							if (Mathf.Abs (myTile.elevation - t.elevation) < maxMeleeElevationDifference) {
								GameManager.instance.counteringUnits.Add (this);
							}
						}
					}
				}
			}

			// If it's not a melee weapon or AoE play a gunshot and subtract ammo
			if( !attackingUnit.equippedWeapon.melee && !attackingUnit.aoeAttack) {
				AudioSource.PlayClipAtPoint( GameManager.instance.audioGunshot , Camera.main.transform.position);
				attackingUnit.equippedWeapon.ammoInClip --;
			} else { // If this is melee or AoE, there is no cover modifier
				if( attackingUnit.equippedWeapon.melee || attackingUnit.aoeAttack)
					AudioSource.PlayClipAtPoint( GameManager.instance.audioSword , Camera.main.transform.position);

				coverModifier = 0;
			}

			//attack logic
			//roll to hit
			if ( GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].shortRange ) {	// Is the attacker shooting from short range?
				weaponRangeModifier = attackingUnit.equippedWeapon.shortToHit;
			} else {	// If not, they're shooting from long range
				weaponRangeModifier = attackingUnit.equippedWeapon.longToHit;
			}

			int diceRoll = Random.Range( 1, diceNumSides+1);
			// CHANGE CHANCE OF HITTING BASED ON FACING DIRECTION
			// HACK If the attacking unit is behind us, Increase chance of hitting
			if( GameManager.CheckRelativePos( attackingUnit.transform, transform) == -1){
				diceRoll += 20;
				Debug.Log ("Diceroll +20: " + diceRoll);
			}
			// If the attacking unit is to the side, increase chance of hitting
			if( GameManager.CheckRelativePos( attackingUnit.transform, transform) == 0 ){
				diceRoll += 10;
				Debug.Log ("Diceroll +10: " + diceRoll);
			}

			bool hit;
			if( attackingUnit.equippedWeapon.melee ){ // If this is a melee attack use the melee hit rules
				hit = diceRoll >= 100 - attackingUnit.agility - agility - attackingUnit.meleeSkill;
			} else { // If its not melee, use the projectile hit rules
				hit = diceRoll + weaponRangeModifier + coverModifier >= 100 - attackingUnit.ballisticSkill;
			}
			//		Debug.Log ("dice Roll: " + diceRoll);
			//		Debug.Log ("Cover Mod: " + coverModifier);
			//		Debug.Log ("Wep Range Mod: " + weaponRangeModifier);
			bool criticalHit = false;
			if( diceRoll > 95 ){	// If we roll a "natural 20" the hit auto lands and causes double dmg
				criticalHit = true;
				hit = true;
			}

			if (hit) {
				//pinned = true;
				//Debug.Log(attackingUnit.unitName + " hit " + unitName + "!");

				int dmgToDeal = (int)GameManager.instance.DamageToDeal( attackingUnit );
				if( criticalHit ) // Double damage
					dmgToDeal *= 2;

				TakeDamage (dmgToDeal, attackingUnit);
				bool knockback = false;

				if (attackingUnit.equippedWeapon.abilityWeapon) {	// If we're using an ability weapon, check it's effects
					foreach (string s in attackingUnit.equippedWeapon.GetComponent<Ability>().effects) {
						if (s == "knockback")
							knockback = true;
					}
				}
				if (criticalHit && !outOfAction) {	// If it was a critical hit, knock us back
					knockback = true;
				}

				// Knockback if possible
				if (knockback) {
					Tile knockbackTile = GameManager.instance.map [(int)gridPosition.x] [(int)gridPosition.y].KnockbackToNeighbor (attackingUnit.transform.position);

					// Move us if it isn't the same tile that we're on now
					if (knockbackTile != GameManager.instance.map [(int)gridPosition.x] [(int)gridPosition.y]) {
						GameManager.instance.knockbackAttackedUnit (this, attackingUnit, knockbackTile);

						// If we're knocked back this cancels counter-attacks
						if (GameManager.instance.counteringUnits.Contains (this)) {
							GameManager.instance.counteringUnits.Remove (this);
						}
					}
				} else {
					HitFX ();	// Only do the HitFX if we're not knocked back (strange things happen with the Tweens happening on top of eachother)
				}

				//Debug.Log(currUnit.unitName + " successfuly hit " + target.unitName + " for " + amountOfDamage + " damage!");
			} else {
				GameObject missedParticle = (GameObject)Instantiate(Resources.Load("Helpers/MissedParticle", typeof(GameObject)));
				missedParticle.transform.position = this.transform.position;
				Destroy( missedParticle, 5);
				//Debug.Log(attackingUnit.unitName + " missed " + unitName + "!");
			}

			yield return new WaitForSeconds( attackingUnit.equippedWeapon.firingRate);
		}

		StartCoroutine( GameManager.instance.currUnit.CompletedAction() );
	}

	public void TakeDamage( int amt, Unit attackingUnit ){
		currHP -= amt;

		GameManager.instance.ShowMessageInWorld( ("-" + amt).ToString() + " HP", transform.position, Color.red);

		if( currHP <= 0 ){ // we dead!
			// Give the attacking unit XP if the unit is killed, if it's not a friendly unit
			if( attackingUnit.teamID != teamID)
				attackingUnit.xpForThisBattle += 5;

			KillFX();
			OutOfAction();
		}
	}

	public void HealFromDown() {
		Unit healingUnit = GameManager.instance.currUnit;

		GameManager.instance.removeTileHighlights();

		if( down ){
			down = false;

			//pinned = true;
			PinAnimation();
		}

		meleeSkill = 3;
		ballisticSkill = 3;

		currHP += healAmount;
		if( currHP > totalHP )
			currHP = totalHP;

		GameObject healedParticle = (GameObject)Instantiate(Resources.Load("Helpers/HealedParticle", typeof(GameObject)));
		healedParticle.transform.position = this.transform.position;
		Destroy( healedParticle, 5);

		//healingUnit.numOfHeals--;
        StartCoroutine(	healingUnit.CompletedAction());
		//if( healingUnit.currItem != null)
			//healingUnit.currItem.Use();
		healingUnit.healing = false;
		healingUnit.selectingTarget = false;
		healingUnit.selectingMagic = false;
		healingUnit.selectingItems = false;
	}

	public void AttemptToRecruit() {
		Unit recruitingUnit = GameManager.instance.currUnit;

		GameManager.instance.removeTileHighlights();

		int randRoll = Random.Range (1, 101);

		if (randRoll > (int)(currHP/totalHP*100)) {	// Chance to recruit, for now just more likely as they get weaker
			teamID = recruitingUnit.teamID;
			isAI = false;
			Destroy (gameObject.GetComponent<AI> ());

			myClan = recruitingUnit.myClan;

			// Add it to the clan
			Clan recruitingClan = GameManager.instance.team1Clan;	// For now, always the player's clan
			recruitingClan.units.Add( name );

			// Add equipment and items to clan
			foreach (string e in equipmentStrings) {
				if (!string.IsNullOrEmpty (e)) {	// Don't add empty strings!
					if (recruitingClan.equipNames.Contains (e)) {	// If the clan is already holding equipment of this type, just add it to held and equipped
						int equipIndex = recruitingClan.equipNames.IndexOf (e);
						recruitingClan.equipHeld [equipIndex]++;
						recruitingClan.equipEquipped [equipIndex]++;
					} else {	// If the clan doesn't have any of this equipment, add it!
						recruitingClan.equipNames.Add( e );
						recruitingClan.equipHeld.Add (1);
						recruitingClan.equipEquipped.Add (1);
					}
				}
			}

			foreach (string i in itemStrings) {
				if (!string.IsNullOrEmpty (i)) {	// Don't add empty strings!
					if (recruitingClan.itemStrings.Contains (i)) {	// If the clan is already holding items of this type, just add it to held and equipped
						int itemIndex = recruitingClan.itemStrings.IndexOf (i);
						recruitingClan.itemsHeld [itemIndex]++;
						recruitingClan.itemsEquipped [itemIndex]++;
					} else {	// If the clan doesn't have any of this item, add it!
						recruitingClan.itemStrings.Add( i );
						recruitingClan.itemsHeld.Add (1);
						recruitingClan.itemsEquipped.Add (1);
					}
				}
			}

			// Add my known jobs to the clan's known jobs
			foreach (string job in jobsLearned) {
				if (!string.IsNullOrEmpty (job)) {	// Don't add empty strings!
					if (!recruitingClan.knownJobs.Contains (job)) {	// If the clan doesn't know this job, add it!
						recruitingClan.knownJobs.Add (job);
					}
				}
			}

			GameManager.instance.CheckForWinner ();
		}


		GameObject recruitParticle = (GameObject)Instantiate(Resources.Load("Helpers/HealedParticle", typeof(GameObject)));
		recruitParticle.transform.position = this.transform.position;
		Destroy( recruitParticle, 5);

		StartCoroutine(	recruitingUnit.CompletedAction());

		recruitingUnit.recruiting = false;
		recruitingUnit.selectingTarget = false;
		recruitingUnit.selectingMagic = false;
		recruitingUnit.selectingItems = false;
	}

	public void SelectMove(){
		if (!moving) {
			moving = true;
			attacking = false;
			meleeAttacking = false;
			running = false;
			selectingTarget = true;

			// Give the run option only for players
			if( decisionPoints > 1 && !isAI ){
				GameManager.instance.highlightTilesAt(gridPosition, GameManager.blueColor, movementPerDecisionPoint*2);
			} else {
				GameManager.instance.highlightTilesAt(gridPosition, GameManager.blueColor, movementPerDecisionPoint);
			}
			
		}
	}

	public void SelectCrawl(){
		if (!moving) {
			GameManager.instance.removeTileHighlights();
			moving = true;
			attacking = false;
			meleeAttacking = false;
			selectingTarget = true;
			GameManager.instance.highlightTilesAt(gridPosition, GameManager.blueColor, movementPerDecisionPoint/2);
		}
	}

	public void SelectHeal( int amount ){
		if (!attacking) {
			healAmount = amount;
			GameManager.instance.removeTileHighlights();
			moving = false;
			attacking = false;
			meleeAttacking = false;
			running = false;
			healing = true;
			selectingTarget = true;
			GameManager.instance.highlightTilesAt(gridPosition,GameManager.whiteColor, 1);
		}
	}

	public void SelectRecruit( ){
		if (!attacking) {
			GameManager.instance.removeTileHighlights();
			moving = false;
			attacking = false;
			meleeAttacking = false;
			running = false;
			healing = false;
			recruiting = true;
			selectingTarget = true;
			GameManager.instance.highlightTilesAt(gridPosition,GameManager.whiteColor, 1);
		}
	}

	public void SelectWeapon( int weaponSlot, Weapon passedWep = null ){
		Weapon selectedWeapon = weapons[0];
		if( weaponSlot == 2)
			selectedWeapon = weapons[1];
		if( weaponSlot == 3)
			selectedWeapon = weapons[2];
		if( weaponSlot == 99)	// Special case for grenade
			selectedWeapon = GetGrenade();
		//Debug.Log (selectedWeapon.weaponName);
		// If we have a shield, disable it while we attack!
		if (shield != null) {
			shield.SetActive(false);
		}

		if (passedWep) {	// If we've been passed a Weapon, equip that. This is only done for Ability Weapons for now, so we also do extra stuff for that
			if (equippedWeapon)	// If we already have a weapon equipped, set that one aside
				setAsideWeapon = equippedWeapon;
			
			selectedWeapon = passedWep;
		}

		// If this is melee, special rules
		if (selectedWeapon.melee) {
			if (!attacking) {
				equippedWeapon = selectedWeapon;
				GameManager.instance.removeTileHighlights();
				moving = false;
				attacking = true;
				meleeAttacking = true;
				running = false;
				selectingTarget = true;

				// If this is a melee attack with AoE, we also need to place the AoE trigger to be on the unit
				if( equippedWeapon.areaOfEffect ){
					AttackingWithAoE( true );
					GameManager.instance.aoeTrigger.transform.position = transform.position;
					SelectingFacing( true);
					
				}

				GameManager.instance.highlightTilesAt(gridPosition, GameManager.redColor, equippedWeapon.longRange);
			}
		} else { // Not melee!
			if (!attacking) {
				if( selectedWeapon.ammoInClip < 1 ) {	// We need to reload
					if( selectedWeapon.totalAmmoRemaining > 0) {
						Reload( selectedWeapon );
					} else {
						Debug.Log(selectedWeapon.GetComponent<Equipment>().inGameName + " out of ammo!");
					}
				} else {	// We have enough ammo to shoot, so do it!
					equippedWeapon = selectedWeapon;
					GameManager.instance.removeTileHighlights();
					moving = false;
					attacking = true;
					meleeAttacking = false;
					running = false;
					selectingTarget = true;
					//spriteAnimator.SetBool("Aiming", true);

					if( equippedWeapon.areaOfEffect ){
						AttackingWithAoE( true );
					}

					GameManager.instance.highlightTilesAt(gridPosition,GameManager.redColor, equippedWeapon.longRange);
				}
			}
		}

	}

	public void UpdateWeapons(){
		weapons.Clear ();
		// Check hands for objects (first two equip slots)
		for (int i = 0; i < 2; i++) {
			if( !string.IsNullOrEmpty( equipmentStrings[i])){
				if (equipmentStrings[i].Substring (0, 1) == "W") {	// If the string starts with a W, it's a weapon
					if (equipmentGOs[i])
						Destroy (equipmentGOs[i]);
					GameObject wep = (GameObject)Instantiate (Resources.Load ("Equipment/" + equipmentStrings[i]), transform.position, transform.rotation);
					wep.transform.parent = transform;
					weapons.Add( wep.GetComponent<Weapon> () );

					equipmentGOs[i] = wep;
				}
			}
		}

		if (weapons.Count > 0)
			equippedWeapon = weapons [0];
		else {	// no weapons, use fists
			Transform t = transform.Find ("WEP_Fists(Clone)");
			GameObject findWep = null;
			if (t)
				findWep = t.gameObject;

			if (findWep) {
				weapons.Clear ();
				weapons.Add (findWep.GetComponent<Weapon>());
				equippedWeapon = findWep.GetComponent<Weapon> ();
			} else {
				GameObject wep = (GameObject)Instantiate (Resources.Load ("Equipment/WEP_Fists"), transform.position, transform.rotation);
				wep.transform.parent = transform;
				weapons.Add( wep.GetComponent<Weapon> () );

				equippedWeapon = wep.GetComponent<Weapon> ();
			}
		}
	}

	public void UpdateArmorAndAccessories(){
		for (int i = 2; i < 5; i++) {
			if (!string.IsNullOrEmpty (equipmentStrings [i])) {
				if (equipmentGOs [i])	// Destroy the old equipment GO
					Destroy (equipmentGOs [i]);

				GameObject tempEquip = (GameObject)Instantiate (Resources.Load ("Equipment/" + equipmentStrings [i]), transform.position, transform.rotation);
				tempEquip.transform.parent = transform;

				equipmentGOs [i] = tempEquip;
			}
		}
	}


	public void UpdateAbilsFromEquipment(){
		for (int i = 0; i < 5; i++) {	//Check through all 5 equipment slots...
			if (equipmentGOs[i]) {	// If there's equipment in this slot...
				foreach (string abilString in equipmentGOs[i].GetComponent<Equipment>().abilities) {	// For each ability on the equipment...
					if( !abilityStrings.Contains( abilString )){	// If we don't already have this ability in our ability strings, add it!
						abilityStrings.Add (abilString);
						abilityPoints.Add (0);	// Add ability points, starting at 0, for this new ability
					}
				}
			}
		}

		// After adding new abilities, look through all the equipped abilities in R and P, and unequip abilities that aren't mastered or on equipment
		for (int abilIndex = 2; abilIndex < 4; abilIndex++) {
			bool unequip = false;
			if (!string.IsNullOrEmpty (equippedAbilityStrings [abilIndex])) {	// If the slot isn't empty, check if it's on equipment or mastered
				bool found = false;	// Have we found the ability on equipment or as a mastered ability?
				int abilStringIndex = abilityStrings.IndexOf( equippedAbilityStrings [abilIndex] );
				if (abilityPoints [abilStringIndex] == 9999) {
					found = true;	// If the ability is a mastered ability, we don't unequip
				} else { // If it isn't mastered, check through all equipment to see if it's equipped
					for (int equipNum = 0; equipNum < 5; equipNum++) {	//Check through all 5 equipment slots...
						if (equipmentGOs[equipNum]) {	// If there's equipment in this slot...
							foreach (string abilString in equipmentGOs[equipNum].GetComponent<Equipment>().abilities) {	// For each ability on the equipment...
								if( equippedAbilityStrings [abilIndex] == abilString){	// If the equipped ability string matches one on the equipment, it's been found
									found = true;
									break;
								}
							}
						}
					}
				}

				if (!found) {	// If we didn't find the ability, we need to unequip it from the slot
					Destroy( equippedAbilities[ abilIndex].gameObject );
					equippedAbilityStrings[abilIndex] = null;
				}
			}
		}

		UpdateAbilitiesFromStrings ();
		UpdateEquippedAbils ();
	}

	public void UpdateAbilitiesFromStrings(){
		foreach (Ability abil in abilities) {
			if( abil.gameObject)
				Destroy (abil.gameObject);
		}
		abilities.Clear ();

		if (abilityStrings.Count > 0) {	// If we have any ability strings...
			for (int i = 0; i < abilityStrings.Count; i++) {
				GameObject tempAbil = (GameObject)Instantiate (Resources.Load ("Abilities/" + abilityStrings[i]), transform.position, transform.rotation);
				tempAbil.transform.parent = transform;

				abilities.Add (tempAbil.GetComponent<Ability> ());
				//Destroy (tempAbil);
			}
		}
	}

	public void UpdateItemsFromStrings(){
		if (items.Count > 0) {
			foreach (Item item in items) {
				if (item)
					Destroy (item.gameObject);
			}
		}
		items.Clear ();

		for (int itemS = 0; itemS < 3; itemS++) {
			items.Add (null);
			if (!string.IsNullOrEmpty (itemStrings [itemS])) {
				GameObject tempItem = (GameObject)Instantiate (Resources.Load ("Items/" + itemStrings [itemS]), transform.position, transform.rotation);
				tempItem.transform.parent = transform;
				items [itemS] = tempItem.GetComponent<Item> ();
			}
		}
	}

	public void UpdateEquippedAbils(){
		equippedAbilities.Clear ();

		// Initialize equipped Job ability Categories
		for (int catIndex = 0; catIndex < 2; catIndex++) {
			equippedAbilities.Add (null);

			// Delete old ability category and GO
			if (equippedAbilCats [catIndex]) {
				Destroy (equippedAbilCats [catIndex].gameObject);
				equippedAbilCats [catIndex] = null;
			}

			if( equippedAbilCats.Count < 2)
				equippedAbilCats.Add (null);

			if (!string.IsNullOrEmpty (equippedAbilityStrings [catIndex])) {	// If the slot isn't empty, set up the game object for it					
				GameObject tempCat = (GameObject)Instantiate (Resources.Load ("AbilityCategories/" + equippedAbilityStrings [catIndex]), transform.position, transform.rotation);
				tempCat.transform.parent = transform;

				equippedAbilCats[catIndex] =  tempCat.GetComponent<AbilityCategory> ();
			}
		}

		// Initialize equipped abilities
		for (int abilIndex = 2; abilIndex < 4; abilIndex++) {
			equippedAbilities.Add (null);

			// Delete old ability category and GO
			if (equippedAbilities [abilIndex]) {
				Destroy (equippedAbilities [abilIndex].gameObject);
				equippedAbilities [abilIndex] = null;
			}

			if (!string.IsNullOrEmpty (equippedAbilityStrings [abilIndex])) {	// If the slot isn't empty, set up the game object for it
				equippedAbilities[abilIndex] =  abilities[ abilityStrings.IndexOf(equippedAbilityStrings [abilIndex]) ];
			}
		}
	}

	// If we click on the unit in the Unit Info Screen
	void OnMouseUp(){
		if (UnitInfoSceneManager.instance) {
			if (!UnitInfoSceneManager.instance.spotlightLocked) {
				UnitInfoSceneManager.instance.currUnit = this;
				UnitInfoSceneManager.instance.menuManager.ToggleUnitPanel ();
			}
		}
	}

	// Show info on this unit when we mouse over it!
	void OnMouseEnter() {
		if (GameManager.instance) {
			Unit currUnit = GameManager.instance.currUnit;
			// Reset cover modifier so its not using the cover mod from a previous check
			coverModifier = 0;

			// If we're just looking for movement capability
			if( !GameManager.instance.spotlightLocked){
				if (GameManager.instance.currHighlightedTiles.Count < 1 && currUnit && !currUnit.isAI && currUnit != this ) {	// Show movement highlights as long as there aren't currently any highlighted tiles, not in an AI turn, and not the current unit for the player
					GameManager.instance.ShowMovementDistance( gridPosition, movementPerDecisionPoint*2 );
				}
			}

			// If the spotlight isn't locked, current unit's attacking and it's not the AI, we can set the target unit to be the moused-over unit
			if (!GameManager.instance.spotlightLocked && currUnit && currUnit.attacking && !currUnit.isAI ) {
				if (GameManager.instance.map [(int)gridPosition.x] [(int)gridPosition.y].highlighted) {
					if (!GameManager.instance.currUnit.aoeAttack) GameManager.instance.targetUnit = this;
					else {	// If we're doing an AoE attack, make sure the highlighted tile is aoeHighlighted
						if (GameManager.instance.map [(int)gridPosition.x] [(int)gridPosition.y].aoeHighlighted) GameManager.instance.targetUnit = this;
					}

				}


				GameManager.instance.spotlightBracket.transform.position = GameManager.instance.map [(int)gridPosition.x] [(int)gridPosition.y].transform.position;
				GameManager.instance.currElevationTile = GameManager.instance.map [(int)gridPosition.x] [(int)gridPosition.y];
			}

			// If we're being targeted for an attack, show the target unit panel
			if (currUnit != null && currUnit.selectingTarget && currUnit.attacking && GameManager.instance.targetUnit ) {
				if (!GameManager.instance.inbattleMenuManager.targetUnitInfoPanelOn) {
					GameManager.instance.inbattleMenuManager.ToggleTargetUnitInfoPanel ();
				}
			}
		}

		if (UnitInfoSceneManager.instance) {
			if( !UnitInfoSceneManager.instance.spotlightLocked )
				UnitInfoSceneManager.instance.spotlightBracket.transform.position = transform.position - Vector3.up;
		}
	}

	void OnMouseOver() {
		if (GameManager.instance) {
			Unit currUnit = GameManager.instance.currUnit;

			// If we're being targeted for healing
			if( currUnit != null && currUnit.healing && currUnit.selectingTarget ) {
				if( currUnit != this) {
					if( GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].highlighted ) {
						GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].MousingOver();
					}
				}
			}
		}
	}

	void OnMouseExit() {
		if (GameManager.instance) {
			//mousedOver = false;
			losTooltipActive = false;
			if( !outOfAction )
				GameManager.instance.map[(int)gridPosition.x][(int)gridPosition.y].MouseOut();

			// CLEAR TEMPORARY MOVEMENT RANGE HIGHLIGHTS
			if( GameManager.instance.showingMoveRange ){
				GameManager.instance.removeTileHighlights ();
			}


			// SPOTLIGHT STUFF
			if (!GameManager.instance.spotlightLocked && GameManager.instance.currUnit && !GameManager.instance.currUnit.isAI) {	// Only clear the target unit if it's the player looking around
				// Hide the target unit panel
				if (GameManager.instance.inbattleMenuManager.targetUnitInfoPanelOn) {
					GameManager.instance.inbattleMenuManager.ToggleTargetUnitInfoPanel ();
				}

				GameManager.instance.targetUnit = null;
			}
		}
	}

	public void CancelAction(){
		choseToWait = false;
		moving = false;
		attacking = false;
		meleeAttacking = false;
		running = false;
		selectingTarget = false;
		AttackingWithAoE( false );

		if (setAsideWeapon) {	// If we set aside our equipped weapon to use an ability weapon, re-equip it!
			equippedWeapon = setAsideWeapon;
			setAsideWeapon = null;
		}

		recruiting = false;
		healing = false;
		selectingWeapon = false;
		selectingMagic = false;
		selectingItems = false;
		SelectingFacing( false );
		GameManager.instance.targetUnit = null;
		GameManager.instance.CancelAction ();
		GameManager.instance.SetUnitOccupyingSpaces();
		GameManager.instance.removeTileHighlights();
	}

	public void SelectingFacing( bool selecting ){
		selectingFacing = selecting;
		GameManager.instance.facingArrows.BroadcastMessage("DeselectAll", SendMessageOptions.DontRequireReceiver);
		GameManager.instance.facingArrows.transform.position = transform.position + Vector3.up;
		GameManager.instance.facingArrows.SetActive( selectingFacing);

		// For player
		if( selectingFacing && !isAI){
			confirmingAction = true;
		}

		// For AI finishing turn
		if( isAI && decisionPoints < 1){
			confirmingAction = true;
		}

		// Set our facing to our current direction if they are just being turned on
		if( selectingFacing){
			GameManager.instance.targetUnit = null;	// Fixing target unit info panel showing up when AI is selecting facing at end of turn
			GameManager.instance.SelectFacingArrowCurrentDirection();
		}
	}

	public bool CanHeal(){
		bool canHeal = false;
		for( int i = 0; i < items.Count; i++ ) {
			if (items [i]) {
				if( items[i].type == "Potion" ){
					if( items[i].numOfUses > 0){
						canHeal = true;
					}
				}
			}
		}
		return canHeal;
	}

	public Weapon GetGrenade(){
		Weapon gren = null;
		for( int i = 0; i < items.Count; i++ ) {
			if (items [i]) {
				if( items[i].type == "Grenade" ){
					if( items[i].numOfUses > 0){
						gren = items[i].GetComponent<Weapon>();
					}
				}
			}
		}
		return gren;
	}

	public void AddAbilPointsToEquippedAbils( int pointsToAdd ){
		for (int i = 0; i < 5; i++) {	// Check through all 5 equipment slots
			if (equipmentGOs [i]) {	// If there's equipment in that slot...
				for (int j = 0; j < equipmentGOs [i].GetComponent<Equipment> ().abilities.Count; j++) {	// Check through all abilities on that equipment...
					if (equipmentGOs [i].GetComponent<Equipment> ().jobForAbilities [j] == currJobString) { // If that ability's job requirement matches Unit's current job, add AP
						int indexOfAbil = abilityStrings.IndexOf( equipmentGOs [i].GetComponent<Equipment> ().abilities[j] );
						abilityPoints [indexOfAbil] += pointsToAdd;

						GameObject tempAbilGO = (GameObject)Instantiate (Resources.Load ("Abilities/" + equipmentGOs [i].GetComponent<Equipment> ().abilities[j]), transform.position, transform.rotation);
						Ability tempAbil = tempAbilGO.GetComponent<Ability> ();
						if (abilityPoints [indexOfAbil] >= tempAbil.requiredAPToMaster) {	// If we've gotten enough AP to master the ability, MASTER IT
							abilityPoints [indexOfAbil] = 9999;
							UpdateLearnedJobs ();
						}
						Destroy (tempAbilGO);
					}
				}
			}
		}
	}

	public void UpdateLearnedJobs(){
		// Check through each known job
		foreach( string clanJob in myClan.knownJobs) {
			// For each known job, check if the unit doesn't know it
			if (!jobsLearned.Contains(clanJob)) {	// if NOT...
				GameObject tempJobGO = (GameObject)Instantiate (Resources.Load ("Jobs/" + clanJob));
				Job tempJob = tempJobGO.GetComponent<Job> ();
				bool meetsReqs = true;

				for (int j = 0; j < tempJob.requirements.Count; j++) {	// Check each of the requirements...
					int numMasteredInRequiredCat = 0;

					for (int k = 0; k < abilities.Count; k++) {
						if (abilityPoints [k] == 9999) {	// If this is a mastered ability...
							if (abilities [k].active) {	// Is it Active?
								// Is it in the right category?
								GameObject tempCatGO = (GameObject)Instantiate (Resources.Load ("AbilityCategories/" + tempJob.requirements[j] ));
								AbilityCategory tempAC = tempCatGO.GetComponent<AbilityCategory> ();

								if (tempAC.abilities.Contains (abilities [k].internalName)) {	// If the Ability Category contains the current ability, increase the number of mastered in required category
									numMasteredInRequiredCat++;
								}

								Destroy (tempCatGO);
							}
						}
					}

					if (numMasteredInRequiredCat < tempJob.numRequired [j]) {	// If we have don't have enough mastered in this category, we fail the requirements
						meetsReqs = false;
						break;
					}
				}

				if (meetsReqs) {	// If the unit meets the requirements, add the job to it's learned list
					jobsLearned.Add( clanJob );
				}

				Destroy (tempJobGO);
			}
		}
	}

	public bool IsAbilOnEquipment( string abilToCheck ){
		bool isAbilOnEquipment = false;

		for (int i = 0; i < 5; i++) {	// Check through all 5 equipment slots
			if (equipmentGOs [i]) {
				foreach (string equipAbilString in equipmentGOs [i].GetComponent<Equipment>().abilities) {
					if (abilToCheck == equipAbilString) {
						isAbilOnEquipment = true;
						break;
					}
				}
			}
		}

		return isAbilOnEquipment;
	}

	// For Active ability categories
	public bool IsJobAbilCategoryEquippable( string jobToCheck ){
		bool equippable = false;

		if (jobsLearned.Contains( jobToCheck)) {	// If the job is learned by this unit, it's equippable
			equippable = true;
		}

		return equippable;
	}

	// For REACTIVE and PASSIVE abilities
	public bool IsRorPAbilEquippable( string abilToCheck ){
		bool equippable = false;
		int indexOfAbil = abilityStrings.IndexOf( abilToCheck );

		// First, check if this is a mastered ability...
		if (abilityPoints [indexOfAbil] == 9999) {
			equippable = true;	// Equippable if it's mastered
		}

		if (!equippable) {	// If it's not mastered, see if it's on any of our equipment for our job
			for (int i = 0; i < 5; i++) {	// Check through all 5 equipment slots
				if (equipmentGOs [i]) {	// If there's equipment in that slot...
					for (int j = 0; j < equipmentGOs [i].GetComponent<Equipment> ().abilities.Count; j++) {	// Check through all abilities on that equipment...
						if (equipmentGOs [i].GetComponent<Equipment> ().jobForAbilities [j] == currJobString) { // If that ability's job requirement matches Unit's current job...
							if (equipmentGOs [i].GetComponent<Equipment> ().abilities [j] == abilToCheck) {	// If the ability is the same ability, it's equippable
								equippable = true;
								break;
							}
						}
					}
				}
			}
		}

		return equippable;
	}

	public void SetValsFromJob(){
		if (currJobGO) {
			Destroy (currJobGO);
		}
		currJobGO = (GameObject)Instantiate (Resources.Load ("Jobs/" + currJobString), transform.position, transform.rotation);
		currJobGO.transform.parent = transform;
		//Debug.Log (tempAbil);
		currJob = currJobGO.GetComponent<Job>();

		movementPerDecisionPoint = currJob.move;
		jumpHeight = currJob.jump;
		// TODO evade?
	}

	public void ChangeJob( Job newJob ){
		// When changing jobs, replace the first ACTION slot with the new job's ability categor
		if (equippedAbilCats [0]) {
			Destroy (equippedAbilCats [0].gameObject);
		}
		equippedAbilityStrings [0] = newJob.abilityCategory;
		GameObject tempCat = (GameObject)Instantiate (Resources.Load ("AbilityCategories/" + equippedAbilityStrings [0]), transform.position, transform.rotation);
		tempCat.transform.parent = transform;
		equippedAbilCats[0] =  tempCat.GetComponent<AbilityCategory> ();
		// If the second slot is the same as the new ability category, remove it
		if (equippedAbilCats [1]) {
			if (equippedAbilCats [1].categoryName == equippedAbilCats [0].categoryName) {	// If it's a duplicate, remove it
				Destroy (equippedAbilCats [1].gameObject);
				equippedAbilityStrings [1] = null;
			}
		}

		if (newJob != currJob) {
			// Check if unit's equipped equipment is allowed
			for (int i = 0; i < 5; i++) {
				if (equipmentGOs[i]) {
					bool allowed = false;
					foreach (EquipmentCategory allowedCat in newJob.allowedEquipmentTypes) {	// Check through all allowed equipment types of the new job
						if (allowedCat == equipmentGOs[i].GetComponent<Equipment> ().category) {	// if the equipment is in an allowed category, this one's ok
							allowed = true;
						}
					}

					if (!allowed) {	// REMOVE EQUIPMENT IF IT'S NOT ALLOWED WITH THIS JOB
						int indexOfRemovedEquip = UnitInfoSceneManager.instance.currClan.equipNames.IndexOf (equipmentGOs[i].GetComponent<Equipment> ().internalName);
						UnitInfoSceneManager.instance.currClan.equipEquipped [indexOfRemovedEquip]--;	// Decrease the number of those equipped

						equipmentStrings[i] = null;
						Destroy( equipmentGOs[i] );
						equipmentGOs[i] = null;
					}
				}
			}
				
			UpdateWeapons ();
			UpdateArmorAndAccessories ();
			UpdateAbilsFromEquipment ();

			currJobString = newJob.internalString;
			SetValsFromJob ();
		}
	}

	public bool JobChangeAffectsEquipOrAbils( Job newJob ){
		//TODO check abilities!
		bool changes = false;
		if (newJob != currJob) {
			// Check if unit's equipped equipment is allowed
			for (int i = 0; i < 5; i++) {
				if (equipmentGOs[i]) {
					bool allowed = false;
					foreach (EquipmentCategory allowedCat in newJob.allowedEquipmentTypes) {	// Check through all allowed equipment types of the new job
						if (allowedCat == equipmentGOs[i].GetComponent<Equipment> ().category) {	// if the equipment is in an allowed category, this one's ok
							allowed = true;
						}
					}
					if (!allowed) {
						changes = true;
					}
				}
			}
		}

		return changes;
	}

	void OnDrawGizmos() {
		if( teamID == 0 )
			Gizmos.color = Color.blue;
		if( teamID == 1 )
			Gizmos.color = Color.red;
		
		Gizmos.DrawCube( transform.position, Vector3.one); 
		Gizmos.DrawIcon(transform.position, "UnitGizmo.psd", true);
	}
		
	public void SaveUnitData(){
		BinaryFormatter bf = new BinaryFormatter ();
		if (File.Exists (Application.persistentDataPath + "/" + name.ToString () + ".dat")) {
			//Debug.Log ("delete");
			File.Delete( Application.persistentDataPath + "/" + name.ToString () + ".dat");
		}

		FileStream file = File.Create (Application.persistentDataPath + "/" + name.ToString () + ".dat");

		UnitData data = new UnitData ();

		// VARIABLES TO SAVE
		data.name = name;
		data.isAI = isAI;
		data.movementPerDecisionPoint = movementPerDecisionPoint;
		data.movementPerDecisionPoint = movementPerDecisionPoint;
		data.totalHP = totalHP;
		data.meleeSkill = meleeSkill;
		data.ballisticSkill = ballisticSkill;
		data.agility = agility;

		data.xp = xp;
		data.level = level;
		data.myClanName = myClanName;
		data.jobsLearned = jobsLearned;
		data.currJobString = currJobString;

		// ABILITIES
		data.abilityStrings = abilityStrings;
		data.abilityPoints = abilityPoints;	// How many points each ability has accumulated

		// Equipped abilities
		// 0, 1= Active
		// 2 = Reactive
		// 3 = Passive
		data.equippedAbilityStrings = equippedAbilityStrings;

		// Equipped Equipment!
		data.equipmentStrings = equipmentStrings;


		data.itemStrings = itemStrings;

		data.jumpHeight = jumpHeight;

		data.visualPrefabString = visualPrefabString;

		// END VARIABLES
		bf.Serialize( file, data );
		file.Close ();
	}

	public void LoadUnitData(){
		if (File.Exists (Application.persistentDataPath + "/" + name.ToString () + ".dat")) {
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file = File.Open (Application.persistentDataPath + "/" + name.ToString () + ".dat", FileMode.Open);
			UnitData data = (UnitData)bf.Deserialize (file);

			// VARIABLES TO LOAD
			name = data.name;
			isAI = data.isAI;
			movementPerDecisionPoint = data.movementPerDecisionPoint;
			movementPerDecisionPoint = data.movementPerDecisionPoint;
			totalHP = data.totalHP;
			meleeSkill = data.meleeSkill;
			ballisticSkill = data.ballisticSkill;
			agility = data.agility;

			xp = data.xp;
			level = data.level;
			myClanName = data.myClanName;
			jobsLearned = data.jobsLearned;
			currJobString = data.currJobString;

			// ABILITIES
			abilityStrings = data.abilityStrings;
			abilityPoints = data.abilityPoints;	// How many points each ability has accumulated

			// Equipped abilities
			// 0, 1= Active
			// 2 = Reactive
			// 3 = Passive
			equippedAbilityStrings = data.equippedAbilityStrings;

			// Equipped Equipment!
			equipmentStrings = data.equipmentStrings;

			itemStrings = data.itemStrings;

			jumpHeight = data.jumpHeight;

			visualPrefabString = data.visualPrefabString;
			file.Close ();
		}
	}

	public void DeleteUnitData(){
		BinaryFormatter bf = new BinaryFormatter ();
		if (File.Exists (Application.persistentDataPath + "/" + name.ToString () + ".dat")) {
			//Debug.Log ("delete");
			File.Delete( Application.persistentDataPath + "/" + name.ToString () + ".dat");
		}
	}
}

[Serializable]
class UnitData
{
	public string name;
	public bool isAI;

	public int movementPerDecisionPoint;
	public int totalHP;
	public int meleeSkill;
	public int ballisticSkill;
	public int agility;

	public int xp;
	public int level;
	public string myClanName;
	public List<string> jobsLearned;
	public string currJobString;

	// ABILITIES
	public List<string> abilityStrings;
	public List<int> abilityPoints;	// How many points each ability has accumulated

	// Equipped abilities
	// 0, 1= Active
	// 2 = Reactive
	// 3 = Passive
	public List<string> equippedAbilityStrings;

	// Equipped Equipment!
	public List<string> equipmentStrings;

	public List<string> itemStrings;

	public float jumpHeight;

	public string visualPrefabString;
}