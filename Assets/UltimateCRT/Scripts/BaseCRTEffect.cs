using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class BaseCRTEffect : MonoBehaviour {
	public static void SetupDefaultPreset(BaseCRTEffect effect) {
		effect.blurSize						= 0.7f;	
		effect.blurStrength 				= 0.6f;
		effect.bleedingSize 				= 0.75f;
		effect.bleedingStrength 			= 0.5f;
		effect.chromaticAberrationOffset 	= 1.25f;
		effect.RGBMaskIntensivity 			= 0.6f;
		effect.RGBMaskStrength 				= 0.6f;
		effect.RGBMaskBleeding 				= 0.1f;
		effect.colorNoiseMode 				= NoiseMode.Add;
		effect.colorNoiseStrength			= 0.15f;
		effect.whiteNoiseMode 				= NoiseMode.Lighten;
		effect.whiteNoiseStrength			= 0.25f;
		effect.darkestLevel 				= Color.black;
		effect.brightestLevel 				= Color.Lerp(Color.black, Color.white, 235.0f / 255.0f);
		effect.darkestColor					= Color.Lerp(Color.black, Color.white, 40.0f / 255.0f);
		effect.brightestColor 				= Color.white;
		effect.brightness 					= 0.2f;
		effect.contrast 					= 0.1f;
		effect.saturation 					= -0.05f;
		effect.interferenceWidth 			= 25.0f;
		effect.interferenceSpeed 			= 3.0f;
		effect.interferenceStrength 		= 0.0f;
		effect.interferenceSplit 			= 0.25f;
		effect.maskMode 					= MaskMode.Dense;
		effect.maskStrength 				= 0.35f;		// 0.0 - 1.0
		effect.curvatureX 					= 0.6f;	// uniform float crtBend;
		effect.curvatureY 					= 0.6f;	// uniform float crtBend;
		effect.overscan						= 0.0f; // uniform float crtOverscan;			// 0.0 - 1.0			
		effect.vignetteSize					= 0.35f;
		effect.vignetteStrength				= 0.1f;
		effect.textureScaling				= TextureScalingMode.AdjustForHeight;
		effect.scalingPolicy				= TextureScalingPolicy.DownscaleOnly;
		effect.textureSize					= 768;
	}

	public static void SetupHowiesPreset(BaseCRTEffect effect)
	{
		effect.blurSize = 2f;
		effect.blurStrength = 1f;
		effect.bleedingSize = 0.5f;
		effect.bleedingStrength = 0.5f;
		effect.chromaticAberrationOffset = 1.5f;
		effect.RGBMaskIntensivity = 0.25f;
		effect.RGBMaskStrength = 1.0f;
		effect.RGBMaskBleeding = 0.75f;
		effect.colorNoiseMode = NoiseMode.Multiply;
		effect.colorNoiseStrength = 0f;
		effect.whiteNoiseMode = NoiseMode.Lighten;
		effect.whiteNoiseStrength = 0.2f;
		effect.darkestLevel = Color.black;
		effect.brightestLevel = Color.Lerp(Color.black, Color.white, 255.0f / 255.0f);
		effect.darkestColor = Color.black; ;
		effect.brightestColor = Color.white;
		effect.brightness = 0.8f;
		effect.contrast = 0.5f;
		effect.saturation = 0.125f;
		effect.interferenceWidth = 25.0f;
		effect.interferenceSpeed = 3.0f;
		effect.interferenceStrength = 0.0f;
		effect.interferenceSplit = 0.25f;

		effect.maskMode = MaskMode.Denser;
		effect.maskStrength = 0.25f;        // 0.0 - 1.0
		effect.curvatureX = 0.35f;   // uniform float crtBend;
		effect.curvatureY = 0.35f;   // uniform float crtBend;
		effect.overscan = 0.0f; // uniform float crtOverscan;			// 0.0 - 1.0			
		effect.vignetteSize = 0.85f;
		effect.vignetteStrength = 1f;
		effect.textureScaling = TextureScalingMode.AdjustForHeight;
		effect.scalingPolicy = TextureScalingPolicy.Always;
		effect.textureSize = 1200;
	}

	public static void SetupKitchenTVPreset(BaseCRTEffect effect) {
		effect.blurSize						= 0.7f;	
		effect.blurStrength					= 0.6f;
		effect.bleedingSize 				= 0.75f;
		effect.bleedingStrength 			= 0.5f;
		effect.chromaticAberrationOffset 	= 1.25f;
		effect.RGBMaskIntensivity			= 0.4f;
		effect.RGBMaskStrength				= 0.4f;
		effect.RGBMaskBleeding				= 0.1f;
		effect.colorNoiseMode				= NoiseMode.Add;
		effect.colorNoiseStrength			= 0.15f;
		effect.whiteNoiseMode				= NoiseMode.Lighten;
		effect.whiteNoiseStrength			= 0.15f;
		effect.darkestLevel					= Color.black;
		effect.brightestLevel				= Color.Lerp(Color.black, Color.white, 235.0f / 255.0f);
		effect.darkestColor					= Color.Lerp(Color.black, Color.white, 25.0f / 255.0f);
		effect.brightestColor 				= Color.white;
		effect.brightness					= 0.1f;
		effect.contrast						= 0.1f;
		effect.saturation					= -0.05f;
		effect.interferenceWidth			= 25.0f;
		effect.interferenceSpeed			= 3.0f;
		effect.interferenceStrength			= 0.0f;
		effect.interferenceSplit			= 0.25f;
		effect.maskMode						= MaskMode.Dense;
		effect.maskStrength 				= 0.25f;
		effect.curvatureX					= 0.3f;
		effect.curvatureY					= 0.3f;
		effect.overscan						= 0.0f;			
		effect.vignetteSize					= 0.35f;
		effect.vignetteStrength				= 0.1f;
		effect.textureScaling				= TextureScalingMode.AdjustForHeight;
		effect.scalingPolicy				= TextureScalingPolicy.DownscaleOnly;
		effect.textureSize					= 768;
	}

	public static void SetupMiniCRTPreset(BaseCRTEffect effect) {
		effect.blurSize						= 0.8f;	
		effect.blurStrength					= 0.8f;
		effect.bleedingSize 				= 0.5f;
		effect.bleedingStrength 			= 1.0f;
		effect.chromaticAberrationOffset 	= 2.5f;
		effect.RGBMaskIntensivity			= 0.8f;
		effect.RGBMaskStrength				= 0.8f;
		effect.RGBMaskBleeding				= 0.3f;
		effect.colorNoiseMode				= NoiseMode.Add;
		effect.colorNoiseStrength			= 0.25f;
		effect.whiteNoiseMode				= NoiseMode.Lighten;
		effect.whiteNoiseStrength			= 0.25f;
		effect.darkestLevel					= Color.black;
		effect.brightestLevel				= Color.Lerp(Color.black, Color.white, 225.0f / 255.0f);
		effect.darkestColor					= Color.Lerp(Color.black, Color.white, 35.0f / 255.0f);
		effect.brightestColor 				= Color.white;
		effect.brightness					= 0.3f;
		effect.contrast						= 0.3f;
		effect.saturation					= -0.1f;
		effect.interferenceWidth			= 25.0f;
		effect.interferenceSpeed			= 3.0f;
		effect.interferenceStrength			= 0.0f;
		effect.interferenceSplit			= 0.25f;
		effect.maskMode						= MaskMode.Denser;
		effect.maskStrength 				= 1.0f;
		effect.curvatureX					= 0.7f;
		effect.curvatureY					= 0.7f;
		effect.overscan						= 0.0f;			
		effect.vignetteSize					= 0.5f;
		effect.vignetteStrength				= 0.425f;
		effect.textureScaling				= TextureScalingMode.AdjustForHeight;
		effect.scalingPolicy				= TextureScalingPolicy.DownscaleOnly;
		effect.textureSize					= 768;
	}

	public static void SetupColorTVPreset(BaseCRTEffect effect) {
		effect.blurSize						= 0.9f;	
		effect.blurStrength					= 0.6f;
		effect.bleedingSize 				= 0.85f;
		effect.bleedingStrength 			= 0.75f;
		effect.chromaticAberrationOffset 	= 1.75f;
		effect.RGBMaskIntensivity			= 0.4f;
		effect.RGBMaskStrength				= 0.4f;
		effect.RGBMaskBleeding				= 0.1f;
		effect.colorNoiseMode				= NoiseMode.Add;
		effect.colorNoiseStrength			= 0.3f;
		effect.whiteNoiseMode				= NoiseMode.Lighten;
		effect.whiteNoiseStrength			= 0.2f;
		effect.darkestLevel					= Color.black;
		effect.brightestLevel				= Color.Lerp(Color.black, Color.white, 235.0f / 255.0f);
		effect.darkestColor					= Color.Lerp(Color.black, Color.white, 35.0f / 255.0f);
		effect.brightestColor 				= new Color(245.0f / 255.0f, 1.0f, 1.0f);
		effect.brightness					= 0.0f;
		effect.contrast						= 0.2f;
		effect.saturation					= 0.1f;
		effect.interferenceWidth			= 25.0f;
		effect.interferenceSpeed			= 3.0f;
		effect.interferenceStrength			= 0.0f;
		effect.interferenceSplit			= 0.25f;
		effect.maskMode						= MaskMode.Denser;
		effect.maskStrength 				= 0.2f;
		effect.curvatureX					= 0.5f;
		effect.curvatureY					= 0.5f;
		effect.overscan						= 0.1f;			
		effect.vignetteSize					= 0.4f;
		effect.vignetteStrength				= 0.5f;
		effect.textureScaling				= TextureScalingMode.AdjustForHeight;
		effect.scalingPolicy				= TextureScalingPolicy.DownscaleOnly;
		effect.textureSize					= 768;
	}

	public static void SetupOldTVPreset(BaseCRTEffect effect) {
		effect.blurSize						= 0.9f;	
		effect.blurStrength					= 0.8f;
		effect.bleedingSize 				= 0.95f;
		effect.bleedingStrength 			= 0.95f;
		effect.chromaticAberrationOffset 	= 1.9f;
		effect.RGBMaskIntensivity			= 0.7f;
		effect.RGBMaskStrength				= 0.7f;
		effect.RGBMaskBleeding				= 0.3f;
		effect.colorNoiseMode				= NoiseMode.Add;
		effect.colorNoiseStrength			= 0.5f;
		effect.whiteNoiseMode				= NoiseMode.Darken;
		effect.whiteNoiseStrength			= 0.55f;
		effect.darkestLevel					= Color.black;
		effect.brightestLevel				= Color.Lerp(Color.black, Color.white, 235.0f / 255.0f);
		effect.darkestColor					= Color.Lerp(Color.black, Color.white, 35.0f / 255.0f);
		effect.brightestColor 				= new Color(245.0f / 255.0f, 1.0f, 1.0f);
		effect.brightness					= 0.0f;
		effect.contrast						= -0.1f;
		effect.saturation					= -0.05f;
		effect.interferenceWidth			= 35.0f;
		effect.interferenceSpeed			= 2.0f;
		effect.interferenceStrength			= 0.075f;
		effect.interferenceSplit			= 0.25f;
		effect.maskMode						= MaskMode.Thin;
		effect.maskStrength 				= 0.75f;
		effect.curvatureX					= 0.625f;
		effect.curvatureY					= 0.625f;
		effect.overscan						= 0.1f;			
		effect.vignetteSize					= 0.4f;
		effect.vignetteStrength				= 0.5f;
		effect.textureScaling				= TextureScalingMode.AdjustForHeight;
		effect.scalingPolicy				= TextureScalingPolicy.DownscaleOnly;
		effect.textureSize					= 768;
	}

	public static void SetupHighEndMonitorPreset(BaseCRTEffect effect) {
		effect.blurSize						= 0.35f;	
		effect.blurStrength					= 0.5f;
		effect.bleedingSize 				= 0.5f;
		effect.bleedingStrength 			= 0.8f;
		effect.chromaticAberrationOffset 	= 0.5f;
		effect.RGBMaskIntensivity			= 0.4f;
		effect.RGBMaskStrength				= 0.4f;
		effect.RGBMaskBleeding				= 0.1f;
		effect.colorNoiseMode				= NoiseMode.Lighten;
		effect.colorNoiseStrength			= 0.15f;
		effect.whiteNoiseMode				= NoiseMode.Lighten;
		effect.whiteNoiseStrength			= 0.1f;
		effect.darkestLevel					= Color.black;
		effect.brightestLevel				= Color.white;
		effect.darkestColor					= Color.black;
		effect.brightestColor 				= Color.white;
		effect.brightness					= 0.1f;
		effect.contrast						= 0.0f;
		effect.saturation					= 0.05f;
		effect.interferenceWidth			= 25.0f;
		effect.interferenceSpeed			= 3.0f;
		effect.interferenceStrength			= 0.0f;
		effect.interferenceSplit			= 0.25f;
		effect.maskMode						= MaskMode.Thin;
		effect.maskStrength 				= 0.3f;
		effect.curvatureX					= 0.0f;
		effect.curvatureY					= 0.0f;
		effect.overscan						= 0.0f;			
		effect.vignetteSize					= 0.0f;
		effect.vignetteStrength				= 0.0f;
		effect.textureScaling				= TextureScalingMode.AdjustForHeight;
		effect.scalingPolicy				= TextureScalingPolicy.DownscaleOnly;
		effect.textureSize					= 768;
	}

	public static void SetupArcadeDisplayPreset(BaseCRTEffect effect) {
		effect.blurSize						= 0.5f;	
		effect.blurStrength					= 0.7f;
		effect.bleedingSize 				= 0.65f;
		effect.bleedingStrength 			= 0.8f;
		effect.chromaticAberrationOffset 	= 0.9f;
		effect.RGBMaskIntensivity			= 0.4f;
		effect.RGBMaskStrength				= 0.4f;
		effect.RGBMaskBleeding				= 0.2f;
		effect.colorNoiseMode				= NoiseMode.Lighten;
		effect.colorNoiseStrength			= 0.15f;
		effect.whiteNoiseMode				= NoiseMode.Lighten;
		effect.whiteNoiseStrength			= 0.1f;
		effect.darkestLevel					= Color.black;
		effect.brightestLevel				= Color.white;
		effect.darkestColor					= Color.black;
		effect.brightestColor 				= Color.white;
		effect.brightness					= 0.1f;
		effect.contrast						= 0.1f;
		effect.saturation					= 0.1f;
		effect.interferenceWidth			= 25.0f;
		effect.interferenceSpeed			= 3.0f;
		effect.interferenceStrength			= 0.0f;
		effect.interferenceSplit			= 0.25f;
		effect.maskMode						= MaskMode.Scanline;
		effect.maskStrength 				= 0.75f;
		effect.curvatureX					= 0.0f;
		effect.curvatureY					= 0.0f;
		effect.overscan						= 0.0f;			
		effect.vignetteSize					= 0.3f;
		effect.vignetteStrength				= 0.2f;
		effect.textureScaling				= TextureScalingMode.AdjustForHeight;
		effect.scalingPolicy				= TextureScalingPolicy.DownscaleOnly;
		effect.textureSize					= 768;
	}

	public static void SetupBrokenBlackAndWhitePreset(BaseCRTEffect effect) {
		effect.blurSize						= 0.9f;	
		effect.blurStrength					= 1.0f;
		effect.bleedingSize 				= 0.75f;
		effect.bleedingStrength 			= 0.9f;
		effect.chromaticAberrationOffset 	= 2.5f;
		effect.RGBMaskIntensivity			= 0.6f;
		effect.RGBMaskStrength				= 0.6f;
		effect.RGBMaskBleeding				= 0.1f;
		effect.colorNoiseMode				= NoiseMode.Add;
		effect.colorNoiseStrength			= 0.75f;
		effect.whiteNoiseMode				= NoiseMode.Lighten;
		effect.whiteNoiseStrength			= 0.5f;
		effect.darkestLevel					= Color.Lerp(Color.black, Color.white, 15.0f / 255.0f);
		effect.brightestLevel				= Color.Lerp(Color.black, Color.white, 225.0f / 255.0f);
		effect.darkestColor					= Color.Lerp(Color.black, Color.white, 60.0f / 255.0f);
		effect.brightestColor 				= Color.white;
		effect.brightness					= 0.0f;
		effect.contrast						= -0.2f;
		effect.saturation					= -1.0f;
		effect.interferenceWidth			= 85.0f;
		effect.interferenceSpeed			= 2.5f;
		effect.interferenceStrength			= 0.05f;
		effect.interferenceSplit			= 0.0f;
		effect.maskMode						= MaskMode.Denser;
		effect.maskStrength 				= 0.15f;
		effect.curvatureX					= 0.6f;
		effect.curvatureY					= 0.6f;
		effect.overscan						= 0.4f;			
		effect.vignetteSize					= 0.75f;
		effect.vignetteStrength				= 0.5f;
		effect.textureScaling				= TextureScalingMode.AdjustForHeight;
		effect.scalingPolicy				= TextureScalingPolicy.DownscaleOnly;
		effect.textureSize					= 768;
	}

	public static void SetupGreenTerminalPreset(BaseCRTEffect effect) {
		effect.blurSize						= 0.9f;	
		effect.blurStrength					= 1.0f;
		effect.bleedingSize 				= 0.8f;
		effect.bleedingStrength 			= 0.65f;
		effect.chromaticAberrationOffset 	= 0.0f;
		effect.RGBMaskIntensivity			= 0.7f;
		effect.RGBMaskStrength				= 0.7f;
		effect.RGBMaskBleeding				= 0.2f;
		effect.colorNoiseMode				= NoiseMode.Add;
		effect.colorNoiseStrength			= 0.0f;
		effect.whiteNoiseMode				= NoiseMode.Lighten;
		effect.whiteNoiseStrength			= 0.0f;
		effect.darkestLevel					= Color.Lerp(Color.black, Color.white, 10.0f / 255.0f);
		effect.brightestLevel				= Color.Lerp(Color.black, Color.white, 205.0f / 255.0f);
		effect.darkestColor 				= new Color(0.0f, 30.0f / 255.0f, 0.0f);
		effect.brightestColor 				= new Color(25.0f / 255.0f, 1.0f, 25.0f / 255.0f);
		effect.brightness					= 0.4f;
		effect.contrast						= -0.1f;
		effect.saturation					= -0.8f;
		effect.interferenceWidth			= 300.0f;
		effect.interferenceSpeed			= 25.0f;
		effect.interferenceStrength			= 0.0035f;
		effect.interferenceSplit			= 0.0f;
		effect.maskMode						= MaskMode.DenseScanline;
		effect.maskStrength 				= 0.25f;
		effect.curvatureX					= 0.55f;
		effect.curvatureY					= 0.55f;
		effect.overscan						= 0.0f;			
		effect.vignetteSize					= 0.35f;
		effect.vignetteStrength				= 0.35f;
		effect.textureScaling				= TextureScalingMode.AdjustForHeight;
		effect.scalingPolicy				= TextureScalingPolicy.DownscaleOnly;
		effect.textureSize					= 768;
	}

	public static void SetupYellowMonitorPreset(BaseCRTEffect effect) {
		effect.blurSize						= 0.9f;	
		effect.blurStrength					= 0.6f;
		effect.bleedingSize 				= 0.85f;
		effect.bleedingStrength 			= 0.75f;
		effect.chromaticAberrationOffset 	= 1.75f;
		effect.RGBMaskIntensivity			= 0.4f;
		effect.RGBMaskStrength				= 0.4f;
		effect.RGBMaskBleeding				= 0.1f;
		effect.colorNoiseMode				= NoiseMode.Multiply;
		effect.colorNoiseStrength			= 0.4f;
		effect.whiteNoiseMode				= NoiseMode.Darken;
		effect.whiteNoiseStrength			= 0.2f;
		effect.darkestLevel					= Color.Lerp(Color.black, Color.white, 10.0f / 255.0f);
		effect.brightestLevel				= Color.Lerp(Color.black, Color.white, 205.0f / 255.0f);
		effect.darkestColor 				= new Color(30.0f / 255.0f, 30.0f / 255.0f, 0.0f);
		effect.brightestColor 				= new Color(1.0f, 1.0f, 25.0f / 255.0f);
		effect.brightness					= 0.5f;
		effect.contrast						= -0.1f;
		effect.saturation					= -1.0f;
		effect.interferenceWidth			= 300.0f;
		effect.interferenceSpeed			= 25.0f;
		effect.interferenceStrength			= 0.0035f;
		effect.interferenceSplit			= 0.0f;
		effect.maskMode						= MaskMode.DenseScanline;
		effect.maskStrength 				= 0.25f;
		effect.curvatureX					= 0.4f;
		effect.curvatureY					= 0.4f;
		effect.overscan						= 0.0f;
		effect.vignetteSize					= 0.35f;
		effect.vignetteStrength				= 0.35f;
		effect.textureScaling				= TextureScalingMode.AdjustForHeight;
		effect.scalingPolicy				= TextureScalingPolicy.DownscaleOnly;
		effect.textureSize					= 768;
	}

	public enum Preset { Custom, Howies, Default, KitchenTV, MiniCRT, ColorTV, OldTV, HighEndMonitor, ArcadeDisplay, BrokenBlackAndWhite, GreenTerminal, YellowMonitor };

	public enum NoiseMode { Add, Subtract, Multiply, Divide, Lighten, Darken };  
	public enum MaskMode { Thin, Dense, Denser, ThinScanline, Scanline, DenseScanline };
	public enum TextureScalingMode { Off, AdjustForWidth, AdjustForHeight };
	public enum TextureScalingPolicy { Always, UpscaleOnly, DownscaleOnly };

	public Preset predefinedModel = Preset.Custom;
	protected Preset preset = Preset.Custom;

	[Header("Blur")]
	[Range(0.0f, 3.0f)]
	[Tooltip("Blur ammount. How blurry the blurred layer is.")]
	public float blurSize = 0.7f;	

	[Range(0.0f, 3.0f)]
	[Tooltip("How much of the blurred image is mixed with the base image.")]
	public float blurStrength = 0.6f;

	[Header("Luminosity bleeding")]
	[Range(0.0f, 2.0f)]
	[Tooltip("How many adjacent pixels is going to be overlaped by a given brighter pixel.")]
	public float bleedingSize = 0.75f;

	[Range(0.0f, 1.0f)]
	[Tooltip("How much of the bleeded image is mixed with the base image.")]
	public float bleedingStrength = 0.5f;

	[Header("Chromatic aberration")]
	[Range(-2.5f, 2.5f)]
	[Tooltip("How many pixels the blurred layer is shifted for Red and Blue channels.")]
	public float chromaticAberrationOffset = 1.25f;

	[Header("RGB Mask")]
	[Range(0.0f, 1.0f)]
	[Tooltip("How much each channel blocks other channels for a given pixel. E.g. if set to 1.0 all red channel pixels will show no green or blue values.")]
	public float RGBMaskIntensivity = 0.6f;

	[Range(0.0f, 1.0f)]
	[Tooltip("How much of the masked image is mixed with the base image.")]
	public float RGBMaskStrength = 0.6f;

	[Range(0.0f, 1.0f)]
	[Tooltip("How much each channel passes through other channels for a given pixel. Basically opposite of RGB Mask Intensitity.")]
	public float RGBMaskBleeding = 0.1f;

	[Header("Noise")]
	[Tooltip("What blending mode is used when mixing base image pixels with color noise pixels.")]
	public NoiseMode colorNoiseMode = NoiseMode.Add;

	[Range(0.0f, 1.0f)]
	[Tooltip("How much of the generated color noise image is mixed with the base image.")]
	public float colorNoiseStrength	= 0.15f;

	[Space(4)]
	[Tooltip("What blending mode is used when mixing base image pixels with white noise pixels.")]
	public NoiseMode whiteNoiseMode = NoiseMode.Lighten;

	[Range(0.0f, 1.0f)]
	[Tooltip("How much of the generated white noise image is mixed with the base image.")]
	public float whiteNoiseStrength	= 0.25f;

	[Header("Color adjustments")]
	[Tooltip("This color becomes the new darkest color (works similarly to Photoshop 'Levels' adjustment).")]
	public Color darkestLevel = Color.black;

	[Tooltip("This color becomes the new brightest color (works similarly to Photoshop 'Levels' adjustment).")]
	public Color brightestLevel = Color.Lerp(Color.black, Color.white, 235.0f / 255.0f);

	[Space(4)]
	[Tooltip("Darkest color of the output image, makes image brigther and contrast lower, if brighter than black.")]
	public Color darkestColor	= Color.Lerp(Color.black, Color.white, 40.0f / 255.0f);

	[Tooltip("Brightest color of the output image, makes image darker and contrast lower if darker than white.")]
	public Color brightestColor = Color.white;

	[Space(4)]
	[Range(-1.0f, 1.0f)]
	[Tooltip("Brightness adjustment.")]
	public float brightness = 0.2f;

	[Range(-1.0f, 1.0f)]
	[Tooltip("Contrast adjustment.")]
	public float contrast =  0.1f;

	[Range(-1.0f, 1.0f)]
	[Tooltip("Saturation adjustment.")]
	public float saturation = -0.05f;

	[Header("Horizontal interference")]
	[Range(0, 1000)]
	[Tooltip("How wide (in pixels) is the horizontal interference bar.")]
	public float interferenceWidth = 25.0f;

	[Range(-25.0f, 25.0f)]
	[Tooltip("How fast does the horizontal interference bar move (in pixels per secons).")]
	public float interferenceSpeed = 3.0f;

	[Range(0.0f, 1.0f)]
	[Tooltip("How much of the interference is mixed with the base image.")]
	public float interferenceStrength = 0.0f;

	[Range(0.0f, 1.0f)]
	[Tooltip("How much the RGB channels of the interference are appart (in percent of interference bar width, it's basically chromatic aberration just for the interference).")]
	public float interferenceSplit = 0.25f;

	[Header("CRT")]
	[Tooltip("CRT mask type. Makes some pixels darker than the adjacent ones to simulate CRT display mask.")]
	public MaskMode maskMode = MaskMode.Dense;

	[Range(0.0f, 1.0f)]
	[Tooltip("How much of the mask is mixed with the base image.")]
	public float maskStrength = 0.35f;		// 0.0 - 1.0

	[Space(4)]
	[Range(-1.0f, 1.0f)]
	[Tooltip("How curvy the display is, on X-asis.")]
	public float curvatureX = 0.6f;	// uniform float crtBend;

	[Range(-1.0f, 1.0f)]
	[Tooltip("How curvy the display is, on Y-asis.")]
	public float curvatureY = 0.6f;	// uniform float crtBend;

	[Range(0.0f, 1.0f)]
	[Tooltip("Enlarges the image.")]
	public float overscan = 0.0f; // uniform float crtOverscan;			// 0.0 - 1.0			

	[Space(4)]
	[Range(0.0f, 1.0f)]
	[Tooltip("How much of the center part is covered by vignette.")]
	public float vignetteSize = 0.35f;

	[Range(0.0f, 1.0f)]
	[Tooltip("How much of the vignette is mixed with the base image.")]
	public float vignetteStrength = 0.1f;

	[Header("Camera's texture")]
	[Tooltip("Internal texture (if used) will be created proportionally to camera's display size.")]
	public TextureScalingMode textureScaling = TextureScalingMode.AdjustForHeight;

	[Tooltip("Internal texture will be created only when the condition is met (e.g., for 'Downscale Only' it will be created only, if camera's display is bigger than the texture size).")]
	public TextureScalingPolicy scalingPolicy = TextureScalingPolicy.DownscaleOnly;

	[Range(0, 4096)]
	[Tooltip("How wide or high the internally used texture will be.")]
	public int textureSize = 768;

	[Header("Multi-camera setup workaround")]
	[Tooltip("If you're using more than one camera to render the scene (like with Pro Camera 2D parallax setup), drag your cameras onto this property and add the effect to the last camera on your rendering path.")]
	public Camera[] otherCameras;

	protected Camera mainCamera;
	protected RenderTexture cameraTarget;
	protected RenderTexture oldCameraTarget;

	protected Material blurMaterial;
	protected Material postProMaterial;
	protected Material finalPostProMaterial;

	protected float seconds = 0.0f;

	protected float blurSigma		= float.NaN;
	protected float[] blurKernel	= new float[2];
	protected float blurZ			= float.NaN;

	protected float currentBrightness = float.NaN;
	protected Matrix4x4 brightnessMat	= new Matrix4x4();

	protected float currentContrast 	= float.NaN;
	protected Matrix4x4 contrastMat	= new Matrix4x4();

	protected float currentSaturation = float.NaN;
	protected Matrix4x4 saturationMat	= new Matrix4x4();

	protected Matrix4x4 colorMat = new Matrix4x4();
	protected Vector4 colorTransform;

	protected void InternalPreRender() {
		var newCameraTarget = CreateCameraTexture(cameraTarget);
		
		oldCameraTarget 			= mainCamera.targetTexture;
		mainCamera.targetTexture	= newCameraTarget;

		if(newCameraTarget != cameraTarget) {
			cameraTarget = newCameraTarget;

			if(otherCameras != null && otherCameras.Length != 0) {
				foreach(Camera otherCam in otherCameras) {
					if(otherCam == null)
						continue;

					otherCam.targetTexture = cameraTarget;
					otherCam.Render();
				}
			}

			if(oldCameraTarget != null) 
				oldCameraTarget.Release();
		}

		OnCameraPreRender();
	}

	void OnPostRender() {
		if(mainCamera.targetTexture != oldCameraTarget) {
			var targetTexture = mainCamera.targetTexture;
			mainCamera.targetTexture = oldCameraTarget;

			OnCameraPostRender(targetTexture);

			targetTexture.DiscardContents();
		}
	}

	void Awake() {
		mainCamera = GetComponent<Camera>();

		blurMaterial			= new Material(Shader.Find("CRT/Blur"));
		postProMaterial 		= new Material(Shader.Find("CRT/Postprocess"));
		finalPostProMaterial	= new Material(Shader.Find("CRT/FinalPostprocess"));
	}

	void Update () {
		seconds += Time.deltaTime;

		if(predefinedModel != preset) {
			preset = predefinedModel;

			switch(preset) {
				case Preset.Custom:
					break;

				case Preset.Default:
					SetupDefaultPreset(this);
					break;

				case Preset.Howies:
					SetupHowiesPreset(this);
					break;

				case Preset.KitchenTV:
					SetupKitchenTVPreset(this);
					break;

				case Preset.MiniCRT:
					SetupMiniCRTPreset(this);
					break;

				case Preset.ColorTV:
					SetupColorTVPreset(this);
					break;

				case Preset.OldTV:
					SetupOldTVPreset(this);
					break;

				case Preset.HighEndMonitor:
					SetupHighEndMonitorPreset(this);
					break;

				case Preset.ArcadeDisplay:
					SetupArcadeDisplayPreset(this);
					break;

				case Preset.BrokenBlackAndWhite:
					SetupBrokenBlackAndWhitePreset(this);
					break;

				case Preset.GreenTerminal:
					SetupGreenTerminalPreset(this);
					break;

				case Preset.YellowMonitor:
					SetupYellowMonitorPreset(this);
					break;
			}
		}
	}

	void OnDisable() {
		mainCamera.targetTexture = oldCameraTarget;
		
		if(otherCameras != null && otherCameras.Length > 0) {
			foreach(Camera otherCamera in otherCameras) {
				if(otherCamera == null)
					continue;

				otherCamera.targetTexture = null;
			}
		}

		if(cameraTarget != null)
			cameraTarget.Release();
		
		cameraTarget = null;
	}

	virtual protected RenderTexture CreateCameraTexture(RenderTexture currentCameraTexture) {
		if(textureScaling == TextureScalingMode.Off || textureSize == 0)
			return null;
		
		float displayWidth	= Screen.width;
		float displayHeight	= Screen.height;

		int w, h;

		if(textureScaling == TextureScalingMode.AdjustForHeight) {
			h = textureSize;
			w = Mathf.RoundToInt((displayWidth * textureSize) / displayHeight);
		} 
		else {
			w = textureSize;
			h = Mathf.RoundToInt((displayHeight * textureSize) / displayWidth);
		}

		if(scalingPolicy == TextureScalingPolicy.Always
		|| (scalingPolicy == TextureScalingPolicy.DownscaleOnly && (displayWidth > w || displayHeight > h))
		|| (scalingPolicy == TextureScalingPolicy.UpscaleOnly && (displayWidth < w || displayHeight < h))) {
			if(currentCameraTexture == null || currentCameraTexture.width != w || currentCameraTexture.height != h)
				return new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
			else
				return currentCameraTexture;
		}
		else {
			return null;
		}
	}
		
	virtual protected void OnCameraPreRender() {
		// do nothing by default
	}

	virtual protected void OnCameraPostRender(Texture texture) {
		// do nothing by default
	}

	protected void ProcessEffect(Texture src, RenderTexture dest) {
        var tempBlurTex				= RenderTexture.GetTemporary(src.width, src.height, 0);
		var tempPostProTex			= RenderTexture.GetTemporary(src.width, src.height, 0);

		var realBlurSize = Mathf.Lerp(0.00001f, 1.99999f, blurSize);

		UpdateBlurKernel(realBlurSize);

		var realBrightness	= Mathf.Lerp(0.8f, 1.2f, (brightness + 1.0f) / 2.0f);
		var realContrast	= Mathf.Lerp(0.5f, 1.5f, (contrast + 1.0f) / 2.0f);
		var realSaturation	= Mathf.Lerp(0.0f, 2.0f, (saturation + 1.0f) / 2.0f);

		UpdateColorMatrices(realBrightness - 1.5f, realContrast, realSaturation);

		blurMaterial.SetFloat("pixelSizeX", (float) (1.0 / src.width));
		blurMaterial.SetFloat("pixelSizeY", (float) (1.0 / src.height));
		blurMaterial.SetFloat("blurSigma", realBlurSize);
		blurMaterial.SetVector("blurKernel", new Vector4(blurKernel[0], blurKernel[1]));
		blurMaterial.SetFloat("blurZ", blurZ);

		postProMaterial.SetTexture("_BlurTex", tempBlurTex);
		postProMaterial.SetFloat("pixelSizeX", (float) (1.0 / src.width));
		postProMaterial.SetFloat("pixelSizeY", (float) (1.0 / src.height));
		postProMaterial.SetFloat("seconds", seconds);
		postProMaterial.SetFloat("blurStr", 1.0f - blurStrength);
		postProMaterial.SetFloat("bleedDist", bleedingSize);
		postProMaterial.SetFloat("bleedStr", bleedingStrength);
		postProMaterial.SetFloat("rgbMaskStr", Mathf.Lerp(0.0f, 0.3f, RGBMaskStrength));
		postProMaterial.SetFloat("rgbMaskSub", RGBMaskIntensivity);
		postProMaterial.SetFloat("rgbMaskSep", 1.0f - RGBMaskBleeding);
		postProMaterial.SetFloat("colorNoiseStr", Mathf.Lerp(0.0f, 0.4f, colorNoiseStrength));
		postProMaterial.SetInt("colorNoiseMode", (int) colorNoiseMode);
		postProMaterial.SetFloat("monoNoiseStr", Mathf.Lerp(0.0f, 0.4f, whiteNoiseStrength));
		postProMaterial.SetInt("monoNoiseMode", (int) whiteNoiseMode);
		postProMaterial.SetMatrix("colorMat", colorMat);
		postProMaterial.SetColor("minLevels", darkestLevel);
		postProMaterial.SetColor("maxLevels", brightestLevel);
		postProMaterial.SetColor("blackPoint", darkestColor);
		postProMaterial.SetColor("whitePoint", brightestColor);
		postProMaterial.SetFloat("interWidth", interferenceWidth);
		postProMaterial.SetFloat("interSpeed", interferenceSpeed);
		postProMaterial.SetFloat("interStr", interferenceStrength);
		postProMaterial.SetFloat("interSplit", interferenceSplit);
		postProMaterial.SetFloat("aberStr", -chromaticAberrationOffset);

		var realCurvatureX = Mathf.Lerp(0.25f, 0.45f, curvatureX);
		var realCurvatureY = Mathf.Lerp(0.25f, 0.45f, curvatureY);

		finalPostProMaterial.SetFloat("pixelSizeX", (float) (1.0 / src.width));
		finalPostProMaterial.SetFloat("pixelSizeY", (float) (1.0 / src.height));
		finalPostProMaterial.SetFloat("vignetteStr", vignetteStrength);
		finalPostProMaterial.SetFloat("vignetteSize", 1.0f - vignetteSize);
		finalPostProMaterial.SetFloat("maskStr", maskStrength / 10.0f);
		finalPostProMaterial.SetInt("maskMode", (int) maskMode);
		finalPostProMaterial.SetFloat("crtBendX", Mathf.Lerp(1.0f, 100.0f, (1.0f - realCurvatureX) / Mathf.Exp(10.0f * realCurvatureX)));
		finalPostProMaterial.SetFloat("crtBendY", Mathf.Lerp(1.0f, 100.0f, (1.0f - realCurvatureY) / Mathf.Exp(10.0f * realCurvatureY)));
		finalPostProMaterial.SetFloat("crtOverscan", Mathf.Lerp(0.1f, 0.25f, overscan));
        finalPostProMaterial.SetInt("flipV", src.texelSize.y < 0 && cameraTarget != null ? 1 : 0);

        Graphics.Blit(src, tempBlurTex, blurMaterial);
		Graphics.Blit(src, tempPostProTex, postProMaterial);

		if (dest == null)
			DrawFullScreenQuad(tempPostProTex, finalPostProMaterial);
		else
			Graphics.Blit(tempPostProTex, dest, finalPostProMaterial);

		tempBlurTex.DiscardContents();
		tempPostProTex.DiscardContents();

		RenderTexture.ReleaseTemporary(tempBlurTex);
		RenderTexture.ReleaseTemporary(tempPostProTex);
	}

	protected void DrawFullScreenQuad(Texture src, Material material) {
		RenderTexture.active = null;

		GL.PushMatrix();
		GL.LoadOrtho();
		GL.Viewport(new Rect(0, 0, Screen.width, Screen.height));

		material.mainTexture = src;

		for(int i = 0; i < material.passCount; ++i) {
			material.SetPass(i);

			GL.Begin(GL.QUADS);
			GL.Color(Color.white);
			GL.TexCoord2(0, 0);
			GL.Vertex3(0, 0, 0.1f);
			
			GL.TexCoord2(1, 0);
			GL.Vertex3(1, 0, 0.1f);
			
			GL.TexCoord2(1, 1);
			GL.Vertex3(1, 1, 0.1f);
			
			GL.TexCoord2(0, 1);
			GL.Vertex3(0, 1, 0.1f);
			GL.End();
		}

		GL.PopMatrix();

		material.mainTexture = null;
	}

	protected float CalculateBlurWeight(float x, float sigma) {
		return 0.39894f * Mathf.Exp(-0.5f * x * x / (sigma * sigma)) / sigma;
	}

	protected void UpdateBlurKernel(float sigma) {
		if(sigma == blurSigma)
			return;

		blurSigma		= sigma;
		const int kSize	= 1;

		blurZ = 0.0f;
		for(int j = 0; j <= kSize; ++j) {
			var normal				= CalculateBlurWeight(j, sigma);
			blurKernel[kSize - j]	= normal;

			if(j > 0)
				blurZ += 2 * normal;
			else
				blurZ += normal;
		}

		blurZ *= blurZ;
	}

	protected void UpdateColorMatrices(float b, float c, float s) {
		var rebuildColorMat = false;

		if(b != currentBrightness) {
			rebuildColorMat = true;
			currentBrightness = b;

			brightnessMat.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
			brightnessMat.SetColumn(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
			brightnessMat.SetColumn(2, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
			brightnessMat.SetColumn(3, new Vector4(b, b, b, 1.0f));
		}

		if(c != currentContrast) {
			rebuildColorMat = true;
			currentContrast = c;

			float t = (1.0f - contrast) / 2.0f;

			contrastMat.SetColumn(0, new Vector4(c, 0.0f, 0.0f, 0.0f));
			contrastMat.SetColumn(1, new Vector4(0.0f, c, 0.0f, 0.0f));
			contrastMat.SetColumn(2, new Vector4(0.0f, 0.0f, c, 0.0f));
			contrastMat.SetColumn(3, new Vector4(t, t, t, 1.0f));
		}

		if(s != currentSaturation) {
			rebuildColorMat = true;
			currentSaturation = s;

			Vector3 luminance = new Vector3(0.3086f, 0.6094f, 0.0820f);
			float t = 1.0f - s;

			Vector4 red		= new Vector4(luminance.x * t + s, luminance.x * t, luminance.x * t, 0.0f);
			Vector4 green 	= new Vector4(luminance.y * t, luminance.y * t + s, luminance.y * t, 0.0f);
			Vector4 blue 	= new Vector4(luminance.z * t, luminance.z * t, luminance.z * t + s, 0.0f);

			saturationMat.SetColumn(0, red);
			saturationMat.SetColumn(1, green);
			saturationMat.SetColumn(2, blue);
			saturationMat.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
		}

		if(rebuildColorMat)
			colorMat = brightnessMat * contrastMat * saturationMat;

	}
}
