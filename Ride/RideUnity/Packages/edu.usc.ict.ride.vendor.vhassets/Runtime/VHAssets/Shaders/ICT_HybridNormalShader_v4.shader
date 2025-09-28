Shader "VH/ICT_HyrbridNormalShader_v4" {

Properties {
    _DiffuseIntensity ("Diffuse Intensity", Range (0,1)) = 1.0 // sliders
    _SpecularIntensity ("Specular Intensity", Range (0,1)) = 1.0 // sliders
    _SpecularExponent1 ("Specular Exponent 1", Range (0,10)) = 4.0 // sliders
    _SpecularExponent2 ("Specular Exponent 2", Range (0,10)) = 8.0 // sliders
    _SpecularSplit ("Specular Exponent Split", Range (0,1)) = 0.25 // sliders
    _FresnelExponent ("Fresnel Exponent", Range (0,5)) = 5.0 // sliders
    _AsperityIntensity ("Asperity Intensity", Range (0,1)) = 1.0 // sliders
    _AsperityExponent ("Asperity Exponent", Range (1,8)) = 4.0 // sliders
    _SSSColor ("SSS Color", Color) = (0,0,0,1)
    _Albedo ("Albedo", 2D) = "white" {} // [NOTE] pack spec into alpha
    _NormalD ("Normal D", 2D) = "bump" {}
    _NormalS ("Normal S", 2D) = "bump" {}
}

SubShader {
Tags { "RenderType" = "Opaque" }

CGPROGRAM
    // Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
    #pragma exclude_renderers gles
    #pragma surface surf CustomLighting
    #pragma target 3.0
    //#pragma debug
    #include "UnityCG.cginc"

    //struct SurfaceOutput
    //{
    //    half3 Albedo;
    //    half3 Normal;
    //    half3 Emission;
    //    half Specular;
    //    half Gloss;
    //    half Alpha;
    //};

    sampler2D _Albedo;
    sampler2D _NormalD;
    sampler2D _NormalS;
    sampler2D _Albedo1;
    uniform float _DiffuseIntensity;
    uniform float _SpecularIntensity;
    uniform float _SpecularExponent1;
    uniform float _SpecularExponent2;
    uniform float _SpecularSplit;
    uniform float _FresnelExponent;
    uniform float _AsperityIntensity;
    uniform float _AsperityExponent;
    uniform float3 _SSSColor;
    float3 NormalRVec;
    float3 NormalGVec;
    float3 NormalBVec;
    float3 NormalSVec;

    struct Input
    {
        // because all the textures use the same scale and offset's we only need one
        // set of texture coordinates
        float2 uv_Albedo;
    };

    //! Diffuse reflection using per-color-channel diffuse normal vectors.
    /*!
      Diffuse reflection for light direction L and normal direction
      specifed for each of red, green, and blue channels.
    */
    float3 diffuseReflection(float3 normal_R, float3 normal_G, float3 normal_B, float3 L, half3 V, float3 albedoD)
    {
        // [NOTE] omitting fresnel transmission term to help approximate SSS and make up for missing specular environment map
        float3 dots = float3(dot(normal_R, L), dot(normal_G, L), dot(normal_B, L));
        return pow(saturate((1.0f - dots) * _SSSColor + dots), _SSSColor * 4.0f + 1.0f) * albedoD;  // [NOTE] spherical cosine lobe helps approximate SSS
    }

    //! Specular reflection using specular normal vector.
    /*!
      Blinn-Phong with energy-preserving modification. specularExponent is one
      of the material parameters set before export.
    */
    float3 specularReflection(float3 N, float3 L, half3 V, float3 albedoS, float exponent)
    {
        // Adjust specular exponent for normal variation [REF] Toksvig
        float l = length(N);    // [NOTE] You can put normals with length < 1 for spatially varying gloss
        N /= l;
        l = min(1, l);  // [NOTE] This is here to avoid artifacts from anomalies in the texture maps
        float n = exp2(exponent);
        n = (n * l) / (l + n * (1 - l));
        float3 H = normalize(L + V);
        // Asperity lobe
        float asperity = 1.125f * max(0.0f, dot(N, H));
        float asperityStrength = pow(min(1, 1 - dot(N, V)), _AsperityExponent) * _AsperityIntensity;
        // Blinn-Phong lobe
        //float normalization = (n + 2.0f) * (n + 4.0f) * 0.125f / (n + pow(0.5f, n * 0.5f));
        float normalization = n * 0.125f + 1.0f; // [NOTE] while the above normalization is correct, this one is almost the same
        float fresnel = pow(min(1, 1 - dot(N, V)), _FresnelExponent) * 0.92f + 0.08f;
        float blinnPhong = normalization * pow(max(0.0f, dot(N, H)), n);
        float blinnPhongStrength = min(fresnel * _SpecularIntensity, 1 - asperityStrength);
        return (blinnPhong * blinnPhongStrength + asperity * asperityStrength) * max(0.0f, dot(N, L)) * albedoS;
    }

    half4 LightingCustomLighting (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
    {
        float3 diffuseColor = diffuseReflection(NormalRVec, NormalGVec, NormalBVec, lightDir, viewDir, s.Albedo * _DiffuseIntensity);
        float3 specularColor = specularReflection(NormalSVec, lightDir, viewDir, s.Specular * (1.0f - _SpecularSplit), _SpecularExponent1)
                                + specularReflection(NormalSVec, lightDir, viewDir, s.Specular * _SpecularSplit, _SpecularExponent2);
        return half4((diffuseColor + specularColor) * _LightColor0.rgb * atten * 2.0f, 1);  // [NOTE] atten * 2 is the unity standard definition for light intensity attenuation
    }

    // main function
    void surf (Input IN, inout SurfaceOutput Out)
    {
        // specular normals
        NormalSVec = UnpackNormal(tex2D(_NormalS, IN.uv_Albedo));
        Out.Normal = NormalSVec;    // [NOTE] putting ANYTHING here tells Unity to work in tangent coordinates, which is nice

        // diffuse normals
        float3 nd = UnpackNormal(tex2D(_NormalD, IN.uv_Albedo));
        float3 nalpha = pow(1 + _SSSColor, -5) - (1.0 / 32.0);  // [NOTE] empirical fit
        NormalRVec = normalize(lerp(nd, NormalSVec, nalpha.r));
        NormalGVec = normalize(lerp(nd, NormalSVec, nalpha.g));
        NormalBVec = normalize(lerp(nd, NormalSVec, nalpha.b));

        // albedo
        float4 albedo = tex2D(_Albedo, IN.uv_Albedo);
        Out.Albedo = albedo.rgb;
        Out.Specular = pow(albedo.a, 2.2f); // [NOTE] Unity assumes alpha is linear
    }
ENDCG

} //subshader
Fallback "VertexLit"
}
