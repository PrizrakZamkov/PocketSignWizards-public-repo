Shader "Custom/FrozenSprite"
{
    Properties
    {
        _MainTex      ("Sprite Texture", 2D)        = "white" {}
        _FrozenAmount ("Frozen Amount", Range(0,1)) = 0
        _IceColor     ("Ice Color", Color)          = (0.45, 0.82, 1.0, 1.0)
        _IceIntensity ("Ice Overlay", Range(0,1))   = 0.55
        _Desaturate   ("Desaturate", Range(0,1))    = 0.45
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            // Убираем все варианты которые не нужны спрайтам
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;       // цвет вершины SpriteRenderer
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float     _FrozenAmount;
            float4    _IceColor;
            float     _IceIntensity;
            float     _Desaturate;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            // Процедурный паттерн кристаллов льда — без текстур, только математика.
            // frac + abs = diamond grid, две частоты дают прожилки.
            float IcePattern(float2 uv)
            {
                float2 g1 = frac(uv * 5.5) - 0.5;
                float2 g2 = frac(uv * 11.0 + 0.5) - 0.5;

                float diamonds = abs(g1.x) + abs(g1.y);   // ромбовидная сетка
                float veins    = abs(g2.x) + abs(g2.y);   // прожилки

                return smoothstep(0.44, 0.50, diamonds) * 0.55
                     + smoothstep(0.44, 0.50, veins)    * 0.25;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // Ранний выход — нет оверхеда когда эффект не активен
                clip(col.a - 0.001);
                if (_FrozenAmount < 0.001) return col;

                float pattern = IcePattern(i.uv);

                // 1. Подмешиваем ледяной цвет
                float3 rgb = lerp(col.rgb, _IceColor.rgb, _IceIntensity * _FrozenAmount);

                // 2. Прожилки кристаллов поверх
                rgb = lerp(rgb, _IceColor.rgb, pattern * _FrozenAmount);

                // 3. Лёгкая десатурация (замёрзшее выглядит тусклым)
                float gray = dot(rgb, float3(0.299, 0.587, 0.114));
                rgb = lerp(rgb, gray.xxx, _Desaturate * _FrozenAmount);

                return fixed4(rgb, col.a);
            }
            ENDCG
        }
    }

    // Fallback на случай старого железа
    Fallback "Sprites/Default"
}
