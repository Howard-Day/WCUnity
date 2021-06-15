// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "CRT/Postprocess" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_BlurTex ("Texture", 2D) = "white" {}
	}
	SubShader {
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uvBase : TEXCOORD0;
				float2 uvBlur : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D _BlurTex;
			float4 _BlurTex_TexelSize;
			float pixelSizeX;
			float pixelSizeY;	
			float seconds;	

			float bleedDist;			// in pixels
			float bleedStr;			// 0.0 - 1.0
			float blurStr;			// 0.0 - 1.0
			float rgbMaskSub;
			float rgbMaskSep;
			float rgbMaskStr;		// 0.0 - 1.0
			int colorNoiseMode;
			float colorNoiseStr;		// 0.0 - 1.0
			int monoNoiseMode;
			float monoNoiseScale;	// 0.0 - 1.0 
			float monoNoiseStr;		// 0.0 - 1.0

			float4x4 colorMat;
									
			half3 minLevels;
			half3 maxLevels;
			half3 blackPoint;
			half3 whitePoint;
									
			float interWidth;		// in pixels
			float interSpeed;		// in px per second
			float interStr;			// 0.0 - 1.0
			float interSplit;

			float aberStr;			// in pixels

			half4 alphaBlend(half4 top, half4 bottom) {
				half4 result;
			    result.a	= top.a + bottom.a * (1.0 - top.a);
			    result.rgb	= (top.rgb * top.aaa + bottom.rgb * bottom.aaa * (half3(1.0, 1.0, 1.0) - top.aaa)) / result.aaa;
			    
			    return result;
			}

			half4 blur(float2 uv) {
				if(aberStr == 0.0) {
					return tex2D(_BlurTex, uv);
				}
				else {
					return half4(
						tex2D(_BlurTex, uv + half2(-pixelSizeX * aberStr, 0.0)).r,
						tex2D(_BlurTex, uv).g,
						tex2D(_BlurTex, uv + half2(pixelSizeX * aberStr, 0.0)).b,
						1.0
					);
				}
			}

			half4 bleed(float2 uv) {
			    half4 a = blur(uv + half2(0.0, bleedDist * pixelSizeY));
			    half4 b = blur(uv + half2(0.0, -bleedDist * pixelSizeY));
			    half4 c = blur(uv + half2(bleedDist * pixelSizeX, 0.0));
			    half4 d = blur(uv + half2(-bleedDist * pixelSizeX, 0.0));

			    return max(max(a, b), max(c, d));
			}

			half noise(float n) {
			    return frac(cos(n * 89.42) * 343.42);
			}

			half3 interference(float2 coord, half3 screen) {
				screen.r += sin((interSplit * pixelSizeY + coord.y / (interWidth * pixelSizeY) + (seconds * interSpeed))) * interStr;
				screen.g += sin((coord.y / (interWidth * pixelSizeY) + (seconds * interSpeed))) * interStr;
				screen.b += sin((-interSplit + coord.y / (interWidth * pixelSizeY) + (seconds * interSpeed))) * interStr;

			    screen = clamp(screen, half3(0.0, 0.0, 0.0), half3(1.0, 1.0, 1.0));
			    
				return screen;
			}

			v2f vert(appdata_img v) {
				v2f o;
				o.pos		= UnityObjectToClipPos(v.vertex);
				o.uvBase	= v.texcoord;
				o.uvBlur	= v.texcoord;

				#if UNITY_UV_STARTS_AT_TOP
				//if (_MainTex_TexelSize.y < 0)
				//	o.uvBase.y = 1.0 - o.uvBase.y;

				//if (_MainTex_TexelSize.y < 0)
				//	o.uvBlur.y = 1.0 - o.uvBlur.y;
				#endif 

				return o;
			}

			half4 frag(v2f i) : SV_Target { 
				//return tex2D(_BlurTex, i.uv); 

				half4 zero		= half4(0.0, 0.0, 0.0, 0.0);
				half4 one 		= half4(1.0, 1.0, 1.0, 1.0);

			    half4 base		= tex2D(_MainTex, i.uvBase);
			    half4 blured 	= blur(i.uvBlur);
			    half4 bleeded	= bleed(i.uvBlur);
			    half4 final;

			    final.a = 1.0;
			    half3 tmp;

			    // 1. mix tmp with blured in lighten mode
			    tmp = max(base.rgb, blured.rgb);
			    final.rgb = alphaBlend(half4(tmp, blurStr), blured).rgb;

			    // 2. mix bleeded with base in lighten mode
			    tmp = max(bleeded.rgb, final.rgb);
			    final.rgb = alphaBlend(half4(tmp, bleedStr), final).rgb;

			    float delta = fmod(seconds, 60.0);
			    
			    // 3. add color noise
			    half3 colorNoise = half3(
			        noise(sin(i.uvBase.x / pixelSizeX) * i.uvBase.y / pixelSizeY + delta), 
			        noise(sin(i.uvBase.y / pixelSizeY) * i.uvBase.x / pixelSizeX + delta),
			        noise(sin(i.uvBase.x / pixelSizeX) * sin(i.uvBase.y / pixelSizeY) + delta)
			    );

			    if(colorNoiseMode == 0)
			    	tmp = final.rgb + colorNoise;
			    else if(colorNoiseMode == 1)
			    	tmp = final.rgb - colorNoise;
			    else if(colorNoiseMode == 2)
					tmp = final.rgb * colorNoise;
				else if(colorNoiseMode == 3)
			    	tmp = final.rgb / colorNoise;
			    else if(colorNoiseMode == 4)
			    	tmp = max(colorNoise, final.rgb);
			    else if(colorNoiseMode == 5)
			    	tmp = min(colorNoise, final.rgb);

			    tmp = clamp(tmp, zero.rgb, one.rgb);
			    final.rgb = alphaBlend(half4(tmp, colorNoiseStr), final).rgb;

			    // 4. add monochromatic noise
			    float monoNoiseVal = noise(sin(i.uvBase.x / pixelSizeX) * i.uvBase.y / pixelSizeY + delta);
			    half3 monoNoise = half3(monoNoiseVal, monoNoiseVal, monoNoiseVal);

			    if(monoNoiseMode == 0)
			    	tmp = final.rgb + monoNoise;
			    else if(monoNoiseMode == 1)
			    	tmp = final.rgb - monoNoise;
			    else if(monoNoiseMode == 2)
					tmp = final.rgb * monoNoise;
				else if(monoNoiseMode == 3)
			    	tmp = final.rgb / monoNoise;
			    else if(monoNoiseMode == 4)
			    	tmp = max(monoNoise, final.rgb);
			    else if(monoNoiseMode == 5)
			    	tmp = min(monoNoise, final.rgb);

			    tmp = clamp(tmp, zero.rgb, one.rgb);
			    final.rgb = alphaBlend(half4(tmp, monoNoiseStr), final).rgb;

			    // 5. mix rgb mask with final
			    float modulo = floor(fmod(i.uvBase.y / pixelSizeX, 2));
			    tmp = final.rgb;

			    if(modulo == 0.0)
			        tmp -= half3(0, rgbMaskSub * rgbMaskSep, rgbMaskSub * rgbMaskSep * 2.0);
			    else if(modulo == 1.0)
			        tmp -= half3(rgbMaskSub * rgbMaskSep, 0, rgbMaskSub * rgbMaskSep);
			    else
			        tmp -= half3(rgbMaskSub * rgbMaskSep * 2.0, rgbMaskSub * rgbMaskSep, 0);

		        final.rgb = alphaBlend(half4(tmp, rgbMaskStr), final).rgb;

			    // 6. interference
			    final.rgb = interference(i.uvBase, final.rgb);

			    // 7. color adjustment
			    final = mul(colorMat, final);

			    // 8. levels adjustment
			    final.rgb = lerp(
			        zero.rgb,
			        one.rgb,
			        final.rgb / (maxLevels - minLevels) + minLevels);

			    final.rgb = clamp(final.rgb, blackPoint, whitePoint);

			    return final;
			   	
			}
			ENDCG
		}
	}
}