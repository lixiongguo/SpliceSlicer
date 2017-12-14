using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AutoDrawLine : MonoBehaviour {

    public GameObject spriteObj;
    float lastTimeUpdate;
    List<Vector3> positions = new List<Vector3>();

    private LineRenderer line;

    void Start() {
        line = gameObject.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Particles/Additive"));
        line.SetVertexCount(0);
        line.SetWidth(0.5f, 0.5f);
        line.SetColors(Color.green, Color.green);
        line.useWorldSpace = true;

        line.sortingLayerName = "Ignore Raycast";
        line.sortingOrder = 999;
    }
    void Update () {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 oriPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 pinPos = new Vector3(oriPos.x, oriPos.y, 0);
            GameObject obj = GameObject.Instantiate(spriteObj);
            obj.transform.position = pinPos;
            positions.Add(pinPos);
            line.SetVertexCount(positions.Count);
            line.SetPositions(positions.ToArray());
        }
        
    }
}
