Shader "Retro/PxlCrush"
{
    Properties
    {
        [PerRendererData] _MainTex ("Render Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        
        [Header(Pixelation)]
        [Toggle] _UseManualPixelation ("Manual Pixelation (Snapping)", Float) = 0
        _PixelScale ("Resolution Downscale", Range(16, 2048)) = 320
        
        [Header(Color Depth)]
        _ColorCount ("Color Depth (Channels)", Range(2, 256)) = 16
        _ColorCrushStrength ("Color Crush Strength", Range(0, 1)) = 1
        
        [Header(CRT Scanlines)]
        [Toggle] _UseScanlines ("Use CRT Scanlines", Float) = 1
        _ScanlineFrequency ("Scanline Count", Float) = 360
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.15
        
        [Header(Dithering)]
        [Toggle] _UseDithering ("Use Dithering", Float) = 1
        _DitherStrength ("Dither Strength", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float _PixelScale;
            float _UseManualPixelation;
            float _ColorCount;
            float _ColorCrushStrength;
            
            float _UseScanlines;
            float _ScanlineFrequency;
            float _ScanlineStrength;
            
            float _UseDithering;
            float _DitherStrength;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // Matriz Bayer 4x4 para Dithering retro ordenado
            static const float4x4 bayerMatrix = float4x4(
                0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0, 4.0/16.0, 14.0/16.0,  6.0/16.0,
                3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0, 7.0/16.0, 13.0/16.0,  5.0/16.0
            );

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                
                // 1. Pixelación Manual (opcional, por si la Render Texture no tiene Point-filtered)
                if (_UseManualPixelation > 0.5)
                {
                    uv = floor(uv * _PixelScale) / _PixelScale;
                }
                
                // Muestrear textura
                fixed4 texColor = tex2D(_MainTex, uv);
                
                // 2. Dithering (Ruido retro ordenado de paletas)
                if (_UseDithering > 0.5)
                {
                    // Obtener posición del píxel en base a la resolución de pixelación (100% independiente de plataforma)
                    float2 pixelPos = floor(uv * _PixelScale) % 4.0;
                    float ditherValue = bayerMatrix[pixelPos.x][pixelPos.y] - 0.5;
                    
                    // Aplicar dither antes del color crush para un degradado hermoso
                    texColor.rgb += ditherValue * _DitherStrength;
                }

                // 3. Color Crush (Reducción de profundidad de color, e.g. 16 colores por canal)
                float3 crushedColor = floor(texColor.rgb * _ColorCount) / _ColorCount;
                texColor.rgb = lerp(texColor.rgb, crushedColor, _ColorCrushStrength);
                
                // 4. CRT Scanlines (Líneas de barrido de monitor retro)
                if (_UseScanlines > 0.5)
                {
                    // Onda senoidal basada en la posición de la pantalla o la UV y
                    float scanline = sin(uv.y * _ScanlineFrequency * 3.14159265);
                    // Suavizar scanline
                    float scanlineFactor = lerp(1.0, (scanline * 0.5 + 0.5), _ScanlineStrength);
                    texColor.rgb *= scanlineFactor;
                }
                
                return texColor * i.color;
            }
            ENDCG
        }
    }
}
