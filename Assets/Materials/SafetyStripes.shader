Shader "Custom/SafetyStripes"
{
    Properties
    {
        _ColorA("Color A", Color) = (1, 0.8, 0, 1)
        _ColorB("Color B", Color) = (0, 0, 0, 1)
        _StripeWidth("Stripe Width", Float) = 0.15
        _Angle("Angle", Float) = 45
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ColorA, _ColorB;
            float _StripeWidth, _Angle;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float rad = _Angle * 3.14159 / 180.0;
                float stripe = cos(rad) * i.uv.x + sin(rad) * i.uv.y;
                float t = frac(stripe / _StripeWidth);
                return t < 0.5 ? _ColorA : _ColorB;
            }
            ENDCG
        }
    }
}