using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpriteSlicerManager : MonoBehaviour 
{
    enum State {  Rotate,Slice,Drag}
    static SpriteSlicerManager _instance;
    public static SpriteSlicerManager Instance {
        get {
            return _instance;
        }
        set {
            _instance = value;
        }
    }

    public Material mat;
    [HideInInspector]
    public List<SlicedSprite> SpriteList = new List<SlicedSprite>();

    void Awake() {
        _instance = this;
    }
	void Update () 
	{
        UpdateMouseOnSprite();
        if (Input.GetMouseButtonDown(1))
        {
            ClearCursor();
            ClearDrawLine();
        }
        State curState = CheckIconState();
        switch (curState) {
            case State.Slice:
                //Debug.Log("Slice");
                UpdateDrawLine();
                break;
            case State.Drag:
              //  Debug.Log("Drag");//drag的处理逻辑在SpriteSlicer中
                break;
            case State.Rotate:
               // Debug.Log("Rotate");
                UpdateDragRotate();
                break;
        }
        //UpdateDrawLine();
    }
    int currentpointNum = 0;
    const int MAX_POINT_NUM = 100;
    Vector4[] screenPos = new Vector4[MAX_POINT_NUM];
    Vector3[] WorldMousePositions = new Vector3[MAX_POINT_NUM];

    void UpdateDrawLine()
    {
      
        if (Input.GetMouseButtonDown(0))
        {
            //if (mouseOnSprite)
            //{
            //    ClearDrawLine();
            //    return;
            //}
            if (currentpointNum < MAX_POINT_NUM)
            {
                currentpointNum++;
                WorldMousePositions[currentpointNum - 1] = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                AddOnePointAt(currentpointNum-1);
                return;
            }
        }
       
        if(currentpointNum > 0)//Draw active point
        {
            AddOnePointAt(currentpointNum);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            CutThis();
            ClearDrawLine();
        }
    }
    void CutThis()
    {
        if (currentpointNum >= 2)
        {
            Vector3 worldPosStart = WorldMousePositions[currentpointNum - 2];
            Vector3 worldPosEnd = WorldMousePositions[currentpointNum - 1];
            SpriteSlicer2D.SliceAllSprites(worldPosStart, worldPosEnd);
        }
    }
    void ClearDrawLine() {
        currentpointNum = 0;
        mat.SetVectorArray("Value", screenPos);
        mat.SetInt("PointNum", 0);
    }
    void ClearCursor() {
        foreach (SlicedSprite sprite in SpriteList) {
            sprite.ShowIcon = false;
        }
    }
    State CheckIconState() {
        for (int i = 0; i < SpriteList.Count; i++) {
            SlicedSprite sprite = SpriteList[i];
            if (sprite.Is_Now_Dragging)
            {
                return State.Drag;
            }
            else if (sprite.ShowIcon) {
                curRotateSpriteGo = sprite.gameObject;
                return State.Rotate;
            }
        }
        return State.Slice;
    }
    void AddOnePointAt(int index) {
        Vector3 v3 = Input.mousePosition;
        screenPos[index] = new Vector4(v3.x, Screen.height - v3.y, v3.z, 0);
        mat.SetVectorArray("Value", screenPos);
        mat.SetInt("PointNum", index + 1);
    }
    //void UpdateCursor() {
    //    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //    cursorGo.transform.position = mouseOnSprite ? 
    //        new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0) : new Vector3(cursorGo.transform.position.x, cursorGo.transform.position.y, -100);
    //}
    bool mouseOnSprite;
    void UpdateMouseOnSprite() {
        mouseOnSprite = false;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector3.forward, 100);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.transform.gameObject.tag == "sliceObject")
            {
                mouseOnSprite = true;
                break;
            }
        }
    }
    bool start;
    Vector3 startWorld0; //鼠标点击的初始点//
    GameObject curRotateSpriteGo;
    void UpdateDragRotate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //RaycastHit hitStart;
            //if (Physics.Raycast(ray.origin, ray.direction, out hitStart))
            //{
            //      Debug.Log("start : "  + hitStart.point);
            //    startWorld = hitStart.point;
            //    start = true;
            //}
            startWorld0 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            start = true;
        }
        if (Input.GetMouseButton(0) && start)
        {
            //Ray rayRun = Camera.main.ScreenPointToRay(Input.mousePosition);
            //RaycastHit hitRun;
            //if (Physics.Raycast(rayRun.origin, rayRun.direction, out hitRun))
            //{
            //    Debug.DrawLine(startWorld, hitRun.point, Color.blue);
            //}
            Vector3 startWorld = new Vector3(startWorld0.x, startWorld0.y, 0);
            Vector3 runWorld0 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 runWorld = new Vector3(runWorld0.x, runWorld0.y, 0);
            Debug.DrawLine(startWorld, runWorld, Color.blue);
            SlicedSprite curRotateSprit = curRotateSpriteGo.GetComponent<SlicedSprite>();
            Vector3 spriteCenterWorld = curRotateSprit.transform.TransformPoint(curRotateSprit.centerLocal);
            Debug.DrawLine(spriteCenterWorld, startWorld, Color.green);
            Vector3 v1 = startWorld - spriteCenterWorld;
           // Debug.Log(string.Format("start:{0},run:{1},center:{2}",startWorld,runWorld,spriteCenterWorld));
           
            Vector3 v2 = Vector3.back;
            Vector3 v3 = Vector3.Normalize(Vector3.Cross(v1, v2));
            Vector3 v4 = runWorld - startWorld;//hitRun.point - startWorld;
            float f = Vector3.Dot(v3, v4);
            Vector3 v5 = f * v3;
          
            Debug.DrawRay(startWorld, v5, Color.red);
            Vector3 endPoint = startWorld + v5/3;
            Vector3 v6 = endPoint - spriteCenterWorld;
            Debug.DrawLine(spriteCenterWorld, endPoint, Color.black);
            Quaternion q =   Quaternion.FromToRotation(v1,v6);
            GameObject parentGo = curRotateSprit.parentGo;
            parentGo.transform.localRotation *= q;
          //  curRotateSpriteGo.transform.RotateAround(spriteCenterWorld, Vector3.back, q.eulerAngles.z);
            //Debug.Log("drag .... ") ;
        }
        if (Input.GetMouseButtonUp(0))
        {
           //Vector3 endWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
           // Debug.Log("end: " + endWorld);
            start = false;
        }
    }
    //void SetSprite() {
    //    SpriteRenderer cursorSprite = cursorGo.AddComponent<SpriteRenderer>();
    //    Texture2D res = Resources.Load("cursor") as Texture2D;
    //    Sprite spriteRes = Sprite.Create(res,new Rect(0,0,512,512),new Vector2(0.5f,0.5f));
    //    cursorGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
    //    cursorSprite.sprite = spriteRes;
    //}
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest, mat);
    }
}
