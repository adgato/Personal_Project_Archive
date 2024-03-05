package uk.ac.cam.cl.gfxintro.ac2620.tick1;

import java.io.File;
import java.io.IOException;
import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;

public class BumpySphere extends Sphere {

	private float BUMP_FACTOR = 5f;
	private float[][] heightMap;
	private int bumpMapHeight;
	private int bumpMapWidth;

	public BumpySphere(Vector3 position, double radius, ColorRGB colour, String bumpMapImg) {
		super(position, radius, colour);
		try {
			BufferedImage inputImg = ImageIO.read(new File(bumpMapImg));
			bumpMapHeight = inputImg.getHeight();
			bumpMapWidth = inputImg.getWidth();
			heightMap = new float[bumpMapHeight][bumpMapWidth];
			for (int row = 0; row < bumpMapHeight; row++) {
				for (int col = 0; col < bumpMapWidth; col++) {
					float height = (float) (inputImg.getRGB(col, row) & 0xFF) / 0xFF;
					heightMap[row][col] = BUMP_FACTOR * height;
				}
			}
		} catch (IOException e) {
			System.err.println("Error creating bump map");
			e.printStackTrace();
		}
	}

	// Get normal to surface at position
	@Override
	public Vector3 getNormalAt(Vector3 position) {
		
		//(x,y,z) => (r,φ,θ) = (xyz.magnitude, atan2(z, x), acos(y))
		//(r,φ,θ) => (x,y,z) = (rcosφsinθ, rsinφsinθ, rcosθ)
		//∂(x,y,z)/∂φ = (-rsinφsinθ, rcosφsinθ, 0)
		//∂(x,y,z)/∂θ = (rcosφcosθ, rsinφcosθ, -rsinθ)

		Vector3 normalUp = position.subtract(this.position).normalised();
		double x = normalUp.x;
		double y = normalUp.y;
		double z = normalUp.z;
		double phi = Math.atan2(z, x) + Math.PI;
		double theta = Math.acos(y);
		int w = bumpMapWidth;
		int h = bumpMapHeight;
		int u = (int) (phi / (2 * Math.PI) * w % w);
		int v = (int) (Math.min(Math.max(theta / Math.PI * h, 0), h - 1));
		
		Vector3 normalRht = new Vector3(-Math.sin(phi) * Math.sin(theta), Math.cos(phi) * Math.sin(theta), 0).normalised();
		Vector3 normalFwd = new Vector3(Math.cos(phi) * Math.cos(theta), Math.sin(phi) * Math.cos(theta), -Math.sin(theta)).normalised();

		
		double deltaX = heightMap[v][((u + 1) % w + w) % w] - heightMap[v][u];
		double deltaY = heightMap[Math.min(v + 1, h - 1)][u] - heightMap[v][u];

		return (normalUp.add(normalRht.scale(deltaX)).add(normalFwd.scale(deltaY))).normalised();
	}
}
