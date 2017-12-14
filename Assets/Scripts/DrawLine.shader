Shader "Custom/DrawLine"
{
	Properties  
    {  
        //定义基本属性，可以从编辑器里面进行设置的变量  
        // _MainTex ("Texture", 2D) = "white" {}  
    }  

	CGINCLUDE
			//从应用程序传入顶点函数的数据结构定义 
	 		struct appdata  
	        {  
	            float4 vertex : POSITION;  
	            float2 uv : TEXCOORD0;  
	        };  
	        //从顶点函数传入片段函数的数据结构定义  
	        struct v2f  
	        {  
	            float2 uv : TEXCOORD0;  
	            float4 vertex : SV_POSITION;  
	        };  
	        //定义贴图变量  
	        sampler2D _MainTex;  
	        // float4 _MainTex_ST;  

	        //定义与脚本进行通信的变量
	        vector Value[100]; 
	        int PointNum =0;

	        //计算两点间的距离的函数
	        float Dis(float4 v1,float4 v2)
	        {
	        	return sqrt(pow((v1.x-v2.x),2)+pow((v1.y-v2.y),2));
	        }   

	        //绘制线段
	        bool DrawLineSegment(float4 p1, float4 p2, float lineWidth,v2f i)
	        {
	            float4 center = float4((p1.x+p2.x)/2,(p1.y+p2.y)/2,0,0);
	            //计算点到直线的距离  
	            float d = abs((p2.y-p1.y)*i.vertex.x + (p1.x - p2.x)*i.vertex.y +p2.x*p1.y -p2.y*p1.x )/sqrt(pow(p2.y-p1.y,2) + pow(p1.x-p2.x,2));  
	            //小于或者等于线宽的一半时，属于直线范围  
	            float lineLength = sqrt(pow(p1.x-p2.x,2)+pow(p1.y-p2.y,2));
	            if(d<=lineWidth/2 && Dis(i.vertex,center)<lineLength/2)  
	            {  
	                return true;  
	            }  
	            return false;
	        }

	        v2f vert (appdata v)  
	        {  
	            v2f o;  
	            o.vertex = mul(UNITY_MATRIX_MVP,v.vertex);  
	            return o;  
	        }             
	        fixed4 frag (v2f i) : SV_Target  
	        {  
	            
	       		//绘制多边形顶点
	            for(int j=0;j<PointNum;j++)
	            {
	                if(Dis(i.vertex, Value[j])<3)
	                {
	                    return fixed4(1,0,0,0.5);
	                }
	            }
	            for(int k=0;k<PointNum-1;k++)
	            {
	                if(DrawLineSegment(Value[k],Value[k+1],2,i))
	                {
	                    return fixed4(1,1,0,0.5);
	                }
	            }
	            return fixed4(0,0,0,0);
	          
	        }  
	ENDCG

    SubShader  
    {  
        Tags { "RenderType"="Opaque" }  
        LOD 100  
        Pass  
        {  
            //选取Alpha混合方式  
            Blend  SrcAlpha OneMinusSrcAlpha  
            //在CGPROGRAM代码块中写自己的处理过程  
            CGPROGRAM  
            //定义顶点函数和片段函数的入口分别为vert和frag  
            #pragma vertex vert  
            #pragma fragment frag  
            //包含基本的文件，里面有一些宏定义和基本函数  
            #include "UnityCG.cginc"               
           
            ENDCG  
        }  
    }  
}