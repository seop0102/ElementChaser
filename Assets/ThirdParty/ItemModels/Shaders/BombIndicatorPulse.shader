Shader "ElementChaser/Item/Bomb Indicator Pulse"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorA ("First Color", Color) = (1, 0.05, 0.02, 1)
        _ColorB ("Second Color", Color) = (1, 0.8, 0.02, 1)

        _CycleDuration ("Cycle Duration", Range(0.1, 5)) = 1
        _EmissionStrength ("Emission Strength", Range(0, 3)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface Surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _ColorA;
        fixed4 _ColorB;

        float _CycleDuration;
        half _EmissionStrength;

        struct Input
        {
            float2 uv_MainTex;
        };

        // 중앙 표시등의 색상을 반복 전환
        void Surf(Input input, inout SurfaceOutputStandard output)
        {
            float duration = max(0.1, _CycleDuration);
            float phase = _Time.y / duration * 6.2831853;
            float progress = (1.0 - cos(phase)) * 0.5;

            fixed4 color =
                lerp(_ColorA, _ColorB, progress)
                * tex2D(_MainTex, input.uv_MainTex);

            output.Albedo = color.rgb;
            output.Emission = color.rgb * _EmissionStrength;
            output.Metallic = 0;
            output.Smoothness = 0.2;
            output.Alpha = color.a;
        }

        ENDCG
    }

    FallBack "Diffuse"
}