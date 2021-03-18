// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MassSpawner/Layer"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		_SlopeMap("Slopemap", 2D) = "white" {}
		_HeightRange("HeightRange", Vector) = (0,1,0,0)
		_SlopeRange("SlopeRange", Vector) = (0,1,0,0)
		_ShowPlacement("Show placement", Float) = 0
		_ObjectPlaces("Placement", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

	}

	SubShader
	{
		LOD 0

		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
		
		Stencil
		{
			Ref [_Stencil]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
			CompFront [_StencilComp]
			PassFront [_StencilOp]
			FailFront Keep
			ZFailFront Keep
			CompBack Always
			PassBack Keep
			FailBack Keep
			ZFailBack Keep
		}


		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		
		Pass
		{
			Name "Default"
		CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_CLIP_RECT
			#pragma multi_compile __ UNITY_UI_ALPHACLIP
			
			
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				
			};
			
			uniform fixed4 _Color;
			uniform fixed4 _TextureSampleAdd;
			uniform float4 _ClipRect;
			uniform sampler2D _MainTex;
			uniform float _ShowPlacement;
			uniform float4 _MainTex_ST;
			uniform float2 _HeightRange;
			uniform sampler2D _SlopeMap;
			uniform float4 _SlopeMap_ST;
			uniform float2 _SlopeRange;
			uniform sampler2D _ObjectPlaces;
			uniform float4 _ObjectPlaces_ST;
			float FloatInRange7( float Value , float Min , float Max )
			{
				if (Value >= Min && Value <= Max)
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}
			
			float FloatInRange8( float Value , float Min , float Max )
			{
				if (Value >= Min && Value <= Max)
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}
			

			
			v2f vert( appdata_t IN  )
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID( IN );
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				OUT.worldPosition = IN.vertex;
				
				
				OUT.worldPosition.xyz +=  float3( 0, 0, 0 ) ;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;
				
				OUT.color = IN.color * _Color;
				return OUT;
			}

			fixed4 frag(v2f IN  ) : SV_Target
			{
				float2 uv_MainTex = IN.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 tex2DNode2 = tex2D( _MainTex, uv_MainTex );
				float4 color12 = IsGammaSpace() ? float4(0,0,0,0) : float4(0,0,0,0);
				float4 color13 = IsGammaSpace() ? float4(1,1,0,0.5882353) : float4(1,1,0,0.5882353);
				float Value7 = tex2DNode2.r;
				float Min7 = _HeightRange.x;
				float Max7 = _HeightRange.y;
				float localFloatInRange7 = FloatInRange7( Value7 , Min7 , Max7 );
				float2 uv_SlopeMap = IN.texcoord.xy * _SlopeMap_ST.xy + _SlopeMap_ST.zw;
				float Value8 = tex2D( _SlopeMap, uv_SlopeMap ).r;
				float Min8 = _SlopeRange.x;
				float Max8 = _SlopeRange.y;
				float localFloatInRange8 = FloatInRange8( Value8 , Min8 , Max8 );
				float temp_output_9_0 = ( localFloatInRange7 * localFloatInRange8 );
				float4 lerpResult14 = lerp( color12 , color13 , temp_output_9_0);
				float4 ifLocalVar11 = 0;
				if( tex2DNode2.a <= 0.0 )
				ifLocalVar11 = color12;
				else
				ifLocalVar11 = lerpResult14;
				float2 uv_ObjectPlaces = IN.texcoord.xy * _ObjectPlaces_ST.xy + _ObjectPlaces_ST.zw;
				float ifLocalVar17 = 0;
				if( tex2DNode2.a <= 0.0 )
				ifLocalVar17 = 0.0;
				else
				ifLocalVar17 = 1.0;
				float4 lerpResult23 = lerp( float4( 0,0,0,0 ) , tex2D( _ObjectPlaces, uv_ObjectPlaces ) , ( ifLocalVar17 * temp_output_9_0 ));
				float4 lerpResult25 = lerp( ifLocalVar11 , lerpResult23 , lerpResult23.a);
				float4 ifLocalVar16 = 0;
				if( _ShowPlacement <= 0.0 )
				ifLocalVar16 = ifLocalVar11;
				else
				ifLocalVar16 = lerpResult25;
				
				half4 color = ifLocalVar16;
				
				#ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
				
				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif

				return color;
			}
		ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=18301
