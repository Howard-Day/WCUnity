Shader "RetroAA/Unlit Cutout"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color ) = (1,1,1,0)
		_pixelsPerUnit("Pixels Per Unit", Float) = 16
	}
	SubShader
	{
		Tags { "QUEUE"="Geometry"}
		LOD 100

		Pass
		{
			//AlphaToMask On
			Blend off
			ZWrite On
			ZTest on
			Cull off
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				
				#include "RetroAA.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					UNITY_VERTEX_OUTPUT_STEREO
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _MainTex_TexelSize;
				float _pixelsPerUnit;
				float4 _Color;

				float4 AlignToPixelGrid(float4 vertex)
				{
					float4 worldPos = mul(unity_ObjectToWorld, vertex);

					worldPos.x = floor(worldPos.x * _pixelsPerUnit + 0.5) / _pixelsPerUnit;
					worldPos.y = floor(worldPos.y * _pixelsPerUnit + 0.5) / _pixelsPerUnit;

					return mul(unity_WorldToObject, worldPos);
				}			
				v2f vert (appdata_t v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					float4 alignedPos = AlignToPixelGrid(v.vertex);

				

					o.vertex = UnityObjectToClipPos(alignedPos);
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}
				
				fixed4 frag (v2f i) : SV_Target
				{
					fixed4 col = RetroAA(_MainTex, i.texcoord, _MainTex_TexelSize);
					col.rgb += _Color.rgb;
					clip(col.a - 16 / 255.0);
					UNITY_APPLY_FOG(i.fogCoord, col);
					return col;
				}
			ENDCG
		}
	}
}
