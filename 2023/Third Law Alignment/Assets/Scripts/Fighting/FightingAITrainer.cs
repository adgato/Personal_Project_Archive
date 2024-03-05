using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightingAITrainer : Umpire
{
    [Tooltip("The root for all future agents, set default parameters to this to apply to all")]
    [SerializeField] private RLAgent adam;
    [SerializeField] private int updatesPerFrame;

    private RLAgent[] currentPool;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < updatesPerFrame; i++)
        {
            UpdateInputs();
            UpdateGame();
        }
    }
}
