/*
	Code for a turn-based tactics game by Ian Snyder
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TilePath {
	public List<Tile> listOfTiles = new List<Tile>();

	public int costOfPath = 0;	
	
	public Tile lastTile;
	
	public TilePath() {}
	
	public TilePath(TilePath tp) {
		listOfTiles = tp.listOfTiles.ToList();
		costOfPath = tp.costOfPath;
		lastTile = tp.lastTile;
	}
	
	public void addTile(Tile t) {
		costOfPath += t.movementCost;	// Terrain roughness
		bool tempWalkable = false;
		bool tempHoppable = false;

		// Only check terrain height if you're moving for now
		if( GameManager.instance.currUnit.moving || GameManager.instance.showingMoveRange) {
			float elevationDiff =  t.elevation - lastTile.elevation ;

			// Should be walkable if it's less than the jump height diff
			if (Mathf.Abs (elevationDiff) <= GameManager.instance.currUnit.jumpHeight) {
				tempWalkable = true;
			} else {
				// If the new tile is too low, check if we can hop to a neighbor tile of the new tile
				if (elevationDiff < -GameManager.instance.currUnit.jumpHeight) {
					bool possibleToHop = false;
					foreach (Tile neighbor in t.neighbors) {
						// Check if there's a neighbor tile at the right height
						float eDiff = neighbor.elevation - lastTile.elevation;
						if (Mathf.Abs (eDiff) < GameManager.instance.currUnit.jumpHeight && neighbor != lastTile) {
							// If a tile is already walkable, don't set it to be hoppable
							if (!t.walkable) {
								// Check to make sure the neighbor and the last tile are in a straight line from each other
								if ( neighbor.gridPosition.x == lastTile.gridPosition.x || neighbor.gridPosition.y == lastTile.gridPosition.y) {
									tempHoppable = true;
									possibleToHop = true;
								}
							}
						}
					}

					if (!possibleToHop) {
						costOfPath += 9999;
					}
				}

				if (elevationDiff > GameManager.instance.currUnit.jumpHeight && !lastTile.hoppable) {
					costOfPath += 99999;	// Make heights bigger than jumpHeight impassable
					tempWalkable = false;
				}
			}
		}

		if (!tempHoppable)
			t.walkable = true;

		if (tempHoppable && !t.walkable && !tempWalkable) {
			t.hoppable = true;
			//Debug.Log ("Hoppable: "+t+ " Last Tile: " + lastTile);
		}

		if (tempWalkable)
			t.hoppable = false;

		listOfTiles.Add (t);

		lastTile = t;
	}
}