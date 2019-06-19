// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*
	Basic Sprite Shader for aligning pixel art to the same grid, based on the Unity Sprite Shader.
	Create one Material where you assign the same Pixels Per Unit value you use on your imported Sprites,
	then reuse this Material on all appropriate Sprite Renderers.
	(You can use Shader.SetGlobalFloat to set that Pixels Per Unit value for all your shaders:
	https://docs.unity3d.com/ScriptReference/Shader.SetGlobalFloat.html)

	This is not for scaled or rotated artwork. If you need those features, look at low res render textures.

	Use this however you want.

	@talecrafter
*/

Shader "Sprites/PixelArt"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_pixelsPerUnit("Pixels Per Unit", Float) = 16
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag			
			#pragma multi_compile _ UNITY_ETC1_EXTERNAL_ALPHA

			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord  : TEXCOORD0;
			};			
			
			fixed4 _Color;
			float _pixelsPerUnit;

			float4 AlignToPixelGrid(float4 vertex)
			{
				float4 worldPos = mul(unity_ObjectToWorld, vertex);

				worldPos.x = floor(worldPos.x * _pixelsPerUnit + 0.5) / _pixelsPerUnit;
				worldPos.y = floor(worldPos.y * _pixelsPerUnit + 0.5) / _pixelsPerUnit;

				return mul(unity_WorldToObject, worldPos);
			}

			v2f vert(appdata_t IN)
			{
				float4 alignedPos = AlignToPixelGrid(IN.vertex);

				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(alignedPos);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;

				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _AlphaTex;

			fixed4 SampleSpriteTexture(float2 uv)
			{
				fixed4 color = tex2D(_MainTex, uv);

				#if ETC1_EXTERNAL_ALPHA
				color.a = tex2D(_AlphaTex, uv).r;
				#endif

				return color;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = SampleSpriteTexture (IN.texcoord);
				c.rgb += IN.color.rgb;
				c.rgb *= c.a;


				return c;
			}
		ENDCG
		}
	}
}