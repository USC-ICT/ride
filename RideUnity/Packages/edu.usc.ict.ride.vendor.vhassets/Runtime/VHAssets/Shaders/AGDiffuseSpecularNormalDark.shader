Shader "AG/Diffuse Specular Normal Dark" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1)
        _SpecColor ("Spec Color", Color) = (1,1,1)
        _Shininess ("Shininess", Range(0.01, 1)) = 0.078125

        _MainTex ("Diffuse (RGB)", 2D) = "white" {}
        _SpecMap ("Specular Map (Grayscale)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _MultiplyMap ("Decal Map (Uses 2nd UV Set)", 2D) = "white" {}
    }

    SubShader {
        Tags {"Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque"}
        LOD 400

        CGPROGRAM
        #pragma surface surf BlinnPhongAG fullforwardshadows
        #pragma exclude_renderers flash

        //Copied from Unity 4's Lighting.cginc, since Unity 5's BlinnPhong lighing model interprets spec as roughness instead
        inline fixed4 LightingBlinnPhongAG(SurfaceOutput s, fixed3 lightDir, half3 viewDir, fixed atten)
        {
            half3 h = normalize(lightDir + viewDir);

            fixed diff = max(0, dot(s.Normal, lightDir));

            float nh = max(0, dot(s.Normal, h));
            float spec = pow(nh, s.Specular*128.0) * s.Gloss;

            fixed4 c;
            c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * _SpecColor.rgb * spec) * atten;
            c.a = s.Alpha + _LightColor0.a * _SpecColor.a * spec * atten;;

            return c;
        }

        sampler2D _MainTex;
        sampler2D _SpecMap;
        sampler2D _BumpMap;
        sampler2D _MultiplyMap;
        float4 _Color;
        float _Shininess;
        float _Height;

        struct Input {
            float2 uv_MainTex;
            float2 uv2_MultiplyMap;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            half4 tex = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            half3 spc = tex2D(_SpecMap, IN.uv_MainTex);

            o.Albedo = tex.rgb ;
            o.Albedo = tex.rgb * tex2D(_MultiplyMap, IN.uv2_MultiplyMap);
            o.Gloss = spc.rgb;
            o.Specular = _Shininess;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
        }
    ENDCG
    }

FallBack "Bumped Diffuse"
}
