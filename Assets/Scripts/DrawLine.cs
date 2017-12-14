using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//[RequireComponent(typeof(LineRenderer))]  
public class DrawLine : MonoBehaviour
{
    private LineRenderer line;
    private bool isMousePressed;
    private List<Vector3> pointsList;
    private Vector3 mousePos;
    //private Vector3 m_DownCamPos;  
    //private Vector3 m_mouseDownStartPos;  
    //主相机节点下  
    // Structure for line points  
    struct myLine
    {
        public Vector3 StartPoint;
        public Vector3 EndPoint;
    };
    //  -----------------------------------   
    void Awake()
    {
        _Init();
    }
    bool m_init = false;
    private void _Init()
    {
        if (m_init) return;
        m_init = true;
        line = gameObject.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Particles/Additive"));
        line.SetVertexCount(0);
        line.SetWidth(0.5f, 0.5f);
        line.SetColors(Color.green, Color.green);
        line.useWorldSpace = true;

        line.sortingLayerName = "Ignore Raycast";
        line.sortingOrder = 999;
       
    }
    
    

}