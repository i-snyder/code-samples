/*
	Code for a turn-based tactics game by Ian Snyder
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TileHighlight {

	public TileHighlight () {
		
	}
	
	public static List<Tile> FindHighlight(Tile originTile, int movementPoints) {
		List<Tile> closed = new List<Tile>();
		List<TilePath> open = new List<TilePath>();
		
		TilePath originPath = new TilePath();
		originPath.lastTile = originTile;
		originPath.addTile(originTile);
		
		open.Add(originPath);
		
		while (open.Count > 0) {
			TilePath current = open[0];
			open.Remove(open[0]);
			
			if (closed.Contains(current.lastTile)) {
				continue;
			} 
			if (current.costOfPath > movementPoints + 1) {
				continue;
			}

			// If we're going somewhere that costs more than our movement per action point, we must be running there so mark it
			current.lastTile.runDestination = false;
			if( current.costOfPath > GameManager.instance.currUnit.movementPerDecisionPoint+1 ){
				current.lastTile.runDestination = true;
			}

			closed.Add(current.lastTile);

			//Tile t = current.lastTile.neighbors [2];
			foreach (Tile t in current.lastTile.neighbors) {
				if( t.impassable) continue;
				//if (t.deepWater) continue;
				if( GameManager.instance.currUnit.moving) {	// Fix to not clear enemy tiles when attacking, color highlighting is wrong!
					if( t.occupied != -1 && t.occupied != GameManager.instance.currUnit.teamID) continue;
				}

				// HOPPING
				if (current.lastTile.hoppable) {
					float elevDiff = t.elevation - current.lastTile.elevation;
					if (Mathf.Abs (elevDiff) < GameManager.instance.currUnit.jumpHeight)
						continue;
				}

				TilePath newTilePath = new TilePath(current);
				newTilePath.addTile(t);
				open.Add(newTilePath);
			}
		}

		// If we're healing, don't remove the origin tile so we can heal ourselves
		if( !GameManager.instance.currUnit.healing){
			closed.Remove(originTile);
		}


		// make tiles with characters in them impassable
		originTile.occupied = GameManager.instance.currUnit.teamID;

		return closed;
	}

	public static void FindLineOfSightHighlight(GameObject origin, float shortRange, float longRange) {
		for (int i = 0; i < GameManager.instance.mapSize; i++) {
			for (int j = 0; j < GameManager.instance.mapSize; j++) {
				Tile t = GameManager.instance.map[i][j];

				Vector3 shooterBarrelPos = new Vector3( origin.transform.position.x,
				                                       origin.transform.position.y + 0.2f,
				                                       origin.transform.position.z );

				Vector3 targetHighPos = new Vector3( t.gameObject.transform.position.x,
				                                    t.gameObject.transform.position.y + 1.4f,
				                                    t.gameObject.transform.position.z );
				
				Ray rayHigh = new Ray( shooterBarrelPos, ( targetHighPos - shooterBarrelPos ).normalized);
				
				RaycastHit hitHigh;
				int tilesLayer = 8;
				int shieldsLayer = 11;
				//int breakablesLayer = 9;
				//int unitsLayer = 10;
				//int layerMask = (1<< tilesLayer)|(1<<unitsLayer);
				int layerMask = (1<< tilesLayer)|( 1<<shieldsLayer );
				
				float distanceToCast = Vector3.Distance( shooterBarrelPos, targetHighPos );

				if( distanceToCast <= longRange){	// This optimized quite a bit :O
					if( Physics.Raycast(rayHigh, out hitHigh, distanceToCast, layerMask) ) {
						
						if (hitHigh.collider.gameObject != t.gameObject) {	// No line of sight
							t.UnhighlightTile();
						}
					} else {	// We have line of sight
						
						if( distanceToCast <= shortRange  ) {	// Short range
							t.HighlightTile( GameManager.yellowColor );
							t.shortRange = true;
							
							
						} else if( distanceToCast <= longRange ) {	// Long range
							t.HighlightTile( GameManager.redColor );
							t.shortRange = false;
						}
					}
				}
			}
		}

		// Add and remove tiles with proper line of sight check against all enemies
		foreach( Unit p in GameManager.instance.units){
			if( !p.outOfAction ){
				Tile t = GameManager.instance.map[(int)p.gridPosition.x][(int)p.gridPosition.y];

				if( !GameManager.instance.HaveLineOfSight( GameManager.instance.currUnit.transform.position, p.gameObject)){
					t.UnhighlightTile();
				} else { // We actually have line of sight, highlight this tile
					if( !t.highlighted ){
						Vector3 shooterBarrelPos = new Vector3( origin.transform.position.x,
						                                       origin.transform.position.y + 0.2f,
						                                       origin.transform.position.z );
						
						Vector3 targetHighPos = new Vector3( t.gameObject.transform.position.x,
						                                    t.gameObject.transform.position.y + 1.4f,
						                                    t.gameObject.transform.position.z );

						float distanceToCast = Vector3.Distance( shooterBarrelPos, targetHighPos );

						if( distanceToCast <= shortRange  ) {	// Short range
							t.HighlightTile( GameManager.yellowColor );
							t.shortRange = true;
						} else if( distanceToCast <= longRange ) {	// Long range
							t.HighlightTile( GameManager.redColor );
							t.shortRange = false;
						}
					}
				}
			}
		}

		if( GameManager.instance.currUnit.isAI ){
			Unit target = GameManager.instance.currUnit.GetComponent<AI>().unitToAttack;
			Tile t = GameManager.instance.map[(int)target.gridPosition.x][(int)target.gridPosition.y];

			Vector3 shooterBarrelPos = new Vector3( origin.transform.position.x,
			                                       origin.transform.position.y + 0.2f,
			                                       origin.transform.position.z );
			
			Vector3 targetHighPos = new Vector3( t.gameObject.transform.position.x,
			                                    t.gameObject.transform.position.y + 1.4f,
			                                    t.gameObject.transform.position.z );
			
			float distanceToCast = Vector3.Distance( shooterBarrelPos, targetHighPos );
			
			if( distanceToCast <= shortRange  ) {	// Short range
				t.HighlightTile( GameManager.yellowColor );
				t.shortRange = true;
			} else {	// Long range
				t.HighlightTile( GameManager.redColor );
				t.shortRange = false;
			}
		}
	}

	//GRENADE LOB LINE OF SIGHT CHECK
	public static void FindGrenadeLobHighlight(GameObject origin, float shortRange, float longRange) {
		for (int i = 0; i < GameManager.instance.mapSize; i++) {
			for (int j = 0; j < GameManager.instance.mapSize; j++) {
				Tile t = GameManager.instance.map [i] [j];

				Vector3 shooterBarrelPos = new Vector3 (origin.transform.position.x,
                                       origin.transform.position.y + 0.2f,
                                       origin.transform.position.z);

				Vector3 targetHighPos = new Vector3 (t.gameObject.transform.position.x,
                                    t.gameObject.transform.position.y + 1.4f,
                                    t.gameObject.transform.position.z);

				Ray rayHigh = new Ray (shooterBarrelPos, (targetHighPos - shooterBarrelPos).normalized);

				RaycastHit hitHigh;
				int tilesLayer = 8;
				//int shieldsLayer = 11;
				//int breakablesLayer = 9;
				//int unitsLayer = 10;
				//int layerMask = (1<< tilesLayer)|(1<<unitsLayer);
				int layerMask = (1 << tilesLayer);

				float distanceToCast = Vector3.Distance (shooterBarrelPos, targetHighPos);

				if (distanceToCast <= longRange) {	// This optimized quite a bit :O
					if (Physics.Raycast (rayHigh, out hitHigh, distanceToCast, layerMask)) {
						if (hitHigh.collider.gameObject != t.gameObject) {	// No line of sight
								t.UnhighlightTile ();
						}
					} else {	// We have line of sight
						if (distanceToCast <= shortRange) {	// Short range
								t.HighlightTile (GameManager.yellowColor);
								t.shortRange = true;
	
	
						} else if (distanceToCast <= longRange) {	// Long range
								t.HighlightTile (GameManager.redColor);
								t.shortRange = false;
						}
					}
				}
			}
		}
	}
}
