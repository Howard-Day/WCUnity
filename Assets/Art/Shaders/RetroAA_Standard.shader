Shader "RetroAA/Standard" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_EmissionColor ("Color", Color) = (1,1,1,1)	
		_EmissionMap ("Emission (RGB)", 2D) = "white" {}
		_NormalMap ("Normals (RGB)", 2D) = "normals" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#include "RetroAA.cginc"

		#pragma surface surf Standard fullforwardshadows

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		sampler2D _EmissionMap;
		float4 _EmissionMap_TexelSize;

		sampler2D _NormalMap;
		float4 _NormalMap_TexelSize;

		struct Input {
			float2 uv_MainTex;
			float2 uv_EmissionMap;
			float2 uv_NormalMap;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
    float4 _EmissionColor;
		void surf(Input i, inout SurfaceOutputStandard o){
			fixed4 color = _Color*RetroAA(_MainTex, i.uv_MainTex, _MainTex_TexelSize);
			o.Albedo = color;
			o.Normal = UnpackNormal(RetroAA(_NormalMap, i.uv_NormalMap, _NormalMap_TexelSize));
      o.Emission = _EmissionColor*RetroAA(_EmissionMap, i.uv_EmissionMap, _EmissionMap_TexelSize);
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = color.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
