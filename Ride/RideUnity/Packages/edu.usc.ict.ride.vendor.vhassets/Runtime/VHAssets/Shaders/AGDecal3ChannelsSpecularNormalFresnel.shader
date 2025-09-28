Shader "AG/Decal/Decal 3 Channels Specular Normal Fresnel (2 UV sets)" {

    Properties {
        _Color ("Main Color", Color) = (1, 1, 1, 1)
        _SpecColor ("Spec Color", Color) = (1,1,1,1)
        _Shininess ("Shininess", Range (0.01,1)) = 0.078125
        _FresnelColor ("Fresnel Color", Color) = (1,1,1)
        _FresnelStrength ("Fresnel Strength", Float) = 0.5
        _FresnelSpread ("Fresnel Spread", Float) = 0.5

        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "grey" {}
        _SpecMap ("Specular (Grayscale)", 2D) = "white" {}
        _FresMap ("Fresnel Map (RGB)", 2D) = "white" {}

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

        struct SurfaceOutputAG {
            fixed3 Albedo;
            fixed3 Normal;
            fixed3 Emission;
            half Specular;
            fixed Gloss;
            fixed Alpha;
            half4 Custom;
        };

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _SpecMap;
        sampler2D _FresMap;

        sampler2D _Decal01Tex;
        sampler2D _Decal01Bump;
        sampler2D _Decal01SpecMap;

        sampler2D _Decal02Tex;
        sampler2D _Decal02Bump;
        sampler2D _Decal02SpecMap;

        float4 _Color;
        float _Shininess;
        float4 _FresnelColor;
        float _FresnelStrength;
        float _FresnelSpread;

        /*
        Using a custom lighting model in order to access [stable] lighting intensity for fresnel effect;
        UNITY_LIGHTMODEL_AMBIENT & _LightColor0.rgb are unstable in surface function and cause flickers.

        Lighting model copied directly from Unity 4's Lighting.cginc with the following modifications:
        - Removed atten multiplier in c.rgb calculation (was attenx2) so it behaves like regular diffuse
        - Added line 01
        */
        // NOTE: some intricacy in shader compiler on some GLES2.0 platforms (iOS) needs 'viewDir' & 'h'
        // to be mediump instead of lowp, otherwise specular highlight becomes too bright.
        inline fixed4 LightingBlinnPhongAG(SurfaceOutputAG s, fixed3 lightDir, half3 viewDir, fixed atten)
        {
            half3 h = normalize(lightDir + viewDir);

            fixed diff = max(0, dot(s.Normal, lightDir));

            float nh = max(0, dot(s.Normal, h));
            float spec = pow(nh, s.Specular*128.0) * s.Gloss;

            fixed4 c;
            c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * _SpecColor.rgb * spec) * atten;
            c.a = s.Alpha + _LightColor0.a * _SpecColor.a * spec * atten;;

            //Added line 01
            //Push custom struct attribute calculated in surf to create fresnel in albedo, creating fresnel effect
            //Mix of directional, point, and ambient
            float attenMix = (1 - _WorldSpaceLightPos0.w) + (atten * _WorldSpaceLightPos0.w) + UNITY_LIGHTMODEL_AMBIENT.rgb;
            c.rgb += s.Custom * (UNITY_LIGHTMODEL_AMBIENT.rgb + _LightColor0.rgb) * attenMix;

            return c;
        }

        struct v2f {
            V2F_SHADOW_CASTER;
        };

        struct Input {
            float2 uv_MainTex : TEXCOORD0;
            float2 uv2_Decal01Tex : TEXCOORD1;
            float3 viewDir;
            float3 worldPos;
        };

        v2f vert (inout appdata_full v) {
            v2f o;
            return o;
        }

        void surf (Input IN, inout SurfaceOutputAG o) {
            half4 tex = tex2D(_MainTex, IN.uv_MainTex);
            half4 decal01Tex = tex2D(_Decal01Tex, IN.uv2_Decal01Tex);
            half4 decal02Tex = tex2D(_Decal02Tex, IN.uv2_Decal01Tex);
            half4 spc = tex2D(_SpecMap, IN.uv_MainTex);
            half4 decal01Spc = tex2D(_Decal02SpecMap, IN.uv2_Decal01Tex);
            half4 decal02Spc = tex2D(_Decal02SpecMap, IN.uv2_Decal01Tex);
            half4 nrm = tex2D(_BumpMap, IN.uv_MainTex);
            half4 decal01Nrm = tex2D(_Decal01Bump, IN.uv2_Decal01Tex);
            half4 decal02Nrm = tex2D(_Decal02Bump, IN.uv2_Decal01Tex);
            half3 frn = tex2D(_FresMap, IN.uv_MainTex);

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
            o.Specular = _Shininess;
            o.Normal = UnpackNormal(normalLayer02);

            //Fresnel
            //Light direction and attenuation
            float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - (IN.worldPos.xyz * _WorldSpaceLightPos0.w); //_WorldSpaceLightPos0.w = 0 = Directional
            float one_over_distance = 1.0 / length(vertexToLightSource); //linear attenuation
            float attenuation = lerp(1.0, one_over_distance, _WorldSpaceLightPos0.w); //Switch for dir vs spot lights
            //float3 lightDirection = vertexToLightSource * one_over_distance;

            half fresnelFactor = clamp(dot(normalize(IN.viewDir), UnpackNormal(nrm)), 0.1, 1); //The clamping gets rid of rendering artifacts when we approach the extreme viewDirs; Use lightDirection?
            float3 fresnelOutput = frn.rgb * _FresnelColor.rgb * (_FresnelStrength - fresnelFactor * _FresnelStrength);
            float3 fresnelOutput2 = pow(fresnelOutput, (_FresnelSpread * fresnelFactor)) * _FresnelStrength;
            //float3 ambientFresnelOutput = fresnelOutput2 * UNITY_LIGHTMODEL_AMBIENT.rgb;

            //For use in custom lighting model to calculate fresnel that reacts to light
            o.Custom.rgb = fresnelOutput2* (1 - _WorldSpaceLightPos0.w) + (fresnelOutput2 * attenuation) * _WorldSpaceLightPos0.w; //Originally had -0.2 to tune out fresnel when point light is far away enough


            //Fresnel
            //The clamping gets rid of rendering artifacts when we approach the extreme viewDirs
            //half fresnelFactor = clamp(dot(normalize(IN.viewDir), o.Normal), 0.1, 1);
            //o.Emission.rgb = frn.rgb * _FresnelColor.rgb * (_FresnelStrength - fresnelFactor * _FresnelStrength);
            //o.Emission.rgb = pow(o.Emission.rgb, (_FresnelSpread * fresnelFactor)) * _FresnelStrength;
        }
    ENDCG

    }

    Fallback "Bumped Diffuse"

}
