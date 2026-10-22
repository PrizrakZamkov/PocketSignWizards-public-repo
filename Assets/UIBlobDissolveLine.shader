Shader "Custom/UIBlobDissolveLine"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _BlobSize ("Blob Size", Float) = 12
        _Softness ("Softness", Range(0.001, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _DissolveAmount;
            float _BlobSize;
            float _Softness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 id = floor(p);
                float2 gv = frac(p);
                float minDist = 10.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y);
                        float2 cell = id + offset;

                        float2 randomPoint = float2(
                            hash(cell),
                            hash(cell + 25.32)
                        );

                        float2 diff = offset + randomPoint - gv;
                        float dist = length(diff);

                        minDist = min(minDist, dist);
                    }
                }

                return minDist;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                if (col.a <= 0.001)
                    discard;

                float n = noise(i.uv * _BlobSize);
                float threshold = lerp(-0.3, 1.2, _DissolveAmount);

                if (n < threshold)
                    discard;

                return col;
            }
            ENDCG
        }
    }
}