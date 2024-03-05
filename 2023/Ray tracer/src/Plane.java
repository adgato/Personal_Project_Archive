package uk.ac.cam.cl.gfxintro.ac2620.tick1;

public class Plane extends SceneObject {
	
	// Plane constants
	private static final double DEFAULT_PLANE_KD = 0.6;
	private static final double DEFAULT_PLANE_KS = 0.0;
	private static final double DEFAULT_PLANE_ALPHA = 0.0;
	private static final double DEFAULT_PLANE_REFLECTIVITY = 0.1;

	// A point in the plane
	private Vector3 point;

	// The normal of the plane
	private Vector3 normal;

	public Plane(Vector3 point, Vector3 normal) {
		this.point = point;
		this.normal = normal;
	}

	public Plane(Vector3 point, Vector3 normal, ColorRGB colour) {
		this.point = point;
		this.normal = normal;
		this.colour = colour;

		this.phong_kD = DEFAULT_PLANE_KD;
		this.phong_kS = DEFAULT_PLANE_KS;
		this.phong_alpha = DEFAULT_PLANE_ALPHA;
		this.reflectivity = DEFAULT_PLANE_REFLECTIVITY;
	}

	public Plane(Vector3 point, Vector3 normal, ColorRGB colour, double kD, double kS, double alphaS, double reflectivity) {
		this.point = point;
		this.normal = normal;
		this.colour = colour;

		this.phong_kD = kD;
		this.phong_kS = kS;
		this.phong_alpha = alphaS;
		this.reflectivity = reflectivity;
	}

	// Intersect this plane with a ray
	@Override
	protected RaycastHit intersection(Ray ray, int normalScale) {
		// Get ray parameters
		Vector3 O = ray.getOrigin();
		Vector3 D = ray.getDirection();
		
		// Get plane parameters
		Vector3 Q = this.point;
		Vector3 N = this.normal.scale(normalScale);

		if (D.dot(N) >= 0)
			return new RaycastHit();

		double s = Q.subtract(O).dot(N) / D.dot(N);

		if (s < 0)
			return new RaycastHit();

		return new RaycastHit(this, s, O.add(D.scale(s)), N);
	}

	// Get normal to the plane
	@Override
	public Vector3 getNormalAt(Vector3 position) {
		return normal; // normal is the same everywhere on the plane
	}
}
