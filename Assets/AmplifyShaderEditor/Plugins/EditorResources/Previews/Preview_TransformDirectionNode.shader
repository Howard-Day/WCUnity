Shader "Hidden/TransformDirectionNode"
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

			ASE_PORT( _A );
			int _From;
			int _To;
			int _Normalize;
			int _SafeNormalize;

			float3 ToWorldDir( float2 uv, float3 dir )
			{
				if( _From == SPACE_OBJECT )
					return PreviewObjectToWorldDir( dir, false );
				else if( _From == SPACE_VIEW )
					return PreviewViewToWorldDir( dir, false );
				else if( _From == SPACE_TANGENT )
					return PreviewFragmentTangentToWorldDir( uv, dir, false );
				else
					return dir;
			}

			float3 FromWorldDir( float2 uv, float3 dir )
			{
				if( _To == SPACE_OBJECT )
					return PreviewWorldToObjectDir( dir, false );
				else if( _To == SPACE_VIEW )
					return PreviewWorldToViewDir( dir, false );
				else if( _To == SPACE_TANGENT )
					return PreviewFragmentWorldToTangentDir( uv, dir, false );
				else if( _To == SPACE_CLIP )
					return mul( preview_MatrixP, float4( PreviewWorldToViewDir( dir, false ), 0 ) ).xyz;
				else
					return dir;
			}

			float4 frag( v2f_img i ) : SV_Target
			{
				float3 dir = ASE_SAMPLE_PORT( _A, i.uv ).xyz;

				// matching spaces are a passthrough on the generated code, normalization included
				if( _From == _To )
					return float4( dir, 1 );

				dir = FromWorldDir( i.uv, ToWorldDir( i.uv, dir ) );

				if( _Normalize )
					dir = _SafeNormalize ? dir * rsqrt( max( 1.175494351e-38, dot( dir, dir ) ) ) : normalize( dir );

				return float4( dir, 1 );
			}
			ENDCG
		}
	}
}
