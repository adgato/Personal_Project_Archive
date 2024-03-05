using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateWorld : MonoBehaviour
{
    private void Start()
    {
        UpdateWorld();
    }


    public GameObject tree;
    public GameObject stick;
    public GameObject stone;
    public GameObject seat;

    public int seed;
    public bool randomSeed;
    public GameObject road;
    public GameObject NaturalInstantiate(GameObject prefab, Vector3 pos, Quaternion rotOffset, System.Random pseudoRandom)
    {

        Vector3 rayOriginA = new Vector3(pos.x, 100, pos.z);

        Vector3 rayOriginB = new Vector3(
            pos.x + ((float)pseudoRandom.NextDouble() * 2 - 1),
            100,
            pos.z + ((float)pseudoRandom.NextDouble() * 2 - 1));

        Physics.Raycast(rayOriginA, -Vector3.up, out RaycastHit hitA);
        Physics.Raycast(rayOriginB, -Vector3.up, out RaycastHit hitB);

        Vector3 floorA = new Vector3(rayOriginA.x, rayOriginA.y - hitA.distance, rayOriginA.z);
        Vector3 floorB = new Vector3(rayOriginB.x, rayOriginB.y - hitB.distance, rayOriginB.z);

        Vector3 angle = floorA - floorB;

        GameObject new_object = Instantiate(prefab, floorA + new Vector3(0, pos.y, 0), Quaternion.LookRotation(angle) * rotOffset);

        return new_object;
    }

    public void UpdateWorld()
    {
        ClearWorld();

        //Allow raycast to hit road, the road should ignore raycasting by default so the waypoints can be grounded in GroundWaypoints.cs
        road.layer = 0;

        System.Random pseudoRandom;

        if (randomSeed)
            pseudoRandom = new System.Random(Random.Range(int.MinValue, int.MaxValue));
        else
            pseudoRandom = new System.Random(seed);

        //For each 5x5 grid on the grass plane
        for (int x = -240; x <= 240; x += 10)
        {
            for (int z = -240; z <= 240; z += 10)
            {
                float xPos = x + (float)pseudoRandom.NextDouble() * 7.5f;
                float zPos = z + (float)pseudoRandom.NextDouble() * 7.5f;
                Physics.Raycast(new Vector3(xPos, 100, zPos), -Vector3.up, out RaycastHit hitRoad);

                //If the grid is away from the campfires and on terrain only
                if ((Mathf.Abs(xPos) > 15 || zPos < 25 || zPos > 55) && (xPos < 124 || xPos > 130 || zPos < -144 || zPos > -138) && hitRoad.collider.gameObject == gameObject)
                {

                    //Spawn a tree
                    if (pseudoRandom.Next(0, 4) != 0)
                    {
                        Physics.Raycast(new Vector3(xPos, 100, zPos), -Vector3.up, out RaycastHit hit);

                        //more likely to get shorter trees
                        GameObject new_object = Instantiate(tree,
                                                            new Vector3(
                                                                xPos, 
                                                                (100 - hit.distance) + (4 - Mathf.Pow(2 * (float)pseudoRandom.NextDouble(), 2)), 
                                                                zPos),
                                                            Quaternion.Euler(
                                                                0, 
                                                                (float)pseudoRandom.NextDouble() * 360, 
                                                                0)
                                                            );

                        new_object.transform.SetParent(transform.Find("Trees").transform);

                        //Remove 0-2 branches
                        Transform[] branches = new_object.GetComponentsInChildren<Transform>(); //branch indexes start at 2
                        if (pseudoRandom.Next(0, 4) != 0)
                        {
                            DestroyImmediate(branches[2].gameObject);
                            if (pseudoRandom.Next(0, 2) != 0)
                            {
                                DestroyImmediate(branches[pseudoRandom.Next(3, 5)].gameObject);
                            }
                        }
                    }

                    //Spawn a stick
                    else if (pseudoRandom.Next(0, 3 - 1) == 0)
                    {
                        GameObject new_object = NaturalInstantiate(stick, new Vector3(xPos, 0.05f, zPos), Quaternion.Euler(90, 0, 0), pseudoRandom);

                        new_object.transform.SetParent(transform.Find("Sticks").transform);
                    }

                    //Spawn a stone
                    else if (pseudoRandom.Next(0, 5 - 2) == 0)
                    {
                        //Physics.Raycast(new Vector3(xPos, 100, zPos), -Vector3.up, out RaycastHit hit);

                        //GameObject new_object = Instantiate(stone, new Vector3(xPos, 100 - hit.distance, zPos), Quaternion.Euler(0, (float)pseudoRandom.NextDouble() * 360, 0));

                        GameObject new_object = NaturalInstantiate(stone, new Vector3(xPos, -0.018f, zPos), Quaternion.Euler(0, 0, 0), pseudoRandom);

                        new_object.transform.SetParent(transform.Find("Stones").transform);
                    }

                    //Spawn a seat
                    else if (pseudoRandom.Next(0, 33 - 3) == 0)
                    {
                        GameObject new_object = NaturalInstantiate(seat, new Vector3(xPos, 0.5f, zPos), Quaternion.Euler(0, 90, 90), pseudoRandom);

                        new_object.transform.localScale = new Vector3(1, (float)pseudoRandom.NextDouble() * 2 + 3, 1);
                        new_object.transform.SetParent(transform.Find("Seats").transform);
                    }
                }

            }
        }

        //Revert to ignore raycast layer once finished
        road.layer = 2;
    }

    public void ClearWorld()
    {
        for (int i = 0; i < this.transform.childCount; i++)
        {
            for (int q = this.transform.GetChild(i).childCount; q > 0; --q)
            {
                DestroyImmediate(this.transform.GetChild(i).GetChild(0).gameObject);
            }
        }
    }

}
