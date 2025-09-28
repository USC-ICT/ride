Shader "AG/Invisible Shadow Caster" {
    Subshader
    {
       //UsePass "VertexLit/SHADOWCOLLECTOR"    
       //UsePass "VertexLit/SHADOWCASTER"

        Pass
        {
		    Name "ShadowCollector"
		    Tags { "LightMode" = "ShadowCollector" }
		
		    Fog { Mode Off }
		    ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcollector 

            #define SHADOW_COLLECTOR_PASS
            #include "UnityCG.cginc"

            struct appdata {
	            float4 vertex : POSITION;
            };

            struct v2f {
	            V2F_SHADOW_COLLECTOR;
            };

            v2f vert (appdata v)
            {
	            v2f o;
	            TRANSFER_SHADOW_COLLECTOR(o)
	            return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
	            SHADOW_COLLECTOR_FRAGMENT(i)
            }
            ENDCG
	    }

        // Pass to render object as a shadow caster
	    Pass
        {
		    Name "ShadowCaster"
		    Tags { "LightMode" = "ShadowCaster" }
		
		    Fog {Mode Off}
		    ZWrite On ZTest LEqual Cull Off
		    Offset 1, 1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f { 
	            V2F_SHADOW_CASTER;
            };

            v2f vert( appdata_base v )
            {
	            v2f o;
	            TRANSFER_SHADOW_CASTER(o)
	            return o;
            }

            float4 frag( v2f i ) : SV_Target
            {
	            SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
	    }

    }
    Fallback off
}
