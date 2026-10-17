// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/ASEBlitCopy" {
	Properties
	{
		_MainTex( "Texture", any ) = "" {}
		_Color( "Multiplicative color", Color ) = ( 1.0, 1.0, 1.0, 1.0 )
		_ColorConversion( "Color Conversion (0 = none, 1 = linear to gamma, -1 = gamma to linear)", Float ) = 0.0
	}
		SubShader{
			Pass {
				ZTest Always Cull Off ZWrite Off

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				UNITY_DECLARE_SCREENSPACE_TEXTURE( _MainTex );
				uniform float4 _MainTex_ST;
				uniform float4 _Color;
				float _ColorConversion;

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_OUTPUT_STEREO
				};

				v2f vert( appdata_t v )
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID( v );
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
					o.vertex = UnityObjectToClipPos( v.vertex );
					o.texcoord = TRANSFORM_TEX( v.texcoord.xy, _MainTex );
					return o;
				}

				float4 frag( v2f i ) : SV_Target
				{
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( i );
					float4 col = UNITY_SAMPLE_SCREENSPACE_TEXTURE( _MainTex, i.texcoord ) * _Color;
					if ( _ColorConversion == 1 )
						return fixed4( LinearToGammaSpace( col.rgb ), col.a );
					if ( _ColorConversion == -1 )
						return fixed4( GammaToLinearSpace( col.rgb ), col.a );
					return col;
				}
				ENDCG

			}
	}
		Fallback Off
}
