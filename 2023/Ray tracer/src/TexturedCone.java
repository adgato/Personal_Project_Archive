package uk.ac.cam.cl.gfxintro.ac2620.tick1;


//Why can't I inherit from both TexturedObject AND Sphere! :(
public class TexturedCone extends TexturedObject {

	private Vector3 tip;
	private Vector3 direction;
	private double radius;
	private double extent;

	private double grad2;
	private double hypot;

	private Vector3 fwdDir;
	private Vector3 rhtDir;

	private Plane disc;

	public TexturedCone(Vector3 tip, Vector3 direction, Vector3 fwdDir, double radius, double height, ColorRGB colour,
			String normalMapName, String textureMapName, String reflectMapName, String specularMapName,
			double kD, double kS, double alphaS, double reflectivity, double refractive_index, ColorRGB transmittance,
			Vector3 normalTiling, Vector3 textureTiling, Vector3 reflectTiling) {

		super(colour, normalMapName, textureMapName, reflectMapName, specularMapName, kD, kS, alphaS, reflectivity, refractive_index, transmittance, normalTiling, textureTiling, reflectTiling);
		
		this.tip = tip;
		this.radius = radius;
		this.extent = height;



		grad2 = extent * extent / (extent * extent + radius * radius);
		hypot = Math.sqrt(extent * extent + radius * radius);

		this.direction = direction.normalised();//Vector3.randomInsideUnitSphere().normalised();

		this.fwdDir = fwdDir.normalised();
		rhtDir = this.direction.cross(fwdDir);

		disc = new Plane(tip.add(direction.scale(extent)), direction);
	}
	
	@Override
	protected TangentVectors getTangentVectors(Vector3 position) {

		int mg = onDisc(position);

		if (mg != 0)
			return new TangentVectors(direction.scale(mg), fwdDir.scale(mg), rhtDir);


		Vector3 up = position.subtract(tip).normalised();
		Vector3 right = direction.cross(up).normalised();
		Vector3 out = right.cross(up);

		return new TangentVectors(out, up, right);
	}
	
	@Override
	protected ColorRGB sampleSurfaceTexture(Vector3 position, Texture texture) {

		int w = texture.getWidth();
		int h = texture.getHeight();

		if (onDisc(position) != 0) {
			Vector3 out = getOutVector(position).scale(1 / radius);
			double discx = 0.5 + 0.5 * rhtDir.dot(out);
			double discy = 0.5 + 0.5 * fwdDir.dot(out);
			int discu = (int) (Math.min(Math.max(discx * w, 0), w - 1));
			int discv = (int) (Math.min(Math.max(discy * h, 0), h - 1));
			return texture.getPixel(discv, discu);
		}

		Vector3 normal = getOutVector(position).normalised();

		double x = normal.dot(rhtDir);
		double y = 0.5 - 0.5 * position.subtract(tip).magnitude() / hypot;
		double z = normal.dot(fwdDir);

		double phi = 2 * Math.PI - Math.atan2(x, z);

		return texture.getPixel(phi / (2 * Math.PI), y);
	}
	
	//Please note, cones do not work great with refraction, because of their geometry i think, so there are some bugs in the method for normalScale < 0
	//Fortunately, I don't really want to use refracting cones in my scene, so this is okay... for now...
	@Override
	public RaycastHit intersection(Ray ray, int normalScale) {
		
		RaycastHit discHit = onDisc(ray, normalScale);
		if (discHit != null)
			return new RaycastHit(this, discHit.getDistance(), discHit.getLocation(), discHit.getNormal());

		// Get ray parameters
		Vector3 O = ray.getOrigin();
		Vector3 D = ray.getDirection();

		// Get sphere parameters
		Vector3 V = direction;
		Vector3 C = tip;

		Vector3 A = O.subtract(C);

		// Calculate quadratic coefficients
		double a = D.dot(V) * D.dot(V) - grad2;
		double b = 2 * (D.dot(V) * A.dot(V) - grad2 * D.dot(A));
		double c = A.dot(V) * A.dot(V) - grad2 * A.dot(A);
		double d = b * b - 4 * a * c;

		if (d >= 0) {

			double s = Math.sqrt(d) * normalScale;
			double dstToCylinder = (-b + s) / (2 * a);

			if (dstToCylinder < 0)
				return new RaycastHit();

			Vector3 location = O.add(D.scale(dstToCylinder));

			double height = getHeight(location);
			if (height < 0 || height > extent)
				return new RaycastHit();

			Vector3 up = location.subtract(tip).normalised();
			Vector3 out = direction.cross(up).cross(up).normalised();

			return new RaycastHit(this, dstToCylinder, location, out.scale(normalScale));
		}

		return new RaycastHit();
	}

	private RaycastHit onDisc(Ray ray, int normalScale) {
		RaycastHit topHit = disc.intersection(ray, normalScale);
		if (topHit.getObjectHit() != null && getOutVector(topHit.getLocation()).sqrMagnitude() <= radius * radius)
			return topHit;

		return null;
	}


	private int onDisc(Vector3 position) {

		double posHeight = getHeight(position);
		if (posHeight > extent * 0.999)
			return 1;
		return 0;
	}

	private Vector3 getOutVector(Vector3 position) {
		Vector3 localPos = position.subtract(tip);
		double vertical = localPos.dot(direction);

		return localPos.subtract(direction.scale(vertical));
	}

	private double getHeight(Vector3 position) {
		return position.subtract(tip).dot(direction);
	}
}
