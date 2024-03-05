using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall
{
    public class StateMachine
    {
        public State state { get; private set; }

        public void SetState(State _state)
        {
            if (!removed)
                state = _state;
        }

        public bool removed { get { return state == State.removed; } }

        public StateMachine(State _value)
        {
            state = _value;
        }
    }

    private GameObject Prefab;
    private GameObject Shelf;
    private GameObject[] Objects;

    public enum State { visible, removed }

    public StateMachine wallState;

    public Vector3Int type { get; private set; }

    public Wall(Vector3Int _type, GameObject _Prefab, GameObject _Shelf, GameObject[] _Objects)
    {
        type = _type;
        Prefab = _Prefab;
        Shelf = _Shelf;
        Objects = _Objects;
        wallState = new StateMachine(State.visible);
    }

    public void Render(Transform parent)
    {
        if (wallState.removed)
            return;

        Transform wallObject = Object.Instantiate(Prefab, parent.position, parent.rotation * Prefab.transform.localRotation, parent).transform;

        wallObject.localScale = Prefab.transform.localScale;

        //wallObject.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = new Material(wallObject.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial);
        //wallObject.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial.SetColor("_tint", wallState.locked ? Color.red : Color.gray);

        if (Random.value < 0.3f)
            SpawnObject(parent);
    }

    public void SpawnObject(Transform parent)
    {
        Quaternion rot = Quaternion.Euler(0, 0, 0);

        if (type == Vector3Int.back)
            rot = Quaternion.Euler(0, 180, 0);
        else if (type == Vector3Int.left)
            rot = Quaternion.Euler(0, -90, 0);
        else if (type == Vector3Int.right)
            rot = Quaternion.Euler(0, 90, 0);
        else if (type != Vector3Int.forward)
            return; //no shelves on the floor


        if (Random.value < 0.6f)
        {
            //Spawn a random object
            Transform randObject = Object.Instantiate(Objects[Random.Range(0, Objects.Length)], parent.position, Quaternion.identity, parent).transform;
            randObject.localRotation = rot;
        }
        else
        {
            //Spawn a shelf
            bool lowest = false;

            for (int i = 0; i < 2; i++)
            {
                if (Random.value < 0.5f || i == 1)
                {
                    if (i == 0)
                        lowest = true;
                    else if (i == 1)
                        lowest = !lowest;

                    Transform shelf = Object.Instantiate(Shelf, parent.position + Vector3.up * i, Quaternion.identity, parent).transform;

                    shelf.localRotation = rot;
                    //shelf.localPosition += 5 * (1 - scale) / 2 * (Random.value * 2 - 1) * shelf.right;

                    shelf.GetComponent<Shelf>().lowest = lowest;
                    shelf.GetComponent<Shelf>().height = i;
                    shelf.GetComponent<Shelf>().Init();
                }
            }
        }
    }
}
