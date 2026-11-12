Shader "ElementChaser/Item/Smoke Wall Clip"
{
    Properties
    {
        _MainTex ("Smoke Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SmokeClipPlane ("Wall Plane", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _SmokeClipPlane;

            struct Attributes
            {
                float4 Position : POSITION;
                fixed4 Color : COLOR;
                float2 UV : TEXCOORD0;
            };

            struct Varyings
            {
                float4 Position : SV_POSITION;
                fixed4 Color : COLOR;
                float2 UV : TEXCOORD0;
                float3 WorldPosition : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.Position = UnityObjectToClipPos(input.Position);
                output.WorldPosition = mul(unity_ObjectToWorld, input.Position).xyz;
                output.Color = input.Color * _Color;
                output.UV = TRANSFORM_TEX(input.UV, _MainTex);
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                // 벽 반대편의 연기 픽셀 제거
                clip(dot(_SmokeClipPlane.xyz, input.WorldPosition) + _SmokeClipPlane.w);
                return tex2D(_MainTex, input.UV) * input.Color;
            }
            ENDCG
        }
    }
}