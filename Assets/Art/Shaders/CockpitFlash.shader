// Made with Amplify Shader Editor v1.9.6.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CockpitFlash"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_BaseTex("BaseTex", 2D) = "white" {}
		_Dithering("Dithering", 2D) = "white" {}
		_Brightness("Brightness", Range( 0 , 1)) = 1
		_BrightColor("BrightColor", Color) = (1,0.5137255,0.2941177,1)
		_DarkColor("DarkColor", Color) = (0.8352942,0.2509804,0.6392157,1)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Custom"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		ZWrite Off
		Blend One One
		BlendOp Max
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
			float4 screenPos;
		};

		uniform float4 _DarkColor;
		uniform float4 _BrightColor;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _BaseTex;
		uniform float4 _BaseTex_ST;
		uniform sampler2D _Dithering;
		uniform float _Brightness;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float3 temp_output_21_0 = ( _BrightColor.rgb * 1.25 );
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float2 uv_BaseTex = i.uv_texcoord * _BaseTex_ST.xy + _BaseTex_ST.zw;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float3 lerpResult4 = lerp( ( tex2D( _MainTex, uv_MainTex ).rgb * pow( length( tex2D( _BaseTex, uv_BaseTex ).rgb ) , 0.0025 ) ) , tex2D( _Dithering, ase_screenPosNorm.xy ).rgb , 0.075);
			float div8=256.0/float(42);
			float4 posterize8 = ( floor( float4( ( lerpResult4 * 1.25 * _Brightness ) , 0.0 ) * div8 ) / div8 );
			float4 temp_output_16_0 = saturate( posterize8 );
			float4 temp_cast_2 = (0.65).xxxx;
			float4 temp_cast_3 = (1.0).xxxx;
			float4 temp_cast_4 = (0.0).xxxx;
			float4 temp_cast_5 = (1.0).xxxx;
			float3 lerpResult17 = lerp( _DarkColor.rgb , temp_output_21_0 , saturate( (temp_cast_4 + (temp_output_16_0 - temp_cast_2) * (temp_cast_5 - temp_cast_4) / (temp_cast_3 - temp_cast_2)) ).rgb);
			float4 lerpResult26 = lerp( float4( temp_output_21_0 , 0.0 ) , pow( ( posterize8 + -0.15 ) , 5.0 ) , 0.5);
			o.Emission = ( max( float4( lerpResult17 , 0.0 ) , lerpResult26 ) * pow( temp_output_16_0 , 0.5 ) ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19603
Node;AmplifyShaderEditor.SamplerNode;9;-1616,-32;Inherit;True;Property;_BaseTex;BaseTex;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LengthOpNode;10;-1328,-16;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;2;-1328,176;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;12;-1184,-16;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;0.0025;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-1616,-256;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;0;False;0;False;-1;None;73714f1c2f194f24da42699001f7b0c4;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;3;-1136,160;Inherit;True;Property;_Dithering;Dithering;3;0;Create;True;0;0;0;False;0;False;-1;1dec9d0afe198024b9a43a07728f9376;1dec9d0afe198024b9a43a07728f9376;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-1008,-48;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-816,272;Inherit;False;Constant;_Float0;Float 0;3;0;Create;True;0;0;0;False;0;False;0.075;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;4;-640,80;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-640,208;Inherit;False;Constant;_Float1;Float 1;3;0;Create;True;0;0;0;False;0;False;1.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-768,368;Inherit;False;Property;_Brightness;Brightness;4;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;-416,80;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PosterizeNode;8;-256,80;Inherit;False;42;2;1;COLOR;0,0,0,0;False;0;INT;42;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;30;210.698,-37.14215;Inherit;False;Constant;_Float4;Float 4;4;0;Create;True;0;0;0;False;0;False;-0.15;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;35;-162.1289,418.4081;Inherit;False;Constant;_Float7;Float 7;7;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;36;-162.1289,483.4081;Inherit;False;Constant;_Float8;Float 8;7;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;16;-112,80;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;33;-176,288;Inherit;False;Constant;_Float5;Float 5;7;0;Create;True;0;0;0;False;0;False;0.65;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;34;-176,352;Inherit;False;Constant;_Float6;Float 6;7;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-256,-34;Inherit;False;Constant;_Float2;Float 2;4;0;Create;True;0;0;0;False;0;False;1.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;29;384,-64;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;15;-240,-288;Inherit;False;Property;_BrightColor;BrightColor;5;0;Create;True;0;0;0;False;0;False;1,0.5137255,0.2941177,1;1,0.5137255,0.2941177,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.TFHCRemapNode;32;80,320;Inherit;False;5;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,1;False;3;COLOR;0,0,0,0;False;4;COLOR;1,1,1,1;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;27;512,32;Inherit;False;Constant;_Float3;Float 3;4;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;28;496,-64;Inherit;False;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;5;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;18;48,-448;Inherit;False;Property;_DarkColor;DarkColor;6;0;Create;True;0;0;0;False;0;False;0.8352942,0.2509804,0.6392157,1;0.8352942,0.2509804,0.6392157,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;48,-128;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;37;256,256;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;17;320,-192;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;26;656,-32;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;19;304,80;Inherit;False;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0.5;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;24;832,-48;Inherit;False;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;976,48;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;23;16,224;Inherit;False;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;12;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1200,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;CockpitFlash;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;2;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;True;0;True;Custom;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;4;1;False;;1;False;;0;0;False;;0;False;;5;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;2;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;10;0;9;5
WireConnection;12;0;10;0
WireConnection;3;1;2;0
WireConnection;11;0;1;5
WireConnection;11;1;12;0
WireConnection;4;0;11;0
WireConnection;4;1;3;5
WireConnection;4;2;5;0
WireConnection;6;0;4;0
WireConnection;6;1;7;0
WireConnection;6;2;31;0
WireConnection;8;1;6;0
WireConnection;16;0;8;0
WireConnection;29;0;8;0
WireConnection;29;1;30;0
WireConnection;32;0;16;0
WireConnection;32;1;33;0
WireConnection;32;2;34;0
WireConnection;32;3;35;0
WireConnection;32;4;36;0
WireConnection;28;0;29;0
WireConnection;21;0;15;5
WireConnection;21;1;22;0
WireConnection;37;0;32;0
WireConnection;17;0;18;5
WireConnection;17;1;21;0
WireConnection;17;2;37;0
WireConnection;26;0;21;0
WireConnection;26;1;28;0
WireConnection;26;2;27;0
WireConnection;19;0;16;0
WireConnection;24;0;17;0
WireConnection;24;1;26;0
WireConnection;20;0;24;0
WireConnection;20;1;19;0
WireConnection;23;0;16;0
WireConnection;0;2;20;0
ASEEND*/
//CHKSM=6FA7289183E750596272AF8B4DD4CA476EDB96B7