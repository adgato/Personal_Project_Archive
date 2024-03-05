package uk.ac.cam.cl.gfxintro.ac2620.tick1;


//Why can't I inherit from both TexturedObject AND Sphere! :(
public class TexturedSphere extends TexturedObject {

	private Vector3 centre;
	private double radius;


	public TexturedSphere(Vector3 centre, double radius, ColorRGB colour,
			String normalMapName, String textureMapName, String reflectMapName, String specularMapName,
			double kD, double kS, double alphaS, double reflectivity, double refractive_index, ColorRGB transmittance,
			Vector3 normalTiling, Vector3 textureTiling, Vector3 reflectTiling) {

		super(colour, normalMapName, textureMapName, reflectMapName, specularMapName, kD, kS, alphaS, reflectivity, refractive_index, transmittance,
				normalTiling, textureTiling, reflectTiling);

		this.centre = centre;
		this.radius = radius;
	}
	
	@Override
	protected TangentVectors getTangentVectors(Vector3 position) {
		//(x,y,z) => (r,φ,θ) = (xyz.magnitude, atan2(z, x), acos(y))
		//(r,φ,θ) => (x,y,z) = (rcosφsinθ, rsinφsinθ, rcosθ)
		//∂(x,y,z)/∂φ = (-rsinφsinθ, rcosφsinθ, 0)
		//∂(x,y,z)/∂θ = (rcosφcosθ, rsinφcosθ, -rsinθ)

		Vector3 out = position.subtract(centre).normalised();
		double phi = Math.atan2(out.z, out.x);

		Vector3 right = new Vector3(Math.sin(phi), -Math.cos(phi), 0);
		Vector3 up = out.cross(right);
		return new TangentVectors(out, up, right);
	}
	
	@Override
	protected ColorRGB sampleSurfaceTexture(Vector3 position, Texture texture) {

		Vector3 normalUp = position.subtract(centre).normalised();
		double x = normalUp.x;
		double y = normalUp.y;
		double z = normalUp.z;
		double phi = 2 * Math.PI - Math.atan2(x, z);
		double theta = Math.acos(y);

		return texture.getPixel(phi / (2 * Math.PI), theta / Math.PI);
	}
	
	@Override
	public RaycastHit intersection(Ray ray, int normalScale) {

		// Get ray parameters
		Vector3 O = ray.getOrigin();
		Vector3 D = ray.getDirection();

		// Get sphere parameters
		Vector3 C = centre;
		double r = radius;

		Vector3 A = O.subtract(C);

		// Calculate quadratic coefficients
		double a = 1;
		double b = 2 * D.dot(A);
		double c = A.dot(A) - Math.pow(r, 2);
		double d = b * b - 4 * a * c;

		if (d >= 0) {

			double s = Math.sqrt(d) * normalScale;
			double dstToSphere = -(b + s) / (2 * a);

			if (dstToSphere < 0)
				return new RaycastHit();

			Vector3 location = O.add(D.scale(dstToSphere));
			return new RaycastHit(this, dstToSphere, location, location.subtract(centre).normalised().scale(normalScale));
		}

		return new RaycastHit();
	}

}
