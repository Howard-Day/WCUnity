/// Render a single volumetric line using an alpha blend shader which does not support changing the color
/// 
/// Based on the Volumetric lines algorithm by Sebastien Hillaire
/// http://sebastien.hillaire.free.fr/index.php?option=com_content&view=article&id=57&Itemid=74
/// 
/// Thread in the Unity3D Forum:
/// http://forum.unity3d.com/threads/181618-Volumetric-lines
/// 
/// Unity3D port by Johannes Unterguggenberger
/// johannes.unterguggenberger@gmail.com
/// 
/// Thanks to Michael Probst for support during development.
/// 
/// Thanks for bugfixes and improvements to Unity Forum User "Mistale"
/// http://forum.unity3d.com/members/102350-Mistale
/// 
/// Shader code optimization and cleanup by Lex Darlog (aka DRL)
/// http://forum.unity3d.com/members/lex-drl.67487/
/// 
Shader "VolumetricLine/Animated_AlphaCutout" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_LineWidth ("Line Width", Range(0.01, 100)) = 1.0
		_LineScale ("Line Scale", Float) = 1.0
		_pixelsPerUnit("Pixels Per Unit", Float) = 16
		_animSpeed("Animation Speed", float) = 1
		_animTiles("Vertical Tile Frames", float) = 1
	}
	SubShader {
		// batching is forcefully disabled here because the shader simply won't work with it:
		Tags { "RenderType"="Opaque" }
		LOD 100
		
		Pass {
			
			Cull Off
			Lighting On
			AlphaToMask On
			CGPROGRAM
				#pragma glsl_no_auto_normalization
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile __ FOV_SCALING_OFF
							
				
				#include "_VolLineAnimShader.cginc"
			ENDCG
		}
	}
	FallBack "Diffuse"
}