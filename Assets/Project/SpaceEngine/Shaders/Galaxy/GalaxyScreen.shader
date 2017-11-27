﻿Shader "SpaceEngine/Galaxy/Screen"
{
	Properties
	{
		
	}
	SubShader
	{
		Tags { "PreviewType" = "Plane" }

		Pass
		{
			Name "Dust&Gas"
			Tags 
			{
				"Queue"					= "Transparent"
				"RenderType"			= "Transparent"
				"ForceNoShadowCasting"	= "True"
				"IgnoreProjector"		= "True"

				"LightMode"				= "Always"
			}

			Blend SrcAlpha One
			Cull Off
			ZWrite Off

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform sampler2D _FrameBuffer;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};
			
			void vert(in appdata v, out v2f o)
			{
				o.vertex = float4(v.vertex.x, v.vertex.y, 1.0f, v.vertex.w);
				o.texcoord = v.texcoord.xy;

				#if UNITY_UV_STARTS_AT_TOP
					o.texcoord.y = 1.0f - o.texcoord.y;
				#endif
			}

			void frag(in v2f i, out float4 color : SV_Target)
			{
				float4 frameBuffer = tex2D(_FrameBuffer, i.texcoord);

				float3 outputColor = frameBuffer.rgb;
				float outputAlpha = 1.0f - frameBuffer.a / 16.0f;

				color.rgb = outputColor;
				color.a = outputAlpha;

				color = clamp(color, 0.0f, 65536.0f); // Avoid negative colors...
			}
			ENDCG
		}
	}
}
