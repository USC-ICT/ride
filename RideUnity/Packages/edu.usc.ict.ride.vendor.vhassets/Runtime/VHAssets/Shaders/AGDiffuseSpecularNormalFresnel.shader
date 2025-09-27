/*
Fresnel Update note
1/19/2016 - Joe
- Modified attenuation mix for point lights for proper attenuation calculation
- Added ambient term into fresnel effect
- Added self-shadowing pass to shaders

1/6/2016 - Joe
- Fixed flicker in fresnel
- Added custom lighting model copied from Unity 4, and added one line for modification to create fresnel in
  albedo that reacts to lights (needed internal terms that causes issues if used directly in surface function)
- Added custom surface output struct to include "Custom" attribute
- Removed if-block for directional vs. spot light direction/atttenuation calculation
- Modified specular vs. glossy calculation and range to closer match with Unity defaults

12/9/2015 - Joe
_LightColor0 only works properly for color and light intensity with directional lights. For spot and point lights
we have to calculate light attenuation and give it an offset (since it never goes down to 0), but still have no
light intensity information.
The oddity here is that a point light with no intensity will still give a slight fresnel effect, but should
be small enough to be not detectable. The alternative is to remove fresnel for point lights.

Joe Yip
yip@ict.usc.edu
*/
Shader "AG/Diffuse Specular Normal Fresnel" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1)
        _SpecColor ("Spec Color", Color) = (1,1,1)
        _Shininess ("Shininess", Range (0.01,1)) = 0.078125
        _FresnelColor ("Fresnel Color", Color) = (1,1,1)
        _FresnelStrength ("Fresnel Strength", Float) = 0.5
        _FresnelSpread ("Fresnel Spread", Float) = 0.5

        _MainTex ("Diffuse (RGB)", 2D) = "white" {}
        _SpecMap ("Specular Map (Grayscale)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _FresMap ("Fresnel Map (RGB)", 2D) = "white" {}
    }

    SubShader {
        Tags {"Queue"="Geometry" "IgnoreProjector"="False" "RenderType"="Opaque"}
        LOD 400

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf BlinnPhongAG fullforwardshadows
        #pragma exclude_renderers flash
        #include "UnityCG.cginc"

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
        sampler2D _SpecMap;
        sampler2D _BumpMap;
        sampler2D _FresMap;
        float3 _Color;
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

        struct Input {
            float4 color : COLOR;
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputAG o) {
            float4 tex = tex2D(_MainTex, IN.uv_MainTex);
            float4 spc = tex2D(_SpecMap, IN.uv_MainTex);
            float4 frn = tex2D(_FresMap, IN.uv_MainTex);
            float4 nrm = tex2D(_BumpMap, IN.uv_MainTex);

            o.Normal = UnpackNormal(nrm);
            o.Albedo = tex.rgb *_Color;
            o.Gloss = spc.rgb;
            o.Specular = _Shininess;

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
        }
    ENDCG
    }
FallBack "Bumped Diffuse"
}
