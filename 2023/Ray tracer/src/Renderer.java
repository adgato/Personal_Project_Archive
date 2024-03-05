package uk.ac.cam.cl.gfxintro.ac2620.tick1;

import java.awt.image.BufferedImage;
import java.time.LocalDateTime;
import java.time.temporal.ChronoUnit;
import java.util.List;

public class Renderer {
	
	// The width and height of the image in pixels
	private int width, height;
	
	// Bias factor for reflected and shadow rays
	public static final double EPSILON = 0.0001;

	private final int INTERNAL_BOUNCES = 2;

	//Tick 1*
	private final int SHADOW_RAY_COUNT = 1; // no. of spawned
	private final double LIGHT_SIZE = 0.2; // size of spherical light
	private final int DOF_RAY_COUNT = 10; // no. of spawned DoF rays
	private final double DOF_FOCAL_PLANE = 1; // focal length of source
	private final double DOF_AMOUNT = 0.0002; // amount of DoF effect

	//Tick 1
	//private final int SHADOW_RAY_COUNT = 1;
	//private final double LIGHT_SIZE = 0;
	//private final int DOF_RAY_COUNT = 1;
	//private final double DOF_FOCAL_PLANE = 1;
	//private final double DOF_AMOUNT = 0;

	//No shadows
	//private final int SHADOW_RAY_COUNT = 0;
	//private final double LIGHT_SIZE = 0;
	//private final int DOF_RAY_COUNT = 1;
	//private final double DOF_FOCAL_PLANE = 1;
	//private final double DOF_AMOUNT = 0;

	// The number of times a ray can bounce for reflection
	private int bounces;
	
	// Background colour of the image
	private ColorRGB backgroundColor = new ColorRGB(0.001);

	public Renderer(int width, int height, int bounces) {
		this.width = width;
		this.height = height;
		this.bounces = bounces;
	}

	/*
	 * Trace the ray through the supplied scene, returning the colour to be rendered.
	 * The bouncesLeft parameter is for rendering reflective surfaces.
	 */
	protected ColorRGB trace(Scene scene, Ray ray, int bouncesLeft) {

		// Find closest intersection of ray in the scene
		RaycastHit closestHit = scene.findClosestIntersection(ray);

        // If no object has been hit, return a background colour
        SceneObject object = closestHit.getObjectHit();
        if (object == null) {
            return backgroundColor;
        }
        
        // Otherwise calculate colour at intersection and return
        // Get properties of surface at intersection - location, surface normal
        Vector3 P = closestHit.getLocation();
        Vector3 N = closestHit.getNormal();
		Vector3 O = ray.getOrigin();


		ColorRGB directIllumination = backgroundColor;
		Vector3 V = ray.getDirection().scale(-1);


		if (V.dot(N) < 0)
			return new ColorRGB(1, 0, 1);

		double reflectivity = object.getReflectivityAt(P);
		ColorRGB transmittance = object.getTransmittanceAt(P);
		ColorRGB opaque = transmittance.scale(-1).add(1);

		if (reflectivity != 0 || !opaque.isZero())
			directIllumination = illuminate(scene, object, P, N, O);

		if (bouncesLeft <= 0) // || reflectivity == 0
			return directIllumination;

		Vector3 T = V.refractIn(N, 1 / Math.abs(object.getRefractiveIdx()));

		ColorRGB totalIllumination = directIllumination.scale(1 - reflectivity).scale(opaque);

		if (!T.isZero() && !transmittance.isZero()) {
			Ray refractRay = new Ray(P.subtract(N.scale(EPSILON)), T);

			ColorRGB refractedIllumination = trace(scene, object.handleRefraction(refractRay, INTERNAL_BOUNCES), bouncesLeft - 1).scale(transmittance);

			totalIllumination = totalIllumination.add(refractedIllumination);
		}
		if (reflectivity > 0 && !opaque.isZero()) {
			Vector3 R = V.reflectIn(N);
			Ray reflectRay = new Ray(P.add(N.scale(EPSILON)), R);
			ColorRGB reflectIllumination = trace(scene, reflectRay, bouncesLeft - 2).scale(reflectivity).scale(opaque);
			
			totalIllumination = totalIllumination.add(reflectIllumination);
		}

     	// Illuminate the surface
		
     	return totalIllumination;
	}

