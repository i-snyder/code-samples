/*
	Code for a turn-based tactics game by Ian Snyder
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour {
	
	public Vector2 gridPosition = Vector2.zero;
	
	public int movementCost = 1;
	public float elevation = 0f;
	public bool highlighted = false;
	public bool runDestination = false;
	public bool impassable = false;
	public bool deepWater = false;
	public int occupied = -1;	// -1 for unoccupied, 0 and up is the team occupying it
	public Unit occupyingUnit;

	public int startingTile = -1;	// -1 = Not a starting tile, 1 for Team 1, 2 for Team 2

	public bool terminal = false;
	public bool exitTile = false;
	public bool intelDroppedHere = false;

	public bool shortRange = false;

	public GameObject highlightObject;
	private Color highlightCurrColor;
	private Vector3 highlightDefaultScale;
	private Vector3 highlightCurrPos;

	public bool aoeHighlighted = false;

	// Variable to TEMPORARILY hold if this tile is hoppable
	public bool hoppable = false;
	// Variable to know this is a valid walking position, so if it's flagged hoppable it doesn't get removed
	public bool walkable = false;

	// If this tile is targeted but ACTION HAS NOT BEEN CONFIRMED
	public bool targeted = false;
	
	public List<Tile> neighbors = new List<Tile>();
	
	// Use this for initialization
	void Start () {
		highlighted = false;
		highlightObject = (GameObject)Instantiate(Resources.Load("Tiles/Highlight", typeof(GameObject)), this.gameObject.transform.position + 0.55f * Vector3.up, this.gameObject.transform.rotation);
		highlightObject.layer = 2;
		highlightDefaultScale = new Vector3( 1f, .1f, 1f);
		highlightObject.transform.localScale = Vector3.zero;
		highlightObject.transform.parent = this.gameObject.transform;
		highlightObject.GetComponent<Renderer>().material.color = (Color.clear);
		highlightCurrColor = highlightObject.GetComponent<Renderer>().material.color;
		highlightCurrPos = highlightObject.transform.position;
		//highlightObject.SetActive(false);

		if( terminal ){
			Vector3 intelPos = transform.position;
			intelPos.y += 1;

			Intel.instance.SetPos( intelPos);
		}

		if (startingTile == 1) {
			GameManager.instance.team1StartPositions.Add (transform.position);
		}

		if (startingTile == 2) {
			GameManager.instance.team2StartPositions.Add (transform.position);
		}
	}

	public Tile KnockbackToNeighbor( Vector3 attackerPos ){
		Tile furthestTile = this;
		
		// Return the furthest tile from the attacker
		foreach( Tile t in neighbors ){
			// Not occupied, not impassable
			if( t.occupied < 0 && !t.impassable ) {
				// Only knock back if the new tile is lower or equal height to current tile (can't be knocked uphill)
				if (this.elevation >= t.elevation){
					// If the current tile is further from the attacker than the previous furthest tile (starting with the currently occupied tile), make it the furthest tile
					if( Vector3.Distance( t.transform.position, attackerPos) > Vector3.Distance( furthestTile.transform.position, attackerPos))	{
						furthestTile = t;
					}
				}


			}
		}
		
		return furthestTile;
	}
	
	public void generateNeighbors() {		
		neighbors = new List<Tile>();
		
		//up
		if (gridPosition.y > 0) {
			Vector2 n = new Vector2(gridPosition.x, gridPosition.y - 1);
			neighbors.Add(GameManager.instance.map[(int)Mathf.Round(n.x)][(int)Mathf.Round(n.y)]);
		}
		//down
		if (gridPosition.y < GameManager.instance.mapSize - 1) {
			Vector2 n = new Vector2(gridPosition.x, gridPosition.y + 1);
			neighbors.Add(GameManager.instance.map[(int)Mathf.Round(n.x)][(int)Mathf.Round(n.y)]);
		}		
		
		//left
		if (gridPosition.x > 0) {
			Vector2 n = new Vector2(gridPosition.x - 1, gridPosition.y);
			neighbors.Add(GameManager.instance.map[(int)Mathf.Round(n.x)][(int)Mathf.Round(n.y)]);
		}
		//right
		if (gridPosition.x < GameManager.instance.mapSize - 1) {
			Vector2 n = new Vector2(gridPosition.x + 1, gridPosition.y);
			neighbors.Add(GameManager.instance.map[(int)Mathf.Round(n.x)][(int)Mathf.Round(n.y)]);
		}
	}

	public void HighlightTile( Color highlightColor ) {
		GameManager.instance.currHighlightedTiles.Add( this );

		highlightCurrColor = highlightColor;
		highlighted = true;
		highlightObject.GetComponent<Renderer>().material.color = highlightColor;
		iTween.ScaleTo( highlightObject, iTween.Hash("scale", highlightDefaultScale, "time", 1.6f, "easetype", iTween.EaseType.easeOutElastic ));
	}

	public void UnhighlightTile() {
		highlighted = false;
		hoppable = false;
		walkable = false;
		iTween.ScaleTo( highlightObject, iTween.Hash("scale", Vector3.zero, "time", .2f ));
	}

	public void MousingOver(){
		if (GameManager.instance) {
			if (GameManager.instance.spotlightBracket && !GameManager.instance.spotlightLocked && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject ()) {
				GameManager.instance.spotlightBracket.transform.position = transform.position;
				GameManager.instance.currElevationTile = this;
			}
			//Debug.Log ("MousingOver");
			if( GameManager.instance.currUnit != null && GameManager.instance.currUnit.selectingTarget && highlighted && !GameManager.instance.currUnit.isAI && !GameManager.instance.currUnit.confirmingAction && !GameManager.instance.currUnit.meleeAttacking){
				if( !GameManager.instance.spotlightLocked)
					AoEHover();
			}
		}
	}

	public void AoEHover(){
		if( occupied > -1 ) {
			highlightObject.GetComponent<Renderer>().material.color = GameManager.instance.teamColors[ occupied ];
		} else {
			highlightObject.GetComponent<Renderer>().material.color = GameManager.whiteColor;
		}
		
		// If we're casting a spell, show the AoE radius
		if( GameManager.instance.currUnit.aoeAttack ) {
			GameManager.instance.aoeTrigger.transform.position = this.transform.position;
		}
		
		highlightObject.transform.position = new Vector3(highlightCurrPos.x,
		                                                 highlightCurrPos.y + .25f,
		                                                 highlightCurrPos.z);
	}

	public void AoEHighlight() {
		GameManager.instance.currHighlightedTiles.Add( this );

		aoeHighlighted = true;

		highlightObject.transform.localScale = highlightDefaultScale;
		highlightObject.GetComponent<Renderer>().material.color = GameManager.whiteColor;
		highlightObject.transform.position = new Vector3(highlightCurrPos.x,
		                                                 highlightCurrPos.y + .25f,
		                                                 highlightCurrPos.z);
		
		if( occupyingUnit && !GameManager.instance.aoeTargets.Contains(occupyingUnit)){
			GameManager.instance.aoeTargets.Add (occupyingUnit);
		}
	}

	public void AoEUnhighlight(){
		if( !highlighted){
			iTween.ScaleTo( highlightObject, Vector3.zero, .4f);
		}
		//iTween.ScaleTo( highlightObject, Vector3.zero, .4f);
		iTween.ColorTo( highlightObject, highlightCurrColor, .4f);
		iTween.MoveTo( highlightObject, highlightCurrPos, .4f);

		if( occupyingUnit && GameManager.instance.aoeTargets.Contains(occupyingUnit) ){
			GameManager.instance.aoeTargets.Remove (occupyingUnit);
		}

		aoeHighlighted = false;
	}

	public void TargetBob(){
		Color currColor = new Color (highlightObject.GetComponent<Renderer>().material.color.r, highlightObject.GetComponent<Renderer>().material.color.g, highlightObject.GetComponent<Renderer>().material.color.b);

		if (highlightObject.GetComponent<Renderer>().material.color.r > .1f) {
			currColor.r -= .02f;
			currColor.g -= .02f;
			currColor.b -= .02f;
		} else
			currColor = Color.white;

		highlightObject.GetComponent<Renderer>().material.color = currColor;

		if( occupyingUnit != null)
			occupyingUnit.losTooltipActive = true;
	}

	public void MouseOut(){
		if (GameManager.instance) {
			Unit currUnit = GameManager.instance.currUnit;
			if( currUnit != null && currUnit.selectingTarget && !currUnit.aoeAttack && highlighted){
				iTween.ColorTo( highlightObject, highlightCurrColor, .4f);
				//iTween.ScaleTo( highlightObject, Vector3.zero, .4f);
				iTween.MoveTo( highlightObject, highlightCurrPos, .4f);
			}

			if( occupyingUnit != null)
				occupyingUnit.losTooltipActive = false;
		}
	}

	void OnMouseOver() {
		MousingOver();
	}
	
	void OnMouseExit() {
		MouseOut();
	}

	void OnDrawGizmos() {
		if (impassable ) {
			//Gizmos.color = Color.red;
			Gizmos.DrawIcon(transform.position+Vector3.up, "icon_x.png", true);
		}

		if (startingTile == 1) { // Starting tile for Team 1
			Gizmos.DrawIcon(transform.position+Vector3.up, "triangle_blue.png", true);
		}

		if (startingTile == 2) { // Starting tile for Team 2
			Gizmos.DrawIcon(transform.position+Vector3.up, "triangle_orange.png", true);
		}
	}
}
