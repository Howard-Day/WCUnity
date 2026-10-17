Shader "Hidden/TransformPositionNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			#define SPACE_OBJECT 0
			#define SPACE_WORLD 1
			#define SPACE_VIEW 2
			#define SPACE_TANGENT 3
			#define SPACE_CLIP 4
			#define SPACE_SCREEN 5

			ASE_PORT( _A );
			int _From;
			int _To;
			int _PerspectiveDivide;

			float3 ToWorldPos( float2 uv, float3 pos )
			{
				if( _From == SPACE_OBJECT )
					return mul( preview_ObjectToWorld, float4( pos, 1 ) ).xyz;
				else if( _From == SPACE_VIEW )
					return mul( preview_MatrixInvV, float4( pos, 1 ) ).xyz;
				// tangent space has no origin, so the generated code transforms it as a direction
				else if( _From == SPACE_TANGENT )
					return PreviewFragmentTangentToWorldDir( uv, pos, false );
				else
					return pos;
			}

			float4 FromWorldPos( float2 uv, float3 pos )
			{
				if( _To == SPACE_OBJECT )
					return float4( mul( preview_WorldToObject, float4( pos, 1 ) ).xyz, 1 );
				else if( _To == SPACE_VIEW )
					return float4( mul( preview_MatrixV, float4( pos, 1 ) ).xyz, 1 );
				else if( _To == SPACE_TANGENT )
					return float4( PreviewFragmentWorldToTangentDir( uv, pos, false ), 1 );
				else if( _To == SPACE_CLIP )
				{
					float4 clipPos = mul( preview_MatrixP, mul( preview_MatrixV, float4( pos, 1 ) ) );
					return _PerspectiveDivide ? clipPos / clipPos.w : clipPos;
				}
				else if( _To == SPACE_SCREEN )
				{
					float4 clipPos = mul( preview_MatrixP, mul( preview_MatrixV, float4( pos, 1 ) ) );
					float4 screenPos = ComputeScreenPos( clipPos );
					return float4( screenPos.xyz / screenPos.w, 1 );
				}
				else
					return float4( pos, 1 );
			}

			float4 frag( v2f_img i ) : SV_Target
			{
				float3 pos = ASE_SAMPLE_PORT( _A, i.uv ).xyz;

				// matching spaces are a passthrough on the generated code
				if( _From == _To )
					return float4( pos, 1 );

				return FromWorldPos( i.uv, ToWorldPos( i.uv, pos ) );
			}
			ENDCG
		}
	}
}
