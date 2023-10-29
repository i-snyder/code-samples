/*
	Code for a turn-based tactics game by Ian Snyder
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TilePathFinder : MonoBehaviour {

	public static List<Tile> FindPath(Tile originTile, Tile destinationTile) {
		List<Tile> closed = new List<Tile>();
		List<TilePath> open = new List<TilePath>();
		
		TilePath originPath = new TilePath();
		originPath.lastTile = originTile;
		originPath.addTile(originTile);
		
		open.Add(originPath);
		
		while (open.Count > 0) {
			//open = open.OrderBy(x => x.costOfPath).ToList();
			TilePath current = open[0];
			open.Remove(open[0]);
			
			if (closed.Contains(current.lastTile)) {
				continue;
			} 

			int moveDist = GameManager.instance.currUnit.movementPerDecisionPoint;
			if (destinationTile.runDestination)
				moveDist *= 2;
			if (current.costOfPath > moveDist + 1) {
				continue;
			}

			if (current.lastTile == destinationTile) {
				current.listOfTiles.Remove (originTile);
				return current.listOfTiles;
			}
			
			closed.Add(current.lastTile);
			
			foreach (Tile t in current.lastTile.neighbors) {
				if( t.impassable) continue;
				if( GameManager.instance.currUnit.moving) {
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
		return null;
	}
}
