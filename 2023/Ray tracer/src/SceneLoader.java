package uk.ac.cam.cl.gfxintro.ac2620.tick1;

import java.io.File;
import java.io.IOException;

import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;

import org.w3c.dom.Element;
import org.w3c.dom.NodeList;
import org.xml.sax.SAXException;

public class SceneLoader {
	// Loads our scene from an XML file
	
	private Scene scene;

	public SceneLoader(String filename) {
		scene = new Scene();

		Element document = null;
		try {
			document = DocumentBuilderFactory.newInstance().newDocumentBuilder().parse(new File(filename))
					.getDocumentElement();
		} catch (ParserConfigurationException e) {
			assert false;
		} catch (IOException e) {
			throw new RuntimeException("error reading file:\n" + e.getMessage());
		} catch (SAXException e) {
			throw new RuntimeException("error loading XML.");
		}

		if (document.getNodeName() != "scene")
			throw new RuntimeException("scene file does not contain a scene element");

		NodeList elements = document.getElementsByTagName("*");
		for (int i = 0; i < elements.getLength(); ++i) {
			Element element = (Element) elements.item(i);
			switch (element.getNodeName()) {

				case "sphere":
					Sphere sphere = new Sphere(getPosition(element), getDouble(element, "radius", 1),
							getColour(element),
							getDouble(element, "kD", 0.8), getDouble(element, "kS", 1.2),
							getDouble(element, "alphaS", 10), getDouble(element, "reflectivity", 0.3),
							getTransmittance(element));
					scene.addObject(sphere);
					break;

			case "bumpy-sphere":
				BumpySphere bumpySphere = new BumpySphere(getPosition(element), getDouble(element, "radius", 1), getColour(element), getString(element, "bump-map"));
				scene.addObject(bumpySphere);
				break;

			case "plane":
				Plane plane = new Plane(getPosition(element), getNormal(element), getColour(element),
						getDouble(element, "kD", 0.8), getDouble(element, "kS", 1.2),
						getDouble(element, "alphaS", 10), getDouble(element, "reflectivity", 0.3)
						);
				scene.addObject(plane);
				break;

			case "point-light":
				PointLight light = new PointLight(getPosition(element), getColour(element),
						getDouble(element, "intensity", 100), getDouble(element, "casts-shadows", 1) > 0.5);
				scene.addPointLight(light);
				break;

			case "ambient-light":
				scene.setAmbientLight(getColour(element).scale(getDouble(element, "intensity", 1)));
				break;

			case "sphere-t":
				TexturedSphere texturedSphere = new TexturedSphere(getPosition(element), getDouble(element, "radius", 1), 
						getColour(element),
						getString(element, "normal-map"), getString(element, "texture-map"), getString(element, "reflect-map"), getString(element, "specular-map"),
						getDouble(element, "kD", 0.8), getDouble(element, "kS", 1.2),
						getDouble(element, "alphaS", 10), getDouble(element, "reflectivity", 0.3), getDouble(element, "refractive-index", 1),
						getTransmittance(element), getTiling(element, "nm"), getTiling(element, "tm"), getTiling(element, "rm"));
				scene.addObject(texturedSphere);
				break;

			case "cylinder-t":
				TexturedCylinder texturedCylinder = new TexturedCylinder(getPosition(element), getNormal(element), getForward(element),
						getDouble(element, "radius", 1), getDouble(element, "height", 1), 
						getColour(element),
						getString(element, "normal-map"), getString(element, "texture-map"), getString(element, "reflect-map"), getString(element, "specular-map"),
						getDouble(element, "kD", 0.8), getDouble(element, "kS", 1.2),
						getDouble(element, "alphaS", 10), getDouble(element, "reflectivity", 0.3), getDouble(element, "refractive-index", 1),
						getTransmittance(element), getTiling(element, "nm"), getTiling(element, "tm"), getTiling(element, "rm"));
				scene.addObject(texturedCylinder);
				break;

			case "cone-t":
				TexturedCone texturedCone = new TexturedCone(getPosition(element), getNormal(element), getForward(element),
						getDouble(element, "radius", 1), getDouble(element, "height", 1), 
						getColour(element),
						getString(element, "normal-map"), getString(element, "texture-map"), getString(element, "reflect-map"), getString(element, "specular-map"),
						getDouble(element, "kD", 0.8), getDouble(element, "kS", 1.2),
						getDouble(element, "alphaS", 10), getDouble(element, "reflectivity", 0.3), getDouble(element, "refractive-index", 1),
						getTransmittance(element), getTiling(element, "nm"), getTiling(element, "tm"), getTiling(element, "rm"));
				scene.addObject(texturedCone);
				break;

			case "cube-t":
				TexturedCube texturedCube = new TexturedCube(getPosition(element), getNormal(element), getForward(element),
						getDouble(element, "height", 1), 
						getColour(element),
						getString(element, "normal-map"), getString(element, "texture-map"), getString(element, "reflect-map"), getString(element, "specular-map"),
						getDouble(element, "kD", 0.8), getDouble(element, "kS", 1.2),
						getDouble(element, "alphaS", 10), getDouble(element, "reflectivity", 0.3), getDouble(element, "refractive-index", 1),
						getTransmittance(element), getTiling(element, "nm"), getTiling(element, "tm"), getTiling(element, "rm"));
				scene.addObject(texturedCube);
				break;

			case "plane-t":
				TexturedPlane texturedPlane = new TexturedPlane(getPosition(element), getNormal(element), getForward(element),
						getColour(element),
						getString(element, "normal-map"), getString(element, "texture-map"), getString(element, "reflect-map"), getString(element, "specular-map"),
						getDouble(element, "kD", 0.8), getDouble(element, "kS", 1.2),
						getDouble(element, "alphaS", 10), getDouble(element, "reflectivity", 0.3),
						getTiling(element, "nm"), getTiling(element, "tm"), getTiling(element, "rm"));
				scene.addObject(texturedPlane);
				break;

			default:
				throw new RuntimeException("unknown object tag: " + element.getNodeName());
			}
		}
	}

