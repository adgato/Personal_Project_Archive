using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Android : MonoBehaviour
{
    [SerializeField] private AndroidVision androidVision;
    [SerializeField] private FabrikLeg[] legs;
    [SerializeField] private Transform Chest;
    private RoboEyes roboEyes;
    public float height;

    [SerializeField] private float moveSpeed = 5;
    private Rigidbody rb;
    private List<Vector3> path;
    private HiveGen hiveGen;
    private Vector3 prevHivePos;
    private int framesDetected = 0;

    // Start is called before the first frame update
    void Start()
    {
        roboEyes = new RoboEyes(androidVision);

        hiveGen = FindObjectOfType<HiveGen>();
        rb = GetComponent<Rigidbody>();

        transform.parent = FindObjectOfType<HiveGen>().transform;

        androidVision.Eyes[0].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = new Material(androidVision.Eyes[0].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial);
        androidVision.Eyes[1].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = new Material(androidVision.Eyes[1].GetChild(0).GetComponent<MeshRenderer>().sharedMaterial);
    }

    // Update is called once per frame
    void Update()
    {
        if (CameraState.isPaused)
            return;

        CameraState.isDead |= (Camera.main.transform.position - transform.position).sqrMagnitude < 4;

        roboEyes.SetEyeColour(Color.yellow);

        Vector3 position = transform.position;

        //If the Android sees the player
        if (Physics.Raycast(position, Camera.main.transform.position - position, out RaycastHit hitInfo) && Camera.main.transform.IsChildOf(hitInfo.collider.transform))
        {
            framesDetected++;
            roboEyes.SetEyeColour(Color.Lerp(Color.yellow, Color.red, framesDetected * Time.smoothDeltaTime));        
        }
        else
            framesDetected = 0;

        //If the Android has seen the player for more than one second, the Android moves towards the Camera's position
        if (framesDetected * Time.smoothDeltaTime > 1)
        {
            MoveTowards(Camera.main.transform.position);

            Debug.DrawLine(position, Camera.main.transform.position - Camera.main.transform.up, Color.green, Time.smoothDeltaTime);
        }
        //If there is a valid path from the Android to the player, the Android moves towards the next point on the path
        else if (hiveGen.PathBetweenPoints(rb.position, Camera.main.transform.position, out path) && path.Count > 1)
        {
            Vector3 nextPointOnPath = path[0];
            for (int i = 1; i < path.Count; i++)
            {
                if (Physics.Raycast(position, path[i] - position, out RaycastHit groundInfo) && groundInfo.collider.CompareTag("Ground") && (groundInfo.collider.transform.position - path[i]).sqrMagnitude < 25)
                    nextPointOnPath = path[i];
            }

            MoveTowards(nextPointOnPath);

            Debug.DrawRay(rb.position, nextPointOnPath - position, Color.blue, Time.smoothDeltaTime);
        }

        //Update the Android's legs based on the movement of the hive (which moves relative to the player)
        Vector3 displacement = hiveGen.transform.position - prevHivePos;
        prevHivePos = hiveGen.transform.position;
        foreach (FabrikLeg leg in legs)
        {
            leg.deltaPos = transform.position - position - displacement;
            leg.prevTarget += displacement;
            leg.midTarget += displacement;
            leg.endTarget += displacement;
        }
    }

    void MoveTowards(Vector3 point)
    {
        androidVision.LookTowards(point);

        if (Physics.Raycast(transform.position, -hiveGen.transform.up, out RaycastHit groundInfo) && groundInfo.collider.CompareTag("Ground"))
            transform.position = groundInfo.point + hiveGen.transform.up * height;
        transform.position -= Chest.forward * Time.deltaTime * (roboEyes.colour != Color.yellow ? moveSpeed : 5);
    }
    
}
