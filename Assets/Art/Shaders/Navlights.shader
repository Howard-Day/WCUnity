// Made with Amplify Shader Editor v1.9.6.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Navlights"
{
	Properties
	{
		_Navlights("Navlights", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Custom"  "Queue" = "Transparent+0" "ForceNoShadowCasting" = "True" "IsEmissive" = "true"  }
		Cull Back
		ZWrite Off
		Offset  -1 , -1
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow exclude_path:deferred vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
			float clampDepth;
			float4 vertexColor : COLOR;
		};

		uniform sampler2D _Navlights;


		float MyCustomExpression7(  )
		{
			 return  unity_CameraProjection._m11;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			v.vertex.xyz += ( ase_worldViewDir * 4.0 );
			v.vertex.w = 1;
			o.clampDepth = -UnityObjectToViewPos( v.vertex.xyz ).z * _ProjectionParams.w;
		}

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_TexCoord23 = i.uv_texcoord * float2( 1,0.125 );
			float lerpResult11 = lerp( 0.0 , 96.0 , pow( i.clampDepth , 1.25 ));
			float clampResult10 = clamp( lerpResult11 , 0.0 , 1.5 );
			float3 ase_objectScale = float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) );
			float localMyCustomExpression7 = MyCustomExpression7();
			float2 appendResult26 = (float2((uv_TexCoord23).x , ( (uv_TexCoord23).y + saturate( ( floor( ( saturate( ( ( ( clampResult10 / ase_objectScale ) * 1.0 ) / localMyCustomExpression7 ) ) * 8.0 ) ) / 8.0 ) ) ).x));
			float4 tex2DNode12 = tex2D( _Navlights, appendResult26 );
			o.Emission = ( ( float4( pow( tex2DNode12.rgb , 6.0 ) , 0.0 ) * (i.vertexColor).grba * 5.0 ) + ( i.vertexColor * float4( pow( tex2DNode12.rgb , 0.5 ) , 0.0 ) * 4.0 ) ).xyz;
			o.Alpha = ( tex2DNode12.r * 2.0 * tex2DNode12.a );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19603
Node;AmplifyShaderEditor.SurfaceDepthNode;8;-2752,224;Inherit;False;1;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;9;-2496,224;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1.25;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;11;-2256,176;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;96;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ObjectScaleNode;2;-2080,304;Inherit;False;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ClampOpNode;10;-2048,176;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;1;-1808,176;Inherit;False;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;30;-1792,288;Inherit;False;Constant;_Float1;Float 1;2;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-1632,176;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CustomExpressionNode;7;-1792,416;Inherit;False; return  unity_CameraProjection._m11@;1;Create;0;My Custom Expression;True;False;0;;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;4;-1440,176;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;13;-1328,176;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-1312,304;Inherit;False;Constant;_Float3;Float 3;3;0;Create;True;0;0;0;False;0;False;8;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;-1120,160;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;4;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FloorOpNode;18;-960,160;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;20;-672,160;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;23;-880,-16;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,0.125;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;24;-576,48;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;52;-548.6101,158.4456;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;25;-320,112;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;27;-576,-32;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;26;-192,96;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;12;-32,16;Inherit;True;Property;_Navlights;Navlights;0;0;Create;True;0;0;0;False;0;False;-1;290ae375aeab5dc4bbe6778ba43300b2;290ae375aeab5dc4bbe6778ba43300b2;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.VertexColorNode;31;32,-176;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;34;336,-32;Inherit;False;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0.5;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;50;356.6178,58.67926;Inherit;False;Constant;_Float2;Float 2;2;0;Create;True;0;0;0;False;0;False;4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;42;304,-176;Inherit;False;FLOAT4;1;0;2;3;1;0;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;53;336,-112;Inherit;False;Constant;_Float5;Float 5;2;0;Create;True;0;0;0;False;0;False;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;36;336,-272;Inherit;False;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;6;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;37;512,-224;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;512,-64;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;46;416,160;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;48;535.1024,360.9094;Inherit;False;Constant;_Float0;Float 0;2;0;Create;True;0;0;0;False;0;False;4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;35;720,-176;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ColorNode;43;16,-384;Inherit;False;Constant;_Color0;Color 0;2;0;Create;True;0;0;0;False;0;False;0,0.2151163,1,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;28;960,-224;Inherit;False;Constant;_Float4;Float 4;3;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;784,16;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;2;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;726.7336,173.6584;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1104,-224;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Navlights;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;2;False;;0;False;;True;-1;False;;-1;False;;False;0;Custom;0;True;False;0;True;Custom;;Transparent;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;9;0;8;0
WireConnection;11;2;9;0
WireConnection;10;0;11;0
WireConnection;1;0;10;0
WireConnection;1;1;2;0
WireConnection;3;0;1;0
WireConnection;3;1;30;0
WireConnection;4;0;3;0
WireConnection;4;1;7;0
WireConnection;13;0;4;0
WireConnection;45;0;13;0
WireConnection;45;1;21;0
WireConnection;18;0;45;0
WireConnection;20;0;18;0
WireConnection;20;1;21;0
WireConnection;24;0;23;0
WireConnection;52;0;20;0
WireConnection;25;0;24;0
WireConnection;25;1;52;0
WireConnection;27;0;23;0
WireConnection;26;0;27;0
WireConnection;26;1;25;0
WireConnection;12;1;26;0
WireConnection;34;0;12;5
WireConnection;42;0;31;0
WireConnection;36;0;12;5
WireConnection;37;0;36;0
WireConnection;37;1;42;0
WireConnection;37;2;53;0
WireConnection;32;0;31;0
WireConnection;32;1;34;0
WireConnection;32;2;50;0
WireConnection;35;0;37;0
WireConnection;35;1;32;0
WireConnection;49;0;12;1
WireConnection;49;2;12;4
WireConnection;47;0;46;0
WireConnection;47;1;48;0
WireConnection;0;2;35;0
WireConnection;0;9;49;0
WireConnection;0;11;47;0
ASEEND*/
//CHKSM=CF34F2839B7CE738200B458F0964942574EAC4EC