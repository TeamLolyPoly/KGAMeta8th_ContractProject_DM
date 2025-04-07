Shader "Unlit/LedShader"
{
    Properties
    {
		[NoScaleOffset]_MainTex ("Texture", 2D) = "white" {}
		[NoScaleOffset]_LEDTex("LED Texture",2D ) = "white"{}
		_Tiling("Tiling",float) = 1
		_Brightness("brightness",range(0,10)) = 1
		_OffsetX("OffsetX",float) = 0
		_OffsetY("OffsetY",float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;		
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
			sampler2D _LEDTex;
			float4 _MainTex_ST;
			float _Tiling;
			float _Brightness;
			float _OffsetX, _OffsetY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
				float4 c = tex2D(_MainTex, float2(_OffsetX,_OffsetY) + (floor(i.uv*_Tiling)/ (_Tiling)));
				float4 d = tex2D(_LEDTex, i.uv*_Tiling);

                return c*d*_Brightness;
            }
            ENDCG
        }
    }
}
