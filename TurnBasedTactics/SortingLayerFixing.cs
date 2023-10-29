/*
	Code for a turn-based tactics game by Ian Snyder
*/

using UnityEngine;
using System.Collections;

public class SortingLayerFixing : MonoBehaviour {
	private Renderer spriteRenderer;
	private int sortOrder;

	// Use this for initialization
	void Start () {
		spriteRenderer = GetComponent<Renderer>();
		sortOrder = spriteRenderer.sortingOrder;
		spriteRenderer.GetComponent<Renderer>().sortingLayerName = "Units";
	}
	
	// Update is called once per frame
	void LateUpdate () {
		spriteRenderer.sortingOrder = ( (Screen.height - ((int)Camera.main.WorldToScreenPoint (transform.root.gameObject.transform.position).y) * 10)) + sortOrder;
	}
}
