Shader "Horizon Forge/SplatBlit"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _SplatMap("SplatMap", 2D) = "white" {}
        _Splat0("Splat0", 2D) = "white" {}
        _Splat1("Splat1", 2D) = "white" {}
        _Splat2("Splat2", 2D) = "white" {}
        _Splat3("Splat3", 2D) = "white" {}
    }

    CGINCLUDE

        #include "UnityCG.cginc"
        #include "Noise.hlsl"
        
		struct appdata
		{
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };
        
        sampler2D _SplatMap;
        float4 _SplatMap_ST;

        float2 _CurvatureCenter;

        sampler2D _Splat0;
        float4 _Splat0_ST;
        sampler2D _Splat1;
        float4 _Splat1_ST;
        sampler2D _Splat2;
        float4 _Splat2_ST;
        sampler2D _Splat3;
        float4 _Splat3_ST;

        v2f VertBlit(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.texcoord;
            return o;
        }

        float4 normalize_splat(float4 col)
        {
            float mag = col.r + col.g + col.b + col.a;
            return col * (1.0 / mag);
        }

        half4 FragBlit(v2f i) : SV_Target
        {
            float noise = pow(1 + PerlinNoise(float3(i.uv * 7, 0))*0.5, 3);
            float2 center = noise*_CurvatureCenter + 0.5;
            float2 splatUV = ((i.uv - center) * 1.8) + center;

            float4 col = tex2D(_SplatMap, TRANSFORM_TEX(splatUV, _SplatMap));
            //col = normalize_splat(pow(col, 2));
            //return col;

            float4 splat = tex2D(_Splat0, TRANSFORM_TEX(i.uv, _Splat0))*col.r
                 + tex2D(_Splat1, TRANSFORM_TEX(i.uv, _Splat1))*col.g
                 + tex2D(_Splat2, TRANSFORM_TEX(i.uv, _Splat2))*col.b
                 + tex2D(_Splat3, TRANSFORM_TEX(i.uv, _Splat3))*col.a;

            splat.a = 1; // terrain textures don't use alpha
            return splat;
        }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM

                #pragma vertex VertBlit
                #pragma fragment FragBlit

            ENDCG
        }
    }
}
