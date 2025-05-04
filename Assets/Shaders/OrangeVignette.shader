Shader "Custom/OrangeVignettePostProcess"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _VignetteColor ("Vignette Color", Color) = (1.0, 0.5, 0.0, 1.0)
        _VignetteStrength ("Strength", Range(0, 1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _VignetteColor;
            float _VignetteStrength;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float dist = length(uv) * 2.0;
                float vignette = saturate(dist);
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb = lerp(col.rgb, _VignetteColor.rgb, _VignetteStrength * vignette);
                return col;
            }
            ENDCG
        }
    }
}
