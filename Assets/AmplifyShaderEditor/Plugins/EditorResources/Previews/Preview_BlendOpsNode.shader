Shader "Hidden/BlendOpsNode"
{
	Properties
	{
		_A ("_Source", 2DArray) = "white" {}
		_B ("_Destiny", 2DArray) = "white" {}
		_C ("_Alpha", 2DArray) = "white" {}
	}
	SubShader
	{
		Pass //colorburn
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( 1.0 - ( ( 1.0 - des) / max( src,0.00001)) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp(des, c, alpha);
				}

				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //colordodge
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( des/ max( 1.0 - src,0.00001 ) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //darken
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( min( src , des ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //divide
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( des / max( src,0.00001) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //difference
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( abs( src - des ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //exclusion
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( 0.5 - 2.0 * ( src - 0.5 ) * ( des - 0.5 ) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //softlight
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( src > 0.5 ? ( sqrt( des ) * ( 2.0 * src - 1.0 ) + 2.0 * des * ( 1.0 - src ) ) : ( 2.0 * des * src + des * des * ( 1.0 - 2.0 * src ) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //hardlight
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = (  ( src > 0.5 ? ( 1.0 - ( 1.0 - 2.0 * ( src - 0.5 ) ) * ( 1.0 - des ) ) : ( 2.0 * src * des ) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //hardmix
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( round( 0.5 * ( src + des ) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //lighten
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( max( src, des ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //linearburn
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( src + des - 1.0 ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //lineardodge
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( src + des ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //linearlight
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( src > 0.5 ? ( des + 2.0 * src - 1.0 ) : ( des + 2.0 * ( src - 0.5 ) ) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //multiply
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( src * des ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //overlay
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( des > 0.5 ? ( 1.0 - 2.0 * ( 1.0 - des )  * ( 1.0 - src ) ) : ( 2.0 * des * src ) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //pinlight
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( src > 0.5 ? max( des, 2.0 * ( src - 0.5 ) ) : min( des, 2.0 * src ) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //subtract
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( des - src ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //screen
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( 1.0 - ( 1.0 - src ) * ( 1.0 - des ) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //vividlight
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( ( src > 0.5 ? ( des / max( ( 1.0 - src ) * 2.0 ,0.00001) ) : ( 1.0 - ( ( ( 1.0 - des ) * 0.5 ) / max(src,0.00001) ) ) ) );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //darkercolor
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( dot( src.rgb, float3( 0.3, 0.59, 0.11 ) ) < dot( des.rgb, float3( 0.3, 0.59, 0.11 ) ) ) ? src : des;
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //lightercolor
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = ( dot( src.rgb, float3( 0.3, 0.59, 0.11 ) ) > dot( des.rgb, float3( 0.3, 0.59, 0.11 ) ) ) ? src : des;
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //hue
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float ASEBlendLum( float3 c ) { return dot( c, float3( 0.3, 0.59, 0.11 ) ); }
			float ASEBlendSat( float3 c ) { return max( max( c.r, c.g ), c.b ) - min( min( c.r, c.g ), c.b ); }
			float3 ASEBlendClipColor( float3 c )
			{
				float l = ASEBlendLum( c );
				float n = min( min( c.r, c.g ), c.b );
				float x = max( max( c.r, c.g ), c.b );
				if ( n < 0.0 ) c = l + ( ( c - l ) * l ) / ( l - n );
				if ( x > 1.0 ) c = l + ( ( c - l ) * ( 1.0 - l ) ) / ( x - l );
				return c;
			}
			float3 ASEBlendSetLum( float3 c, float l ) { c += l - ASEBlendLum( c ); return ASEBlendClipColor( c ); }
			float3 ASEBlendSetSat( float3 c, float s )
			{
				float mn = min( min( c.r, c.g ), c.b );
				float mx = max( max( c.r, c.g ), c.b );
				return ( mx > mn ) ? ( ( c - mn ) * s / ( mx - mn ) ) : float3( 0.0, 0.0, 0.0 );
			}

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = float4( ASEBlendSetLum( ASEBlendSetSat( src.rgb, ASEBlendSat( des.rgb ) ), ASEBlendLum( des.rgb ) ), des.a );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //saturation
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float ASEBlendLum( float3 c ) { return dot( c, float3( 0.3, 0.59, 0.11 ) ); }
			float ASEBlendSat( float3 c ) { return max( max( c.r, c.g ), c.b ) - min( min( c.r, c.g ), c.b ); }
			float3 ASEBlendClipColor( float3 c )
			{
				float l = ASEBlendLum( c );
				float n = min( min( c.r, c.g ), c.b );
				float x = max( max( c.r, c.g ), c.b );
				if ( n < 0.0 ) c = l + ( ( c - l ) * l ) / ( l - n );
				if ( x > 1.0 ) c = l + ( ( c - l ) * ( 1.0 - l ) ) / ( x - l );
				return c;
			}
			float3 ASEBlendSetLum( float3 c, float l ) { c += l - ASEBlendLum( c ); return ASEBlendClipColor( c ); }
			float3 ASEBlendSetSat( float3 c, float s )
			{
				float mn = min( min( c.r, c.g ), c.b );
				float mx = max( max( c.r, c.g ), c.b );
				return ( mx > mn ) ? ( ( c - mn ) * s / ( mx - mn ) ) : float3( 0.0, 0.0, 0.0 );
			}

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = float4( ASEBlendSetLum( ASEBlendSetSat( des.rgb, ASEBlendSat( src.rgb ) ), ASEBlendLum( des.rgb ) ), des.a );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //color
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float ASEBlendLum( float3 c ) { return dot( c, float3( 0.3, 0.59, 0.11 ) ); }
			float3 ASEBlendClipColor( float3 c )
			{
				float l = ASEBlendLum( c );
				float n = min( min( c.r, c.g ), c.b );
				float x = max( max( c.r, c.g ), c.b );
				if ( n < 0.0 ) c = l + ( ( c - l ) * l ) / ( l - n );
				if ( x > 1.0 ) c = l + ( ( c - l ) * ( 1.0 - l ) ) / ( x - l );
				return c;
			}
			float3 ASEBlendSetLum( float3 c, float l ) { c += l - ASEBlendLum( c ); return ASEBlendClipColor( c ); }

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = float4( ASEBlendSetLum( src.rgb, ASEBlendLum( des.rgb ) ), des.a );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}

		Pass //luminosity
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			int _Sat;
			int _Lerp;

			float ASEBlendLum( float3 c ) { return dot( c, float3( 0.3, 0.59, 0.11 ) ); }
			float3 ASEBlendClipColor( float3 c )
			{
				float l = ASEBlendLum( c );
				float n = min( min( c.r, c.g ), c.b );
				float x = max( max( c.r, c.g ), c.b );
				if ( n < 0.0 ) c = l + ( ( c - l ) * l ) / ( l - n );
				if ( x > 1.0 ) c = l + ( ( c - l ) * ( 1.0 - l ) ) / ( x - l );
				return c;
			}
			float3 ASEBlendSetLum( float3 c, float l ) { c += l - ASEBlendLum( c ); return ASEBlendClipColor( c ); }

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = ASE_SAMPLE_PORT( _A, i.uv );
				float4 des = ASE_SAMPLE_PORT( _B, i.uv );

				float4 c = float4( ASEBlendSetLum( des.rgb, ASEBlendLum( src.rgb ) ), des.a );
				if (_Lerp == 1)
				{
					float alpha = ASE_SAMPLE_PORT( _C, i.uv).r;
					c = lerp (des, c, alpha);
				}
				if( _Sat == 1 )
					c = saturate( c );
				return c;
			}
			ENDCG
		}
	}
}
