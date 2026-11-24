Shader "ElementChaser/Item/Fire Sway"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)

        _FlameStrength ("Flame Strength", Range(0, 0.5)) = 0.15
        _PulseStrength ("Pulse Strength", Range(0, 0.5)) = 0.12
        _FlameSpeed ("Flame Speed", Range(0, 10)) = 4
        _EmissionStrength ("Emission Strength", Range(0, 3)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface Surf Standard fullforwardshadows vertex:Vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;

        float _FlameStrength;
        float _PulseStrength;
        float _FlameSpeed;
        half _EmissionStrength;

        struct Input
        {
            float2 uv_MainTex;
        };

        // 밑동은 고정하고 불꽃 끝을 변형
        void Vert(inout appdata_full input)
        {
            float weight = saturate(input.color.r);

            float phase = dot(
                input.vertex.xyz,
                float3(1.1, 0.7, 0.9));

            float time = _Time.y * _FlameSpeed;

            float sway =
                sin(time + phase) * 0.65
                + sin(time * 1.71 + phase * 1.3) * 0.35;

            input.vertex.x +=
                sway * _FlameStrength * weight;

            input.vertex.z +=
                sin(time * 0.83 + phase)
                * _FlameStrength * 0.4 * weight;

            input.vertex.y +=
                sin(time * 1.31 + phase)
                * _PulseStrength * weight;
        }

        void Surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 color =
                tex2D(_MainTex, input.uv_MainTex) * _Color;

            output.Albedo = color.rgb;
            output.Emission = color.rgb * _EmissionStrength;
            output.Metallic = 0;
            output.Smoothness = 0;
            output.Alpha = color.a;
        }

        ENDCG
    }

    FallBack "Diffuse"
}