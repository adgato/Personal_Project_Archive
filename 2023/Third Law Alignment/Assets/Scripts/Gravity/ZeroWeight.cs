using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZeroWeight : Weight
{
	public override float Mass => 0;
	public override bool IsZeroWeight => true;
	public bool IsGrounded => collisionDetection.isGrounded;
	public Vector3 NormalUp { get; private set; } = Vector3.up;
	public Vector3 HorizontalUnsafeDisplacement { get; private set; } = Vector3.zero;

	[SerializeField] private CollisionDetection collisionDetection;

	public Weight Closest { get; private set; }

	public bool EqualsClosest(Weight weight) => Closest != null && Closest == weight;

	protected override void Start()
    {
		base.Start();
		gameObject.layer = LayerMask.NameToLayer("ZeroWeight");
		collisionDetection.Init(this);
    }

	public void UpdateClosest()
    {
		Closest = null;
		float closestFarness = float.MaxValue;

		foreach (Weight weight in gravity.GetWeights())
        {
			float sqrDist = (Position - weight.Position).sqrMagnitude;
			if (!weight.IsZeroWeight && sqrDist < closestFarness)
            {
				Closest = weight;
				closestFarness = sqrDist;
            }
        }
		if (Closest != null)
        {
			Closest.UpdatePosition();
			Interpolate(Closest.GetDisplacement());
		}
	}

	public override void UpdatePosition()
	{
		if (Closest == null || positionUpToDate)
        {
			base.UpdatePosition();
			return;
		}

		Vector3 relativeDisplacement = GetDisplacement() - Closest.GetDisplacement();

		//Ocean
		if ((Position + relativeDisplacement - Closest.Position).sqrMagnitude < Mathx.Square(Closest.Radius))
			relativeDisplacement -= Vector3.Dot(relativeDisplacement, NormalUp) * NormalUp;

		collisionDetection.GetSafeMotion(relativeDisplacement / Time.deltaTime, out Vector3 safeDisplacement, out Vector3 advisedImpulse);

		if (IsGrounded)
			velocity = Vector3.Lerp(velocity, Closest.Velocity, 0.1f);

		Interpolate(safeDisplacement);

		NormalUp = (Position - Closest.Position).normalized;
		HorizontalUnsafeDisplacement = Vector3.ProjectOnPlane(relativeDisplacement - safeDisplacement, NormalUp);

		positionUpToDate = true;
    }

    public override void UpdateColliders() { }

    //http://web.archive.org/web/20221209225802/https://github.com/marmitoTH/Unity-Kinematic-Body
    [System.Serializable]
	private class CollisionDetection
	{
		private enum CapsuleAxis { xAxis, yAxis, zAxis }

		[SerializeField] private float slopeLimit = 70;
		[SerializeField] private float stepOffset = 0.3f;
		[SerializeField] private float skinWidth = 0.02f;

		[SerializeField] private Vector3 center = Vector3.zero;
		[SerializeField] private float radius = 0.7f;
		[SerializeField] private float height = 3.23f;
		[SerializeField] private CapsuleAxis axis;

		[SerializeField] private bool isUpright;

		private Vector3 m_position;
		private Vector3 m_upDirection;
		private Vector3 velocity;

		private ZeroWeight body;
		private CapsuleCollider collider;

		private readonly Collider[] m_overlaps = new Collider[5];
		private readonly List<RaycastHit> m_contacts = new List<RaycastHit>();

		private const int MaxSweepSteps = 5;
		private const float MinMoveDistance = 0f;
		private const float MinCeilingAngle = 145;
		[ReadOnly] public bool isGrounded;

		public void Init(ZeroWeight zeroWeight)
		{
			body = zeroWeight;
			if (!body.TryGetComponent(out collider))
				collider = body.gameObject.AddComponent<CapsuleCollider>();

			collider.center = center;
			collider.height = height;
			collider.radius = radius + Physics.defaultContactOffset;
			collider.direction = (int)axis;
		}


		public void GetSafeMotion(Vector3 motion, out Vector3 safeDisplacement, out Vector3 advisedImpulse)
		{
			m_position = body.Position;

            switch (axis) 
			{
				case CapsuleAxis.xAxis: m_upDirection = Vector3.ProjectOnPlane(body.transform.right, body.NormalUp).normalized; break;
				case CapsuleAxis.yAxis: m_upDirection = body.NormalUp; break;
				case CapsuleAxis.zAxis: m_upDirection = Vector3.ProjectOnPlane(body.transform.forward, body.NormalUp).normalized; break;
			}

            velocity = motion;

			m_contacts.Clear();
			isGrounded = false;

			//Calculate a safe displacement to move by
			if (velocity.sqrMagnitude > MinMoveDistance)
			{
				if (!isUpright)
				{
					CapsuleSweep(velocity.normalized, velocity.magnitude * Time.deltaTime, 0);
				}
				else
				{
					Vector3 localVelocity = body.baseTransform.InverseTransformDirection(velocity);
					Vector3 lateralVelocity = new Vector3(localVelocity.x, 0, localVelocity.z);
					Vector3 verticalVelocity = new Vector3(0, localVelocity.y, 0);

					lateralVelocity = body.baseTransform.TransformDirection(lateralVelocity) * Time.deltaTime;
					verticalVelocity = body.baseTransform.TransformDirection(verticalVelocity) * Time.deltaTime;

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

					if (angle <= slopeLimit && contact.collider.gameObject.layer != LayerMask.NameToLayer("ZeroWeight") || !isUpright)
						isGrounded = true;

					velocity -= Vector3.Project(velocity, contact.normal);
				}
			}

			//Fix any overlaps (these cannot be fixed by CapsuleSweep)

			float capsuleOffset = height * 0.5f - radius;
			Vector3 top = m_position + m_upDirection * capsuleOffset;
			Vector3 bottom = m_position - m_upDirection * capsuleOffset;
			int overlapsNum = Physics.OverlapCapsuleNonAlloc(top, bottom, collider.radius, m_overlaps);
			if (overlapsNum > 0)
			{
				for (int i = 0; i < overlapsNum; i++)
				{
					bool mask = m_overlaps[i].transform == body.transform || m_overlaps[i].gameObject.layer == 2 || m_overlaps[i].transform.IsChildOf(body.baseTransform);
					if (!mask &&
						Physics.ComputePenetration(collider, m_position, body.transform.rotation, m_overlaps[i], m_overlaps[i].transform.position, m_overlaps[i].transform.rotation, out Vector3 direction, out float distance))
					{
						m_position += direction * (distance + skinWidth);
						velocity -= Vector3.Project(velocity, -direction);
					}
				}
			}

			safeDisplacement = m_position - body.Position;
			advisedImpulse = velocity - motion;
		}

		private void CapsuleSweep(Vector3 direction, float distance, float stepOffset, float minSlideAngle = 0, float maxSlideAngle = 360)
		{
			float capsuleOffset = height * 0.5f - radius;

			//int layerMask = isShip ? ~(1 << 6 | 1 << 11 | 1 << 12) : ~(1 << 3); //IF SHIP 1...1011111 ELSE 1...1111011

			for (int i = 0; i < MaxSweepSteps; i++)
			{
				Vector3 world_centre = body.baseTransform.TransformDirection(center.normalized) * center.magnitude;
				Vector3 origin = m_position + world_centre - direction * radius;
				Vector3 bottom = origin - m_upDirection * (capsuleOffset - stepOffset);
				Vector3 top = origin + m_upDirection * capsuleOffset;

				if (Physics.CapsuleCast(top, bottom, radius, direction, out RaycastHit hitInfo, distance + radius))
				{
					float slideAngle = Vector3.Angle(m_upDirection, hitInfo.normal);
					float safeDistance = hitInfo.distance - radius - skinWidth;
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
		}
	}
}
