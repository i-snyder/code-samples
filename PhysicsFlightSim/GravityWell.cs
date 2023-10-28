/*
	Code for a physics-based flight sim shooter by Ian Snyder
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityWell : MonoBehaviour {
	float range;
	public bool pulling = true;
    int maxHeldRBs = 100;
	List<Rigidbody> heldRigidbodies = new List<Rigidbody>();

	SpaceFighterController mySpaceFighterController;

	void Start(){
		range = GetComponent<SphereCollider> ().radius;
		mySpaceFighterController = GetComponentInParent<SpaceFighterController> ();
	}

	void FixedUpdate () 
	{
		if (pulling ) {

			Collider[] cols  = Physics.OverlapSphere(transform.position, range); 
			foreach(Collider c in cols)
			{
				if (c.attachedRigidbody) {
					if ( c.tag == "Coin") {
						Rigidbody rb = c.attachedRigidbody;
						if(rb != null && rb != GetComponent<Rigidbody>() && !heldRigidbodies.Contains(rb) && heldRigidbodies.Count < maxHeldRBs)
						{
							//Debug.Log (rb.gameObject.name);
							heldRigidbodies.Add(rb);
							rb.velocity = Vector3.zero;
						}
					}
				}
			}
		}

		if (pulling && heldRigidbodies.Count>0) {
			foreach (Rigidbody rb in heldRigidbodies) {
				Vector3 offset = (transform.position - rb.transform.position).normalized;
				float distance = Vector3.Distance (transform.position, rb.transform.position);


				//rb.AddForce( offset / offset.sqrMagnitude * rb.mass * 100f);
				if (distance > 0f) {
					rb.AddForce (offset * distance * rb.mass * 90f);
				}

				// Remove any debris that falls out of the pulling range for any reason (gets knocked out from running into something or whatever)
				if (distance > range) {
					heldRigidbodies.Remove (rb);
					break;
				}
			}
		}
	}
}
