package uk.ac.cam.cl.gfxintro.ac2620.tick1;

public class PointLight {

	// Point light parameters
	private Vector3 position;
	private ColorRGB colour;
	private double intensity;
	private boolean castShadows;

	public static final double EFFECT_RANGE = 100;

	public PointLight(Vector3 position, ColorRGB colour, double intensity, boolean castShadows) {
		this.position = position;
		this.colour = colour;
		this.intensity = intensity;
		this.castShadows = castShadows;
	}

	public Vector3 getPosition() {
		return position;
	}

	public ColorRGB getColour() {
		return colour;
	}

	public double getIntensity() {
		return intensity;
	}

	public boolean doesCastShadows() {
		return castShadows;
	}

	// Get colour of light at a certain distance away
	public ColorRGB getIlluminationAt(double distance) {
		return colour.scale(intensity / (Math.PI * 4 * Math.pow(distance, 2)));
	}
}
