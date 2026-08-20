Shader "UFR_CutoutCarapace"
{
    Properties
    {
        _MainTex ("Texture", any) = "white" {}
        _MaskTex ("Mask texture", 2D) = "black" {}
        _DrawColor ("Draw Color", Vector) = (1,1,1,1)
        _DrawColorTwo ("Draw Color Two", Vector) = (1,1,1,1)
        _DrawColorThree ("Draw Color Three", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _MainTex_ST;
            float4 _DrawColor;
            float4 _DrawColorTwo;
            float4 _DrawColorThree;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed4 mask = tex2D(_MaskTex, i.uv);

				float lumen = dot(col.rgb, float3(0.299, 0.587, 0.114));

				float mx = max(mask.r, max(mask.g, mask.b));
				float coverage = step(0.25, mx);

				float3 zone = _DrawColor.rgb;
				if (mask.g > mask.r && mask.g >= mask.b) zone = _DrawColorTwo.rgb;
				if (mask.b > mask.r && mask.b > mask.g) zone = _DrawColorThree.rgb;

				col.rgb = lerp(col.rgb, zone * lumen, coverage);
				clip(col.a - 0.5);
				return col;
			}
			
            ENDCG
        }
    }
}