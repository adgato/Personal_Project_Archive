using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZeroWeight : Weight
{
	//Zero weight for a reason
    private void OnValidate() { Mass = 0; }
    public override float mass { get { return 0; } }

	[Space]
	[Header("Collsion Detection Settings")]
	public CollisionDetection kinematicBody;
	public enum CapsuleAxis { xAxis, yAxis, zAxis }
	public CapsuleAxis capsuleAxis;
	public bool isShip { get; protected set; } = false;

	public virtual void Start()
    {
		kinematicBody.Init(this);
	}

	public virtual Vector3 MoveRelative(Vector3 displacement)
	{
		Vector3 motion = displacement / Time.fixedDeltaTime;
		kinematicBody.GetSafeMotion(motion, out Vector3 safeDisplacement, out _);

		if (CameraState.inHive && !isShip)
			sigWeight.transform.GetChild(6).position -= safeDisplacement;
		else
			Teleport(safeDisplacement);

		//We need friction as otherwise velocity will grow too large and 0weight will clip, since we can't add advisedImpulse without losing control of the 0weight.
		if (kinematicBody.isGrounded && sigWeight != null)
			velocity = Vector3.Lerp(velocity, sigWeight.velocity, 0.1f);

		return safeDisplacement;
	}

	[System.Serializable]
	public class CollisionDetection
    {
		public float slopeLimit = 70;
		public float stepOffset = 0.3f;
		public float skinWidth = 0.02f;

		[SerializeField] public Vector3 m_center = Vector3.zero;
		[SerializeField] public float m_radius = 0.7f;
		[SerializeField] public float m_height = 3.23f;
		[SerializeField] private FabrikLeg[] legs;

		private Vector3 m_position;
		private Vector3 m_upDirection;
		private CapsuleAxis axis;
		private bool isShip = false;

		private ZeroWeight body;
		private CapsuleCollider collider;

		private readonly Collider[] m_overlaps = new Collider[5];
		private readonly List<RaycastHit> m_contacts = new List<RaycastHit>();

		private const int MaxSweepSteps = 5;
		private const float MinMoveDistance = 0f;
		private const float MinCeilingAngle = 145;

		public Vector3 velocity { get; set; }
		public bool isGrounded { get; private set; }
		public Vector3 center
		{
			get { return m_center; }
			set
			{
				m_center = value;
				collider.center = value;
			}
		}
		public float radius
		{
			get { return m_radius; }
			set
			{
				m_radius = value;
				collider.radius = value;
			}
		}
		public float height
		{
			get { return m_height; }
			set
			{
				m_height = value;
				collider.height = value;
			}
		}

		public void Init(ZeroWeight _thisWeight)
        {
			body = _thisWeight;
			isShip = body.isShip;
			axis = body.capsuleAxis;

			if (!body.TryGetComponent(out collider))
				collider = body.gameObject.AddComponent<CapsuleCollider>();

			collider.center = m_center;
			collider.height = m_height;
			collider.radius = m_radius + Physics.defaultContactOffset;
			collider.direction = (int)axis;
		}
		
		public void GetSafeMotion(Vector3 motion, out Vector3 safeDisplacement, out Vector3 advisedImpulse)
		{
			m_position = body.position;
			m_upDirection = axis == CapsuleAxis.xAxis ? body.transform.right : axis == CapsuleAxis.yAxis ? body.transform.up : body.transform.forward;
			velocity = motion;

			m_contacts.Clear();
			isGrounded = false;

			//Calculate a safe displacement to move by
			if (velocity.sqrMagnitude > MinMoveDistance)
			{
				if (isShip)
				{
					CapsuleSweep(velocity.normalized, velocity.magnitude * Time.fixedDeltaTime, 0);
				}
                else
                {
					Vector3 localVelocity = body.transform.InverseTransformDirection(velocity);
					Vector3 lateralVelocity = new Vector3(localVelocity.x, 0, localVelocity.z);
					Vector3 verticalVelocity = new Vector3(0, localVelocity.y, 0);

					lateralVelocity = body.transform.TransformDirection(lateralVelocity) * Time.fixedDeltaTime;
					verticalVelocity = body.transform.TransformDirection(verticalVelocity) * Time.fixedDeltaTime;

					CapsuleSweep(lateralVelocity.normalized, lateralVelocity.magnitude, stepOffset, MinCeilingAngle);
					CapsuleSweep(verticalVelocity.normalized, verticalVelocity.magnitude, 0, 0, slopeLimit);
				}
			}

			//Adjust velocity here as a whole vector
			if (m_contacts.Count > 0)
			{
				foreach (RaycastHit contact in m_contacts)
				{
					float angle = Vector3.Angle(m_upDirection, contact.normal);

					if (angle <= slopeLimit || isShip)
						isGrounded = true;

					if (isShip)
						((ShipWeight)body).ApplyNormal(contact.normal);

					velocity -= Vector3.Project(velocity, contact.normal);
				}
			}

			//Fix any overlaps (these cannot be fixed by CapsuleSweep)

			///START OF NOT MY CODE

			float capsuleOffset = m_height * 0.5f - m_radius;
			Vector3 top = m_position + m_upDirection * capsuleOffset;
			Vector3 bottom = m_position - m_upDirection * capsuleOffset;
			int overlapsNum = Physics.OverlapCapsuleNonAlloc(top, bottom, collider.radius, m_overlaps);
			if (overlapsNum > 0)
			{
				for (int i = 0; i < overlapsNum; i++)
				{
					//Don't want the ship capsule to detect collsion with its individual colliders or the player capsule (as then the ship would displace away from the player and itself)
					//Don't want the player capsule to detect collsion with the ship capsule (as then the player couldn't enter the ship)
					bool layerMask = isShip && (m_overlaps[i].gameObject.layer == 6 || m_overlaps[i].gameObject.layer == 11 || m_overlaps[i].gameObject.layer == 12) || !isShip && m_overlaps[i].gameObject.layer == 3;
					if (!layerMask && m_overlaps[i].transform != body.transform &&
						Physics.ComputePenetration(collider, m_position, body.transform.rotation, m_overlaps[i], m_overlaps[i].transform.position, m_overlaps[i].transform.rotation, out Vector3 direction, out float distance))
					{
						m_position += direction * (distance + skinWidth);
						velocity -= Vector3.Project(velocity, -direction);
					}
				}
			}

			///END OF NOT MY CODE

			safeDisplacement = m_position - body.position;
			advisedImpulse = velocity - motion;

			//Update leg targets by substracting safeDisplacement (remember the player position is always zero so stuff like this needs to be done explicitly)
			foreach (FabrikLeg leg in legs)
			{
				leg.deltaPos = safeDisplacement;
				leg.prevTarget -= safeDisplacement;
				if (leg.walkCycle >= 2)
				{
					leg.midTarget -= safeDisplacement;
					leg.endTarget -= safeDisplacement;
				}
			}
		}

		private void CapsuleSweep(Vector3 direction, float distance, float stepOffset, float minSlideAngle = 0, float maxSlideAngle = 360)
		{
			float capsuleOffset = m_height * 0.5f - m_radius;

			//Don't want the ship capsule to detect collsion with its individual colliders or the player capsule (as then the ship would displace away from the player and itself)
            //Don't want the player capsule to detect collsion with the ship capsule (as then the player couldn't enter the ship)
			int layerMask = isShip ? ~(1 << 6 | 1 << 11 | 1 << 12) : ~(1 << 3); //IF SHIP 1...1011111 ELSE 1...1111011
			
			///START OF NOT MY CODE

			for (int i = 0; i < MaxSweepSteps; i++)
			{
				Vector3 world_centre = body.transform.TransformDirection(m_center.normalized) * m_center.magnitude;
				Vector3 origin = m_position + world_centre - direction * m_radius;
				Vector3 bottom = origin - m_upDirection * (capsuleOffset - stepOffset);
				Vector3 top = origin + m_upDirection * capsuleOffset;
				
				if (Physics.CapsuleCast(top, bottom, m_radius, direction, out RaycastHit hitInfo, distance + m_radius, layerMask))
				{
					float slideAngle = Vector3.Angle(m_upDirection, hitInfo.normal);
					float safeDistance = hitInfo.distance - m_radius - skinWidth;
					m_position += direction * safeDistance;
					m_contacts.Add(hitInfo);

					if ((slideAngle >= minSlideAngle) && (slideAngle <= maxSlideAngle))
						break;

					direction = Vector3.ProjectOnPlane(direction, hitInfo.normal);
					distance -= safeDistance;
				}
				else
				{
					m_position += direction * distance;
					break;
				}
			}

			///END OF NOT MY CODE
		}
	}
}
