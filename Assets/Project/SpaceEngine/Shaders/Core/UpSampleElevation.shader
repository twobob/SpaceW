﻿Shader "SpaceEngine/Terrain/UpSampleElevation" 
{
	SubShader 
	{
		Pass 
		{
			ZTest Always 
			Cull Off 
			ZWrite Off
			Fog { Mode Off }

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag
			
			#include "ImprovedPerlinNoise3D.cginc"
			
			// tile border size
			#define BORDER 2.0 
			
			//x - size in pixels of one tile (including borders), 
			//y - size in meters of a pixel of the elevation texture, 
			//z - (tileWidth - 2*BORDER) / grid mesh size for display, 
			uniform float4 _TileWSD;
			// coarse level texture
			uniform sampler2D _CoarseLevelSampler; 
			// lower left corner of patch to upsample, one over size in pixels of coarse level texture, layer id
			uniform float4 _CoarseLevelOSL; 
			
			uniform sampler2D _ResidualSampler;
			uniform float4 _ResidualOSH;
			
			uniform float _Octaves;
			uniform float _Amp;
			uniform float4 _Offset;
			uniform float4x4 _LocalToWorld;
			
			static float4x4 slopexMatrix[4] = {
			{0.0, 0.0, 0.0, 0.0,
			 1.0, 0.0, -1.0, 0.0,
			 0.0, 0.0, 0.0, 0.0,
			 0.0, 0.0, 0.0, 0.0},
			{0.0, 0.0, 0.0, 0.0,
			 0.5, 0.5, -0.5, -0.5,
			 0.0, 0.0, 0.0, 0.0,
			 0.0, 0.0, 0.0, 0.0},
			{0.0, 0.0, 0.0, 0.0,
			 0.5, 0.0, -0.5, 0.0,
			 0.5, 0.0, -0.5, 0.0,
			 0.0, 0.0, 0.0, 0.0},
			{0.0, 0.0, 0.0, 0.0,
			 0.25, 0.25, -0.25, -0.25,
			 0.25, 0.25, -0.25, -0.25,
			 0.0, 0.0, 0.0, 0.0}};
			 
			static float4x4 slopeyMatrix[4] = {
			{0.0, 1.0, 0.0, 0.0,
			 0.0, 0.0, 0.0, 0.0,
			 0.0, -1.0, 0.0, 0.0,
			 0.0, 0.0, 0.0, 0.0},
			{0.0, 0.5, 0.5, 0.0,
			 0.0, 0.0, 0.0, 0.0,
			 0.0, -0.5, -0.5, 0.0,
			 0.0, 0.0, 0.0, 0.0},
			{0.0, 0.5, 0.0, 0.0,
			 0.0, 0.5, 0.0, 0.0,
			 0.0, -0.5, 0.0, 0.0,
			 0.0, -0.5, 0.0, 0.0},
			{0.0, 0.25, 0.25, 0.0,
			 0.0, 0.25, 0.25, 0.0,
			 0.0, -0.25, -0.25, 0.0,
			 0.0, -0.25, -0.25, 0.0}};
			 
			static float4x4 curvatureMatrix[4] = {
			{0.0, -1.0, 0.0, 0.0,
			 -1.0, 4.0, -1.0, 0.0,
			 0.0, -1.0, 0.0, 0.0,
			 0.0, 0.0, 0.0, 0.0},
			{0.0, -0.5, -0.5, 0.0,
			 -0.5, 1.5, 1.5, -0.5,
			 0.0, -0.5, -0.5, 0.0,
			 0.0, 0.0, 0.0, 0.0},
			{0.0, -0.5, 0.0, 0.0,
			 -0.5, 1.5, -0.5, 0.0,
			 -0.5, 1.5, -0.5, 0.0,
			 0.0, -0.5, 0.0, 0.0},
			{0.0, -0.25, -0.25, 0.0,
			 -0.25, 0.5, 0.5, -0.25,
			 -0.25, 0.5, 0.5, -0.25,
			 0.0, -0.25, -0.25, 0.0}};
			 
			static float4x4 upsampleMatrix[4] = {
			{0.0, 0.0, 0.0, 0.0,
			 0.0, 1.0, 0.0, 0.0,
			 0.0, 0.0, 0.0, 0.0,
			 0.0, 0.0, 0.0, 0.0},
			{0.0, 0.0, 0.0, 0.0,
			 -1.0/16.0, 9.0/16.0, 9.0/16.0, -1.0/16.0,
			 0.0, 0.0, 0.0, 0.0,
			 0.0, 0.0, 0.0, 0.0},
			{0.0, -1.0/16.0, 0.0, 0.0,
			 0.0, 9.0/16.0, 0.0, 0.0,
			 0.0, 9.0/16.0, 0.0, 0.0,
			 0.0, -1.0/16.0, 0.0, 0.0},
			{1.0/256.0, -9.0/256.0, -9.0/256.0, 1.0/256.0,
			 -9.0/256.0, 81.0/256.0, 81.0/256.0, -9.0/256.0,
			 -9.0/256.0, 81.0/256.0, 81.0/256.0, -9.0/256.0,
			 1.0/256.0, -9.0/256.0, -9.0/256.0, 1.0/256.0}};
			 
			struct v2f 
			{
				float4  pos : SV_POSITION;
				float2  uv : TEXCOORD0;
				float2  st : TEXCOORD1;
			};

			v2f vert(appdata_base v)
			{
				v2f OUT;

				OUT.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				OUT.uv = v.texcoord.xy;
				OUT.st = v.texcoord.xy * _TileWSD.x;

				return OUT;
			}
			
			float mdot(float4x4 a, float4x4 b) 
			{
				return dot(a[0], b[0]) + dot(a[1], b[1]) + dot(a[2], b[2]) + dot(a[3], b[3]);
			}
			
			float4 frag(v2f IN) : COLOR
			{
				float2 p_uv = floor(IN.st) * 0.5;
				float2 uv = (p_uv - frac(p_uv) + float2(0.5, 0.5)) * _CoarseLevelOSL.z + _CoarseLevelOSL.xy;
				
				float2 residual_uv = p_uv * _ResidualOSH.z + _ResidualOSH.xy;
				float zf = _ResidualOSH.w * tex2Dlod(_ResidualSampler, float4(residual_uv, 0, 0)).x;
				
				float4x4 cz = 
				{
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(0.0, 0.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(1.0, 0.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(2.0, 0.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(3.0, 0.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(0.0, 1.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(1.0, 1.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(2.0, 1.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(3.0, 1.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(0.0, 2.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(1.0, 2.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(2.0, 2.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(3.0, 2.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(0.0, 3.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(1.0, 3.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(2.0, 3.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x,
					tex2Dlod(_CoarseLevelSampler, float4(uv + float2(3.0, 3.0) *  _CoarseLevelOSL.z, 0.0, 0.0)).x
				};
				
				int i = int(dot(frac(p_uv), float2(2.0, 4.0)));
				float3 n = float3(mdot(cz, slopexMatrix[i]), mdot(cz, slopeyMatrix[i]), 2.0 * _TileWSD.y);
				float slope = length(n.xy) / n.z;
				float curvature = mdot(cz, curvatureMatrix[i]) / _TileWSD.y;
				float noiseAmp = max(clamp(4.0 * curvature, 0.0, 1.5), clamp(2.0 * slope - 0.5, 0.1, 4.0));
				
				float u = (0.5+BORDER) / (_TileWSD.x-1-BORDER*2);
				float2 vert = IN.uv * (1.0+u*2.0) - u;
				vert = vert * _Offset.z + _Offset.xy;
				
				float3 P = float3(vert, _Offset.w);
				float3x3 LTW = _LocalToWorld;
				float3 p = normalize(mul(LTW, P)).xyz;
				
				float noise = inoise(p * _Frequency);
				
				if (_Amp < 0.0) 
				{
					zf -= _Amp * noise;
				}
				else 
				{
					zf += noiseAmp * _Amp * noise;
				}
				
				float zc = zf;

				if (_CoarseLevelOSL.x != -1.0) 
				{
					zf = zf + mdot(cz, upsampleMatrix[i]);

					float2 ij = floor(IN.st - float2(BORDER, BORDER));
					float4 uvc = float4(BORDER + 0.5, BORDER + 0.5, BORDER + 0.5, BORDER + 0.5);

					uvc += _TileWSD.z * floor((ij / (2.0 * _TileWSD.z)).xyxy + float4(0.5, 0.0, 0.0, 0.5));
					
					float zc1 = tex2Dlod(_CoarseLevelSampler, float4(uvc.xy * _CoarseLevelOSL.z + _CoarseLevelOSL.xy, 0.0, 0.0)).x;
					float zc3 = tex2Dlod(_CoarseLevelSampler, float4(uvc.zw * _CoarseLevelOSL.z + _CoarseLevelOSL.xy, 0.0, 0.0)).x;
					
					zc = (zc1 + zc3) * 0.5;
				}
				
				return float4(zf, zc, 0.0, 0.0);
				
			}
			
			ENDCG
		}
	}
}