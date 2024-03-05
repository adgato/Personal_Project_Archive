using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dijkstras
{
    private int recursionLimit = 9999;
    private int recursionCount = 0;
    struct NodeInfo
    {
        public bool visited;
        public float shortest;
        public int parent;
    }

    int numNodes;
    float[,] adjMatrix;

    public Dijkstras(Vector3[] nodes, float maxDist)
    {
        numNodes = nodes.Length;
        CreateAdjMatrix(nodes, maxDist);
    }
    void CreateAdjMatrix(Vector3[] nodes, float maxDist)
    {
        float maxSqrDist = maxDist * maxDist;
        adjMatrix = new float[numNodes, numNodes];
        for (int i = 0; i < numNodes; i++)
        {
            for (int j = 0; j < numNodes; j++)
            {
                if (j > i)
                {
                    //Incentivise shorter distances with square distance
                    float sqrDist = (nodes[i] - nodes[j]).sqrMagnitude; 
                    if (sqrDist < maxSqrDist)
                    {
                        adjMatrix[i, j] = sqrDist;
                    }
                    else
                        adjMatrix[i, j] = float.MaxValue;
                }
                else if (j == i)
                    adjMatrix[i, j] = float.MaxValue;
                else
                    adjMatrix[i, j] = adjMatrix[j, i];
            }
        }
    }
    public void ShortestPath(int from, int to, out List<int> path, out float pathLength)
    {
        //Initialise infoTable
        NodeInfo[] infoTable = new NodeInfo[numNodes];
        for (int i = 0; i < numNodes; i++)
        {
            infoTable[i] = new NodeInfo();
            infoTable[i].visited = false;
            infoTable[i].shortest = float.MaxValue;
        }
        infoTable[from].shortest = 0;
        infoTable[from].parent = -1;

        recursionCount = 0;
        CaluclatePaths(from, ref infoTable);

        path = new List<int>(numNodes);

        recursionCount = 0;
        PathTo(to, infoTable, ref path);
        pathLength = infoTable[to].shortest;
    }
    void CaluclatePaths(int from, ref NodeInfo[] infoTable)
    {
        if (recursionCount == recursionLimit)
        {
            Debug.LogError("Error: recursion limit reached");
            return;
        }
        else if (from == -1)
        {
            Debug.LogError("Error: Graph is not connected");
            return;
        }

        infoTable[from].visited = true;

        bool allVisited = true;
        for (int i = 0; i < numNodes; i++)
        {
            if (infoTable[i].visited)
                continue;

            allVisited = false;

            //Calculate the weight of the path from the starting node to the current node
            float arcWeight = infoTable[from].shortest + adjMatrix[from, i];
            //If this path is shorter than the current shortest path to the node, update shortest path and parent node
            if (arcWeight < infoTable[i].shortest)
            {
                infoTable[i].shortest = arcWeight;
                infoTable[i].parent = from;
            }
        }
        if (allVisited)
            return;

        //Find the unvisited node with the shortest path from the starting node
        (int next, float minWeight) = (-1, float.MaxValue);
        for (int i = 0; i < infoTable.Length; i++)
        {
            if (!infoTable[i].visited && infoTable[i].shortest < minWeight)
                (next, minWeight) = (i, infoTable[i].shortest);
        }

        //Recursively calculate shortest paths starting from the next node
        recursionCount++;
        CaluclatePaths(next, ref infoTable);
        recursionCount--;
    }
    void PathTo(int child, NodeInfo[] infoTable, ref List<int> path)
    {
        if (recursionCount == recursionLimit)
        {
            Debug.LogError("Error: recursion limit reached");
            return;
        }
        //If child node is the starting node
        else if (child == -1)
            return;

        //Add current node to the path list and recursively call PathTo on the parent node

        path.Add(child);

        recursionCount++;
        PathTo(infoTable[child].parent, infoTable, ref path);
        recursionCount--;
    }
}
