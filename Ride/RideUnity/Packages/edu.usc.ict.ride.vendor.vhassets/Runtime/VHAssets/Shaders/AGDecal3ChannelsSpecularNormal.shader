Shader "AG/Decal/Decal 3 Channels Specular Normal (2 UV sets)" {

Properties {
    _Color ("Main Color", Color) = (1, 1, 1, 1)
    _SpecColor ("Spec Color", Color) = (1,1,1,1)
    _Shininess ("Shininess", Range (0.01,1)) = 0.078125

    _MainTex ("Base (RGB)", 2D) = "white" {}
    _BumpMap ("Normal Map", 2D) = "grey" {}
    _SpecMap ("Specular (Grayscale)", 2D) = "white" {}

    _Decal01Tex ("Decal01 (UV2) (RGBA)", 2D) = "black" {}
    _Decal01Bump ("Decal01 Normal Map", 2D) = "grey" {}
    _Decal01SpecMap ("Decal01 Specular (Grayscale)", 2D) = "white" {}

    _Decal02Tex ("Decal02 (UV2) (RGBA)", 2D) = "black" {}
    _Decal02Bump ("Decal02 Normal Map", 2D) = "grey" {}
    _Decal02SpecMap ("Decal02 Specular (Grayscale)", 2D) = "white" {}

}

SubShader {
    Tags {
    "Queue"="Geometry"
    "IgnoreProjector"="False"
    "RenderType"="Opaque"
    }

    LOD 400

    CGPROGRAM
        #pragma surface surf BlinnPhongAG fullforwardshadows
        #pragma exclude_renderers flash
        //Compile to Pixel Shader 3.0 for OpenGL machines' temporary register limit of 16
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _SpecMap;

        sampler2D _Decal01Tex;
        sampler2D _Decal01Bump;
        sampler2D _Decal01SpecMap;

        sampler2D _Decal02Tex;
        sampler2D _Decal02Bump;
        sampler2D _Decal02SpecMap;

        float4 _Color;
        float _Shininess;

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

        struct v2f {
            V2F_SHADOW_CASTER;
        };

        struct Input {
            float2 uv_MainTex : TEXCOORD0;
            float2 uv2_Decal01Tex : TEXCOORD1;
        };

        v2f vert (inout appdata_full v) {
            v2f o;
            return o;
        }

        void surf (Input IN, inout SurfaceOutput o) {
            half4 tex = tex2D(_MainTex, IN.uv_MainTex);
            half4 decal01Tex = tex2D(_Decal01Tex, IN.uv2_Decal01Tex);
            half4 decal02Tex = tex2D(_Decal02Tex, IN.uv2_Decal01Tex);
            half4 spc = tex2D(_SpecMap, IN.uv_MainTex);
            half4 decal01Spc = tex2D(_Decal02SpecMap, IN.uv2_Decal01Tex);
            half4 decal02Spc = tex2D(_Decal02SpecMap, IN.uv2_Decal01Tex);
            half4 nrm = tex2D(_BumpMap, IN.uv_MainTex);
            half4 decal01Nrm = tex2D(_Decal01Bump, IN.uv2_Decal01Tex);
            half4 decal02Nrm = tex2D(_Decal02Bump, IN.uv2_Decal01Tex);

            //Combine one layer at a time, and treat each layer as a 'base' after combination.
            //Albedo
            float3 albedoLayer01 = lerp(tex, decal01Tex, decal01Tex.a).rgb;
            float3 albedo = lerp(albedoLayer01, decal02Tex, decal02Tex.a).rgb;
            //Normal
            float4 normalLayer01 = lerp(nrm, decal01Nrm, decal01Tex.a);
            float4 normalLayer02 = lerp(normalLayer01, decal02Nrm, decal02Tex.a);
            //Specular
            float3 specularLayer01 = lerp(spc, decal01Spc, decal01Tex.a).rgb;
            float3 specularLayer02 = lerp(specularLayer01, decal02Spc, decal02Tex.a).rgb;

            //Output
            o.Albedo = albedo;
            o.Gloss = specularLayer02.rgb;
            o.Specular = specularLayer02.rgb * _Shininess;
            o.Normal = UnpackNormal(normalLayer02);

        }
    ENDCG

    }

    Fallback "Bumped Diffuse"

}