	/*
	 * Illuminate a surface on and object in the scene at a given position P and surface normal N,
	 * relative to ray originating at O
	 */
	private ColorRGB illuminate(Scene scene, SceneObject object, Vector3 P, Vector3 N, Vector3 O) {

		ColorRGB I_a = scene.getAmbientLighting(); // Ambient illumination intensity

		ColorRGB C_diff = object.getColour(P); // Diffuse colour defined by the object

		// Get Phong reflection model coefficients
		double k_d = object.getPhong_kD();
		double k_s = object.getPhong_kS(P);
		double alpha = object.getPhong_alpha();

		ColorRGB colourToReturn = C_diff.scale(I_a);

		Vector3 V = O.subtract(P).normalised();
		Vector3 n = object.getNormalAt(P);

		// Loop over each point light source
		List<PointLight> pointLights = scene.getPointLights();
		for (int i = 0; i < pointLights.size(); i++) {
			PointLight light = pointLights.get(i); // Select point light

			Vector3 lightPos = light.getPosition();

			// Calculate point light constants
			double distanceToLight = lightPos.subtract(P).magnitude();

			if (distanceToLight > PointLight.EFFECT_RANGE) //light is too far away
				continue;

			ColorRGB C_spec = light.getColour();
			ColorRGB I = light.getIlluminationAt(distanceToLight);

			Vector3 L = lightPos.subtract(P).normalised();

			if (L.dot(n) < 0 || I.r + I.g + I.b < backgroundColor.r + backgroundColor.g + backgroundColor.b)
				continue;

			Vector3 R = L.reflectIn(n);

			double shadowIntensity = 0;
			if (SHADOW_RAY_COUNT > 0 && light.doesCastShadows())
				shadowIntensity = getSoftShadow(scene, P.add(N.scale(EPSILON)), lightPos);
			ColorRGB diffuse = C_diff.scale(k_d).scale(I).scale(Math.max(0, L.dot(n)));
			ColorRGB specular = C_spec.scale(k_s).scale(I).scale(Math.pow(Math.max(0, R.dot(V)), alpha));

			ColorRGB shadow = object.getTransmittanceAt(P).scale(-1).add(1).scale(-shadowIntensity).add(1);

			colourToReturn = colourToReturn.add(diffuse.add(specular).scale(shadow));
		}
		return colourToReturn;
	}
	
	private double getSoftShadow(Scene scene, Vector3 P, Vector3 lightPos) {
		double hits = 0;

		double distanceToLight = lightPos.subtract(P).magnitude();

		for (int i = 0; i < SHADOW_RAY_COUNT; i++) {
			Vector3 L = lightPos.subtract(P.add(Vector3.randomInsideUnitSphere().scale(LIGHT_SIZE * 0.5))).normalised();
			RaycastHit shadow = scene.findClosestIntersection(new Ray(P, L));
			if (shadow.getObjectHit() != null && shadow.getDistance() < distanceToLight) {
				ColorRGB transmission = shadow.getObjectHit().getTransmittanceAt(shadow.getLocation());
				hits += 1 - (transmission.r + transmission.g + transmission.b) / 3;
			}
		}
		return hits / SHADOW_RAY_COUNT;
	}

	// Render image from scene, with camera at origin
	public BufferedImage render(Scene scene) {
		
		// Set up image
		BufferedImage image = new BufferedImage(width, height, BufferedImage.TYPE_INT_RGB);
		
		// Set up camera
		Camera camera = new Camera(width, height);

		long startTime = System.nanoTime();

		// Loop over all pixels
		for (int y = 0; y < height; ++y) {
			for (int x = 0; x < width; ++x) {
				Ray ogRay = camera.castRay(x, y); // Cast ray through pixel
				//RaycastHit hitInfo = new Plane(new Vector3(0, 0, DOF_FOCAL_PLANE), new Vector3(0, 0, -1), new ColorRGB(0)).intersectionWith(ogRay);
				Vector3 focalPoint = ogRay.getDirection().scale(DOF_FOCAL_PLANE / ogRay.getDirection().z);
				ColorRGB linearRGB = new ColorRGB(0);
				for (int i = 0; i < DOF_RAY_COUNT; i++) {
					double randX = (Math.random() - 0.5) * DOF_AMOUNT;
					double randY = (Math.random() - 0.5) * DOF_AMOUNT;
					Vector3 apatureOrigin = new Vector3(randX, randY, 0);
					Ray ray = new Ray(apatureOrigin, focalPoint.subtract(apatureOrigin).normalised());
					linearRGB = linearRGB.add(trace(scene, ray, bounces)); // Trace path of cast ray and determine colour
				}
				ColorRGB gammaRGB = tonemap(linearRGB.scale(1.0 / DOF_RAY_COUNT));
				image.setRGB(x, y, gammaRGB.toRGB()); // Set image colour to traced colour
			}
			// Display progress every 10 lines
			if (y % 10 == 9 || y == (height - 1)) {
				double complete = y / (float) (height - 1);
				double elapsed = (System.nanoTime() - startTime) / 1_000_000_000d;
				String eta = LocalDateTime.now().plus(Math.round(elapsed / Math.pow(complete, 1.15) * (1 - Math.pow(complete, 1.15) )), ChronoUnit.SECONDS).toString().split("T")[1];
				System.out.println(String.format("%.2f", 100 * complete) + "% completed. ETA " + eta);
			}
			    
		}
		return image;
	}


	// Combined tone mapping and display encoding
	public ColorRGB tonemap( ColorRGB linearRGB ) {
		double invGamma = 1./2.2;
		double a = 2;  // controls brightness
		double b = 1.3; // controls contrast

		// Sigmoidal tone mapping
		ColorRGB powRGB = linearRGB.power(b);
		ColorRGB displayRGB = powRGB.scale( powRGB.add(Math.pow(0.5/a,b)).inv() );

		// Display encoding - gamma
		ColorRGB gammaRGB = displayRGB.power( invGamma );

		return gammaRGB;
	}


}
