package uk.ac.cam.cl.gfxintro.ac2620.tick1;


import java.io.File;
import java.io.IOException;
import java.util.HashMap;
import java.util.Map;

import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;

public class Texture {

	private static Map<String, ColorRGB[][]> loadedTextures = new HashMap<String, ColorRGB[][]>();

	private String textureName;
	private int width;
	private int height;
	private double tileX;
	private double tileY;
	private boolean beenSet = false;

	public int getWidth() {
		return width;
	}

	public int getHeight() {
		return height;
	}

	public boolean isNull() {
		return !beenSet;
	}

	public Texture(String textureName, double tileX, double tileY) {
		this.textureName = textureName;
		this.tileX = tileX;
		this.tileY = tileY;

		if (textureName == "")
			return;
		else if (loadedTextures.containsKey(textureName)) {
			height = loadedTextures.get(textureName).length;
			width = loadedTextures.get(textureName)[0].length;
			beenSet = true;
		}
		try {
			BufferedImage textureImg = ImageIO.read(new File(textureName));
			height = textureImg.getHeight();
			width = textureImg.getWidth();
			ColorRGB[][] texture = new ColorRGB[height][width];
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int pixel = textureImg.getRGB(x, y);
					texture[y][x] = new ColorRGB(((pixel >> 16) & 0xFF) / 255.0,
							((pixel >> 8) & 0xFF) / 255.0, (pixel & 0xFF) / 255.0);
				}
			}
			loadedTextures.put(textureName, texture);
			beenSet = true;
		} catch (IOException e) {
			System.err.println("Error creating texture");
			e.printStackTrace();
		}
	}
	
	public ColorRGB getPixel(double y01, double x01) {

		int x = (int) ((x01 * width * tileX) % width + width) % width;
		int y = (int) ((y01 * height * tileY) % height + height) % height;
		return loadedTextures.get(textureName)[y][x];
	}
}
