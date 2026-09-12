// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. All rights reserved.
// Editor-only preview for normal maps. BC5 and DXT5nm keep X and Y only, so a raw
// preview reads green. This rebuilds Z the same way a shader does at render time.

Shader "Hidden/KaleidoVR/NormalMapPreview"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "ForceSupported" = "True" }

        Lighting Off
        Blend One Zero
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 packed = tex2D(_MainTex, i.uv);
                float3 normal = UnpackNormal(packed);
                return fixed4(normal * 0.5 + 0.5, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
