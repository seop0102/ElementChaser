Shader "ElementChaser/Item/Invisibility Pulse"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Glossiness ("Smoothness", Range(0, 1)) = 0.2
        _Metallic ("Metallic", Range(0, 1)) = 0

        _CycleDuration ("Cycle Duration", Range(0.1, 10)) = 2.5
        _MinAlpha ("Minimum Alpha", Range(0, 1)) = 0
        _MaxAlpha ("Maximum Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        LOD 200
        ZWrite Off

        CGPROGRAM

        #pragma surface Surf Standard alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;

        float _CycleDuration;
        float _MinAlpha;
        float _MaxAlpha;

        struct Input
        {
            float2 uv_MainTex;
        };

        // 외형의 투명도를 반복 전환
        void Surf(Input input, inout SurfaceOutputStandard output)
        {
            float duration = max(0.1, _CycleDuration);
            float phase = _Time.y / duration * 6.2831853;
            float progress = (1.0 + cos(phase)) * 0.5;

            float alpha = lerp(
                _MinAlpha,
                _MaxAlpha,
                progress);

            fixed4 color =
                tex2D(_MainTex, input.uv_MainTex) * _Color;

            output.Albedo = color.rgb;
            output.Metallic = _Metallic;
            output.Smoothness = _Glossiness;
            output.Alpha = color.a * alpha;
        }

        ENDCG
    }

    FallBack Off
}