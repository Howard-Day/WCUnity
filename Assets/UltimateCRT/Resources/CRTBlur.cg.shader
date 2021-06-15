// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "CRT/Blur" {
	Properties {
		_MainTex ("Texture Image", 2D) = "white" {} 
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
			float blurSigma; 		// in pixels
			float4 blurKernel;
			half blurZ;

			v2f vert(appdata_img v) {
				v2f o;
				o.pos	= UnityObjectToClipPos(v.vertex);
				o.uv	= v.texcoord;

				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1.0 - o.uv.y;
				#endif 

				return o;
			}

			half4 blur(half2 uv, half sigma) {
				half3 final 	= half3(0.0, 0.0, 0.0);

				float cornerWeight 	= blurKernel.x * blurKernel.x;
				float edgeWeight	= blurKernel.x * blurKernel.y;
				float centerWeight	= blurKernel.y * blurKernel.y;

				final += tex2D(_MainTex, uv + float2(-pixelSizeX,	-pixelSizeY)).rgb * cornerWeight;
				final += tex2D(_MainTex, uv + float2(0, 			-pixelSizeY)).rgb * edgeWeight;
				final += tex2D(_MainTex, uv + float2(pixelSizeX,	-pixelSizeY)).rgb * cornerWeight;

				final += tex2D(_MainTex, uv + float2(-pixelSizeX,	0)).rgb * edgeWeight;
				final += tex2D(_MainTex, uv + float2(0,			0)).rgb * centerWeight;
				final += tex2D(_MainTex, uv + float2(pixelSizeX,	0)).rgb * edgeWeight;

				final += tex2D(_MainTex, uv + float2(-pixelSizeX,	pixelSizeY)).rgb * cornerWeight;
				final += tex2D(_MainTex, uv + float2(0,				pixelSizeY)).rgb * edgeWeight;
				final += tex2D(_MainTex, uv + float2(pixelSizeX,	pixelSizeY)).rgb * cornerWeight;

				return half4(final / half3(blurZ, blurZ, blurZ), 1.0);
			}

			half4 frag(v2f i) : SV_Target { 
				return blur(i.uv, blurSigma); 
			}
			ENDCG
		}
	}
}