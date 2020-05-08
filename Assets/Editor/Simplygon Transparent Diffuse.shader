Shader "Simplygon/Transparent Diffuse" {

	Properties {
	  _Color ("Main Color", Color) = (1.0,1.0,1.0,1.0)	  	  
	  _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}	  
	}

	SubShader {
	    Tags { "Queue"="Transparent -1" "IgnoreProjector"="False" "RenderType"="Transparent" }
	    Blend SrcAlpha OneMinusSrcAlpha
		LOD 400

		CGPROGRAM		
		#pragma surface surf Simplygon addshadow

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
		  half4 c;
		  c.rgb = (s.Albedo * _LightColor0.rgb * diff) * (atten * 2);
		  c.a = s.Alpha;
		  return c;
		}
		
		struct Input {
		  float2 uv_MainTex;		  		  
		};

		sampler2D _MainTex;				
		fixed4 _Color;

		void surf (Input IN, inout MySurfaceOutput o)
		{
		  o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb * _Color;		  		  
		  o.Alpha = tex2D (_MainTex, IN.uv_MainTex).a * _Color.a;
		}

		ENDCG
	}

 	Fallback "Diffuse"
}
