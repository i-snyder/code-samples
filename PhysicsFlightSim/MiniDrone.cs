/*
	Code for a physics-based flight sim shooter by Ian Snyder
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

public class MiniDrone : TargetableEntity {
	public float followSpeed = 100f;
    public ObscuredInt numCoins = 1;


    void OnEnable(){
		// Stuff to reset when using the pool manager
		gameObject.GetComponent<Rigidbody> ().velocity = Vector3.zero;
		alive = true;
    }

	protected override void StartCustom () {
		alive = true;
	}

	protected override void DieCustom(string dieFromWhat = "a bullet", EnemyType.Type fromType = EnemyType.Type.Turret)
    {
		alive = false;
		SFGameManager.instance.vfxShipExplosionPool.TryGetNextObject (transform.position, Quaternion.identity);

        if (faction == Faction.Empire)
        {
            SFGameManager.instance.RemoveEnemy(this);
        }

        // COINS
        if (fromType != EnemyType.Type.Terrain) SFGameManager.instance.SpawnCoins(numCoins, rb.position);
        
        gameObject.SetActive(false);
    }

	void FixedUpdate(){
		transform.LookAt (CalculateLead());
		rb.AddForce (transform.forward * followSpeed);
	}

	Vector3 CalculateLead () {
		Vector3 targetPos = SFGameManager.instance.players[0].transform.position;
		Vector3 V = SFGameManager.instance.players[0].GetComponent<Rigidbody>().velocity;
		Vector3 D = targetPos - transform.position;
		float A = V.sqrMagnitude - followSpeed * followSpeed;
		float B = 2 * Vector3.Dot (D, V);
		float C = D.sqrMagnitude;
		if (A >= 0) {
			//Debug.LogError ("No solution exists");
			return targetPos;
		} else {
			float rt = Mathf.Sqrt (B*B - 4*A*C);
			float dt1 = (-B + rt) / (2 * A);
			float dt2 = (-B - rt) / (2 * A);
			float dt = (dt1 < 0 ? dt2 : dt1);
			return targetPos + V * dt;
		}
	}
}
