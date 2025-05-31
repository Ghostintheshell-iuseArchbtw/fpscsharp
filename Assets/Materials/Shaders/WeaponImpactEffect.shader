Shader "FPS/PostProcess/WeaponImpactEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.5
        _VignetteColor ("Vignette Color", Color) = (1, 0, 0, 1)
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.1)) = 0.01
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _DistortionCenter ("Distortion Center", Vector) = (0.5, 0.5, 0, 0)
    }
    
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float _VignetteIntensity;
            float4 _VignetteColor;
            float _ChromaticAberration;
            float _DistortionStrength;
            float4 _DistortionCenter;
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate distance from center for vignette and distortion
                float2 center = _DistortionCenter.xy;
                float2 uv = i.uv;
                float2 distFromCenter = uv - center;
                float distSquared = dot(distFromCenter, distFromCenter);
                
                // Apply radial distortion when vignette is active
                float distortionFactor = _DistortionStrength * _VignetteIntensity;
                uv = uv + distFromCenter * distortionFactor * distSquared;
                
                // Apply chromatic aberration 
                float2 direction = normalize(distFromCenter + 0.0001) * _ChromaticAberration;
                
                // Sample the texture with chromatic aberration
                fixed4 col;
                col.r = tex2D(_MainTex, uv + direction).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv - direction).b;
                col.a = 1;
                
                // Apply vignette effect
                float vignette = 1.0 - distSquared * 2.0;
                vignette = smoothstep(0, 1, vignette);
                
                // Mix the vignette color with the original color
                col.rgb = lerp(col.rgb, _VignetteColor.rgb, (1.0 - vignette) * _VignetteIntensity);
                
                return col;
            }
            ENDCG
        }
    }
}
