/*
	Code for a turn-based tactics game by Ian Snyder
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class ClanInfoSaveAndLoad : MonoBehaviour{
	public static void LoadClanIntoUnitInfoScene( Clan clanToLoad  ){
		string nameOfClanToLoad = clanToLoad.clanName;

		if (File.Exists (Application.persistentDataPath + "/CLAN_" + nameOfClanToLoad + ".dat")) {
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file = File.Open (Application.persistentDataPath + "/CLAN_" + nameOfClanToLoad + ".dat", FileMode.Open);
			ClanInfo data = (ClanInfo)bf.Deserialize (file);

			// VARIABLES TO LOAD
			Vector3 currPos = new Vector3( 0f, 1f, 0f );
			clanToLoad.units = data.units;
			clanToLoad.equipNames = data.equipNames;
			clanToLoad.equipHeld = data.equipHeld;
			clanToLoad.equipEquipped = data.equipEquipped;
			clanToLoad.knownJobs = data.knownJobs;
			clanToLoad.itemStrings = data.itemStrings;
			clanToLoad.itemsHeld = data.itemsHeld;
			clanToLoad.itemsEquipped = data.itemsEquipped;

			foreach (string u in clanToLoad.units) {
				GameObject unitGO = (GameObject)Instantiate (Resources.Load ("Units/UNIT_GENERIC"), currPos, Quaternion.identity);
				Unit thisUnit = unitGO.GetComponent<Unit> ();
				thisUnit.name = u;
				thisUnit.LoadUnitData ();
				currPos += Vector3.forward;
			}

			file.Close ();
		}
	}

	public static void LoadClanIntoBattleScene( Clan clanToLoad  ){
		string nameOfClanToLoad = clanToLoad.clanName;

		if (File.Exists (Application.persistentDataPath + "/CLAN_" + nameOfClanToLoad + ".dat")) {
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file = File.Open (Application.persistentDataPath + "/CLAN_" + nameOfClanToLoad + ".dat", FileMode.Open);
			ClanInfo data = (ClanInfo)bf.Deserialize (file);

			// VARIABLES TO LOAD
			int currPos = 0;
			clanToLoad.units = data.units;
			clanToLoad.equipNames = data.equipNames;
			clanToLoad.equipHeld = data.equipHeld;
			clanToLoad.equipEquipped = data.equipEquipped;
			clanToLoad.knownJobs = data.knownJobs;
			clanToLoad.itemStrings = data.itemStrings;
			clanToLoad.itemsHeld = data.itemsHeld;
			clanToLoad.itemsEquipped = data.itemsEquipped;

			foreach (string u in clanToLoad.units) {
				GameObject unitGO = (GameObject)Instantiate (Resources.Load ("Units/UNIT_GENERIC"), GameManager.instance.team1StartPositions[currPos] + Vector3.up, Quaternion.identity);
				Unit thisUnit = unitGO.GetComponent<Unit> ();
				thisUnit.name = u;
				thisUnit.LoadUnitData ();
				currPos ++;
			}

			file.Close ();
		}
	}
}