130;101;1410;839;-398.4285;466.3994;1.746626;True;True
Node;AmplifyShaderEditor.TemplateShaderPropertyNode;43;-742.3567,-49.54141;Inherit;False;0;0;_MainTex;Shader;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;3;-785.2434,196.7426;Inherit;True;Property;_SlopeMap;Slopemap;0;0;Create;False;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.Vector2Node;5;-448.9153,422.448;Inherit;False;Property;_HeightRange;HeightRange;1;0;Create;True;0;0;False;0;False;0,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;6;-441.055,549.7836;Inherit;False;Property;_SlopeRange;SlopeRange;2;0;Create;False;0;0;False;0;False;0,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;2;-563.2473,-52.88387;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;4;-563.6742,191.3576;Inherit;True;Property;_TextureSample1;Texture Sample 1;2;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CustomExpressionNode;8;-148.3098,654.4418;Inherit;False;if (Value >= Min && Value <= Max)${$	return 1@$}$else${$	return 0@$};1;False;3;True;Value;FLOAT;0;In;;Inherit;False;True;Min;FLOAT;0;In;;Inherit;False;True;Max;FLOAT;0;In;;Inherit;False;FloatInRange;True;False;0;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;7;-147.7468,521.2996;Inherit;False;if (Value >= Min && Value <= Max)${$	return 1@$}$else${$	return 0@$};1;False;3;True;Value;FLOAT;0;In;;Inherit;False;True;Min;FLOAT;0;In;;Inherit;False;True;Max;FLOAT;0;In;;Inherit;False;FloatInRange;True;False;0;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;248.0077,808.7537;Inherit;False;Constant;_Float0;Float 0;5;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;245.0077,883.7537;Inherit;False;Constant;_Float1;Float 1;5;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;17;402.1956,758.092;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;58.39024,599.8416;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;21;361.6487,529.9885;Inherit;True;Property;_ObjectPlaces;Placement;4;0;Create;False;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;22;584.301,532.0842;Inherit;True;Property;_TextureSample2;Texture Sample 2;6;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;12;-166.7001,166.8688;Inherit;False;Constant;_Black;Black;4;0;Create;True;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;13;-165.2288,344.8802;Inherit;False;Constant;_Yellow;Yellow;4;0;Create;True;0;0;False;0;False;1,1,0,0.5882353;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;594.0099,884.3024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;23;953.1553,743.4838;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;14;192.2648,378.7169;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;24;1112.084,861.684;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.ConditionalIfNode;11;340.2701,132.285;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;15;1310.25,3.377059;Inherit;False;Property;_ShowPlacement;Show placement;3;0;Create;False;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;25;1370.558,694.0191;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ConditionalIfNode;16;1512.871,44.28787;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;42;1721.252,48.75533;Float;False;True;-1;2;ASEMaterialInspector;0;6;MassSpawner/Layer;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;True;2;False;-1;True;True;True;True;True;0;True;-9;False;False;False;True;True;0;True;-5;255;True;-8;255;True;-7;0;True;-4;0;True;-6;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;0;True;-11;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;0;;0;0;Standard;0;0;1;True;False;;0
WireConnection;2;0;43;0
WireConnection;4;0;3;0
WireConnection;8;0;4;1
WireConnection;8;1;6;1
WireConnection;8;2;6;2
WireConnection;7;0;2;1
WireConnection;7;1;5;1
WireConnection;7;2;5;2
WireConnection;17;0;2;4
WireConnection;17;2;18;0
WireConnection;17;3;19;0
WireConnection;17;4;19;0
WireConnection;9;0;7;0
WireConnection;9;1;8;0
WireConnection;22;0;21;0
WireConnection;20;0;17;0
WireConnection;20;1;9;0
WireConnection;23;1;22;0
WireConnection;23;2;20;0
WireConnection;14;0;12;0
WireConnection;14;1;13;0
WireConnection;14;2;9;0
WireConnection;24;0;23;0
WireConnection;11;0;2;4
WireConnection;11;2;14;0
WireConnection;11;3;12;0
WireConnection;11;4;12;0
WireConnection;25;0;11;0
WireConnection;25;1;23;0
WireConnection;25;2;24;3
WireConnection;16;0;15;0
WireConnection;16;2;25;0
WireConnection;16;3;11;0
WireConnection;16;4;11;0
WireConnection;42;0;16;0
ASEEND*/
//CHKSM=482F5D5338C73DAA0A373740C40AB082E028C130