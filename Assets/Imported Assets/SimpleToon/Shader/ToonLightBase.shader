Shader "Lpk/LightModel/ToonLightSafe"
{
    Properties
    {
        _BaseMap            ("Texture", 2D)                       = "grey" {}
        _BaseColor          ("Color", Color)                      = (0.5, 0.5, 0.5, 1)
        
        [Space]
        _BumpMap            ("Normal Map", 2D)                    = "bump" {}
        _BumpScale          ("Normal Scale", Float)               = 1.0
        
        [Space]
        _HeightMap          ("Displacement Map", 2D)              = "black" {}
        _HeightAmplitude    ("Displacement Amplitude", Float)     = 0.02
        
        [Space]
        _ShadowBands        ("Shadow Bands", Range(1, 10))        = 3.0
        _ShadowStep         ("Shadow Step", Range(0, 1))          = 0.5
        _ShadowStepSmooth   ("Shadow Step Smooth", Range(0.001, 1)) = 0.04
        
        [Space] 
        _SpecularMap        ("Specular/Gloss Map (RGB)", 2D)      = "white" {}
        _SpecularStep       ("SpecularStep", Range(0, 1))         = 0.6
        _SpecularStepSmooth ("SpecularStepSmooth", Range(0.001, 1)) = 0.05
        [HDR]_SpecularColor ("SpecularColor", Color)              = (1, 1, 1, 1)
        
        [Space]
        _RimStep            ("RimStep", Range(0, 1))              = 0.65
        _RimStepSmooth      ("RimStepSmooth", Range(0.001, 1))    = 0.4
        _RimColor           ("RimColor", Color)                   = (1, 1, 1, 1)
        
        [Space]   
        _OutlineWidth       ("OutlineWidth", Range(0.0, 1.0))     = 0.15
        _OutlineColor       ("OutlineColor", Color)               = (0.0, 0.0, 0.0, 1)
    }

    // HLSLINCLUDE ensures both the Forward and Outline pass share the exact same CBUFFER.
    // This makes the shader SRP Batcher compatible, which is a massive performance boost.
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    
    CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float _BumpScale;
        float _HeightAmplitude;
        float _ShadowBands;
        float _ShadowStep;
        float _ShadowStepSmooth;
        float _SpecularStep;
        float _SpecularStepSmooth;
        float4 _SpecularColor;
        float _RimStepSmooth;
        float _RimStep;
        float4 _RimColor;
        float _OutlineWidth;
        float4 _OutlineColor;
        float4 _BaseMap_ST;   
    CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Fixed URP shadow pragmas so shadows don't break across cascades
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
             
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_HeightMap); SAMPLER(sampler_HeightMap);
            TEXTURE2D(_SpecularMap); SAMPLER(sampler_SpecularMap);

            struct Attributes
            {     
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            }; 

            struct Varyings
            {
                float2 uv            : TEXCOORD0;
                float4 normalWS      : TEXCOORD1;
                float4 tangentWS     : TEXCOORD2;
                float4 bitangentWS   : TEXCOORD3;
                float3 viewDirWS     : TEXCOORD4;
                float4 shadowCoord   : TEXCOORD5;
                float4 fogCoord      : TEXCOORD6;
                float3 positionWS    : TEXCOORD7;
                float4 positionCS    : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float height = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, input.uv, 0).r;
                input.positionOS.xyz += input.normalOS * height * _HeightAmplitude;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                float3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap); 
                
                output.normalWS = float4(normalInput.normalWS, viewDirWS.x);
                output.tangentWS = float4(normalInput.tangentWS, viewDirWS.y);
                output.bitangentWS = float4(normalInput.bitangentWS, viewDirWS.z);
                output.viewDirWS = viewDirWS;
                
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;
                
                float3 N = normalize(input.normalWS.xyz);
                float3 T = normalize(input.tangentWS.xyz);
                float3 B = normalize(input.bitangentWS.xyz);
                float3 V = normalize(input.viewDirWS.xyz);
                
                // Proper URP light extraction
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 L = normalize(mainLight.direction);
                float shadow = mainLight.shadowAttenuation;
                
                half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
                float3 normalTS = UnpackNormalScale(normalSample, _BumpScale);
                N = normalize(normalTS.x * T + normalTS.y * B + normalTS.z * N); 
                
                float3 H = normalize(V + L);
                
                // SAFE MATH
                float NV = saturate(dot(N, V));
                float NH = saturate(dot(N, H));
                float halfLambert = dot(N, L) * 0.5 + 0.5;

                float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                float4 specularMap = SAMPLE_TEXTURE2D(_SpecularMap, sampler_SpecularMap, uv);

                // --- NEW OVERLAY BLEND LOGIC ---
                float3 baseRGB = baseMap.rgb;
                float3 blendRGB = _BaseColor.rgb;
                
                // Photoshop-style Overlay: Multiplies darks, Screens lights. 
                // Mid-grey (0.5) becomes exactly the _BaseColor.
                float3 overlayColor = lerp(
                    2.0 * baseRGB * blendRGB, 
                    1.0 - 2.0 * (1.0 - baseRGB) * (1.0 - blendRGB), 
                    step(0.5, baseRGB)
                );

                // We use the Alpha of the _BaseColor to control "Paint Opacity"
                // Alpha 0 = Raw Texture, Alpha 1 = Full Overlay Paint
                float3 finalAlbedo = lerp(baseRGB, overlayColor, _BaseColor.a);
                // -------------------------------

                float safeSpecSmooth = max(0.001, _SpecularStepSmooth * 0.05);
                float specEdge = 1.0 - (_SpecularStep * 0.05);
                float specularNH = smoothstep(specEdge - safeSpecSmooth, specEdge + safeSpecSmooth, NH);
                
                float safeBands = max(1.0, _ShadowBands);
                float bandIndex = floor(halfLambert * safeBands);
                float localNL = frac(halfLambert * safeBands);
                
                float safeShadowSmooth = max(0.001, _ShadowStepSmooth);
                float smoothBand = smoothstep(_ShadowStep - safeShadowSmooth, _ShadowStep + safeShadowSmooth, localNL);
                float shadowNL = (bandIndex + smoothBand) / safeBands;

                float safeRimSmooth = max(0.001, _RimStepSmooth * 0.5);
                float rimEdge = 1.0 - _RimStep;
                float rim = smoothstep(rimEdge - safeRimSmooth, rimEdge + safeRimSmooth, 0.5 - NV);
                
                // Apply our new 'finalAlbedo' instead of the raw maps
                float3 diffuse = mainLight.color * finalAlbedo * shadowNL * shadow;
                float3 specular = saturate(_SpecularColor.rgb * specularMap.rgb * shadow * shadowNL * specularNH);
                float3 ambient = rim * _RimColor.rgb + SampleSH(N) * finalAlbedo;
            
                float3 finalColor = diffuse + ambient + specular;
                finalColor = MixFog(finalColor, input.fogCoord);
                
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Outline"
            Cull Front
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            TEXTURE2D(_HeightMap); SAMPLER(sampler_HeightMap);
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0; 
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float4 fogCoord : TEXCOORD0;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                
                float height = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, v.uv, 0).r;
                v.vertex.xyz += v.normal * height * _HeightAmplitude;
                v.vertex.xyz += v.normal * _OutlineWidth * 0.1;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.pos = vertexInput.positionCS;
                o.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 finalColor = MixFog(_OutlineColor.rgb, i.fogCoord);
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}