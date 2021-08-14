using UnityEngine;
using PathCreation;

[RequireComponent(typeof(PathCreator))]
public class GeneratePath : MonoBehaviour {
    public bool autoUpdate;
    public bool closedLoop;

    public void Generate () {
        int n_waypoints = transform.childCount;

        Transform[] waypoints = new Transform[n_waypoints];
        for (int i = 0; i < n_waypoints; i++)
        {
            waypoints[i] = transform.GetChild(i);
        }

        if (n_waypoints > 0) {
            // Create a new bezier path from the waypoints.
            BezierPath bezierPath = new BezierPath(waypoints, closedLoop, PathSpace.xyz);
            GetComponent<PathCreator>().bezierPath = bezierPath;
        }
    }
}