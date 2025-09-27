// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

/* --------------------------------------------------
Description
This shader gives an anisotropic highlight, similar to the highlights found on brushed metal or hair.
The highlight can be blended from anisotropic to blinn based on the blue channel of the specular map.
Supports diffuse, normal, specular and gloss shading with alphatested transparency. Gloss and
specular values also apply to the anisotropic highlight.
The highlight can be shifted up or down the surface using the Anisotropic Highlight Offset value.
The direction of the surface for anisotropic highlight is defined using a directional texture like
these. These act similarly to tangent space normal maps, defining the direction of the surface.
However, they should not be converted to normal maps in Unity.

Usage
Anisotropic Direction:  Direction of the surface highlight. Follows the same directional values as
                        a tangent space normal map.
Anisotropic Offset:     Can be used to push the highlight towards or away from the centre point.

http://wiki.unity3d.com/index.php/Anisotropic_Highlight_Shader
-------------------------------------------------- */
//AnisotropicDoubleSidedDiffuseNormalFresnelAlphaTest
Shader "AG/Hair" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Diffuse (RGB) Alpha (A)", 2D) = "white" {}
        _DecalTex("Decal (RGB) (2nd UV Set)", 2D) = "white" {}
        _SpecularColor ("Specular Intensity", Float) = 1

        //_Shininess ("Shininess", Range (0.01,100)) = 0.01
        _FresnelColor ("Fresnel Color", Color) = (1,1,1)
        _FresnelStrength ("Fresnel Strength", Float) = 0.5
        _FresnelSpread ("Fresnel Spread", Float) = 0.5
        _FresMap ("Fresnel Map (RGB)", 2D) = "white" {}

        _GlossSharpness ("Gloss Sharpness", Range(0,1)) = 0.5
        _BlinnAniMix ("Blinn-Anisotrophic Mix", Range(0,1)) = 1

        _BumpMap ("Normal (Normal)", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0.01,1)) = 1
        _AnisoColor ("Anisotropic Color", Color) = (1, 1, 1)
        _AnisoTex ("Anisotropic Direction (Normal)", 2D) = "bump" {}
        _AnisoOffset ("Anisotropic Highlight Offset", Range(-1,1)) = -0.2
        _Cutoff ("Alpha Cut-Off Threshold", Range(0,1)) = 0.5
        _EdgeBlur("Edge Blur", Range(0, 1)) = 0.5
        _EdgeBlurOpacityBoost("Edge Blur Opacity Boost", Float) = 0
        _EdgeBlurLightBoost("Edge Blur Light Boost", Float) = 1

        _ShadowColor("Shadow Color (RGB)", Color) = (1, 1, 1, 1)
        //_BlurAmount("Blur Amount", Float) = 0.0075
        //_OutlineColor("Outline Color", Color) = (1, 0.5, 0, 1)
        _Outline("Outline Offset", Range(0.0, 0.1)) = .05
        _OutlineOpacity("Outline Opacity", Range(0,1)) = 0.5
        _OutlineBoost("Outline Boost", Float) = 0

    }

    SubShader{



        Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull OFF
        //ZWrite OFF

        CGPROGRAM

        struct SurfaceOutputAniso {
            fixed3 Albedo;
            fixed3 Normal;
            fixed4 AnisoDir;
            fixed3 Emission;
            half Specular;
            fixed Gloss;
            fixed Alpha;
        };

        float _GlossSharpness, _BlinnAniMix, _AnisoOffset, _Cutoff, _EdgeBlur;
        float4 _AnisoColor;
        float4 _ShadowColor;
        inline fixed4 LightingAniso (SurfaceOutputAniso s, fixed3 lightDir, fixed3 viewDir, fixed atten)
        {
            fixed3 h = normalize(normalize(lightDir) + normalize(viewDir));
            float NdotL = saturate(dot(s.Normal, lightDir));

            fixed HdotA = dot(normalize(s.Normal + s.AnisoDir.rgb), h);
            float aniso = max(0, sin(radians((HdotA + _AnisoOffset) * 180)));

            float spec = saturate(dot(s.Normal, h));
            spec = saturate(pow(lerp(spec, aniso, s.AnisoDir.a), s.Gloss * 128) * s.Specular);

            fixed3 cNoShadow = ((s.Albedo * _LightColor0.rgb * NdotL) + (_LightColor0.rgb * spec* _AnisoColor.rgb));
            fixed4 c;
            c.rgb = cNoShadow * (atten * 2);
            c.rgb += cNoShadow * _ShadowColor.rgb * (1.0 - atten); //This control self-shadowing strength/color; default black
            c.a = 1;

            clip(s.Alpha - _Cutoff);
            return c;
        }

        #pragma surface surf Aniso
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_DecalTex;
            float2 uv_AnisoTex;
            float3 viewDir;
        };

        sampler2D _MainTex, _DecalTex, _SpecularTex, _BumpMap, _AnisoTex, _FresMap;
        float4 _Color;
        float _SpecularColor;
        float4 _FresnelColor;
        float _FresnelStrength;
        float _FresnelSpread;
        float _NormalStrength;

        void surf (Input IN, inout SurfaceOutputAniso o)
        {
            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex);
            fixed3 decal = tex2D(_DecalTex, IN.uv2_DecalTex);
            half3 frn = tex2D(_FresMap, IN.uv_MainTex);

            //o.Albedo = lerp(albedo.rgb, decalTex.rgb, decalTex.a) * _Color;
            o.Albedo = albedo.rgb * decal.rgb * _Color;
            o.Alpha = albedo.a;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Normal.z = o.Normal.z/_NormalStrength;
            o.Normal = normalize(o.Normal);
            fixed3 spec = tex2D(_SpecularTex, IN.uv_MainTex).rgb;
            o.Specular = _SpecularColor;
            o.Gloss = _GlossSharpness;
            o.AnisoDir = fixed4(UnpackNormal(tex2D(_AnisoTex, IN.uv_AnisoTex)), _BlinnAniMix);

            //Fresnel
            //The clamping gets rid of rendering artifacts when we approach the extreme viewDirs
            half fresnelFactor = clamp(dot(normalize(IN.viewDir), o.Normal), 0.1, 1);
            o.Emission.rgb = frn.rgb * _FresnelColor.rgb * (_FresnelStrength - fresnelFactor * _FresnelStrength);
            o.Emission.rgb = pow(o.Emission.rgb, (_FresnelSpread * fresnelFactor)) * _FresnelStrength * _LightColor0.rgb;
        }
        ENDCG

        Pass{
            Cull Front
            ZWrite On
            ColorMask RGB
            Blend SrcAlpha One

            Name "Outline"
            Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "LightMode" = "Always" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex : POSITION;
                //float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 texcoord2 : TEXCOORD1;
                float3 normal : NORMAL;
            };

            sampler2D _MainTex, _DecalTex, _BumpMap;
            float _Outline, _OutlineOpacity, _OutlineBoost;
            float4 _Color;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.texcoord = float4(v.texcoord.xy, 0, 0);
                o.texcoord2 = float4(v.texcoord2.xy, 0, 0);
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.color = _OutlineColor;
                o.normal = float3(1, 1, 1);

                float3 norm = UnityObjectToViewPos(v.normal);
                //float3 norm = mul((float3x3)UNITY_MATRIX_MV, v.normal);
                norm.x *= UNITY_MATRIX_P[0][0];
                norm.y *= UNITY_MATRIX_P[1][1];
                o.vertex.xy += norm.xy * _Outline;

                return o;
            };

            fixed4 frag(v2f i):SV_TARGET
            {
                float4 tex = tex2D(_MainTex, i.texcoord) * tex2D(_DecalTex, i.texcoord2) * _Color;
                float4 output = float4(tex.rgb * (1 + _OutlineBoost), tex.a * _OutlineOpacity);

                return output;
            }
            ENDCG

        }
        Pass
        {
            Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "LightMode" = "ForwardBase" }
            Cull OFF
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert alpha
            #pragma fragment frag alpha

            sampler2D _MainTex, _DecalTex;
            fixed4 _Color;
            fixed4 _LightColor0;
            fixed4 _SpecColor;
            //float _Shininess;
            float _GlossSharpness, _BlinnAniMix, _AnisoOffset, _Cutoff, _EdgeBlur, _EdgeBlurOpacityBoost, _EdgeBlurLightBoost;
            float4 _AnisoColor;

            struct v2f
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
            };

            struct fragInput
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR0;
                half2 uv : TEXCOORD0;
                half2 uv2 : TEXCOORD1;
            };

            fragInput vert(v2f v)
            {
                fragInput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = float4(v.texcoord.xy, 0, 0);
                o.uv2 = float4(v.texcoord1.xy, 0, 0);

                float4 vertNorm = float4(v.normal, 1);
                float3 normalDirection = normalize(mul(vertNorm, unity_WorldToObject));
                float3 lightDirection = _WorldSpaceLightPos0.xyz; //This is normalized, if directional
                float attenuation = 1 / length(lightDirection); //1 for directional, varies for spot
                lightDirection = normalize(lightDirection); //Normalizing in the case of spot light

                //Diffuse light = IncomingLight * DiffColor * (N dot L)
                float nDotL = max(dot(normalDirection, lightDirection), 0);
                float3 diffuse = _LightColor0.xyz * _Color.rgb * nDotL * attenuation;

                float3 ambientLight = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;

                o.color = float4(ambientLight + diffuse, 1);

                return o;
            }

            half4 frag(fragInput i) : COLOR
            {
                float4 tex = tex2D(_MainTex, i.uv) * tex2D(_DecalTex, i.uv2) * i.color;

                if (tex.a < _Cutoff)
                {
                    if (tex.a + _EdgeBlur > _Cutoff)
                    {
                        return float4(tex.rgb * (1+_EdgeBlurLightBoost), tex.a * (1+_EdgeBlurOpacityBoost)); //This is the key region for blur
                    }
                    else
                    {
                        return float4(tex.rgb, 0);
                    }
                }
                else
                {
                    return float4(0, 0, 0, 0);
                }
            }

            ENDCG
        }
    }
    FallBack "Transparent/Cutout/VertexLit"
}
