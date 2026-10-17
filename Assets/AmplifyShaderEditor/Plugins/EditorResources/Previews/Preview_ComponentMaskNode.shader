Shader "Hidden/ComponentMaskNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
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

			ASE_PORT( _A );
			ASE_PORT( _B );
			float _Singular;
			float4 _Order;
			float _Mode;
			float _Count;
			float _Clamp;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );

				if(_Mode == 1)
				{
					if(_Count == 0)
						return a;

					float index = ASE_SAMPLE_PORT( _B, i.uv ).r;
					if(_Clamp == 1)
						index = clamp( index, 0, _Count - 1 );

					return a[ (int)index ];
				}

				float4 r = 0;
				if(_Singular == 0)
					r = a.x;
				else if(_Singular == 1)
					r = a.y;
				else if(_Singular == 2)
					r = a.z;
				else if(_Singular == 3)
					r = a.w;

				if ( _Order.x == 0 )
					r.x = a.x;
				else if(_Order.y == 0)
					r.x = a.y;
				else if(_Order.z == 0)
					r.x = a.z;
				else if(_Order.w == 0)
					r.x = a.w;

				if(_Order.y == 1)
					r.y = a.y;
				else if(_Order.z == 1)
					r.y = a.z;
				else if(_Order.w == 1)
					r.y = a.w;

				if(_Order.z == 2)
					r.z = a.z;
				else if(_Order.w == 2)
					r.z = a.w;

				if(_Order.w == 3)
					r.w = a.w;

				return r;
			}
			ENDCG
		}
	}
}
