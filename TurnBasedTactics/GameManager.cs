/*
	Code for a turn-based tactics game by Ian Snyder
*/

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
	public static GameManager instance;
	
	public int mapSize = 11;
	
	public List <List<Tile>> map = new List<List<Tile>>();
	public List <Unit> units = new List<Unit>();
	public Unit currUnit = null;
	public Unit targetUnit = null;
	public Queue unitQueue = new Queue ();
	public int currentTurn = 1;

	public List <Tile> currHighlightedTiles = new List<Tile>();

	public int terminalHackProgress = 0;
	public string terminalString = "Patch in...";

	public List <Color> teamColors = new List<Color>();

	public Clan team1Clan;
	public Clan team2Clan;

	public List<Vector3> team1StartPositions = new List<Vector3>();
	public List<Vector3> team2StartPositions = new List<Vector3> ();

	public static Color whiteColor = new Color( 1f, 1f, 1f, 0.75f );
	public static Color yellowColor = new Color( 1.0f, .95f, 0f, 0.75f );
	public static Color redColor = new Color( 0.95f, .0f, 0f, 0.75f );
	public static Color blueColor = new Color( 0.3f, 0.3f, 1.0f, 0.75f );
	public static Color selectionColor = new Color( .1f, 1f, .1f, .75f );

	public GameObject selectionMarker;
	public GameObject aoeTrigger;

	// Keep track of AoE targets, so we can't confirm AoE attack without at least one target
	public List <Unit> aoeTargets = new List<Unit>();

	public GameObject intelToken;

	private GameObject[] exitArrows;

	public bool somebodyWon = false;
	public int winnerID = -1;

	// Timer for delay at turn end
	public float endTurnDelay = 1;
	private float endTurnDelayTimer;
	public bool endTurnDelayTimerIsRunning = false;

	public bool phaseCutscene = false;
	public bool phaseWait = false;	// Wait for next unit turn phase (when we're counting down unit timers)
	public bool phaseTurn = false;	// Are we in a unit's turn phase?

	public Texture2D cameraFadeTexture;

	public AudioClip audioGunshot;
	public AudioClip audioReload;
	public AudioClip audioExplosion;
	public AudioClip audioHacking;
	public AudioClip audioCutter;
	public AudioClip audioSword;

	public List <AudioClip> audioClips = new List<AudioClip>();

	public Texture2D musicNote;

	public GUISkin guiSkin;

	public GameObject facingArrows;
	private GameObject[] facingArrowsGOs;

	public InbattleMenuManager inbattleMenuManager;
	
	// STUFF FOR SAVING ACTION BEFORE CONFIRM
	public Tile targetedTile;

    // COUNTERING
	public List<Unit> counteringUnits = new List<Unit>();

	// Spotlight bracket for currently moused-over tile
	public GameObject spotlightBracket;
	public bool spotlightLocked = false;

	public Tile currElevationTile = null;

	// Temporarily showing movement range of moused-over unit
	public bool showingMoveRange = false;

	// Storing items and equipment that enemies drop when out of action
	public List<string> itemsAndEquipmentCollected = new List<string>();	// List of the internal names of items and equipment collected from downed enemy units

	void Awake() {
		instance = this;

		// In battle menu setup
		Instantiate( Resources.Load("Menus/GUIAnimSystem"));
		GameObject inbattleMenuCanvasGO = (GameObject)Instantiate( Resources.Load("Menus/Canvas_InBattle"));
		inbattleMenuManager = inbattleMenuCanvasGO.GetComponent<InbattleMenuManager>();
		inbattleMenuCanvasGO.GetComponent<Canvas>().worldCamera = Camera.main;
		inbattleMenuCanvasGO.GetComponent<Canvas>().sortingLayerName = "GUI";

		intelToken = (GameObject)Instantiate(Resources.Load("Equipment/Intel", typeof(GameObject)), Vector3.zero, this.gameObject.transform.rotation);
		intelToken.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0);

		exitArrows = GameObject.FindGameObjectsWithTag("ExitArrow");
		foreach( GameObject arrow in exitArrows){
			arrow.transform.localScale = Vector3.zero;
		}

		endTurnDelayTimer = endTurnDelay;

		loadMapTiles();
		
		selectionMarker = (GameObject)Instantiate(Resources.Load("Tiles/Highlight", typeof(GameObject)), this.gameObject.transform.position, this.gameObject.transform.rotation);
		selectionMarker.transform.localScale = Vector3.zero;
		selectionMarker.transform.parent = this.gameObject.transform;
		//selectionMarker.renderer.material.color = selectionColor;

		//populateUnits();

		iTween.CameraFadeAdd(cameraFadeTexture,200);

		spotlightBracket = (GameObject)Instantiate(Resources.Load("Helpers/tileBracket", typeof(GameObject)), Vector3.zero, Quaternion.identity);
	}

	// Use this for initialization
	void Start () {		
		iTween.CameraFadeFrom(1,1);

		//iTween.AudioFrom( Camera.main.gameObject, 0, 1, 2 );
		StartCoroutine(FadeBGMusic (12f, Fade.In));
		Camera.main.GetComponent<AudioSource>().loop = false;
		Camera.main.GetComponent<AudioSource>().clip = audioClips[0];
		Camera.main.GetComponent<AudioSource>().Play();
		Invoke( "PlayInGameMusic", Camera.main.GetComponent<AudioSource>().clip.length );

		facingArrows = (GameObject)Instantiate(Resources.Load("Helpers/FacingArrows", typeof(GameObject)), Vector3.zero, Quaternion.identity);
		facingArrowsGOs = GameObject.FindGameObjectsWithTag("FacingArrow");
		facingArrows.SetActive( false );

		// Load player clan into scene
		GameObject clanGO = new GameObject();
		clanGO.AddComponent<Clan> ();
		team1Clan = clanGO.GetComponent<Clan> ();
		team1Clan.clanName = "Foot";	// HACK should load from playerprefs or something
		clanGO.name = "CLAN_" + team1Clan.clanName;
		ClanInfoSaveAndLoad.LoadClanIntoBattleScene ( team1Clan );

		// HACK Generate an enemy clan to fight!
		Clan.CreateSimpleRandomEnemyClanInBattle( 2 );
	}

	// Select a facing arrow based on our current direction
	public void SelectFacingArrowCurrentDirection(){
		if( currUnit.transform.rotation.eulerAngles.y < 10f){
			facingArrowsGOs[0].GetComponent<FacingArrow>().Select();
		}

		if( currUnit.transform.rotation.eulerAngles.y > 10f &&  currUnit.transform.rotation.eulerAngles.y < 100f){
			facingArrowsGOs[1].GetComponent<FacingArrow>().Select();
		}

		if( currUnit.transform.rotation.eulerAngles.y > 100f &&  currUnit.transform.rotation.eulerAngles.y < 200f){
			facingArrowsGOs[2].GetComponent<FacingArrow>().Select();
		}

		if( currUnit.transform.rotation.eulerAngles.y > 200f){
			facingArrowsGOs[3].GetComponent<FacingArrow>().Select();
		}
	}

	// For AI, select the closest facing arrow to our targeted attack square
	public void SelectFacingArrowClosestToTargetedTile(){
		float closestDist = 9999f;
		GameObject closestArrow = facingArrowsGOs[0];

		foreach( GameObject arrow in facingArrowsGOs ){
			float tempDist = Vector3.Distance( arrow.transform.position, targetedTile.gameObject.transform.position );
			//Debug.Log(arrow.name + " " + targetedTile.name + "  " + tempDist);
			if( tempDist < closestDist){
				closestDist = tempDist;
				closestArrow = arrow;
			}
		}

		//Debug.Log (closestArrow.name);
		closestArrow.GetComponent<FacingArrow>().Select();
	}

	public void SelectFacingArrowForClosestEnemy(){
		// First find the closest enemy
		Unit closestEnemy = null;
		float shortestDistance =  99999;
		bool decisionMade = false;

		foreach( Unit p in units){
			// Is this a living enemy
			if( p.teamID != currUnit.teamID && !p.outOfAction ){
				float currDist = Vector3.Distance( currUnit.transform.position, p.transform.position );
				
				if( currDist < shortestDistance && !decisionMade){
					closestEnemy = p;
					shortestDistance = currDist;
				}
			}
		}

		// Then face in the direction towards the closest enemy
		float closestDist = 9999f;
		GameObject closestArrow = facingArrowsGOs[0];

		if (closestEnemy != null) {
			foreach( GameObject arrow in facingArrowsGOs ){
				float tempDist = Vector3.Distance( arrow.transform.position, closestEnemy.gameObject.transform.position );
				
				if( tempDist < closestDist){
					closestDist = tempDist;
					closestArrow = arrow;
				}
			}
		}

		closestArrow.GetComponent<FacingArrow>().Select();
	}

	void PlayInGameMusic(){
		Camera.main.GetComponent<AudioSource>().clip = audioClips[1];
		Camera.main.GetComponent<AudioSource>().Play();
		Camera.main.GetComponent<AudioSource>().loop = true;
	}
	
	// Update is called once per frame
	void Update () {
		// Waiting for the next unit's turn
		if( phaseWait ){
			if( unitQueue.Count > 0){
				Unit tempUnit = (Unit)unitQueue.Peek();
				// If the unit in queue is out of action, remove it and move on
				if( tempUnit.outOfAction ){
					unitQueue.Dequeue();
				} else { // If the unit in queue is alive, lets start it's turn
					PhaseToggle( "turn");
					currUnit = (Unit)unitQueue.Peek();
					currUnit.TurnStart();
				}
			}
		}

		// We're in a unit's turn!
		if( phaseTurn){
			if( !endTurnDelayTimerIsRunning && currUnit != null){
				if( currUnit.mainPhase)
					currUnit.TurnUpdate();
			}
			
			if( endTurnDelayTimerIsRunning ) {
				endTurnDelayTimer -= Time.deltaTime;
				
				if( endTurnDelayTimer < 0 ){
					endTurnDelayTimerIsRunning = false;
					endTurnDelayTimer = endTurnDelay;
					nextTurn();
				}
			}


			// PULSE THE TARGETED TILE
			if (targetedTile != null) {
				targetedTile.TargetBob ();

				// Lock the spotlight on the targeted tile if it's not the AI's turn
				if (!spotlightLocked && !currUnit.isAI) {
					spotlightLocked = true;
					spotlightBracket.transform.position = targetedTile.transform.position;
					currElevationTile = targetedTile;
				}
			} else {	// Unlock the spotlight if there's no targeted tile
				if (spotlightLocked)
					spotlightLocked = false;
			}
				
			if( currUnit != null && Input.GetButtonDown( "Fire1" ) && !currUnit.isAI &&  !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()){
				// If we select a new tile, reset the old one
				if( targetedTile != null)
					targetedTile.MouseOut();

				targetedTile = null;
				Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
				RaycastHit hit;
				int tilesLayerMask = 1 << 8;
				int unitsLayerMask = 1 << 10;
				
				if (currUnit.moving) {
					if( Physics.Raycast( ray, out hit, Mathf.Infinity, tilesLayerMask ) ){
						Tile tempTargetTile = hit.collider.GetComponent<Tile>();

						if (tempTargetTile.highlighted) {
							spotlightLocked = false;
							targetedTile = tempTargetTile;
							currUnit.confirmingAction = true;
						}
					}
				}
				if (currUnit.attacking && !currUnit.aoeAttack) {
					if( Physics.Raycast( ray, out hit, Mathf.Infinity, unitsLayerMask ) ){
						Tile tempTargetTile = map [(int)hit.collider.GetComponent<Unit> ().gridPosition.x] [(int)hit.collider.GetComponent<Unit> ().gridPosition.y];

						if( tempTargetTile.highlighted){
							spotlightLocked = false;
							targetedTile = tempTargetTile;
							targetUnit = targetedTile.occupyingUnit;
							inbattleMenuManager.UpdateTargetUnitInfoPanel();
							currUnit.confirmingAction = true;
							currUnit.FaceTarget ();
						}
						else
							targetedTile = null;
					}
				}
				
				if (currUnit.healing || currUnit.recruiting) {
					if( Physics.Raycast( ray, out hit, Mathf.Infinity, unitsLayerMask ) ){
						Tile tempTargetTile = map [(int)hit.collider.GetComponent<Unit> ().gridPosition.x] [(int)hit.collider.GetComponent<Unit> ().gridPosition.y];
						if( tempTargetTile.highlighted ){
							bool conf = false;
							if (currUnit.recruiting) {
								if (tempTargetTile.occupyingUnit.teamID != currUnit.teamID) {
									conf = true;
								}
							} else {
								conf = true;
							}

							if (conf) {
								spotlightLocked = false;
								targetedTile = tempTargetTile;
								targetUnit = targetedTile.occupyingUnit;
								inbattleMenuManager.UpdateTargetUnitInfoPanel();
								currUnit.confirmingAction = true;
								currUnit.FaceTarget ();
							}
						}
					}
				}
				
				if (currUnit.attacking && currUnit.aoeAttack) {
					if( Physics.Raycast ( ray, out hit, Mathf.Infinity, tilesLayerMask ) ){
						//AoEAttack();
						foreach( Tile t in currHighlightedTiles){
							if( t.occupied > -1 && t.aoeHighlighted){	// If any highlighted tile is occupied, we can continue the attack
								currUnit.confirmingAction = true;
								break;
							}
						}
					}
				}
			}
		}
	}

	public void PhaseToggle( string newPhase ){
		if( newPhase == "cutscene"){
			phaseTurn = false;
			phaseWait = false;
			phaseCutscene=true;
		}

		if( newPhase == "turn"){
			phaseWait = false;
			phaseCutscene=false;
			phaseTurn = true;
		}

		if( newPhase == "wait"){
			phaseCutscene=false;
			phaseTurn=false;
			phaseWait=true;
		}
	}

	public void CancelAction(){
		currUnit.confirmingAction = false;
		currUnit.currItem = null;

		if (targetedTile != null) {
			if( targetedTile.occupyingUnit != null)
				targetedTile.occupyingUnit.losTooltipActive = false;
		}
		FocusOnPos( currUnit.transform.position, 1);
		
		targetedTile = null;
	}

	public void ConfirmAction(){
		if (somebodyWon) {
			SceneManager.LoadScene ("world_map");
		}

		currUnit.confirmingAction = false;

		// Toggle the target unit panel if necessary
		if( inbattleMenuManager.targetUnitInfoPanelOn){
			inbattleMenuManager.ToggleTargetUnitInfoPanel();
		}

		if (currUnit.moving) {
			Debug.Log ("mving");
			moveCurrentUnit( targetedTile);
		}
		if (currUnit.attacking && !currUnit.aoeAttack) {
			Debug.Log ("attack non-aoe");
			attackWithCurrentUnit(targetedTile);
		}
		
		if (currUnit.healing) {
			Debug.Log ("heal");
			targetedTile.occupyingUnit.HealFromDown();
		}

		if (currUnit.recruiting) {
			Debug.Log ("Attempt to recruit!");
			targetedTile.occupyingUnit.AttemptToRecruit ();
		}
		
		if (currUnit.attacking && currUnit.aoeAttack) {	// TODO check if there's a valid target before showing the confirm button
			Debug.Log ("aoe");
			AoEAttack();
		}

		if (targetedTile != null) {
			if( targetedTile.occupyingUnit != null)
				targetedTile.occupyingUnit.losTooltipActive = false;
		}

		if (currUnit.currItem) {	// If we just used an item, use it!
			currUnit.currItem.Use ();
		}

		// End turn but select facing direction!
		if (currUnit.selectingFacing && currUnit.decisionPoints < 1) {
            Debug.Log ("endturn");
            currUnit.EndTurn();
		}

		if( currUnit.choseToWait ){
            Debug.Log ("endturn");
            currUnit.EndTurn();

		}

		currUnit.SelectingFacing( false );
	}

	void LateUpdate(){
		selectionMarker.transform.rotation = Quaternion.identity;
	}


	public void FocusOnPos( Vector3 pos, float time ){
		iTween.MoveTo (Camera.main.GetComponent<CameraControl> ().cameraRotPoint, pos, time);
	}
	
	public void nextTurn() {
		Debug.Log ("next turn");
		// Restart their progress bar after their turn
		currUnit.progressIcon.GetComponent<TestProgress> ().EndTurn ();
		currUnit = null;

		currentTurn++;
		PhaseToggle("wait");

		if( unitQueue.Count > 0)
			unitQueue.Dequeue ();
		if (unitQueue.Count < 1) {
		}
	}

	// Used for showing the move distance of a moused-over unit
	public void ShowMovementDistance( Vector2 originLocation, float distance ){
		showingMoveRange = true;
		List <Tile> highlightedTiles = new List<Tile>();

		highlightedTiles = TileHighlight.FindHighlight(map[(int)originLocation.x][(int)originLocation.y], (int)distance);

		foreach (Tile t in highlightedTiles) {
			// Don't highlight tiles that are marked as hoppable! HACK possibly still have issues with this, but in TilePath, I don't mark walkable tiles as hoppable
			if( t.runDestination && !t.hoppable ){
				t.HighlightTile( redColor );
			} else {
				if( !t.hoppable)
					t.HighlightTile( blueColor );
			}
		}

		SetUnitOccupyingSpaces();
	}
	
	public void highlightTilesAt(Vector2 originLocation, Color highlightColor, float distance) {
		List <Tile> highlightedTiles = new List<Tile>();
		// If we're moving or healing
		if( currUnit.moving || currUnit.healing || currUnit.recruiting){
			highlightedTiles = TileHighlight.FindHighlight(map[(int)originLocation.x][(int)originLocation.y], (int)distance);
		}

		// If this is a ranged attack
		if( currUnit.attacking && !currUnit.meleeAttacking && !currUnit.aoeAttack )
			TileHighlight.FindLineOfSightHighlight( currUnit.transform.root.gameObject,
			                                        currUnit.equippedWeapon.shortRange,
			                                        currUnit.equippedWeapon.longRange );

		// If this is a grenade attack
		if( currUnit.attacking && !currUnit.meleeAttacking && currUnit.aoeAttack )
			TileHighlight.FindGrenadeLobHighlight( currUnit.transform.root.gameObject,
			                                       currUnit.equippedWeapon.shortRange,
			                                       currUnit.equippedWeapon.longRange );

		// If we're meleeing
		if( currUnit.attacking && currUnit.meleeAttacking){
			TileHighlight.FindLineOfSightHighlight( currUnit.transform.root.gameObject,
			                                       currUnit.equippedWeapon.shortRange,
			                                       currUnit.equippedWeapon.longRange );
		}

		if(currUnit.moving || currUnit.healing || currUnit.meleeAttacking || currUnit.recruiting) {	// If we're moving or healing
			foreach (Tile t in highlightedTiles) {
				// Don't highlight tiles that are marked as hoppable! HACK possibly still have issues with this, but in TilePath, I don't mark walkable tiles as hoppable
				if( t.runDestination && !t.hoppable ){
					t.HighlightTile( redColor );
				} else {
					if( !t.hoppable)
						t.HighlightTile( highlightColor );
				}
			}

			SetUnitOccupyingSpaces();
		}
	}
	
	public void removeTileHighlights() {
		for (int i = 0; i < mapSize; i++) {
			for (int j = 0; j < mapSize; j++) {
				map [i] [j].walkable = false;
				map [i] [j].hoppable = false;

				if( map[i][j].aoeHighlighted){
					map[i][j].UnhighlightTile();
					map[i][j].AoEUnhighlight();

				}
				if( map[i][j].highlighted) {
					map[i][j].UnhighlightTile();
				}

			}
		}

		currHighlightedTiles.Clear();
		showingMoveRange = false;
	}

	// Moving a unit that has been knocked back
	public void knockbackAttackedUnit( Unit victim, Unit attackingUnit, Tile destTile ){
		iTween.MoveTo(victim.gameObject, iTween.Hash("position", destTile.transform.position + 1.0f * Vector3.up, 
		                                           "easeType", "easeOutQuart", 
		                                           "time", victim.moveAnimationSpeed,
		                                           "orienttopath", false,
		                                           "axis", "y"));

		// Clear the occupation of the tile victim is leaving
		Tile vacatedTile = map[(int)victim.gridPosition.x][(int)victim.gridPosition.y];
		vacatedTile.occupied = -1;
		vacatedTile.occupyingUnit = null;
		
		victim.gridPosition = destTile.gridPosition;
		victim.SetOccupiedToTeamID();

		// Kill the unit if it falls into deep water!
		if (destTile.deepWater) {
			ShowMessageInWorld ("BLUB BLUB!", victim.transform.position, Color.red);
			victim.OutOfAction ();
		}

		if (!victim.outOfAction) {	// If the unit didn't die from falling into water, check for fall damage
			// FALL DAMAGE
			float fallDist = vacatedTile.elevation - destTile.elevation;
			if (fallDist >= victim.jumpHeight * 2f) {	// If the victim falls more than twice it's jump height, it takes damage
				float dmgToTake = fallDist * 10f;
				victim.TakeDamage ((int)dmgToTake, attackingUnit);
				ShowMessageInWorld( ("FELL!\n" + "-" + (int)dmgToTake).ToString() + " HP", victim.transform.position, Color.red);
			}
		}
	}
 	
	public void moveCurrentUnit(Tile destTile) {
		if (destTile.highlighted && !destTile.impassable && !destTile.deepWater && destTile.occupied < 0) {
			currUnit.selectingTarget = false;
			removeTileHighlights();

			foreach(Tile t in TilePathFinder.FindPath(map[(int)currUnit.gridPosition.x][(int)currUnit.gridPosition.y],destTile)) {
				currUnit.positionQueue.Add(map[(int)t.gridPosition.x][(int)t.gridPosition.y].transform.position + 1.0f * Vector3.up);
				//Debug.Log("(" + currUnit.positionQueue[currUnit.positionQueue.Count - 1].x + "," + currUnit.positionQueue[currUnit.positionQueue.Count - 1].y + ")");
			}

			// Clear the occupation of the tile we're leaving
			map[(int)currUnit.gridPosition.x][(int)currUnit.gridPosition.y].occupied = -1;
			map[(int)currUnit.gridPosition.x][(int)currUnit.gridPosition.y].occupyingUnit = null;

			currUnit.gridPosition = destTile.gridPosition;

			removeTileHighlights();
		} else {
			Debug.Log ( destTile.gridPosition );
		}
	}
	
	public void attackWithCurrentUnit(Tile destTile) {
		if (destTile.highlighted && !destTile.impassable && !destTile.deepWater) {
			Unit target = destTile.occupyingUnit;
			
			if (target != null) {
				if( HaveLineOfSight( currUnit.transform.position, target.gameObject ) || currUnit.isAI ) {
					removeTileHighlights();
                    target.BeingAttacked(currUnit);

				} else {
					Debug.Log ("Don't have line of sight!");
				}


			}
		} else {
			Debug.Log ( destTile.gridPosition );
			Debug.Log ("destination invalid");
			Debug.Log (currHighlightedTiles.Count );
		}
	}

	public bool HaveLineOfSight( Vector3 attackerPos, GameObject target ) {
		// Update unit's weapon range modifier as well HACK
		if ( map[(int)currUnit.gridPosition.x][(int)currUnit.gridPosition.y].shortRange ) {	// Is the attacker shooting from short range?
			currUnit.weaponRangeModifier = currUnit.equippedWeapon.shortToHit;
		} else {	// If not, they're shooting from long range
			currUnit.weaponRangeModifier = currUnit.equippedWeapon.longToHit;
		}

		Vector3 shooterBarrelPos = new Vector3( attackerPos.x,	attackerPos.y + 0.2f, attackerPos.z );


		Vector3 targetLowPos = new Vector3( target.transform.position.x,
		                                   target.transform.position.y-.2f,
		                                   target.transform.position.z );

		Vector3 targetHighPos = new Vector3( target.transform.position.x,
		                                    target.transform.position.y + .2f,
		                                    target.transform.position.z );
		Ray rayLow = new Ray( shooterBarrelPos, ( targetLowPos - shooterBarrelPos ).normalized);
		Ray rayMid = new Ray( shooterBarrelPos, ( target.transform.position - shooterBarrelPos ).normalized);
		Ray rayHigh = new Ray( shooterBarrelPos, ( targetHighPos - shooterBarrelPos ).normalized);
		//Debug.DrawRay ( attacker.transform.position, ( targetLowPos - attacker.transform.position ), Color.green);
		RaycastHit hitLow;
		RaycastHit hitMid;
		RaycastHit hitHigh;
		int tilesLayer = 8;
		int unitsLayer = 10;
		int shieldsLayer = 11;
		int layerMask = (1<< tilesLayer)|(1<<unitsLayer)|(1<<shieldsLayer);

		//Debug.DrawLine( shooterBarrelPos, target.transform.position);

		if( Physics.Raycast(rayLow, out hitLow, Mathf.Infinity, layerMask) ) {
			if (hitLow.collider.gameObject != target) {

				if( Physics.Raycast(rayMid, out hitMid, Mathf.Infinity, layerMask) ) {
					if (hitMid.collider.gameObject != target) {
						if( Physics.Raycast(rayHigh, out hitHigh, Mathf.Infinity, layerMask) ) {
							if (hitHigh.collider.gameObject != target) {	// No line of sight
								return false;
							} else {	// in cover
								//Debug.Log("cover");
								
								target.GetComponent<Unit>().coverModifier = -30;	// in cover
								return true;
							}
						}


					} else {	// partial cover 
						//Debug.Log("partial cover");

						target.GetComponent<Unit>().coverModifier = -15;	// in partial cover
						return true;
					}
				}
			} else {
				//Debug.Log("no cover");
				target.GetComponent<Unit>().coverModifier = 0;	// in the open

				return true;
			}
		}

		return false;
	}

	public float DamageToDeal ( Unit attackingUnit )
	{
		float dmgToDeal = attackingUnit.equippedWeapon.strength;

		if( targetUnit.equipmentGOs[2].GetComponent<Equipment>().category == EquipmentCategory.ARM_Light ){ // if it's light armor, vary dmg by 5% off
			dmgToDeal -= dmgToDeal * Random.Range(0f, 0.05f);
		} else if( targetUnit.equipmentGOs[2].GetComponent<Equipment>().category == EquipmentCategory.ARM_Medium ){	// If its medium armor, 10%
			dmgToDeal -= dmgToDeal * Random.Range(0f, 0.1f);
		} else if( targetUnit.equipmentGOs[2].GetComponent<Equipment>().category == EquipmentCategory.ARM_Heavy ){	// Heavy armor reduce dmg up to 20%

			dmgToDeal -= dmgToDeal * Random.Range(0f, 0.2f);

			// If the attacker is using edged or projectile weapon against heavy armor, it may glance off
			if( attackingUnit.equippedWeapon.type == "e" || attackingUnit.equippedWeapon.type == "p" )
			{
				if( Random.Range(0, 10) > 4 ) {	// 50% chance of glancing blow
					dmgToDeal = 0;
					//Debug.Log ("GLANCING BLOW!");
					ShowMessageInWorld("GLANCING BLOW", transform.position, Color.red);
				}
			}
		} else {
			Debug.Log ("Armor type wrong on: " + targetUnit.gameObject.name);
		}

		// HACK If the attacking unit is behind us, DOUBLE DAMAGE for now
		if( GameManager.CheckRelativePos( attackingUnit.transform, transform ) == -1){
			dmgToDeal *= 2f;
		}

		// If the attacking unit is to the side of us, 1.5x dmg
		if( GameManager.CheckRelativePos( attackingUnit.transform, transform ) == 0){
			dmgToDeal *= 1.5f;
		}

		return dmgToDeal;
	}

	public void SetUnitOccupyingSpaces() {
		foreach( Unit p in units) {	// Reset the tile colors under the units
			p.SetOccupiedToTeamID();
		}
	}

	public void AoEAttack() {
		// Move the trigger away to unhighlight tiles
		aoeTrigger.transform.position = new Vector3(9999, 9999, 9999);

		if( aoeTargets.Count > 0){
			foreach( Unit pB in aoeTargets) {	// Check all the marked units and AoE em
				targetUnit = pB;
                pB.BeingAttacked(currUnit);
			}

			currUnit.equippedWeapon.totalAmmoRemaining--;
			removeTileHighlights();

            StartCoroutine( currUnit.CompletedAction() );
		}

		aoeTargets.Clear();
	}


	public void Reload(){
		Application.LoadLevel(0);
	}

	IEnumerator WaitToEndTurn( float waitTime){
		yield return new WaitForSeconds( waitTime);
		Debug.Log ("WaitToEndTurn");
		currUnit.EndTurn();
	}

	enum Fade {In, Out};
	IEnumerator FadeBGMusic (float timer, Fade fadeType) {
		float i = 0.0F;
		float step = 1.0F/timer;
		
		while (i <= 1.0F) {
			i += step * Time.deltaTime;
			Camera.main.GetComponent<AudioSource>().volume =  (1-Mathf.Cos(i*Mathf.PI));
			yield return new WaitForSeconds(step * Time.deltaTime);
		}
	}

	public void CheckForWinner() {
		int previousTeamID = -1;
		bool somebodyWonTemp = true;

		foreach( Unit p in units) {
			if( !p.outOfAction ) {

				if (previousTeamID == -1)	// If this is the first one we're checking that's alive
					previousTeamID = p.teamID;

				if( p.teamID != previousTeamID ) {	// There's an opponent left, keep playing!
					somebodyWonTemp = false;
					break;
				}
			}
		}

		if ( somebodyWonTemp ) {
			// Give each unit on the team +60 XP
			foreach (Unit teamUnit in units) {
				if (teamUnit.teamID == previousTeamID) {
					teamUnit.xpForThisBattle += 60;

					teamUnit.xp += teamUnit.xpForThisBattle;

					if (teamUnit.xp >= 100) {	// LEVEL UP!
						teamUnit.level++;
						teamUnit.xp -= 100;

						// INCREASE STATS
						teamUnit.totalHP += teamUnit.currJob.hp;
						if (Random.Range (1, 100) > teamUnit.currJob.speed) {
							teamUnit.agility += 1;
						}
						teamUnit.meleeSkill += teamUnit.currJob.wepAttack;
						teamUnit.ballisticSkill += teamUnit.currJob.magicAttack;
					}

					teamUnit.AddAbilPointsToEquippedAbils (300);	// Add Ability Points to each unit's equipped abilities
				}
			}

			Debug.Log ("Team " + previousTeamID + " won!");
			somebodyWon = true;
			winnerID = previousTeamID;
			PhaseToggle ("cutscene");

			inbattleMenuManager.ToggleResultsPanel ();

			// Add items to clan TODO add screen showing collected loot
			AddCollectedItemsToPlayerClan();

			// Save clan results when we win
			ClanInfo.SaveClanData( team1Clan );
		}
	}

	void loadMapTiles() {	// Loads the tiles in the scene into the map list
		GameObject[] tileGOs;
		tileGOs = GameObject.FindGameObjectsWithTag("Tile");
		mapSize = (int)Mathf.Sqrt( (float)tileGOs.Length );
		//Debug.Log (mapSize);

		map = new List<List<Tile>>();
		for (int i = 0; i < mapSize; i++) {
			List <Tile> row = new List<Tile>();
			for (int j = 0; j < mapSize; j++) {
				foreach( GameObject t in tileGOs ) {
					if( (int)t.transform.position.x == i && (int)t.transform.position.z == j ) {
						Tile tile = t.GetComponent<Tile>();
						tile.gridPosition = new Vector2(i, j);
						tile.elevation = t.transform.position.y;
						tile.name = "Tile: " + i + "," + j;

						row.Add (tile);
					}
				}
			}
			map.Add(row);
		}

		// Generate neighbors after the list is filled
		for (int k = 0; k < mapSize; k++) {
			for (int m = 0; m < mapSize; m++) {
				map[k][m].generateNeighbors();
			}
		}
	}

	public void ShowMessageInWorld( string message, Vector3 pos, Color col ){
		GameObject newText = new GameObject();
		newText.transform.position = pos;
		newText.name = message + " Text Object";
		TextMesh textMesh = newText.AddComponent<TextMesh>();
		textMesh.text = message;
		textMesh.characterSize = .1f;
		textMesh.anchor = TextAnchor.MiddleCenter;
		textMesh.fontSize = 128;
		Font inWorldFont = (Font)Resources.Load("InWorldFont");
		textMesh.font = inWorldFont;
		textMesh.color = col;
		textMesh.GetComponent<Renderer>().sharedMaterial = inWorldFont.material;
		textMesh.GetComponent<Renderer>().sortingLayerName = "GUI";

		newText.AddComponent<InWorldText>();
	}

	public static int CheckRelativePos( Transform myPos, Transform posToCheck ){	// -1: Behind, 0: To the side, 1: In front
		int inFront = 1; // Assume we're in front

		Vector3 relativePos = posToCheck.transform.InverseTransformPoint( myPos.transform.position );

		
		if (relativePos.z < 0.01f && relativePos.z > -0.01f) {
			// To either side
			inFront = 0;
			//Debug.Log ( "SIDE ATTACK" );
		}
		if( relativePos.z < 0){
			// Behind
			inFront = -1;
			//Debug.Log ( "BACK ATTACK" );
		}

		return inFront;
	}
	
	void generateMap() {
		GameObject tileContainer = new GameObject();
		tileContainer.name = "tileContainer";

		map = new List<List<Tile>>();
		for (int i = 0; i < mapSize; i++) {
			List <Tile> row = new List<Tile>();
			for (int j = 0; j < mapSize; j++) {
				Tile tile = ((GameObject)Instantiate(Resources.Load("Tiles/Tile", typeof(GameObject)), new Vector3(i,0, j), Quaternion.Euler(new Vector3()))).GetComponent<Tile>();
				tile.gridPosition = new Vector2(i, j);

				tile.transform.position = new Vector3(tile.transform.position.x, tile.elevation*.2f, tile.transform.position.z);

				row.Add (tile);
				tile.transform.parent = tileContainer.transform;
			}
			map.Add(row);
		}
	}

	public void AddUnit( Unit thisUnit ){
		thisUnit.gridPosition = new Vector2( (int)thisUnit.gameObject.transform.position.x, (int)thisUnit.gameObject.transform.position.z);
		thisUnit.SetTeam( thisUnit.teamID );
		thisUnit.SetOccupiedToTeamID();

		units.Add (thisUnit);
	}

	public void AddCollectedItemsToPlayerClan(){
		foreach (string s in itemsAndEquipmentCollected) {
			if (s.Substring (0, 3) == "ITM") {	// If it's an item 
				if (team1Clan.itemStrings.Contains (s)) {	// If we already have these items, just add to the number held
					int itemIndex = team1Clan.itemStrings.IndexOf (s);
					team1Clan.itemsHeld [itemIndex]++;
				} else { // If we don't already have this item, add it to the item strings, held and equipped lists
					team1Clan.itemStrings.Add (s);
					team1Clan.itemsHeld.Add (1);	// Holding one...
					team1Clan.itemsEquipped.Add (0);	// But it's not equipped!
				}
			} else {	// If it's not an item, it's equipment
				if (team1Clan.equipNames.Contains (s)) {	// If we already have this equipment, just add to the number held
					int equipIndex = team1Clan.equipNames.IndexOf (s);
					team1Clan.equipHeld [equipIndex]++;
				} else {	// If we don't already have this equipment, add it to the equipment strings, held, and equipped lists
					team1Clan.equipNames.Add( s);
					team1Clan.equipHeld.Add (1);
					team1Clan.equipEquipped.Add (0);
				}
			}
		}
	}
}
