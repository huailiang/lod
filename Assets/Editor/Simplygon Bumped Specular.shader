Shader "Simplygon/Bumped Specular" {

	Properties {
	  _Color ("Main Color", Color) = (1.0,1.0,1.0,1.0)
	  _SpecColor ("Spec color", Color) = (1.0,1.0,1.0,1.0)
	  _Shininess ("Shininess", Range(0.01, 1.0)) = 1.0
	  _MainTex ("Base (RGB)", 2D) = "white" {}
	  _SpecMap ("SpecMap", 2D) = "white" {}
	  _BumpMap ("BumpMap", 2D) = "white" {}
	}

	SubShader {
	    Tags { "RenderType"="Opaque" }
		LOD 400

		CGPROGRAM		
		#pragma surface surf Simplygon

		struct MySurfaceOutput {
		    half3 Albedo;
		    half3 Normal;
		    half3 Emission;
		    half Specular;
		    half3 GlossColor;
		    half Alpha;
		};

		inline half4 LightingSimplygon (MySurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
		{
		  half3 h = normalize (lightDir + viewDir);
		  half diff = max (0, dot (s.Normal, lightDir));
		  float nh = max (0, dot (s.Normal, h));
		  float spec = pow (nh, 128.0 * s.Specular);
		  half3 specCol = spec * s.GlossColor;

		  half4 c;
		  c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * specCol) * (atten * 2);
		  c.a = s.Alpha;
		  return c;
		}
		
		struct Input {
		  float2 uv_MainTex;
		  float2 uv_SpecMap;
		  float2 uv_BumpMap;
		};

		sampler2D _MainTex;
		sampler2D _SpecMap;
		sampler2D _BumpMap;
		half _Shininess;
		fixed4 _Color;

		void surf (Input IN, inout MySurfaceOutput o)
		{
		  o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb * _Color;
		  half4 spec = tex2D (_SpecMap, IN.uv_SpecMap);
		  o.GlossColor = spec.rgb * _SpecColor.rgb;
		  o.Specular = _Shininess;
		  o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));		  
		}

		ENDCG
	}

 	Fallback "Diffuse"
}
