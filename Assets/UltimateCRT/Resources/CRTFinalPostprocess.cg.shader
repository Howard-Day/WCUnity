// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "CRT/FinalPostprocess" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
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
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float pixelSizeX;	
			float pixelSizeY;	

			int maskMode;
			float maskStr;		// 0.0 - 1.0
			float vignetteStr;	// 0.0 - 1.0
			float vignetteSize;	// 0.0 - 1.
			float crtBendX;
			float crtBendY;
			float crtOverscan;			// 0.0 - 1.0
			int flipV;

			v2f vert(appdata_img v) {
				v2f o; 
				o.pos	= UnityObjectToClipPos(v.vertex);
				o.uv	= v.texcoord;
				
				#if UNITY_UV_STARTS_AT_TOP
				if (flipV)
					o.uv.y =  1.0 - o.uv.y;
				#endif 

				return o;
			}

			half4 alphaBlend(half4 top, half4 bottom) {
				half4 result;
			    result.a	= top.a + bottom.a * (1.0 - top.a);
			    result.rgb	= (top.rgb * top.aaa + bottom.rgb * bottom.aaa * (half3(1.0, 1.0, 1.0) - top.aaa)) / result.aaa;
			    
			    return result;
			}

			half3 vignette(float2 uv) {
			    half outer 	= 1.0;
				half inner		= vignetteSize;
				half2 center	= half2(0.5, 0.5); // Center of Screen
				
				float dist  	= distance(center, uv) * 1.414213; // multiplyed by 1.414213 to fit in the range of 0.0 to 1.0 	
				float vig 		= clamp((outer - dist) / (outer - inner), 0.0, 1.0);
				vig = pow(vig, .5);
			    
			    return half3(vig, vig, vig);
			}

			float2 crt(float2 coord, float bendX, float bendY) {
				// to symmetrical coords
				coord = (coord - 0.5) * 2.0 / (crtOverscan + 1.0);

				// overscane a bit by default
				coord *= 1.1;	

				// bend
				coord.x *= .5 + pow((abs(coord.y) / bendX), 3);
				coord.y *= .5 + pow((abs(coord.x) / bendY), 3);

				// transform back to 0.0 - 1.0
				coord  = (coord ) + 0.5;

				return coord;
			}

			half4 frag(v2f i) : SV_Target { 
				//return tex2D(_MainTex, i.uv); 

				float2 crtCoords = crt(i.uv, crtBendX, crtBendY);

			    if(crtCoords.x < 0.0 || crtCoords.x > 1.0 || crtCoords.y < 0.0 || crtCoords.y > 1.0) {
			    	return half4(0.0, 0.0, 0.0, 1.0);
			    	//discard;
			    }
			    else {
					half4 zero	= half4(0.0, 0.0, 0.0, 0.0);
					half4 one	= half4(1.0, 1.0, 1.0, 1.0);

			        half4 final = tex2D(_MainTex, crtCoords);

			        // 9. mix mask with final
			        half3 tmp;

			        if(maskMode == 0) {
				        float moduloX = floor(fmod(i.uv.x / pixelSizeX, 6.0));
				        float moduloY = floor(fmod(i.uv.y / pixelSizeY, 4.0));

				        if(moduloX < 3.0) {
				            if(moduloY < 3.0)
				                tmp.rgb = one.rgb;
				            else
				                tmp.rgb = zero.rgb;
				        }
				        else {
				            if(moduloY == 1.0)
				                tmp.rgb = zero.rgb;
				            else
				                tmp.rgb = one.rgb;
				        }
			        }
			        else if(maskMode == 1) {
				        float moduloX = floor(fmod(i.uv.x / pixelSizeX, 6.0));
				        float moduloY = floor(fmod(i.uv.y / pixelSizeY, 6.0));

				        if(moduloX < 3.0) {
				            if(moduloY == 0.0 || moduloY == 5.0)
				                tmp.rgb = zero.rgb;
				            else
				                tmp.rgb = one.rgb;
				        }
				        else {
				            if(moduloY == 2.0 || moduloY == 3.0)
				                tmp.rgb = zero.rgb;
				            else
				                tmp.rgb = one.rgb;
				        }
			        }
			        else if(maskMode == 2) {
				        float moduloX = floor(fmod(i.uv.x / pixelSizeX, 6.0));
				        float moduloY = floor(fmod(i.uv.y / pixelSizeY, 5.0));

				        if(moduloX < 3.0) {
				            if(moduloY < 3.0)
				                tmp.rgb = one.rgb;
				            else
				                tmp.rgb = zero.rgb;
				        }
				        else {
				            if(moduloY < 2.0)
				                tmp.rgb = zero.rgb;
				            else
				                tmp.rgb = one.rgb;
				        }
			        }
			        else if(maskMode == 3) {
				        float moduloY = floor(fmod(i.uv.y / pixelSizeY, 4.0));

			            if(moduloY < 1.0)
			                tmp.rgb = zero.rgb;
			            else
			                tmp.rgb = one.rgb;
			        }
			        else if(maskMode == 4) {
				        float moduloY = floor(fmod(i.uv.y / pixelSizeY, 4.0));

			            if(moduloY < 2.0)
			                tmp.rgb = zero.rgb;
			            else
			                tmp.rgb = one.rgb;
			        }
			        else if(maskMode == 5) {
				        float moduloY = floor(fmod(i.uv.y / pixelSizeY, 5.0));

			            if(moduloY < 2.0)
			                tmp.rgb = one.rgb;
			            else
			                tmp.rgb = zero.rgb;
			        }

			        tmp = final.rgb * tmp;
			        final.rgb = alphaBlend(half4(tmp, maskStr), final).rgb; 

			        // 10. vignette
					tmp = (final.rgb - (1 - vignette(crtCoords))/6) * (vignette(crtCoords)*.25+.75f);
					final.rgb = alphaBlend(half4(tmp, vignetteStr), final).rgb;

			        return final;
				}
			}
			ENDCG
		}
	}
}