	public Scene getScene() {
		return scene;
	}

	private Vector3 getPosition(Element tag) {
		double x = getDouble(tag, "x", 0);
		double y = getDouble(tag, "y", 0);
		double z = getDouble(tag, "z", 0);
		return new Vector3(x, y, z);
	}

	private Vector3 getNormal(Element tag) {
		double x = getDouble(tag, "nx", 0);
		double y = getDouble(tag, "ny", 1);
		double z = getDouble(tag, "nz", 0);
		return new Vector3(x, y, z).normalised();
	}

	private Vector3 getForward(Element tag) {
		double x = getDouble(tag, "fx", -1);
		double y = getDouble(tag, "fy", 0);
		double z = getDouble(tag, "fz", 0);
		return new Vector3(x, y, z).normalised();
	}

	private Vector3 getTiling(Element tag, String prefix) {
		double x = getDouble(tag, prefix + "x", 1);
		double y = getDouble(tag, prefix + "y", 1);
		return new Vector3(x, y, 0);
	}

	private ColorRGB getColour(Element tag) {

		String hexString = tag.getAttribute("colour");
		double red = Integer.parseInt(hexString.substring(1, 3), 16) / 255.0;
		double green = Integer.parseInt(hexString.substring(3, 5), 16) / 255.0;
		double blue = Integer.parseInt(hexString.substring(5, 7), 16) / 255.0;

		return new ColorRGB(red, green, blue);
	}

	private ColorRGB getTransmittance(Element tag) {

		double tr = getDouble(tag, "tr", 0);
		double tg = getDouble(tag, "tg", 0);
		double tb = getDouble(tag, "tb", 0);

		return new ColorRGB(tr, tg, tb);
	}

	private double getDouble(Element tag, String attribute, double fallback) {
		try {
			return Double.parseDouble(tag.getAttribute(attribute));
		} catch (NumberFormatException e) {
			return fallback;
		}
	}
	
	private String getString(Element tag, String attribute){
		return tag.getAttribute(attribute);
	}

}
