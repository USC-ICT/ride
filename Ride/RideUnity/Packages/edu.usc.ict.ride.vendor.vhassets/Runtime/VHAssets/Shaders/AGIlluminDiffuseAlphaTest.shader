Shader "AG/Self-Illumin/Diffuse Alpha-Test" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
        _Illum ("Illumin (A)", 2D) = "white" {}
        _EmissionLM ("Emission (Lightmapper)", Float) = 0
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {"Queue"="AlphaTest" "IgnoreProjector"="False""RenderType"="TransparentCutout"}
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert alphatest:_Cutoff

        sampler2D _MainTex;
        sampler2D _Illum;
        fixed4 _Color;

        struct Input {
            float2 uv_MainTex;
            float2 uv_Illum;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Emission = c.rgb * tex2D(_Illum, IN.uv_Illum).a;
            o.Alpha = c.a;
        }
        ENDCG
    }
FallBack "Self-Illumin/VertexLit"
}
