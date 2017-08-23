﻿// Procedural planet generator.
// 
// Copyright (C) 2015-2017 Denis Ovchinnikov [zameran] 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. Neither the name of the copyright holders nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION)HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
// 
// Creation Date: Undefined
// Creation Time: Undefined
// Creator: zameran

Shader "SpaceEngine/Planet/Terrain"
{
	Properties
	{
		[NoScaleOffset] _Elevation_Tile("Elevation", 2D) = "white" {}
		[NoScaleOffset] _Normals_Tile("Normals", 2D) = "white" {}
		[NoScaleOffset] _Color_Tile("Color", 2D) = "white" {}
		[NoScaleOffset] _Ortho_Tile("Ortho", 2D) = "white" {}

		[NoScaleOffset] _Ground_Diffuse("Ground Diffuse", 2D) = "white" {}
		[NoScaleOffset] _Ground_Normal("Ground Normal", 2D) = "white" {}
	}
	SubShader 
	{
		CGINCLUDE
		#include "Core.cginc"

		uniform float _Ocean_Sigma;
		uniform float3 _Ocean_Color;
		uniform float _Ocean_DrawBRDF;
		uniform float _Ocean_Level;

		struct a2v
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 texcoord : TEXCOORD0;
		};

		struct v2f 
		{
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
			float3 localVertex : TEXCOORD1;
			float3 direction : TEXCOORD2;
		};

		void VERTEX_POSITION(in float4 vertex, in float2 texcoord, out float4 position, out float3 localPosition, out float2 uv)
		{
			float2 zfc = texTileLod(_Elevation_Tile, texcoord, _Elevation_TileCoords, _Elevation_TileSize).xy;

			#if ATMOSPHERE_ON
				#if OCEAN_ON
					if (zfc.x <= _Ocean_Level && _Ocean_DrawBRDF == 1.0) { zfc = float2(0, 0); }
				#endif
			#endif
			
			float4 vertexUV = float4(vertex.xy, float2(1.0, 1.0) - vertex.xy);
			float2 vertexToCamera = abs(_Deform_Camera.xy - vertex.xy);
			float vertexDistance = max(max(vertexToCamera.x, vertexToCamera.y), _Deform_Camera.z);
			float vertexBlend = clamp((vertexDistance - _Deform_Blending.x) / _Deform_Blending.y, 0.0, 1.0);
				
			float4 alpha = vertexUV.zxzx * vertexUV.wwyy;
			float4 alphaPrime = alpha * _Deform_ScreenQuadCornerNorms / dot(alpha, _Deform_ScreenQuadCornerNorms);

			float3 P = float3(vertex.xy * _Deform_Offset.z + _Deform_Offset.xy, _Deform_Radius);
				
			float h = zfc.x * (1.0 - vertexBlend) + zfc.y * vertexBlend;
			float k = min(length(P) / dot(alpha, _Deform_ScreenQuadCornerNorms) * 1.0000003, 1.0);
			float hPrime = (h + _Deform_Radius * (1.0 - k)) / k;

			//position = mul(_Deform_LocalToScreen, float4(P + float3(0.0, 0.0, h), 1.0));							//CUBE PROJECTION
			position = mul(_Deform_ScreenQuadCorners + hPrime * _Deform_ScreenQuadVerticals, alphaPrime);			//SPHERICAL PROJECTION
			localPosition = (_Deform_Radius + max(h, _Ocean_Level)) * normalize(mul(_Deform_LocalToWorld, P));
			uv = texcoord;
		}

		void VERTEX_PROGRAM(in a2v v, out v2f o)
		{
			VERTEX_POSITION(v.vertex, v.texcoord.xy, o.vertex, o.localVertex, o.texcoord);

			o.direction = 0;

			v.vertex = o.vertex; // Assign calculated vertex position to our data...
		}
		ENDCG

		Pass
		{
			Tags { "LightMode" = "ShadowCaster" }
 
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			#pragma multi_compile ATMOSPHERE_ON ATMOSPHERE_OFF
			#pragma multi_compile SHINE_ON SHINE_OFF
			#pragma multi_compile ECLIPSES_ON ECLIPSES_OFF
			#pragma multi_compile OCEAN_ON OCEAN_OFF
			#pragma multi_compile SHADOW_0 SHADOW_1 SHADOW_2 SHADOW_3 SHADOW_4

			#include "UnityStandardShadow.cginc"

			#pragma multi_compile_shadowcaster
 
			struct v2f_shadowCaster
			{
				V2F_SHADOW_CASTER;
			};
 
			v2f_shadowCaster vert(VertexInput v)
			{
				v2f_shadowCaster o;

				//-----------------------------------------------------------------------------
				float4 outputVertex = 0;
				float3 outputLocalVertex = 0;
				float2 outputTexcoord = 0;

				VERTEX_POSITION(v.vertex, v.uv0.xy, outputVertex, outputLocalVertex, outputTexcoord);

				v.vertex = float4(outputLocalVertex, 1.0);
				//-----------------------------------------------------------------------------

				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)

				return o;
			}
 
			float4 frag(v2f_shadowCaster i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}

		Pass 
		{
			Name "Planet"
			Tags 
			{
				"Queue"					= "Geometry"	// "Opaque"
				"RenderType"			= "Geometry"
				"ForceNoShadowCasting"	= "True"
				"IgnoreProjector"		= "True"

				"LightMode"				= "ForwardBase"		// "Deferred" 
			}

			Cull Back
			ZWrite On
			ZTest On
			Fog { Mode Off }

			CGPROGRAM
			#pragma target 4.0
			#pragma only_renderers d3d11 glcore
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile ATMOSPHERE_ON ATMOSPHERE_OFF
			#pragma multi_compile SHINE_ON SHINE_OFF
			#pragma multi_compile ECLIPSES_ON ECLIPSES_OFF
			#pragma multi_compile OCEAN_ON OCEAN_OFF
			#pragma multi_compile SHADOW_0 SHADOW_1 SHADOW_2 SHADOW_3 SHADOW_4
			
			#include "SpaceStuff.cginc"
			#include "SpaceEclipses.cginc"
			#include "SpaceAtmosphere.cginc"
			#include "Ocean/OceanBRDF.cginc"
			
			void vert(in a2v v, out v2f o)
			{	
				VERTEX_PROGRAM(v, o);

				//o.direction = ((_Atmosphere_WorldCameraPos + _Atmosphere_Origin) - mul(_Globals_CameraToWorld, o.vertex)).xyz;
				o.direction = (_Atmosphere_WorldCameraPos + _Atmosphere_Origin) - (mul(_Globals_CameraToWorld, float4((mul(_Globals_ScreenToCamera, v.vertex)).xyz, 0.0))).xyz;
			}

			void frag(in v2f i, out half4 outDiffuse : SV_Target)
			{
				float3 WCP = _Globals_WorldCameraPos;
				float3 WCPO = _Atmosphere_WorldCameraPos;
				float3 WSD = _Sun_WorldDirections_1[0];
				float4 WSPR = _Sun_Positions_1[0];
				float3 position = i.localVertex;
				float2 texcoord = i.texcoord;

				float height = texTile(_Elevation_Tile, texcoord, _Elevation_TileCoords, _Elevation_TileSize).x;
				float4 ortho = texTile(_Ortho_Tile, texcoord, _Ortho_TileCoords, _Ortho_TileSize);
				float4 color = texTile(_Color_Tile, texcoord, _Color_TileCoords, _Color_TileSize);
				float4 normal = texTile(_Normals_Tile, texcoord, _Normals_TileCoords, _Normals_TileSize);

				normal.xyz = DecodeNormal(normal.xyz);

				float3 V = normalize(position);
				float3 P = V * max(length(position), _Deform_Radius + 10.0); // NOTE : BigToSmall
				float3 PO = P - _Atmosphere_Origin;
				float3 v = normalize(P - WCP - _Atmosphere_Origin); // Body origin take in to account...
				float3 d = normalize(i.direction);

				#if ATMOSPHERE_ON
					#if OCEAN_ON
						if (height <= _Ocean_Level && _Ocean_DrawBRDF == 1.0) {	normal = float4(0.0, 0.0, 1.0, 0.0); }
					#endif
				#endif
				
				normal.xyz = mul(_Deform_TangentFrameToWorld, normal.xyz);

				float4 reflectance = lerp(ortho, color, clamp(length(color.xyz), 0.0, 1.0)); // Just for tests...

				float cTheta = dot(normal.xyz, WSD);

				#ifdef ECLIPSES_ON
					float eclipse = 1;

					float3 invertedLightDistance = rsqrt(dot(WSPR.xyz, WSPR.xyz));
					float3 lightPosition = WSPR.xyz * invertedLightDistance;

					float lightAngularRadius = asin(WSPR.w * invertedLightDistance);

					eclipse *= EclipseShadow(P, lightPosition, lightAngularRadius);
				#endif

				#if SHADOW_1 || SHADOW_2 || SHADOW_3 || SHADOW_4
					float shadow = ShadowColor(float4(PO, 1));	// Body origin take in to account...
				#endif
				
				#if ATMOSPHERE_ON
					float3 sunL = 0.0;
					float3 skyE = 0.0;
					SunRadianceAndSkyIrradiance(P, normal.xyz, WSD, sunL, skyE);

					float3 groundColor = 1.5 * RGB2Reflectance(reflectance).rgb * (sunL * max(cTheta, 0) + skyE) / M_PI;
					
					#if OCEAN_ON
						if (height <= _Ocean_Level && _Ocean_DrawBRDF == 1.0)
						{	
							groundColor = OceanRadiance(WSD, -v, V, _Ocean_Sigma, sunL, skyE, _Ocean_Color, P);
						}
					#endif
					
					float darknessAccumulation = 1.0;
					float3 extinction;
					float3 inscatter = InScattering(WCPO, P, WSD, extinction, 0.0);

					#ifdef ECLIPSES_ON
						inscatter *= eclipse;
					#endif

					#if SHADOW_1 || SHADOW_2 || SHADOW_3 || SHADOW_4
						inscatter *= shadow;
					#endif

					#ifdef SHINE_ON
						inscatter += SkyShineRadiance(P, d);
					#endif

					#ifdef ECLIPSES_ON
						#if SHADOW_1 || SHADOW_2 || SHADOW_3 || SHADOW_4
							darknessAccumulation = eclipse * shadow;
						#else
							darknessAccumulation = eclipse;
						#endif
					#else
						#if SHADOW_1 || SHADOW_2 || SHADOW_3 || SHADOW_4
							darknessAccumulation = shadow;
						#endif
					#endif

					extinction = GroundFade(_ExtinctionGroundFade, extinction, darknessAccumulation);

					float3 finalColor = hdr(groundColor * extinction + inscatter);
				#elif ATMOSPHERE_OFF
					float3 finalColor = 1.5 * reflectance * max(cTheta, 0);
				#endif

				outDiffuse = float4(finalColor, 1.0);
			}
			
			ENDCG
		}
	}
}