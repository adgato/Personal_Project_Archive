using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeCloner
{
    private GameObject[] randomTrees;
    private int seed;

    public TreeCloner(Planet planet)
    {
        seed = planet.planetValues.environmentSeed;
        randomTrees = new GameObject[5];
        Random.InitState(seed);
        for (int i = 0; i < 5; i++)
        {
            randomTrees[i] = Object.Instantiate(planet.Tree);
            randomTrees[i].SetActive(false);
        }

    }
}
