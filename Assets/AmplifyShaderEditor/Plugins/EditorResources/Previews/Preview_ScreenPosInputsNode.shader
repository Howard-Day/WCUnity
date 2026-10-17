Shader "Hidden/ScreenPosInputsNode"
{
	SubShader
	{
		CGINCLUDE
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			uniform float4 _ASEPreviewSize;

			inline float4 CalculateScreenPos( float2 uv, bool norm = true, float z = 0.01 )
			{
				float2 xy = 2 * uv - 1;
				float3 vertexPos = float3( xy, z );
				float4x4 P = float4x4( 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 ); //UNITY_MATRIX_P
				float4x4 V = UNITY_MATRIX_V; //float4x4(1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1); //UNITY_MATRIX_V
				float4x4 M = unity_ObjectToWorld; //float4x4(1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1); //unity_ObjectToWorld
				float4x4 VPmatrix = mul( P, V );
				float4 clipPos = mul( VPmatrix, mul( M, float4( vertexPos, 1.0 ) ) ); //same as object to clip pos
				float4 screenPos = ComputeScreenPos( clipPos );
				screenPos = norm ? screenPos / screenPos.w : screenPos;
				return screenPos;
			}
		ENDCG

		// Normalized
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					return CalculateScreenPos( i.uv );
				}
			ENDCG
		}

		// Raw
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					return CalculateScreenPos( i.uv, false, 0.99 );
				}
			ENDCG
		}

		// Center
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					float4 screenPos = CalculateScreenPos( i.uv );
					return float4( screenPos.xy * 2 - 1, 0, 0 );
				}
			ENDCG
		}

		// Tiled
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					float4 screenPos = CalculateScreenPos( i.uv );
					return frac( float4( ( screenPos.x * 2 - 1 ) * _ASEPreviewSize.x / _ASEPreviewSize.y, screenPos.y * 2 - 1, 0, 0 ) );
				}
			ENDCG
		}

		// Screen
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					float4 screenPos = CalculateScreenPos( i.uv );
					screenPos.xy *= _ASEPreviewSize.xy;
				#if UNITY_UV_STARTS_AT_TOP
					screenPos.xy = float2( screenPos.x, ( _ProjectionParams.x < 0 ) ? _ASEPreviewSize.y - screenPos.y : screenPos.y );
				#else
					screenPos.xy = float2( screenPos.x, ( _ProjectionParams.x > 0 ) ? _ASEPreviewSize.y - screenPos.y : screenPos.y );
				#endif
					return float4( screenPos.xy, 0, 0 );
				}
			ENDCG
		}
	}
}
