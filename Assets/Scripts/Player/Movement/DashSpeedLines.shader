Shader "WesternSamurai/DashSpeedLines"
{
    SubShader
    {
        Tags { "Queue"="Overlay-10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float4 screen : TEXCOORD1; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screen = ComputeScreenPos(output.positionCS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float taper = pow(saturate(1 - abs(input.uv.y * 2 - 1)), .65);
                float edge = abs(input.uv.x * 2 - 1);
                float shape = 1 - smoothstep(taper - .12, taper, edge);
                float2 screen = input.screen.xy / input.screen.w * 2 - 1;
                float mask = smoothstep(.46, .78, length(screen));
                return half4(input.color.rgb, input.color.a * shape * mask);
            }
            ENDHLSL
        }
    }
}
