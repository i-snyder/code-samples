/*
	Code for a turn-based tactics game by Ian Snyder
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AI : MonoBehaviour {
	public Unit aiUnit;

	// Timer for start turn delay
	public float startDelay = 1f;
	private float startDelayTimer;
	public bool startDelayTimerIsRunning = false;

	// Timer for decision delay
	public float decisionDelay = 1f;
	private float decisionDelayTimer;
	public bool decisionDelayTimerIsRunning = false;

	// Timer for showing AoE and possibly other things that should be delayed
	public float showingDelay = 1f;
	private float showingDelayTimer;
	public bool showingDelayTimerIsRunning = false;

	private Unit unitToHeal;
	public Unit unitToAttack;

	private string decisionString = "";

	public int aiState = 0;

	void Awake(){
		decisionDelayTimer = decisionDelay;
		startDelayTimer = startDelay;
		showingDelayTimer = showingDelay;
	}

	// Use this for initialization
	void Start () {
		aiUnit = gameObject.GetComponent<Unit>();
	}

	public void AIUpdate () {
		//Debug.Log (aiState);

		switch ( aiState )
		{
		case 0:	// The beginning of the AI turn
			// Delay starting the turn so it isn't starting to make decisions instantly
            if( startDelayTimerIsRunning   && !aiUnit.completedActionDelayRunning) {
				startDelayTimer -= Time.deltaTime;
				
                if( startDelayTimer < 0){
					startDelayTimerIsRunning = false;
					startDelayTimer = startDelay;
					
					ChooseAction();
					aiState++;
				}
			}
			break;
		case 1:	// Make a decision
			//Debug.Log ("State 1");
            if( decisionDelayTimerIsRunning && !aiUnit.completedActionDelayRunning ) {
				decisionDelayTimer -= Time.deltaTime;

				//Debug.Log(decisionString);

				// AI to face enemy with AoE melee
				if( decisionDelayTimer < decisionDelayTimer*.9 ){
					if( aiUnit.selectingFacing ){
						
						if( !GameManager.instance.targetedTile.aoeHighlighted ){
							Debug.Log ("selectfacing");
							GameManager.instance.SelectFacingArrowClosestToTargetedTile();
						}
					}
				}
				
				if( decisionDelayTimer < 0 ){
					decisionDelayTimerIsRunning = false;
					decisionDelayTimer = decisionDelay;

					Debug.Log(decisionString);

					if( decisionString == "Crawl" ){
						MakeCrawlDecision();
					} 
					
					if( decisionString == "Heal" ){
						MakeHealDecision();
					}
					
					if( decisionString == "Attack" ){
						Debug.Log ( "Making attack decision");
						MakeAttackDecision();
					}

					if (decisionString == "MoveAwayFromEnemies") {
						MoveAwayFromEnemies ();
					}
					
					if( decisionString == "Move" ){
						MakeMoveDecision();
					}
					
					aiUnit.confirmingAction = true;
					decisionDelayTimer = decisionDelay;
				}
			}
			break;
		}

		if( aiUnit.confirmingAction ){
			showingDelayTimer -= Time.deltaTime;

			// Face closest enemy
			if( aiUnit.decisionPoints < 1 && showingDelayTimer < showingDelay*.7){
				if( GameManager.instance.targetedTile ){
					if (!GameManager.instance.targetedTile.aoeHighlighted) {
						Debug.Log ("selectfacing");
						GameManager.instance.SelectFacingArrowForClosestEnemy();
					}
				}
			}
			
			if( showingDelayTimer < 0){
				Debug.Log("Confirm");
				GameManager.instance.ConfirmAction();
				showingDelayTimer = showingDelay;
				aiUnit.confirmingAction = false;
				aiState = 0;
			}
			
		}
	}

	// First, decide what our first DP will be spent on
	public void ChooseAction(){
		bool choseAction = false;
		decisionString = "";

		// Firstly, if we're down, either wait for a neighbor to heal us or crawl towards a friendly that isn't down
		if( aiUnit.down && !choseAction){
			List <Unit> neighborAllies = CheckForAllyNeighbors();
			int numOfHealers = 0;
			Debug.Log ("I'm down");
			// We have a friendly neighbor, make sure they're not also down before we decide to stay here
			if( neighborAllies.Count > 0 ) {
				foreach( Unit p in neighborAllies){
					if( p.down){
						continue;
					} else {
						Debug.Log ("I have a friendly neighbor, so I'll end my turn");
						aiUnit.EndTurn();
						choseAction = true;
						numOfHealers++;
						break;
					}
				}
			}

			// We have no allied neighbors, or they're all down, so move!
			if( neighborAllies.Count == 0 || numOfHealers == 0){
				Debug.Log ("I'm crawling awaaaaaay");
				decisionString = "Crawl";
				choseAction = true;
				decisionDelayTimerIsRunning = true;
				aiUnit.SelectCrawl();
			}

			Debug.Log ("End of check to crawl");
		}

		// Keep going if we haven't chosen an action yet
		if( !choseAction ){
			// If we have the ability to heal someone, check for that first
			if( aiUnit.CanHeal()){
				if( CheckToHeal() ){
					decisionString = "Heal";
					choseAction = true;
				}
			}
		}

		// If we are using a ranged weapon, it's a priority to move out of melee range of enemy units
		if (!aiUnit.equippedWeapon.melee) {	// I have a ranged weapon equipped
			Tile myTile = GameManager.instance.map[(int) aiUnit.gridPosition.x][(int)aiUnit.gridPosition.y];
			List<Unit> enemyNeighbors = CheckForEnemyNeighbors (myTile);

			if (enemyNeighbors.Count > 0) {	// If we have an enemy in a neighboring tile
				Debug.Log("Chose move away from enemies because we're in melee range, and we have a ranged weapon equipped.");
				decisionString = "MoveAwayFromEnemies";
				choseAction = true;
				decisionDelayTimerIsRunning = true;
				aiUnit.SelectMove();
			}
		}

		// Add some randomization so they don't just sit in once spot shooting all the time
		int randomizeChoice = Random.Range( 0, 10 );

		if( randomizeChoice > 1 ){
			if( !choseAction ){
				if( CheckToAttack() ){
					Debug.Log ( "Chose attack action");
					decisionString = "Attack";
					choseAction = true;
				}
			}
		} else {
			if( !choseAction){
				// For now just test random moving
				decisionString = "Move";
				choseAction = true;
				decisionDelayTimerIsRunning = true;
				aiUnit.SelectMove();
			}
		}


		if( !choseAction){
			// For now just test random moving
			decisionString = "Move";
			decisionDelayTimerIsRunning = true;
			aiUnit.SelectMove();
		}
	}

	public void MakeHealDecision(){
		unitToHeal.HealFromDown();

		if( aiUnit.decisionPoints > 0 ){
			startDelayTimerIsRunning = true;
		}
	}

	public void MakeAttackDecision(){
		// If we didn't just reload
		if( aiUnit.attacking ){
			if( aiUnit.aoeAttack ){
				if( aiUnit.meleeAttacking ){
					if( !GameManager.instance.targetedTile.aoeHighlighted ){
						Debug.Log ("selectfacing");
						GameManager.instance.SelectFacingArrowClosestToTargetedTile();
					}
				} else {
					GameManager.instance.targetedTile.AoEHover();
				}

			}
		}

		if( aiUnit.decisionPoints > 0 ){
			startDelayTimerIsRunning = true;
		}
	}

	public void MakeCrawlDecision(){
		float shortestDistance =  99999;
		Tile targetTile = GameManager.instance.map[0][0];
		bool targetFound = false;
		bool decisionMade = false;
		//Debug.Log("MakeCrawlDecision");

		// Check the distance from each available movement tile to each non-DOWN friendly unit, move to closest friendly
		foreach( Tile t in GameManager.instance.currHighlightedTiles ){
			// If the tile isn't occupied, check it against the units
			if( t.occupied < 0 ){
				foreach( Unit p in GameManager.instance.units){
					//Debug.Log(p.unitName);
					// Is this an ally that can heal me?
					if( p.teamID == aiUnit.teamID && !p.down && !p.outOfAction ){
						//Debug.Log("if( p.teamID == aiUnit.teamID && !p.down && !p.outOfAction ){");
						// If we are a neighbor tile to the unit, move to that tile
						Tile goalUnitTile = GameManager.instance.map[ (int)p.gridPosition.x ][ (int)p.gridPosition.y];
						if( goalUnitTile.neighbors.Contains( t ) ) {
							//Debug.Log("targetTile = t;");
							targetTile = t;
							shortestDistance = -10;
							targetFound = true;
							decisionMade = true;
							GameManager.instance.FocusOnPos(targetTile.transform.position, 1 );

							GameManager.instance.targetedTile = targetTile;

							break;
						}
						
						float currDist = Vector3.Distance( t.transform.position, p.transform.position );
						
						if( currDist < shortestDistance && !decisionMade){
							//Debug.Log("targetTile = t;");
							targetTile = t;
							shortestDistance = currDist;
							targetFound = true;
						}
					} 
				}
			}

			if( decisionMade ){
				//Debug.Log("break");
				break;
			}
		}

		if( !targetFound ) {  // If we're the last unit, just pick a random spot to move to
			//Debug.Log("If we're the last unit...");
			int randTile = Random.Range( 0, GameManager.instance.currHighlightedTiles.Count-1 );
			
			//Debug.Log(GameManager.instance.currHighlightedTiles.Count);
			
			// If it's valid, move, otherwise do this again until you find one that's not occupied
			if( GameManager.instance.currHighlightedTiles.Count > 0 && GameManager.instance.currHighlightedTiles[ randTile ].occupied < 0 && !GameManager.instance.currHighlightedTiles[ randTile ].impassable && !GameManager.instance.currHighlightedTiles[ randTile ].deepWater) {
				//Debug.Log ( GameManager.instance.currHighlightedTiles[ randTile ].gridPosition );
				decisionMade = true;
				GameManager.instance.FocusOnPos(targetTile.transform.position, 1 );

				GameManager.instance.targetedTile = GameManager.instance.currHighlightedTiles[ randTile ];

			} else{
				//Debug.Log("decisionMade = true;");
				decisionMade = true;
				MakeCrawlDecision();
			}
			
		}

		if( !decisionMade ){
			//Debug.Log( "crawltarget: " + targetTile.gridPosition );
			//Debug.Log( "num of highlighted tiles: " + GameManager.instance.currHighlightedTiles.Count );
			GameManager.instance.FocusOnPos(targetTile.transform.position, 1 );

			GameManager.instance.targetedTile = targetTile;

		}
	}

	public void MakeMoveDecision(){
		bool decisionMade = false;

		// MOVE TO HEAL
		if( aiUnit.CanHeal() ){
			// check if any allies need healing
			foreach( Unit p in GameManager.instance.units){
				if( p.teamID == aiUnit.teamID && p.down && !p.outOfAction ){
					decisionMade = true;
					MoveToHeal();
					break;
				}
			}
		}
		
		if( !decisionMade ){
			decisionMade = true;

			int randomizeChoice = Random.Range( 2, 10 );    // HACK removing random chance for testing

			// We should usually move to attack, but sometimes just move randomly
			if( randomizeChoice > 1 ){
				MoveToAttack();
			} else {
				int randTile = Random.Range( 0, GameManager.instance.currHighlightedTiles.Count-1 );
				
				// If it's valid, move, otherwise do this again until you find one that's not occupied
				if( GameManager.instance.currHighlightedTiles.Count > 0 && GameManager.instance.currHighlightedTiles[ randTile ].occupied < 0 && !GameManager.instance.currHighlightedTiles[ randTile ].impassable) {
					//Debug.Log ( GameManager.instance.currHighlightedTiles[ randTile ].gridPosition );
					GameManager.instance.FocusOnPos(GameManager.instance.currHighlightedTiles[ randTile ].transform.position, 1 );

					GameManager.instance.targetedTile = GameManager.instance.currHighlightedTiles[ randTile ];
				} else{
					MakeMoveDecision();
				}
			}
		}
	}

	public void MoveToHeal(){
		float shortestDistance =  99999;
		Tile targetTile = GameManager.instance.map[0][0];
		bool decisionMade = false;
		
		// Check the distance from each available movement tile to each non-DOWN friendly unit, move to closest friendly
		foreach( Tile t in GameManager.instance.currHighlightedTiles ){
			// If the tile isn't occupied, check it against the units
			if( t.occupied < 0 ){
				foreach( Unit p in GameManager.instance.units){
					// Is this an ally that I can heal?
					if( p.teamID == aiUnit.teamID && p.down && !p.outOfAction ){
						// If we are a neighbor tile to the unit, move to that tile
						Tile goalUnitTile = GameManager.instance.map[ (int)p.gridPosition.x ][ (int)p.gridPosition.y];
						if( goalUnitTile.neighbors.Contains( t ) ) {
							targetTile = t;
							shortestDistance = -10;
							decisionMade = true;
							GameManager.instance.FocusOnPos(targetTile.transform.position, 1 );

							GameManager.instance.targetedTile = targetTile;

							break;
						}
						
						float currDist = Vector3.Distance( t.transform.position, p.transform.position );
						
						if( currDist < shortestDistance && !decisionMade){
							targetTile = t;
							shortestDistance = currDist;
						}
					} 
				}
			}
			
			if( decisionMade ){
				break;
			}
		}
		
		if( !decisionMade ){
			GameManager.instance.FocusOnPos(targetTile.transform.position, 1 );

			GameManager.instance.targetedTile = targetTile;
		}
	}

	public List <Unit> CheckForAllyNeighbors(){
		Tile myTile = GameManager.instance.map[(int) aiUnit.gridPosition.x][(int)aiUnit.gridPosition.y];
		List <Unit> neighboringAllies = new List<Unit>();


		foreach( Tile t in myTile.neighbors ){
			// If we have a neighboring tile with a friendly in it
			if( t.occupied > -1 && t.occupyingUnit.teamID == aiUnit.teamID ) {
				neighboringAllies.Add( t.occupyingUnit );
			}
		}

		return neighboringAllies;
	}

	public List <Unit> CheckForEnemyNeighbors( Tile tileToCheck = null ){
		List <Unit> neighboringEnemies = new List<Unit>();

		// If we aren't passed a parameter, use our position
		if( tileToCheck == null ){
			tileToCheck = GameManager.instance.map[(int) aiUnit.gridPosition.x][(int)aiUnit.gridPosition.y];
		}
		
		
		foreach( Tile t in tileToCheck.neighbors ){
			// If we have a neighboring tile with a friendly in it
			if( t.occupied > -1 && t.occupyingUnit.teamID != aiUnit.teamID ) {

				// Check that we have line of sight before attacking
				if(	GameManager.instance.HaveLineOfSight( aiUnit.transform.position, t.occupyingUnit.gameObject )){
					neighboringEnemies.Add( t.occupyingUnit );
				} else{
					Debug.Log ("There is a neighboring unit, but we don't have line of sight");
				}
			}
		}
		
		return neighboringEnemies;
	}

	public bool CheckToHeal(){
		bool someoneToHeal = false;
		unitToHeal = null;
		// Are we next to any downed friendly units or < %50 health?
		List <Unit> neighborAllies = CheckForAllyNeighbors();
		// Then heal
		if( neighborAllies.Count > 0 ) {
			foreach( Unit p in neighborAllies){
				if( p.down){
					//HEAL
					unitToHeal = p;
					someoneToHeal = true;

					decisionDelayTimerIsRunning = true;
					break;
				} else {
					continue;
				}
			}
		}

		return someoneToHeal;
	}

	bool CanAttackWith( int weaponNumber ){
		Weapon wepToCheck = aiUnit.weapons[ weaponNumber];
		// First make sure the weapon has ammo
		if( !wepToCheck.melee ){
			if( wepToCheck.totalAmmoRemaining < 1)
				return false;	// We have no ammo, so don't bother with the rest of the checks
		}

		// Checking for enemies that are potentially in range of this weapon
		float range = wepToCheck.longRange;
		List <Unit> potentialTargets = new List<Unit>();
		foreach( Unit p in GameManager.instance.units){
			// For each living enemy, are they in our max attack range?
			if( p.teamID != aiUnit.teamID && !p.outOfAction ){
				if( Vector3.Distance( aiUnit.transform.position, p.transform.position ) <= range ){
					potentialTargets.Add( p );
				}
			}
		}

		// If there's one or more potential targets, check for line of sight with them
		if( potentialTargets.Count > 0 ){
			List <Unit> targetsToRemove = new List<Unit>();	// DON'T MODIFY COLLECTIONS WHILE YOU ITERATE ON THEM!
			foreach( Unit p in potentialTargets ){
				// If we have line of sight, this will set their cover modifiers so we can choose a target based on that later
				// Tile stores if it's short or long range
				if( GameManager.instance.HaveLineOfSight( aiUnit.transform.position, p.gameObject ) ){
					continue;
				} else { // If we don't have line of sight, remove it from potential targets
					targetsToRemove.Add( p );
				}
			}

			// After iterating through potentialTargets we can safely remove the targets that we don't have line of sight on
			if( targetsToRemove.Count > 0){
				foreach( Unit p in targetsToRemove){
					potentialTargets.Remove( p );
				}
			}

			// If there's still one or more potential targets
			if( potentialTargets.Count > 0 ){
				// Shoot at the enemy we're most likely to hit for now
				float bestChanceToHit = 0;
				Unit bestTarget = potentialTargets[0];


				foreach( Unit p in potentialTargets ){
					float chanceToHitP = 0;

					// If we're in range of our primary weapon, check our chance to hit 
					if( Vector3.Distance( aiUnit.transform.position, p.transform.position ) <= range ){
						// Check our range (MAY NOT BE TOTALLY CORRECT...
						float rangeMod = wepToCheck.longToHit;

						if( Vector3.Distance( aiUnit.transform.position, p.transform.position ) <= wepToCheck.shortRange)
							rangeMod = wepToCheck.shortToHit;

						float chanceToHitWithPrimary = 100 - ( 100 - aiUnit.ballisticSkill - p.coverModifier - rangeMod );

						if( chanceToHitWithPrimary > chanceToHitP ){
							chanceToHitP = chanceToHitWithPrimary;
							aiUnit.equippedWeapon = wepToCheck;
						}
					}

					if( chanceToHitP > bestChanceToHit ){
						bestChanceToHit = chanceToHitP;
						bestTarget = p;
					}
				}

				// If we've found a unit that can be hit, this should be the highest chance unit to hit, so attack it!
				if( bestChanceToHit > 0 ){
					unitToAttack = bestTarget;

					int weaponToSelectInt = 1;
					for( int j = 1; j < aiUnit.weapons.Count; j++){
						if( aiUnit.equippedWeapon == aiUnit.weapons[j])
							weaponToSelectInt = j +1;
					}

					aiUnit.SelectWeapon( weaponToSelectInt);

					decisionDelayTimerIsRunning = true;
					return true;
				}
			}
		}

		return false;	// we haven't found a target for this weapon
	}

	public bool CheckToAttack(){
		// Are we able to melee anyone? If so, melee them
		bool someoneToAttack = false;
		unitToAttack = null;

		for( int i = 0; i < aiUnit.weapons.Count; i++ ){
			if( CanAttackWith( i )){
				someoneToAttack =  true;
				break;
			}
		}

		// If we've chosen a target, focus on them
		if( someoneToAttack ){
			GameManager.instance.targetedTile = GameManager.instance.map[(int)unitToAttack.gridPosition.x][(int)unitToAttack.gridPosition.y];

			GameManager.instance.FocusOnPos( unitToAttack.transform.position, 1 );

			GameManager.instance.targetUnit = unitToAttack;
			GameManager.instance.currUnit.FaceTarget ();
		}

		return someoneToAttack;
	}

	public void MoveToAttack(){
		float shortestDistance =  99999;
		Tile targetTile = GameManager.instance.map[0][0];
		bool decisionMade = false;
		
		// Check the distance from each available movement tile to each non-DOWN enemy unit, move to closest enemy
		foreach( Tile t in GameManager.instance.currHighlightedTiles ){
			// If the tile isn't occupied, check it against the units
			if( t.occupied < 0 ){
				foreach( Unit p in GameManager.instance.units){
					// Is this a living enemy
					if( p.teamID != aiUnit.teamID && !p.outOfAction ){
						// If we are a neighbor tile to the unit, move to that tile for melee
						Tile goalUnitTile = GameManager.instance.map[ (int)p.gridPosition.x ][ (int)p.gridPosition.y];
						if( goalUnitTile.neighbors.Contains( t ) ) {
							// If the enemy has a shield, we need to move to an exposed side
							if( p.shield != null ){
								Vector3 relativePos = t.transform.InverseTransformPoint( p.transform.position );

								if (relativePos.z < 0) 
									Debug.Log ("In Front of Unit ");
								else {
									Debug.Log ("Behind Unit ");
									targetTile = t;
									shortestDistance = -10;
									decisionMade = true;
									GameManager.instance.FocusOnPos(targetTile.transform.position, 1 );
									
									GameManager.instance.targetedTile = targetTile;
									
									break;
								}
								
								/*if (relativePos.x < 0)
									Debug.Log ("Left ");
								else 
									Debug.Log ("Right ");*/

							} else {	// If they don't have a shield, we can move to this tile without a problem
								targetTile = t;
								shortestDistance = -10;
								decisionMade = true;
								GameManager.instance.FocusOnPos(targetTile.transform.position, 1 );
								
								GameManager.instance.targetedTile = targetTile;
								
								break;
							}
						}
						
						float currDist = Vector3.Distance( t.transform.position, p.transform.position );
						
						if( currDist < shortestDistance && !decisionMade){
							targetTile = t;
							shortestDistance = currDist;
						}
					} 
				}
			}
			
			if( decisionMade ){
				break;
			}
		}
		
		if( !decisionMade ){
			GameManager.instance.FocusOnPos(targetTile.transform.position, 1 );

			GameManager.instance.targetedTile = targetTile;

		}
	}

	public void MoveAwayFromEnemies(){
		float longestDistance =  0;
		Tile targetTile = GameManager.instance.map[0][0];

		// Check the distance from each available movement tile to each non-DOWN enemy unit, move FROM closest enemy
		foreach( Tile t in GameManager.instance.currHighlightedTiles ){
			// If the tile isn't occupied, check it against the units
			if( t.occupied < 0 ){
				foreach( Unit p in GameManager.instance.units){
					// Is this a living enemy
					if( p.teamID != aiUnit.teamID && !p.outOfAction ){
						float currDist = Vector3.Distance( t.transform.position, p.transform.position );

						if( currDist > longestDistance ){
							if (GameManager.instance.HaveLineOfSight (t.transform.position+Vector3.up, p.gameObject)) {	// Only move to a new position that still has line of sight??
								targetTile = t;
								longestDistance = currDist;
							}

						}
					} 
				}
			}
		}

		GameManager.instance.FocusOnPos(targetTile.transform.position, 1 );
		GameManager.instance.targetedTile = targetTile;
	}
}
