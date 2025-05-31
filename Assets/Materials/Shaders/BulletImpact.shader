Shader "FPS/Weapon Effects/BulletImpact"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Color ("Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 2
        _DecalBlendFactor ("Decal Blend Factor", Range(0, 1)) = 0.5
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _FadeTime ("Fade Time", Range(0, 10)) = 5.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }
        LOD 200
        
        // Depth test but no depth write
        ZWrite Off
        ZTest LEqual
        
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _NormalMap;
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float3 worldPos;
            float4 color : COLOR;
        };
        
        half _Glossiness;
        half _Metallic;
        half _DecalBlendFactor;
        fixed4 _Color;
        fixed4 _EmissionColor;
        float _EmissionIntensity;
        float _FadeTime;
        float _SpawnTime;
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Get the elapsed time since decal was spawned
            float elapsedTime = _Time.y - _SpawnTime;
            
            // Calculate the alpha fade factor
            float fade = 1.0 - saturate(elapsedTime / _FadeTime);
            
            // Sample the texture
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color * IN.color;
            
            // Apply normal map
            fixed3 normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            
            // Output surface properties
            o.Albedo = c.rgb;
            o.Normal = normal;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = c.rgb * _EmissionColor.rgb * _EmissionIntensity;
            o.Alpha = c.a * fade * _DecalBlendFactor;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
