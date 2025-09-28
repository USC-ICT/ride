/*
This is a variant shader on the Unity Standard metallic shader. It uses the decal texture slot as a
"normal" layer, rather than the default behavior of multiplying down to the base layer.

Usage
1. Assign shader
2. Unity Editor Inspector -> Debug mode
3. Within material's keyword section find the "_Detail_*" keyword and change to "_DETAIL_AGDECAL"
4. Get out of Debug mode
Helpful reading: http://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html

Notes
1. "ShadowCaster" and "META" passes are disabled due to clashing variable declaration. Search token: (Yip; 01)

Bugs
1. No specular blend yet

Fixed Bugs
1. Decal normal map has no alpha control (since that is *much* more complex and intertwined. Make sure
the incoming normal maps have neutral normal where it should be alpha'd out. Base layer normal will pollute
decal normal. (Updated "UnityStandardInput.cginc" with "_DETAIL_AGDECAL" keyword)


Joe Yip
2016-Jul-22
yip@ict.usc.edu
*/
Shader "Standard Decal"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		_BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}

		_Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
		_ParallaxMap ("Height Map", 2D) = "black" {}

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		
		_DetailMask("Detail Mask", 2D) = "white" {}

		_DetailAlbedoMap("Detail Albedo x2", 2D) = "black" {}
		_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}

		[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0


		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
	}

	CGINCLUDE
        #include "UnityCG.cginc"
        #include "UnityShaderVariables.cginc"
        #include "UnityStandardConfig.cginc"
        #include "UnityStandardInput.cginc"
        #include "UnityPBSLighting.cginc"
        #include "UnityStandardUtils.cginc"
        #include "UnityStandardBRDF.cginc"
        #include "AutoLight.cginc"

        //Straight copy from "UnityStandardCore.cginc"; copying to avoid including the file, which causes SHADOW_COORD error
        struct FragmentCommonData_AG
        {
	        half3 diffColor, specColor;
	        // Note: oneMinusRoughness & oneMinusReflectivity for optimization purposes, mostly for DX9 SM2.0 level.
	        // Most of the math is being done on these (1-x) values, and that saves a few precious ALU slots.
	        half oneMinusReflectivity, oneMinusRoughness;
	        half3 normalWorld, eyeVec, posWorld;
	        half alpha;

        #if UNITY_OPTIMIZE_TEXCUBELOD || UNITY_STANDARD_SIMPLE
	        half3 reflUVW;
        #endif

        #if UNITY_STANDARD_SIMPLE
	        half3 tangentSpaceNormal;
        #endif
        };

        //Originally from "UnityStandardCore.cginc", modifying call to Albedo() to achieve different blending technique without modifying include file
        inline FragmentCommonData_AG MetallicSetup_AGDecal (float4 i_tex)
        {
	        half3 albedo = tex2D(_MainTex, i_tex.xy).rgb; //"i_tex" is the same as "texcoords" in this case
	        half4 detailAlbedo = tex2D(_DetailAlbedoMap, i_tex.zw);
            albedo = lerp (albedo, detailAlbedo.rgb, detailAlbedo.a) * _Color.rgb;

	        half2 metallicGloss = MetallicGloss(i_tex.xy);
	        half metallic = metallicGloss.x;
	        half oneMinusRoughness = metallicGloss.y;

	        half oneMinusReflectivity;
	        half3 specColor;
	        //half3 diffColor = DiffuseAndSpecularFromMetallic (Albedo(i_tex), metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);
	        half3 diffColor = DiffuseAndSpecularFromMetallic (albedo, metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	        FragmentCommonData_AG o = (FragmentCommonData_AG)0;
	        o.diffColor = diffColor;
	        o.specColor = specColor;
	        o.oneMinusReflectivity = oneMinusReflectivity;
	        o.oneMinusRoughness = oneMinusRoughness;
	        return o;
        }

        // EDF - 06/09/2022 - getting shader errors with this line.
        // Shader error in 'Standard Decal': cannot convert from 'const struct FragmentCommonData_AG' to 'struct FragmentCommonData' at Files/Unity/Hub/Editor/2022.1.0f1/Editor/Data/CGIncludes/UnityStandardCore.cginc(254) (on d3d11)
        // Seems like this approach has been deprecated.  Refs:
        // https://forum.unity.com/threads/help-me-understand-the-code-of-the-standard-shader.324092/
        // https://docs.unity3d.com/2017.3/Documentation/Manual/MetaPass.html
        //Change definition to our modified variant
		//#define UNITY_SETUP_BRDF_INPUT MetallicSetup_AGDecal

	ENDCG

SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300
	

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------
					
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_AGDECAL
			#pragma shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
				
			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			// GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles

			// -------------------------------------

			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_AGDECAL
			#pragma shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd

			#include "UnityStandardCore.cginc"

			ENDCG
		}
        //Disabled due to conflicting variable declaration in "UnityStandardShadow.cginc" (Yip; 01)
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		/*Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------


			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}*/
		// ------------------------------------------------------------------
		//  Deferred pass
		Pass
		{
			Name "DEFERRED"
			Tags { "LightMode" = "Deferred" }

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers nomrt gles
			

			// -------------------------------------

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_AGDECAL
			#pragma shader_feature _PARALLAXMAP

			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
			#pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			#pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
			
			#pragma vertex vertDeferred
			#pragma fragment fragDeferred

			#include "UnityStandardCore.cginc"

			ENDCG
		}

        //Disabled due to conflicting variable declaration in "UnityStandardShadow.cginc" (Yip; 01)
		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		/*Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_AGDECAL

			#include "UnityStandardMeta.cginc"
			ENDCG
		}*/
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 150

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 2.0
			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION 
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_AGDECAL
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP

			#pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
	
			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			
			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_AGDECAL
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP
			#pragma skip_variants SHADOWS_SOFT
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd

			#include "UnityStandardCore.cginc"

			ENDCG
		}
        //Disabled due to conflicting variable declaration in "UnityStandardShadow.cginc" (Yip; 01)
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		/*Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma skip_variants SHADOWS_SOFT
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}*/

        //Disabled due to conflicting variable declaration in "UnityStandardShadow.cginc" (Yip; 01)
		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		/*Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_AGDECAL

			#include "UnityStandardMeta.cginc"
			ENDCG
		}*/
	}


	FallBack "VertexLit"
	CustomEditor "StandardShaderGUI_AGDECAL"
}
