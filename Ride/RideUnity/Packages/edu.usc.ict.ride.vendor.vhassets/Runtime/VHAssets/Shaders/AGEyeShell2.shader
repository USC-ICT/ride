// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

/*This shader is modified from the Phong shader from Unity Wiki
http://en.wikibooks.org/wiki/Cg_Programming/Unity/Smooth_Specular_Highlights#Shader_Code

The main modifications are:
    - Added transparency
    - Removed the second pass for the back side


Versions
1.0 - Initial version
2.0 - Now works with spot lights
    - Changed light mode to "Vertex" to get pixel lighting information (ForwardBase only returns directional light colors)
    - Cleared out if-blocks for optimization (directional vs spot light ifs)
    - Removed "right/wrong side" if-calculation

Joe Yip
yip@ict.usc.edu
*/
Shader "AG/EyeShell 2" {
    Properties {
        _SpecColor ("Spec Color", Color) = (1,1,1,1)
        _Shininess ("Shininess", Float) = 10
        _SpecFallOff ("Specular Fall-Off", Float) = 10
        _SpecIntensity ("Specular Intensity", Float) = 1
        _HighlightOffset ("Highlight offset", Vector) = (0, 0, 0, 0)
    }

    SubShader {
        Tags {"LightMode"="Vertex" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            uniform float4 _LightColor0; // color of light source (from "Lighting.cginc")

            // User-specified properties
            uniform float4 _SpecColor;
            uniform float _Shininess;
            uniform float _SpecFallOff;
            uniform float _SpecIntensity;
            float4 _HighlightOffset;

            struct vertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct vertexOutput {
                float4 pos : SV_POSITION;
                float4 posWorld : TEXCOORD0;
                float3 normalDir : TEXCOORD1;
            };

            vertexOutput vert(vertexInput input)
            {
                vertexOutput output;

                float4x4 modelMatrix = unity_ObjectToWorld;
                float4x4 modelMatrixInverse = unity_WorldToObject; //Multiplication with unity_Scale.w is unnecessary because we normalize transformed vectors

                output.posWorld = mul(modelMatrix, input.vertex);
                output.normalDir = normalize(mul(float4(input.normal, 0.0), modelMatrixInverse).xyz);
                output.pos = UnityObjectToClipPos(input.vertex);
                return output;
            }

            float4 frag(vertexOutput input) : COLOR
            {
                float3 normalDirection = normalize(input.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos - input.posWorld.xyz);
                float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.rgb;

                //Light direction & atten calculation
                float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - (input.posWorld.xyz * _WorldSpaceLightPos0.w); //_WorldSpaceLightPos0.w = 0 = Directional
                float one_over_distance = 1.0 / length(vertexToLightSource); //linear attenuation
                float attenuation = lerp(1.0, one_over_distance, _WorldSpaceLightPos0.w); //Switch for dir vs spot lights
                float3 lightDirection = vertexToLightSource * one_over_distance;

                float3 diffuseReflection = attenuation * _LightColor0.rgb * max(0.0, dot(normalDirection, lightDirection));
                float3 specularReflection = attenuation * _LightColor0.rgb * _SpecColor.rgb * pow(max(0.0, dot(reflect(-lightDirection, normalDirection), viewDirection)), _Shininess);
                float4 output = float4((ambientLighting + diffuseReflection + specularReflection)*_SpecIntensity, specularReflection.r*_SpecFallOff);
                return output;
            }
            ENDCG
        }
    }
}
