package uk.ac.cam.cl.gfxintro.ac2620.tick1;

public abstract class SceneObject {
	
	// The diffuse colour of the object
	protected ColorRGB colour;

	// Coefficients for calculating Phong illumination
	protected double phong_kD, phong_kS, phong_alpha;

	// How reflective this object is
	protected double reflectivity;

	// How much light is transmitted through the object (between 0 and 1)
	protected ColorRGB transmittance;
	protected double refractive_index;


	protected SceneObject() {
		colour = new ColorRGB(1);
		transmittance = new ColorRGB(0);
		phong_kD = phong_kS = phong_alpha = reflectivity = 0;
	}

	// Intersect this object with ray
	public RaycastHit intersectionWith(Ray ray) {
		return intersection(ray, 1);
	}
	protected abstract RaycastHit intersection(Ray ray, int normalScale);

	// Get normal to object at position
	public abstract Vector3 getNormalAt(Vector3 position);

	public ColorRGB getColour(Vector3 position) {
		return colour;
	}

	public void setColour(ColorRGB colour) {
		this.colour = colour;
	}

	public double getPhong_kD() {
		return phong_kD;
	}

	public double getPhong_kS(Vector3 position) {
		return phong_kS;
	}

	public double getPhong_alpha() {
		return phong_alpha;
	}

	public double getReflectivityAt(Vector3 position) {
		return reflectivity;
	}

	public ColorRGB getTransmittanceAt(Vector3 position) {
		return transmittance;
	}

	public double getRefractiveIdx() {
		return refractive_index;
	}

	public void setReflectivity(double reflectivity) {
		this.reflectivity = reflectivity;
	}

	//very basic support for internal reflection (either do it or don't)
	public Ray handleRefraction(Ray ray, int internalBounces) {

		RaycastHit exit = intersection(ray, -1);

		if (exit.getObjectHit() == null) {
			return ray;
		}

		if (internalBounces <= 0)
			new Ray(exit.getLocation().add(exit.getNormal().scale(-Renderer.EPSILON)), exit.getNormal().scale(-1));
			
		Vector3 T = ray.getDirection().scale(-1).refractIn(exit.getNormal(), refractive_index);

		//total internal reflection
		if (T.isZero()) {
			return handleRefraction(
				new Ray(exit.getLocation().add(exit.getNormal().scale(Renderer.EPSILON)), //
						ray.getDirection().scale(-1).reflectIn(exit.getNormal())),
						internalBounces - 1);
		}

		return new Ray(exit.getLocation().add(exit.getNormal().scale(-Renderer.EPSILON)), T);
	}
}
