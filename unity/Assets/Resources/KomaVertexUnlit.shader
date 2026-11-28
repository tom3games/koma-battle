// 背景用: 頂点色だけで塗る不透明の影なしシェーダ（深度を書く・フォグは効く）。Sprites/Default は深度を書かず並びが崩れるため。
Shader "Koma/VertexUnlit"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Off ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; UNITY_FOG_COORDS(0) };
            fixed4 _Color;
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color * _Color; UNITY_TRANSFER_FOG(o, o.pos); return o; }
            fixed4 frag (v2f i) : SV_Target { fixed4 c = fixed4(i.color.rgb, 1); UNITY_APPLY_FOG(i.fogCoord, c); return c; }
            ENDCG
        }
    }
}
