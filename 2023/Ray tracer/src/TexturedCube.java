package uk.ac.cam.cl.gfxintro.ac2620.tick1;


//Why can't I inherit from both TexturedObject AND Sphere! :(
public class TexturedCube extends TexturedObject {

	private Vector3 centre;

	private double extent;

	private Vector3 uppDir;
	private Vector3 fwdDir;
	private Vector3 rhtDir;

	private Plane fwdFace;
	private Plane bckFace;
	private Plane uppFace;
	private Plane dwnFace;
	private Plane rhtFace;
	private Plane lftFace;

	public TexturedCube(Vector3 centre, Vector3 direction, Vector3 fwdDir, double height, ColorRGB colour,
			String normalMapName, String textureMapName, String reflectMapName, String specularMapName,
			double kD, double kS, double alphaS, double reflectivity, double refractive_index, ColorRGB transmittance,
			Vector3 normalTiling, Vector3 textureTiling, Vector3 reflectTiling) {

		super(colour, normalMapName, textureMapName, reflectMapName, specularMapName, kD, kS, alphaS, reflectivity, refractive_index, transmittance,
				normalTiling, textureTiling, reflectTiling);
		
		this.centre = centre;
		this.extent = height / 2;

		uppDir= direction.normalised();//Vector3.randomInsideUnitSphere().normalised();
		this.fwdDir = fwdDir.normalised();
		rhtDir = uppDir.cross(fwdDir);

		fwdFace = new Plane(centre.add(fwdDir.scale(extent)), fwdDir);
		bckFace = new Plane(centre.add(fwdDir.scale(-extent)), fwdDir.scale(-1));
		uppFace = new Plane(centre.add(uppDir.scale(extent)), uppDir);
		dwnFace = new Plane(centre.add(uppDir.scale(-extent)), uppDir.scale(-1));
		rhtFace = new Plane(centre.add(rhtDir.scale(extent)), rhtDir);
		lftFace = new Plane(centre.add(rhtDir.scale(-extent)), rhtDir.scale(-1));
	}
	
	@Override
	protected TangentVectors getTangentVectors(Vector3 position) {

		int face = faceAxis(position);
		int axis = Math.abs(face);
		int dir = (int)Math.signum(face);

		Vector3 out = axis == 1 ? fwdDir.scale(dir) : axis == 2 ? uppDir.scale(dir) : rhtDir.scale(dir);
		Vector3 upp = axis == 1 ? uppDir.scale(dir) : axis == 2 ? rhtDir.scale(dir) : fwdDir.scale(dir);
		Vector3 rht = axis == 1 ? rhtDir.scale(dir) : axis == 2 ? fwdDir.scale(dir) : uppDir.scale(dir);
		
		return new TangentVectors(out, upp, rht);
	}
	
	@Override
	protected ColorRGB sampleSurfaceTexture(Vector3 position, Texture texture) {

		int face = faceAxis(position);
		int axis = Math.abs(face);
		int dir = (int)Math.signum(face);

		Vector3 out = axis == 1 ? fwdDir.scale(dir) : axis == 2 ? uppDir.scale(dir) : rhtDir.scale(dir);
		Vector3 upp = axis == 1 ? uppDir : axis == 2 ? rhtDir : uppDir;
		Vector3 rht = axis == 1 ? rhtDir : axis == 2 ? fwdDir : fwdDir;

		Vector3 localPos = position.subtract(centre.add(out.scale(extent)));

		double discx = 0.5 - 0.5 * rht.dot(localPos);
		double discy = 0.5 - 0.5 * upp.dot(localPos);

		return texture.getPixel(discy, discx);
	}
	
	@Override
	public RaycastHit intersection(Ray ray, int normalScale) {

		RaycastHit hitInfo = fwdFace.intersection(ray, normalScale);
		if (hitInfo.getObjectHit() != null) {
			Vector3 localHitPos = hitInfo.getLocation().subtract(centre.add(fwdDir.scale(extent)));
			if (Math.abs(uppDir.dot(localHitPos)) <= extent && Math.abs(rhtDir.dot(localHitPos)) <= extent)
				return new RaycastHit(this, hitInfo.getDistance(), hitInfo.getLocation(), hitInfo.getNormal());
		}
		hitInfo = bckFace.intersection(ray, normalScale);
		if (hitInfo.getObjectHit() != null) {
			Vector3 localHitPos = hitInfo.getLocation().subtract(centre.add(fwdDir.scale(-extent)));
			if (Math.abs(uppDir.dot(localHitPos)) <= extent && Math.abs(rhtDir.dot(localHitPos)) <= extent)
				return new RaycastHit(this, hitInfo.getDistance(), hitInfo.getLocation(), hitInfo.getNormal());
		}
		hitInfo = uppFace.intersection(ray, normalScale);
		if (hitInfo.getObjectHit() != null) {
			Vector3 localHitPos = hitInfo.getLocation().subtract(centre.add(uppDir.scale(extent)));
			if (Math.abs(fwdDir.dot(localHitPos)) <= extent && Math.abs(rhtDir.dot(localHitPos)) <= extent)
				return new RaycastHit(this, hitInfo.getDistance(), hitInfo.getLocation(), hitInfo.getNormal());
		}
		hitInfo = dwnFace.intersection(ray, normalScale);
		if (hitInfo.getObjectHit() != null) {
			Vector3 localHitPos = hitInfo.getLocation().subtract(centre.add(uppDir.scale(-extent)));
			if (Math.abs(fwdDir.dot(localHitPos)) <= extent && Math.abs(rhtDir.dot(localHitPos)) <= extent)
				return new RaycastHit(this, hitInfo.getDistance(), hitInfo.getLocation(), hitInfo.getNormal());
		}
		hitInfo = rhtFace.intersection(ray, normalScale);
		if (hitInfo.getObjectHit() != null) {
			Vector3 localHitPos = hitInfo.getLocation().subtract(centre.add(rhtDir.scale(extent)));
			if (Math.abs(fwdDir.dot(localHitPos)) <= extent && Math.abs(uppDir.dot(localHitPos)) <= extent)
				return new RaycastHit(this, hitInfo.getDistance(), hitInfo.getLocation(), hitInfo.getNormal());
		}
		hitInfo = lftFace.intersection(ray, normalScale);
		if (hitInfo.getObjectHit() != null) {
			Vector3 localHitPos = hitInfo.getLocation().subtract(centre.add(rhtDir.scale(-extent)));
			if (Math.abs(fwdDir.dot(localHitPos)) <= extent && Math.abs(uppDir.dot(localHitPos)) <= extent)
				return new RaycastHit(this, hitInfo.getDistance(), hitInfo.getLocation(), hitInfo.getNormal());
		}

		return new RaycastHit();
	}

	private int faceAxis(Vector3 position) {

		Vector3 localPos = position.subtract(centre);
		double fwdDot = Math.abs(localPos.dot(fwdDir));
		double uppDot = Math.abs(localPos.dot(uppDir));
		double rhtDot = Math.abs(localPos.dot(rhtDir));
		return (int) (
			fwdDot > uppDot && fwdDot > rhtDot ? Math.signum(localPos.dot(fwdDir)) : 
			uppDot > rhtDot ? 2 * Math.signum(localPos.dot(uppDir)) : 
			3 * Math.signum(localPos.dot(rhtDir)));
	}
}
