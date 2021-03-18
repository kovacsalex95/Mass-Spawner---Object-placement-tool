// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MassSpawner/LayerURP"
{
	Properties
	{
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		_SlopeMap("Slopemap", 2D) = "white" {}
		_HeightRange("HeightRange", Vector) = (0,1,0,0)
		_SlopeRange("SlopeRange", Vector) = (0,1,0,0)
		_ShowPlacement("Show placement", Float) = 0
		_ObjectPlaces("Placement", 2D) = "white" {}
		_MainTex("MainTex", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

		Cull Off
		HLSLINCLUDE
		#pragma target 2.0
		ENDHLSL

		
		Pass
		{
			Name "Unlit"
			

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ ETC1_EXTERNAL_ALPHA

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define SHADERPASS_SPRITEUNLIT

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			

			sampler2D _MainTex;
			sampler2D _SlopeMap;
			sampler2D _ObjectPlaces;
			CBUFFER_START( UnityPerMaterial )
			float4 _MainTex_ST;
			float4 _SlopeMap_ST;
			float4 _ObjectPlaces_ST;
			float2 _HeightRange;
			float2 _SlopeRange;
			float _ShowPlacement;
			CBUFFER_END


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float4 color : TEXCOORD1;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D( _AlphaTex ); SAMPLER( sampler_AlphaTex );
				float _EnableAlphaTexture;
			#endif

			float4 _RendererColor;

			float FloatInRange10( float Value , float Min , float Max )
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
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3( 0, 0, 0 );
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.normal = v.normal;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( v.vertex.xyz );

				o.texCoord0 = v.uv0;
				o.color = v.color;
				o.clipPos = vertexInput.positionCS;

				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				float2 uv_MainTex = IN.texCoord0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 tex2DNode4 = tex2D( _MainTex, uv_MainTex );
				float4 color17 = IsGammaSpace() ? float4(0,0,0,0) : float4(0,0,0,0);
				float4 color15 = IsGammaSpace() ? float4(1,1,0,0.5882353) : float4(1,1,0,0.5882353);
				float Value10 = tex2DNode4.r;
				float Min10 = _HeightRange.x;
				float Max10 = _HeightRange.y;
				float localFloatInRange10 = FloatInRange10( Value10 , Min10 , Max10 );
				float2 uv_SlopeMap = IN.texCoord0.xy * _SlopeMap_ST.xy + _SlopeMap_ST.zw;
				float Value8 = tex2D( _SlopeMap, uv_SlopeMap ).r;
				float Min8 = _SlopeRange.x;
				float Max8 = _SlopeRange.y;
				float localFloatInRange8 = FloatInRange8( Value8 , Min8 , Max8 );
				float temp_output_13_0 = ( localFloatInRange10 * localFloatInRange8 );
				float4 lerpResult18 = lerp( color17 , color15 , temp_output_13_0);
				float4 ifLocalVar21 = 0;
				if( tex2DNode4.a <= 0.0 )
				ifLocalVar21 = color17;
				else
				ifLocalVar21 = lerpResult18;
				float2 uv_ObjectPlaces = IN.texCoord0.xy * _ObjectPlaces_ST.xy + _ObjectPlaces_ST.zw;
				float ifLocalVar12 = 0;
				if( tex2DNode4.a <= 0.0 )
				ifLocalVar12 = 0.0;
				else
				ifLocalVar12 = 1.0;
				float4 lerpResult19 = lerp( float4( 0,0,0,0 ) , tex2D( _ObjectPlaces, uv_ObjectPlaces ) , ( ifLocalVar12 * temp_output_13_0 ));
				float4 lerpResult23 = lerp( ifLocalVar21 , lerpResult19 , lerpResult19.a);
				float4 ifLocalVar24 = 0;
				if( _ShowPlacement <= 0.0 )
				ifLocalVar24 = ifLocalVar21;
				else
				ifLocalVar24 = lerpResult23;
				
				float4 Color = ifLocalVar24;

				#if ETC1_EXTERNAL_ALPHA
					float4 alpha = SAMPLE_TEXTURE2D( _AlphaTex, sampler_AlphaTex, IN.texCoord0.xy );
					Color.a = lerp( Color.a, alpha.r, _EnableAlphaTexture );
				#endif

				Color *= IN.color;

				return Color;
			}

			ENDHLSL
		}
	}
	CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=18301
130;101;1410;839;3329.839;769.7285;1.850106;True;True
Node;AmplifyShaderEditor.TexturePropertyNode;1;-2483.686,144.2267;Inherit;True;Property;_SlopeMap;Slopemap;0;0;Create;False;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TexturePropertyNode;25;-2483.484,-104.8659;Inherit;True;Property;_MainTex;MainTex;6;0;Create;False;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;5;-2262.117,138.8417;Inherit;True;Property;_TextureSample0;Texture Sample 0;2;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;4;-2261.69,-105.3998;Inherit;True;Property;_TextureSample1;Texture Sample 1;2;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;3;-2139.498,497.2677;Inherit;False;Property;_SlopeRange;SlopeRange;3;0;Create;False;0;0;False;0;False;0,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;6;-2147.358,369.9321;Inherit;False;Property;_HeightRange;HeightRange;1;0;Create;False;0;0;False;0;False;0,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.CustomExpressionNode;10;-1846.189,468.7837;Inherit;False;if (Value >= Min && Value <= Max)${$	return 1@$}$else${$	return 0@$};1;False;3;True;Value;FLOAT;0;In;;Inherit;False;True;Min;FLOAT;0;In;;Inherit;False;True;Max;FLOAT;0;In;;Inherit;False;FloatInRange;True;False;0;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;8;-1846.752,601.9258;Inherit;False;if (Value >= Min && Value <= Max)${$	return 1@$}$else${$	return 0@$};1;False;3;True;Value;FLOAT;0;In;;Inherit;False;True;Min;FLOAT;0;In;;Inherit;False;True;Max;FLOAT;0;In;;Inherit;False;FloatInRange;True;False;0;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;9;-1453.435,831.2379;Inherit;False;Constant;_Float1;Float 1;5;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-1450.435,756.2379;Inherit;False;Constant;_Float0;Float 0;5;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-1640.052,547.3257;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;11;-1336.794,477.4726;Inherit;True;Property;_ObjectPlaces;Placement;5;0;Create;False;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.ConditionalIfNode;12;-1296.247,705.5761;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;16;-1114.141,479.5683;Inherit;True;Property;_TextureSample2;Texture Sample 2;6;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-1104.433,831.7866;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;17;-1865.143,114.3529;Inherit;False;Constant;_Black;Black;4;0;Create;True;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;15;-1863.671,292.3643;Inherit;False;Constant;_Yellow;Yellow;4;0;Create;True;0;0;False;0;False;1,1,0,0.5882353;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;19;-745.2872,690.968;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;18;-1506.178,326.201;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;20;-586.3585,809.1682;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.ConditionalIfNode;21;-1358.172,79.76912;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;23;-327.8845,641.5032;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-388.1925,-49.13883;Inherit;False;Property;_ShowPlacement;Show placement;4;0;Create;False;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;24;-185.5715,-8.228004;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;0,0;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;12;MassSpawner/LayerURP;cf964e524c8e69742b1d21fbe2ebcc4a;True;Unlit;0;0;Unlit;3;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;0;0;True;2;5;False;-1;10;False;-1;3;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;1;Vertex Position;1;0;1;True;False;;0
WireConnection;5;0;1;0
WireConnection;4;0;25;0
WireConnection;10;0;4;1
WireConnection;10;1;6;1
WireConnection;10;2;6;2
WireConnection;8;0;5;1
WireConnection;8;1;3;1
WireConnection;8;2;3;2
WireConnection;13;0;10;0
WireConnection;13;1;8;0
WireConnection;12;0;4;4
WireConnection;12;2;7;0
WireConnection;12;3;9;0
WireConnection;12;4;9;0
WireConnection;16;0;11;0
WireConnection;14;0;12;0
WireConnection;14;1;13;0
WireConnection;19;1;16;0
WireConnection;19;2;14;0
WireConnection;18;0;17;0
WireConnection;18;1;15;0
WireConnection;18;2;13;0
WireConnection;20;0;19;0
WireConnection;21;0;4;4
WireConnection;21;2;18;0
WireConnection;21;3;17;0
WireConnection;21;4;17;0
WireConnection;23;0;21;0
WireConnection;23;1;19;0
WireConnection;23;2;20;3
WireConnection;24;0;22;0
WireConnection;24;2;23;0
WireConnection;24;3;21;0
WireConnection;24;4;21;0
WireConnection;0;1;24;0
ASEEND*/
//CHKSM=40C34B3C832BBD6CD95CB88D3D51CDC74BEEDF45