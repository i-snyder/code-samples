/*
	Code for a physics-based flight sim shooter by Ian Snyder
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleBullet : MonoBehaviour {
	void OnTriggerEnter( Collider other){
		Rigidbody otherRB = other.gameObject.GetComponent<Rigidbody>();
		if ( otherRB ) {
			Destroy (GetComponent<Rigidbody> ());
			Destroy (GetComponent<Collider> ());
			transform.parent = other.transform;

			// Create the chain
			SpaceFighterController sfc = SFGameManager.instance.player.GetComponent<SpaceFighterController>();
			sfc.grappleObject = other.gameObject;

			Vector3 dirForChain = (other.transform.position - sfc.transform.position).normalized;
			float distanceToConnect = Vector3.Distance( sfc.transform.position, other.transform.position)/4f ;

			for (int i = 1; i <= 3; i++) {
				GameObject chainLink = (GameObject)Instantiate (Resources.Load ("Spacefighter/ChainLink"), sfc.transform.position + (dirForChain*distanceToConnect*i), Quaternion.identity);
				chainLink.transform.LookAt (other.transform.position);
				chainLink.AddComponent<ConfigurableJoint> ();
				ConfigurableJoint chainlinkCJ = chainLink.GetComponent<ConfigurableJoint> ();
				chainlinkCJ.xMotion = ConfigurableJointMotion.Locked;
				chainlinkCJ.yMotion = ConfigurableJointMotion.Locked;
				chainlinkCJ.zMotion = ConfigurableJointMotion.Locked;
				chainlinkCJ.anchor = new Vector3 (0f, 0f, -1f);
				chainlinkCJ.axis = -Vector3.one;

				sfc.chainLinks.Add (chainLink);
			}

			// Set up joints
			sfc.chainLinks [0].GetComponent<ConfigurableJoint> ().connectedBody = sfc.rb;
			sfc.chainLinks [1].GetComponent<ConfigurableJoint> ().connectedBody = sfc.chainLinks [0].GetComponent<Rigidbody> ();
			sfc.chainLinks [2].GetComponent<ConfigurableJoint> ().connectedBody = sfc.chainLinks [1].GetComponent<Rigidbody> ();
			other.gameObject.AddComponent<ConfigurableJoint> ();
			ConfigurableJoint otherCJ = other.GetComponent<ConfigurableJoint> ();
			otherCJ.xMotion = ConfigurableJointMotion.Locked;
			otherCJ.yMotion = ConfigurableJointMotion.Locked;
			otherCJ.zMotion = ConfigurableJointMotion.Locked;
			otherCJ.anchor = transform.localPosition;
			otherCJ.axis = Vector3.one;
			otherCJ.connectedBody = sfc.chainLinks [2].GetComponent<Rigidbody> ();
		}
	}

	void OnCollisionEnter( Collision col){
		if (col.gameObject.GetComponent<Rigidbody>()) {
			Destroy (GetComponent<Rigidbody> ());
			Destroy (GetComponent<Collider> ());
			transform.parent = col.transform;
		}
	}
}
