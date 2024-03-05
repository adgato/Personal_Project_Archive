using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RobotBody : MonoBehaviour
{
    [System.Serializable]
    public struct TransformList
    {
        public SmoothTransform UpperBody;

        public SmoothTransform Arms;
        public SmoothTransform Head;
        public SmoothTransform Torso;
        public SmoothTransform Hips;

        public SmoothTransform LeftKnee;
        public SmoothTransform LeftAnkle;
        public SmoothTransform RightKnee;
        public SmoothTransform RightAnkle;

        public SmoothTransform LeftArm;
        public SmoothTransform RightArm;
        public SmoothTransform LeftShoulder;
        public SmoothTransform LeftElbow;
        public SmoothTransform LeftHand;
        public SmoothTransform RightShoulder;
        public SmoothTransform RightElbow;
        public SmoothTransform RightHand;

        public Transform LeftLowerLeg;
        public Transform RightLowerLeg;
        public Transform LeftLowerArm;
        public Transform RightLowerArm;

        public Transform LeftEye;
        public Transform RightEye;
    }

    public Vector3 position => transform.position;
    public Quaternion rotation => transform.rotation;
    //private new Rigidbody rigidbody;

    [SerializeField] protected bool IsHumanPlayer;
    [SerializeField] protected float gameSpeed;
    protected readonly FightingAIController AI = new FightingAIController();

    public Controller Controller { get; protected set; } = new Controller();

    [HideInInspector] public Vector3 normalUp = Vector3.up;

    [Tooltip("Index of the child of the left hand that contains the left AttackAction")]
    [SerializeField] protected int leftAttackID;
    [Tooltip("Index of the child of the right hand that contains the right AttackAction")]
    [SerializeField] protected int rightAttackID;
    [HideInInspector] public AttackAction leftAttackAction;
    [HideInInspector] public AttackAction rightAttackAction;
    [SerializeField] private bool randomAttackOnInit;


    public TransformList body;
    public FightingPhysics physics;
    public LegAnimation legs;
    public ArmAnimation arms;
    public RoboEyes eyes;

    public RobotBody Opponent = null; //[HideInInspector] 
    public bool OpponentNull => Opponent == null;

    [SerializeField] private Collider[] hurtBoxes;

    public Vector3 inputDirection { get; protected set; }
    private bool coRobotUpdated;

    /// <summary>
    /// Correct delta time for movement, usually but not necessarily Time.deltaTime.
    /// </summary>
    public float dt { get; private set; }

    public void Init()
    {
        if (IsHumanPlayer)
            Controller = Controller.Player;
        else
        {
            Controller.LoadAIController(AI);
            AI.Start(this);
        }
        if (randomAttackOnInit)
        {
            leftAttackID = Rand.stream.Range(0, 4);
            do
                rightAttackID = Rand.stream.Range(0, 4);
            while (leftAttackID == rightAttackID);
        }
        leftAttackAction = body.LeftHand.transform.GetChild(leftAttackID).GetComponent<AttackAction>();
        rightAttackAction = body.RightHand.transform.GetChild(rightAttackID).GetComponent<AttackAction>();
        leftAttackAction.Init(this, false);
        rightAttackAction.Init(this, true);

        arms.Start(this);
        legs.Start(this);
        eyes.Start(this);
        physics.Start(this);
    }

    public void UpdateInputs()
    {
        UpdateFightingInputDirection();
        arms.Update();
        legs.Update();
        eyes.Update();
        physics.Update();
    }

    public Vector3 GetFixedUpdateDisplacement()
    {
        dt = Time.deltaTime * gameSpeed;

        if (!IsHumanPlayer)
            AI.FixedUpdate();

        arms.FixedUpdate();
        Vector3 displacement = legs.FixedUpdate();
        displacement += physics.FixedUpdate();

        coRobotUpdated = true;

        return displacement;
    }

    public Collider[] GetHitBoxes() => arms.GetCurrentHitBoxes();
    public Collider[] GetHurtBoxes() => hurtBoxes;
    public abstract bool IsGrounded();
    public abstract Vector3 GetGravitationalAcceleration();

    protected abstract void UpdateFightingInputDirection();

    public abstract void RotateTowardsOpponent(float maxDegreesDelta);

    public void Rotate(Quaternion rotation)
    {
        transform.rotation = rotation;
    }

    /// <summary>
    /// This will continue execution of the Coroutine immediatley after the MonoBehaviour.Update() proceeding this Robot's next FixedUpdate()
    /// </summary>
    public WaitUntil WaitForUpdateRobot()
    {
        coRobotUpdated = false;
        return new WaitUntil(() => coRobotUpdated);
    }
}
