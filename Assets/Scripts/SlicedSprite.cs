using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Simple class that takes a sprite and a polygon collider, and creates
/// a render mesh that exactly fits the collider.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SlicedSprite : MonoBehaviour
{

    enum STEP
    {

        NONE = -1,

        IDLE = 0,       // 未得到正解
        DRAGING,        // 拖动中
        FINISHED,       // 放置到了正解位置（已无法再被拖动）
        RESTART,        // 重新开始
        SNAPPING,       // 吸附过程中

        NUM,
    };

    public MeshRenderer MeshRenderer { get { return m_MeshRenderer; } }
    public Vector2 MinCoords { get { return m_MinCoords; } }
    public Vector2 MaxCoords { get { return m_MaxCoords; } }
    public Bounds SpriteBounds { get { return m_SpriteBounds; } }
    public int ParentInstanceID { get { return m_ParentInstanceID; } }
    public int CutsSinceParentObject { get { return m_CutsSinceParentObject; } }
    public bool Rotated { get { return m_Rotated; } }
    public bool HFlipped { get { return m_HFlipped; } }
    public bool VFlipped { get { return m_VFlipped; } }
    public Vector3 centerLocal;
    public GameObject cursorGo;

    MeshRenderer m_MeshRenderer;
    MeshFilter m_MeshFilter;

    Vector2 m_MinCoords;
    Vector2 m_MaxCoords;
    Bounds m_SpriteBounds;
    int m_ParentInstanceID;
    int m_CutsSinceParentObject;
    bool m_Rotated;
    bool m_VFlipped;
    bool m_HFlipped;

    static List<Material> s_MaterialList = new List<Material>();
    public GameObject parentGo;
    /// <summary>
    /// Sliced sprites share materials wherever possible in order to ensure that dynamic batching is maintained when
    /// eg. slicing lots of sprites that share the same sprite sheet. If you want to clear out this list 
    /// (eg. on transitioning to a new scene) then simply call this function
    /// </summary>
    public static void ClearMaterials()
    {
        s_MaterialList.Clear();
    }
    bool is_now_dragging;
    public bool Is_Now_Dragging {
        get {
           return is_now_dragging;
        }
        set {
            is_now_dragging = value;
        }
    }
    STEP step = STEP.NONE;
    STEP next_step = STEP.NONE;
    bool is_snap;
    float lastMouseDownTime;
    bool showIcon;
    public bool ShowIcon {
        get {
            return showIcon;
        }
        set {
            showIcon = value;
            if (showIcon)
            {
                cursorGo.transform.localPosition = centerLocal;
                parentGo.transform.position = cursorGo.transform.position;
            }
            else {
                cursorGo.transform.localPosition = hidPos;
            }
        }
    }
    
    void OnMouseDown()
    {
        is_now_dragging = true;
        if (Time.realtimeSinceStartup - lastMouseDownTime < 0.5f) {
            ShowIcon = true;
        }
        lastMouseDownTime = Time.realtimeSinceStartup;
        
    }
    void OnMouseUp()
    {
        is_now_dragging = false;
    }
    Vector3 finished_position, start_position;
    void Awake()
    {
        // 记录下初始位置＝正解位置
        this.finished_position = this.transform.position;

        // 初始化
        // 后续将使用移动后的位置来替换
        this.start_position = this.finished_position;
    }
    void Update() {

        Color color = Color.white;
        switch (this.step) {
            case STEP.NONE:
                {
                    this.next_step = STEP.RESTART;
                }
                break;
            case STEP.IDLE:
                {
                    if (this.is_now_dragging)
                    {
                        this.next_step = STEP.DRAGING;
                    }
                }
                break;
            case STEP.DRAGING:
                {
                    if (!this.is_now_dragging)  // 松开按键时
                    {
                        this.next_step = STEP.IDLE;
                    }
                }
                break;
            case STEP.SNAPPING:
                {
                }
                break;
          }

        // 状态迁移时的初始化处理

        while (this.next_step != STEP.NONE)
        {

            this.step = this.next_step;

            this.next_step = STEP.NONE;

            switch (this.step)
            {

                case STEP.IDLE:
                    {
                    }
                    break;

                case STEP.RESTART:
                    {
                        this.next_step = STEP.IDLE;
                    }
                    break;

                case STEP.DRAGING:
                    {
                        this.begin_dragging();
                    }
                    break;

                case STEP.FINISHED:
                    {
                      

                    }
                    break;
            }
        }
        // 状态处理
        switch (this.step)
        {

            case STEP.DRAGING:
                {
                    this.do_dragging();
                }
                break;

            case STEP.SNAPPING:
                {
                  
                }
                break;
        }
        this.GetComponent<Renderer>().material.color = color;

       
    }
    bool is_in_snap_range()
    {
        bool ret = false;
        float SNAP_DISTANCE = 0.5f;
        if (Vector3.Distance(this.transform.position, this.finished_position) < SNAP_DISTANCE)
        {

            ret = true;
        }

        return (ret);
    }
    Vector3 grab_offset;
    private void begin_dragging()
    {
        Vector3 world_position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        this.grab_offset = this.transform.position - world_position;
    }
    private void do_dragging()
    {
        Vector3 world_position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 offset = (world_position + this.grab_offset) - transform.position;
        this.parentGo.transform.position += offset;
    }

    
    /// <summary>
    /// Initialise this sliced sprite using an existing SlicedSprite
    /// </summary>
    public void InitFromSlicedSprite(SlicedSprite slicedSprite, PolygonCollider2D polygon)
    {
        InitSprite(slicedSprite.gameObject, polygon, slicedSprite.MinCoords, slicedSprite.MaxCoords, slicedSprite.SpriteBounds, slicedSprite.m_MeshRenderer.sharedMaterial, slicedSprite.Rotated, slicedSprite.HFlipped, slicedSprite.VFlipped);
        m_ParentInstanceID = slicedSprite.GetInstanceID();
        m_CutsSinceParentObject = slicedSprite.CutsSinceParentObject + 1;
    }

    /// <summary>
    /// Initialise using a unity sprite
    /// </summary>
    public void InitFromUnitySprite(SpriteRenderer unitySprite, PolygonCollider2D polygon)
    {
        Material material = null;

        for (int loop = 0; loop < s_MaterialList.Count; loop++)
        {
            if (s_MaterialList[loop].mainTexture.GetInstanceID() == unitySprite.sprite.texture.GetInstanceID())
            {
                material = s_MaterialList[loop];
            }
        }

        if (material == null)
        {
            material = new Material(unitySprite.material.shader);
            material.mainTexture = unitySprite.sprite.texture;

            //material.SetTexture(0, unitySprite.sprite.texture);
            material.name = unitySprite.name + "_sliced";
            s_MaterialList.Add(material);
        }

        Rect textureRect = unitySprite.sprite.textureRect;
        Vector2 minTextureCoords = new Vector2(textureRect.xMin / (float)unitySprite.sprite.texture.width, textureRect.yMin / (float)unitySprite.sprite.texture.height);
        Vector2 maxTextureCoords = new Vector2(textureRect.xMax / (float)unitySprite.sprite.texture.width, textureRect.yMax / (float)unitySprite.sprite.texture.height);

        InitSprite(unitySprite.gameObject, polygon, minTextureCoords, maxTextureCoords, unitySprite.sprite.bounds, material, false, false, false);
        m_ParentInstanceID = unitySprite.gameObject.GetInstanceID();
    }
    
    /// <summary>
    /// Initialise this sprite using the given polygon definition
    /// </summary>
    void InitSprite(GameObject parentObject, PolygonCollider2D polygon, Vector3 minCoords, Vector3 maxCoords, Bounds spriteBounds, Material material, bool rotated, bool hFlipped, bool vFlipped)
    {
        m_MinCoords = minCoords;
        m_MaxCoords = maxCoords;
        m_SpriteBounds = spriteBounds;
        m_VFlipped = vFlipped;
        m_HFlipped = hFlipped;
        m_Rotated = rotated;
        m_SpriteBounds = spriteBounds;

        Mesh spriteMesh = new Mesh();
        spriteMesh.name = "SlicedSpriteMesh";

        m_MeshFilter = GetComponent<MeshFilter>();
        m_MeshFilter.mesh = spriteMesh;

        int numVertices = polygon.points.Length;

        Vector3[] vertices = new Vector3[numVertices];
        Color[] colors = new Color[numVertices];
        Vector2[] uvs = new Vector2[numVertices];
        int[] triangles = new int[numVertices * 3];

        // Convert vector2 -> vector3
        for (int loop = 0; loop < vertices.Length; loop++)
        {
            vertices[loop] = polygon.points[loop];
            colors[loop] = Color.white;
        }

        Vector2 uvWidth = maxCoords - minCoords;

        for (int vertexIndex = 0; vertexIndex < numVertices; vertexIndex++)
        {
            float widthFraction = 0.5f + (polygon.points[vertexIndex].x / spriteBounds.size.x);
            float heightFraction = 0.5f + (polygon.points[vertexIndex].y / spriteBounds.size.y);

            if (hFlipped)
            {
                widthFraction = 1.0f - widthFraction;
            }

            if (vFlipped)
            {
                heightFraction = 1.0f - heightFraction;
            }

            Vector2 texCoords = new Vector2();

            if (rotated)
            {
                texCoords.y = maxCoords.y - (uvWidth.y * (1.0f - widthFraction));
                texCoords.x = minCoords.x + (uvWidth.x * heightFraction);
            }
            else
            {
                texCoords.x = minCoords.x + (uvWidth.x * widthFraction);
                texCoords.y = minCoords.y + (uvWidth.y * heightFraction);
            }

            uvs[vertexIndex] = texCoords;
        }

        int triangleIndex = 0;

        for (int vertexIndex = 1; vertexIndex < numVertices - 1; vertexIndex++)
        {
            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = vertexIndex;
            triangles[triangleIndex++] = vertexIndex + 1;
        }

        spriteMesh.Clear();
        spriteMesh.vertices = vertices;
        spriteMesh.uv = uvs;
        spriteMesh.triangles = triangles;
        spriteMesh.colors = colors;
        spriteMesh.RecalculateBounds();
        spriteMesh.RecalculateNormals();

        m_MeshRenderer = GetComponent<MeshRenderer>();
        m_MeshRenderer.material = material;
        m_MeshRenderer.sortingLayerID = parentObject.GetComponent<Renderer>().sortingLayerID;
        m_MeshRenderer.sortingOrder = parentObject.GetComponent<Renderer>().sortingOrder;
        CalculateCenter(polygon);
        SetSprite();
        GameObject forPivot = new GameObject(gameObject.name + " for pivot");
        forPivot.transform.position = transform.TransformPoint(centerLocal);
        transform.SetParent(forPivot.transform);
        parentGo = forPivot;
    }
    void CalculateCenter(PolygonCollider2D polygon) {
        centerLocal = Vector3.zero;
        if (polygon != null)
        {
            foreach (Vector2 point in polygon.points)
            {
                Vector3 v3 = new Vector3(point.x, point.y, 0);
                centerLocal += v3;
            }
            centerLocal /= polygon.points.Length;
        }
    }
    Vector3 hidPos = Vector3.right * 100;
    void SetSprite() {
        cursorGo = new GameObject();
        SpriteRenderer cursorSprite = cursorGo.AddComponent<SpriteRenderer>();
        Texture2D res = Resources.Load("cursor") as Texture2D;
        Sprite spriteRes = Sprite.Create(res, new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f));
        cursorGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        cursorGo.transform.SetParent(transform);
        cursorSprite.sprite = spriteRes;
        cursorSprite.transform.localPosition = hidPos;

        
    }
}