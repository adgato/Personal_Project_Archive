package uk.ac.cam.cl.gfxintro.ac2620.tick1;


//Why can't I inherit from both TexturedObject AND Sphere! :(
public class TexturedPlane extends TexturedObject {

	// A point in the plane
	private Vector3 point;

	// The normal of the plane
	private Vector3 normal;


	private Vector3 fwdDir;
	private Vector3 rhtDir;

	public TexturedPlane(Vector3 point, Vector3 normal, Vector3 fwdDir, ColorRGB colour,
			String normalMapName, String textureMapName, String reflectMapName, String specularMapName,
			double kD, double kS,
			double alphaS, double reflectivity, Vector3 normalTiling, Vector3 textureTiling, Vector3 reflectTiling) {

		super(colour, normalMapName, textureMapName, reflectMapName, specularMapName, kD, kS, alphaS, reflectivity, 1, new ColorRGB(0),
				normalTiling, textureTiling, reflectTiling);

		this.point = point;
		this.normal = normal;
		this.colour = colour;

		this.fwdDir = fwdDir.normalised();
		rhtDir = this.normal.cross(fwdDir);

		this.phong_kD = kD;
		this.phong_kS = kS;
		this.phong_alpha = alphaS;
		this.reflectivity = reflectivity;
	}


	@Override
	protected ColorRGB sampleSurfaceTexture(Vector3 position, Texture texture) {

		Vector3 out = position.subtract(point);
		double discx = rhtDir.dot(out) - 0.5;
		double discy = fwdDir.dot(out) - 0.5;
		return texture.getPixel(discx, -discy);
	}

	@Override
	protected TangentVectors getTangentVectors(Vector3 position) {
		return new TangentVectors(normal, fwdDir, rhtDir);
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

}
