package uk.ac.cam.cl.gfxintro.ac2620.tick1;


//Why can't I inherit from both TexturedObject AND Sphere! :(
public class TexturedCylinder extends TexturedObject {

	private Vector3 centre;
	private Vector3 direction;
	private double radius;
	private double extent;

	private Vector3 fwdDir;
	private Vector3 rhtDir;

	private Plane topDisc;
	private Plane botDisc;

	public TexturedCylinder(Vector3 centre, Vector3 direction, Vector3 fwdDir, double radius, double height, ColorRGB colour,
			String normalMapName, String textureMapName, String reflectMapName, String specularMapName,
			double kD, double kS, double alphaS, double reflectivity, double refractive_index, ColorRGB transmittance,
			Vector3 normalTiling, Vector3 textureTiling, Vector3 reflectTiling) {

		super(colour, normalMapName, textureMapName, reflectMapName, specularMapName, kD, kS, alphaS, reflectivity, refractive_index, transmittance,
				normalTiling, textureTiling, reflectTiling);
		
		this.centre = centre;
		this.radius = radius;
		this.extent = height / 2;

		this.direction = direction.normalised();//Vector3.randomInsideUnitSphere().normalised();

		this.fwdDir = fwdDir.normalised();
		rhtDir = this.direction.cross(fwdDir);

		topDisc = new Plane(centre.add(direction.scale(extent)), direction);
		botDisc = new Plane(centre.add(direction.scale(-extent)), direction.scale(-1));
	}
	
	@Override
	protected TangentVectors getTangentVectors(Vector3 position) {

		int mg = onDisc(position);

		if (mg != 0)
			return new TangentVectors(direction.scale(mg), fwdDir.scale(mg), rhtDir);

		Vector3 out = getOutVector(position).normalised();

		Vector3 up = direction;
		Vector3 right = up.cross(out);
		
		return new TangentVectors(out, up, right);
	}
	
	@Override
	protected ColorRGB sampleSurfaceTexture(Vector3 position, Texture texture) {

		if (onDisc(position) != 0) {
			Vector3 out = getOutVector(position).scale(1 / radius);
			double discx = 0.5 + 0.5 * rhtDir.dot(out);
			double discy = 0.5 + 0.5 * fwdDir.dot(out);
			return texture.getPixel(discy, discx);
		}

		Vector3 normal = getOutVector(position).normalised();

		double x = normal.dot(rhtDir);
		double y = 0.5 - 0.5 * getHeight(position) / extent;
		double z = normal.dot(fwdDir);

		double phi = 2 * Math.PI - Math.atan2(x, z);

		return texture.getPixel(phi / (2 * Math.PI), y);
	}
	
	@Override
	public RaycastHit intersection(Ray ray, int normalScale) {

		RaycastHit discHit = onDisc(ray, normalScale);
		if (discHit != null)
			return new RaycastHit(this, discHit.getDistance(), discHit.getLocation(), discHit.getNormal());

		// Get ray parameters
		Vector3 O = ray.getOrigin();
		Vector3 D = ray.getDirection();

		// Get sphere parameters
		Vector3 N = direction;
		Vector3 C = centre;
		double r = radius;

		Vector3 A = O.subtract(C);

		Vector3 AN = A.subtract(N.scale(A.dot(N))); //same as getOutVector(O)
		Vector3 DN = D.subtract(N.scale(D.dot(N)));

		// Calculate quadratic coefficients
		double a = DN.dot(DN);
		double b = 2 * DN.dot(AN);
		double c = AN.dot(AN) - Math.pow(r, 2);
		double d = b * b - 4 * a * c;

		if (d >= 0) {

			double s = Math.sqrt(d) * normalScale;
			double dstToCylinder = -(b + s) / (2 * a);

			if (dstToCylinder < 0)
				return new RaycastHit();

			Vector3 location = O.add(D.scale(dstToCylinder));

			if (Math.abs(getHeight(location)) > extent)
				return new RaycastHit();

			return new RaycastHit(this, dstToCylinder, location, getOutVector(location).normalised().scale(normalScale));
		}

		return new RaycastHit();
	}

	private RaycastHit onDisc(Ray ray, int normalScale) {
		RaycastHit topHit = topDisc.intersection(ray, normalScale);
		if (topHit.getObjectHit() != null && getOutVector(topHit.getLocation()).sqrMagnitude() < radius * radius)
			return topHit;

		RaycastHit botHit = botDisc.intersection(ray, normalScale);
		if (botHit.getObjectHit() != null && getOutVector(botHit.getLocation()).sqrMagnitude() < radius * radius)
			return botHit;

		return null;
	}


	private int onDisc(Vector3 position) {

		double posHeight = getHeight(position);
		double sqrPosRad = getOutVector(position).sqrMagnitude();

		if (sqrPosRad >= radius * radius * 0.999)
			return 0;
		else if (posHeight > extent * 0.999)
			return 1;
		else if (posHeight < -extent * 0.999)
			return -1;
		return 0;
	}

	private Vector3 getOutVector(Vector3 position) {
		Vector3 localPos = position.subtract(centre);
		double vertical = localPos.dot(direction);

		return localPos.subtract(direction.scale(vertical));
	}

	private double getHeight(Vector3 position) {
		return position.subtract(centre).dot(direction);
	}
}
