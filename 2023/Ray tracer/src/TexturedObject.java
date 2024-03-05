package uk.ac.cam.cl.gfxintro.ac2620.tick1;


public abstract class TexturedObject extends SceneObject {

	protected Texture normalMap;
	protected Texture textureMap;
	protected Texture reflectMap;
	protected Texture specularMap;

	private final double NORMAL_STRENGTH = 1;

	public TexturedObject(ColorRGB colour, String normalMapName, String textureMapName, String reflectMapName, String specularMapName, double kD, double kS,
			double alphaS, double reflectivity, double refractive_index, ColorRGB transmittance, Vector3 normalTiling, Vector3 textureTiling, Vector3 reflectTiling) {
		this.colour = colour;

		normalMap = new Texture(normalMapName, normalTiling.x, normalTiling.y);
		textureMap = new Texture(textureMapName, textureTiling.x, textureTiling.y);
		reflectMap = new Texture(reflectMapName, reflectTiling.x, reflectTiling.y);
		specularMap = new Texture(specularMapName, textureTiling.x, textureTiling.y);

		this.phong_kD = kD;
		this.phong_kS = kS;
		this.phong_alpha = alphaS;
		this.reflectivity = reflectivity;
		this.refractive_index = refractive_index;
		this.transmittance = transmittance;
	}

	@Override
	public ColorRGB getColour(Vector3 position) {
		if (textureMap.isNull())
			return colour;
		return sampleSurfaceTexture(position, textureMap).scale(colour);
	}

	@Override
	public double getReflectivityAt(Vector3 position) {
		if (reflectMap.isNull())
			return reflectivity;
		return sampleSurfaceTexture(position, reflectMap).r * reflectivity;
	}

	@Override
	public ColorRGB getTransmittanceAt(Vector3 position) {
		if (reflectMap.isNull())
			return transmittance;
		return sampleSurfaceTexture(position, reflectMap).scale(transmittance);
	}

	@Override
	public double getPhong_kS(Vector3 position) {
		if (specularMap.isNull())
			return phong_kS;
		return sampleSurfaceTexture(position, specularMap).r * phong_kS;
	}


	// Get normal to surface at position
	@Override
	public Vector3 getNormalAt(Vector3 position) {

		TangentVectors t = getTangentVectors(position);
		if (normalMap.isNull())
			return t.out;

		ColorRGB normal = sampleSurfaceTexture(position, normalMap).scale(2).subtract(1);

		return (t.out.scale(normal.b).add(t.right.scale(normal.r * NORMAL_STRENGTH)).add(t.up.scale(normal.g * NORMAL_STRENGTH))).normalised();
	}
	
	protected abstract TangentVectors getTangentVectors(Vector3 position);
	
	protected abstract ColorRGB sampleSurfaceTexture(Vector3 position, Texture texture);

}
