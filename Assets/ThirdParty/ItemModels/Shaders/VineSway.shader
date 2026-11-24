Shader "ElementChaser/Item/Vine Sway"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Glossiness ("Smoothness", Range(0, 1)) = 0.1
        _Metallic ("Metallic", Range(0, 1)) = 0

        _SwayStrength ("Sway Strength", Range(0, 0.5)) = 0.12
        _SwaySpeed ("Sway Speed", Range(0, 10)) = 2
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
        half _Glossiness;
        half _Metallic;

        float _SwayStrength;
        float _SwaySpeed;

        struct Input
        {
            float2 uv_MainTex;
        };

        // MotionMask의 빨강 값으로 움직임 제한
        void Vert(inout appdata_full input)
        {
            float weight = saturate(input.color.r);

            float phase = dot(
                input.vertex.xyz,
                float3(1.3, 0.7, 1.1));

            float time = _Time.y * _SwaySpeed;

            float swayX = sin(time + phase);
            float swayZ = cos(time * 0.73 + phase * 1.17);

            input.vertex.x +=
                swayX * _SwayStrength * weight;

            input.vertex.z +=
                swayZ * _SwayStrength * 0.6 * weight;
        }

        void Surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 color =
                tex2D(_MainTex, input.uv_MainTex) * _Color;

            output.Albedo = color.rgb;
            output.Metallic = _Metallic;
            output.Smoothness = _Glossiness;
            output.Alpha = color.a;
        }

        ENDCG
    }

    FallBack "Diffuse"
}