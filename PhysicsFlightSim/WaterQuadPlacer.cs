/*
	Code for a physics-based flight sim shooter by Ian Snyder
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterQuadPlacer : MonoBehaviour
{
    public GameObject[] quads;
    public float placementInterval = 2000f;

    public GameObject quadPlayerIsAbove = null;

    // Start is called before the first frame update
    void Start()
    {
        if( quadPlayerIsAbove == null)
        {
            CheckWhichQuadPlayerIsAbove();
        }
    }

    // Update is called once per frame
    void Update()
    {
        CheckWhichQuadPlayerIsAbove();

        if( quadPlayerIsAbove != null)
        {
            // First figure out the quadrant of the quad we're in
            int quadrantOftheQuad = 0; // 0 Lower Left, 1 Lower Right, 2 Upper Left, 3 Upper Right
            Vector3 currPlayerPos = SFGameManager.instance.playerRB.transform.position;

            // Are we to the left?
            if ( currPlayerPos.x < quadPlayerIsAbove.transform.position.x)
            {
                // Are we in the upper left?
                if( currPlayerPos.z > quadPlayerIsAbove.transform.position.z)
                {
                    quadrantOftheQuad = 2;  // Upper Left
                }
            }
            else
            {
                // We're to the right somewhere
                if( currPlayerPos.z > quadPlayerIsAbove.transform.position.z)
                {
                    quadrantOftheQuad = 3;  // Upper right
                }
                else
                {
                    quadrantOftheQuad = 1;  // Lower right
                }
            }

            for(int i = 0; i < 4; i++)
            {
                if( quads[i] != quadPlayerIsAbove)
                {
                    bool alreadyMoved = false;  // Move the vertical and horizontal, then assume the last to be moved is the corner

                    // Get the next horizontal quad and flip it to the side we need
                    if (Mathf.Approximately(quads[i].transform.position.z, quadPlayerIsAbove.transform.position.z))
                    {
                        // If we're on the right, flip to the right
                        if( quadrantOftheQuad == 3 || quadrantOftheQuad == 1)
                        {
                            Vector3 newPos = quadPlayerIsAbove.transform.position;
                            newPos.x += placementInterval;
                            quads[i].transform.position = newPos;
                            alreadyMoved = true;
                        }
                        else
                        {
                            // If we're on the left of the quad we're above, flip to the left
                            Vector3 newPos = quadPlayerIsAbove.transform.position;
                            newPos.x -= placementInterval;
                            quads[i].transform.position = newPos;
                            alreadyMoved = true;
                        }
                    }

                    // Get the next vertical quad and flip it to the side we need
                    if (Mathf.Approximately(quads[i].transform.position.x, quadPlayerIsAbove.transform.position.x))
                    {
                        // If we're on the top, flip to the top
                        if (quadrantOftheQuad == 2 || quadrantOftheQuad == 3)
                        {
                            Vector3 newPos = quadPlayerIsAbove.transform.position;
                            newPos.z += placementInterval;
                            quads[i].transform.position = newPos;
                            alreadyMoved = true;
                        }
                        else
                        {
                            // If we're on the bottom of the quad we're above, flip to the bottom
                            Vector3 newPos = quadPlayerIsAbove.transform.position;
                            newPos.z -= placementInterval;
                            quads[i].transform.position = newPos;
                            alreadyMoved = true;
                        }
                    }

                    if(!alreadyMoved)
                    {
                        // If this quad hasn't been moved by the previous operations, it's the corner quad and needs to be moved!
                        // 0 Lower Left, 1 Lower Right, 2 Upper Left, 3 Upper Right
                        if ( quadrantOftheQuad == 0)
                        {
                            Vector3 newPos = quadPlayerIsAbove.transform.position;
                            newPos.x -= placementInterval;
                            newPos.z -= placementInterval;
                            quads[i].transform.position = newPos;
                        }
                        if (quadrantOftheQuad == 1)
                        {
                            Vector3 newPos = quadPlayerIsAbove.transform.position;
                            newPos.x += placementInterval;
                            newPos.z -= placementInterval;
                            quads[i].transform.position = newPos;
                        }
                        if (quadrantOftheQuad == 2)
                        {
                            Vector3 newPos = quadPlayerIsAbove.transform.position;
                            newPos.x -= placementInterval;
                            newPos.z += placementInterval;
                            quads[i].transform.position = newPos;
                        }
                        if (quadrantOftheQuad == 3)
                        {
                            Vector3 newPos = quadPlayerIsAbove.transform.position;
                            newPos.x += placementInterval;
                            newPos.z += placementInterval;
                            quads[i].transform.position = newPos;
                        }
                    }
                }
            }
        }
    }

    void CheckWhichQuadPlayerIsAbove()
    {
        RaycastHit quadHit;
        if (Physics.Raycast( SFGameManager.instance.players[0].transform.position, Vector3.down, out quadHit, Mathf.Infinity, SFGameManager.instance.layermaskWaterQuads))
        {
            //Debug.Log(quadHit.collider.name);
            quadPlayerIsAbove = quadHit.collider.gameObject;
        }
    }
}
