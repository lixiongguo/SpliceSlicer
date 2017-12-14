//#define TK2D_SLICING_ENABLED

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// Main sprite slicer class, provides static functions to slice sprites
/// </summary>
public static class SpriteSlicer2D 
{
	public static bool DebugLoggingEnabled { get { return s_DebugLoggingEnabled; } set { s_DebugLoggingEnabled = value; } }

	// Enable or disable debug logging
	static bool s_DebugLoggingEnabled = true;

	// Use at your own risk! Likely to produce odd results, depending on the shape
	static bool s_AllowConvexSlicing = false;


#region SLICING_METHODS

	
	public static void SliceAllSprites(Vector3 worldStartPoint, Vector3 worldEndPoint)
	{
		SliceSpritesInternal(worldStartPoint, worldEndPoint, 0,  -1);
	}

	public static void SliceAllSprites(Vector3 worldStartPoint, Vector3 worldEndPoint ,int maxCutDepth)
	{
		SliceSpritesInternal(worldStartPoint, worldEndPoint,  0,  maxCutDepth);
	}

#endregion


	static void SliceSpritesInternal(Vector3 worldStartPoint, Vector3 worldEndPoint,  int spriteInstanceID, int maxCutDepth)
	{
		Vector3 direction = Vector3.Normalize(worldEndPoint - worldStartPoint);
		float length = Vector3.Distance(worldStartPoint, worldEndPoint);
        Debug.DrawRay(worldStartPoint, direction, Color.blue);
		RaycastHit2D[] cutStartResults = Physics2D.RaycastAll(worldStartPoint, direction, length);
		RaycastHit2D[] cutEndResults = Physics2D.RaycastAll(worldEndPoint, -direction, length);

		if(cutStartResults.Length == cutEndResults.Length)
		{
			for(int cutResultIndex = 0; cutResultIndex < cutStartResults.Length ; cutResultIndex++)
			{
				RaycastHit2D cutEnter = cutStartResults[cutResultIndex];
				RaycastHit2D cutExit = cutEndResults[cutEndResults.Length - 1 - cutResultIndex];
                if (cutEnter.transform == cutExit.transform)
				{
                    Transform parentTransform = cutEnter.transform;
                    Vector3 cutStart = new Vector3(worldStartPoint.x, worldStartPoint.y, 0);
                    Vector3 cutEnd = new Vector3(worldEndPoint.x, worldEndPoint.y, 0);
                    CutThiSprite(parentTransform,cutEnter.point,cutExit.point, cutStart, cutEnd);
                }
			}
		}
	}
    static int sliceCnt = 1;
   static void CutThiSprite(Transform parentTransform,Vector3 cutEnterPoint,Vector3 cutExitPoint,Vector3 cutStart, Vector3 cutEnd) {
        if (!parentTransform)
        {
            return;
        }
        SlicedSprite parentSlicedSprite = null;
        SpriteRenderer parentUnitySprite = null;

        parentUnitySprite = parentTransform.GetComponent<SpriteRenderer>();

        if (parentUnitySprite == null)
        {
            parentSlicedSprite = parentTransform.GetComponent<SlicedSprite>();
            if (parentSlicedSprite == null)
                return;
        }

        Vector3 cutEnterLocalPoint = parentTransform.gameObject.transform.InverseTransformPoint(cutEnterPoint);
        Vector3 cutExitLocalPoint = parentTransform.gameObject.transform.InverseTransformPoint(cutExitPoint);

        Collider2D collider2D = parentTransform.gameObject.GetComponent<Collider2D>();
        Vector2[] polygonPoints = GetPolygonPoints(collider2D);

        Vector3 cutStartLocalPoint = parentTransform.gameObject.transform.InverseTransformPoint(cutStart);
        Vector3 cutEndLocalPoint = parentTransform.gameObject.transform.InverseTransformPoint(cutEnd);

        if (polygonPoints != null)
        {
            if (IsPointInsidePolygon(cutStartLocalPoint, polygonPoints) ||
                           IsPointInsidePolygon(cutEndLocalPoint, polygonPoints))
            {
                if (s_DebugLoggingEnabled)
                {
                    Debug.LogWarning("Failed to slice " + parentTransform.gameObject.name + " - start or end cut point is inside the collision mesh");
                }

                return;;
            }

            if (!s_AllowConvexSlicing && !IsConvex(new List<Vector2>(polygonPoints)))
            {
                if (s_DebugLoggingEnabled)
                {
                    Debug.LogWarning("Failed to slice " + parentTransform.gameObject.name + " - original shape is not convex");
                }

                return;
            }

            List<Vector2> childSprite1Vertices = new List<Vector2>();
            List<Vector2> childSprite2Vertices = new List<Vector2>();

            childSprite1Vertices.Add(cutEnterLocalPoint);
            childSprite1Vertices.Add(cutExitLocalPoint);

            childSprite2Vertices.Add(cutEnterLocalPoint);
            childSprite2Vertices.Add(cutExitLocalPoint);

            for (int vertex = 0; vertex < polygonPoints.Length; vertex++)
            {
                Vector2 point = polygonPoints[vertex];
                float determinant = CalculateDeterminant2x3(cutEnterLocalPoint, cutExitLocalPoint, point);
                if (determinant > 0)
                {
                    childSprite1Vertices.Add(point);
                }
                else
                {
                    childSprite2Vertices.Add(point);
                }
            }

            childSprite1Vertices = new List<Vector2>(ArrangeVertices(childSprite1Vertices));
            childSprite2Vertices = new List<Vector2>(ArrangeVertices(childSprite2Vertices));

            if (!AreVerticesAcceptable(childSprite1Vertices) || !AreVerticesAcceptable(childSprite2Vertices))
            {
                return;
            }
            else
            {
                SlicedSprite childSprite1, childSprite2;
                PolygonCollider2D child1Collider, child2Collider;

                float parentArea = Mathf.Abs(Area(polygonPoints));
                CreateChildSprite(parentTransform, childSprite1Vertices, parentArea, out childSprite1, out child1Collider);
                childSprite1.gameObject.name = parentTransform.gameObject.name + "_child1";

                CreateChildSprite(parentTransform, childSprite2Vertices, parentArea, out childSprite2, out child2Collider);
                childSprite2.gameObject.name = parentTransform.gameObject.name + "_child2";

                if (parentSlicedSprite)
                {
                    childSprite1.InitFromSlicedSprite(parentSlicedSprite, child1Collider);
                    childSprite2.InitFromSlicedSprite(parentSlicedSprite, child2Collider);
                }
                else if (parentUnitySprite)
                {
                    childSprite1.InitFromUnitySprite(parentUnitySprite, child1Collider);
                    childSprite2.InitFromUnitySprite(parentUnitySprite, child2Collider);
                }
                SlicedSprite parentSprite = parentTransform.GetComponent<SlicedSprite>();
                if (parentSprite != null)//初始情况为UnitySprite的所以需要判空
                {
                    SpriteSlicerManager.Instance.SpriteList.Remove(parentSprite);
                }
                SpriteSlicerManager.Instance.SpriteList.Add(childSprite1);
                SpriteSlicerManager.Instance.SpriteList.Add(childSprite2);
                SplitSlices(childSprite1.gameObject, childSprite2.gameObject, cutStart, cutEnd);
                GameObject.Destroy(parentTransform.gameObject);
            }
        }
    }
    static void SplitSlices(GameObject obj1, GameObject obj2, Vector3 cutStart, Vector3 cutEnd) {
        Vector3 dir1 = Vector3.Normalize(cutEnd - cutStart);
        Vector3 dir2 = Vector3.Normalize(obj1.transform.position - cutStart);
        Vector3 dir3 = Vector3.Normalize(obj2.transform.position - cutStart);
        Vector3 dir4 = Vector3.Normalize(Vector3.Cross(dir1,Vector3.back)) * 0.1f;

        if (Vector3.Cross(dir1, dir2).z > 0)
        {
            obj1.transform.position += dir4;
        }
        else {
            obj2.transform.position -= dir4;
        }

    }
   static Vector2[] GetPolygonPoints(Collider2D collider2D) {
        Vector2[] polygonPoints = null;
        //PolygonCollider2D polygonCollider = parentRigidBody.GetComponent<PolygonCollider2D>();
        PolygonCollider2D polygonCollider = collider2D as PolygonCollider2D;
        if (polygonCollider)
        {
            polygonPoints = polygonCollider.points;
        }
        else
        {
            //  BoxCollider2D boxCollider = parentRigidBody.GetComponent<BoxCollider2D>();
            BoxCollider2D boxCollider = collider2D as BoxCollider2D;
            if (boxCollider)
            {
                polygonPoints = new Vector2[4];
                polygonPoints[0] = new Vector2(-boxCollider.size.x * 0.5f, -boxCollider.size.y * 0.5f);
                polygonPoints[1] = new Vector2(boxCollider.size.x * 0.5f, -boxCollider.size.y * 0.5f);
                polygonPoints[2] = new Vector2(boxCollider.size.x * 0.5f, boxCollider.size.y * 0.5f);
                polygonPoints[3] = new Vector2(-boxCollider.size.x * 0.5f, boxCollider.size.y * 0.5f);
            }
            else
            {
                /// CircleCollider2D circleCollider = parentRigidBody.GetComponent<CircleCollider2D>();
                CircleCollider2D circleCollider = collider2D as CircleCollider2D;
                if (circleCollider)
                {
                    int numSteps = 32;
                    float angleStepRate = (Mathf.PI * 2) / numSteps;
                    polygonPoints = new Vector2[32];

                    for (int loop = 0; loop < numSteps; loop++)
                    {
                        float angle = angleStepRate * loop;
                        polygonPoints[loop] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * circleCollider.radius;
                    }

                }
            }
        }
        return polygonPoints;
    }

    /// <summary>
    /// Create a child sprite from the given parent sprite, using the provided vertices
    /// </summary>
    static void CreateChildSprite(Transform parentTransform, List<Vector2> spriteVertices, float parentArea, out SlicedSprite slicedSprite, out PolygonCollider2D polygonCollider)
	{
		float childArea = GetArea(spriteVertices);

		GameObject childObject = new GameObject();
		childObject.transform.parent = parentTransform.parent;
		childObject.transform.position = parentTransform.position;
		childObject.transform.rotation = parentTransform.rotation;
		childObject.transform.localScale = parentTransform.localScale;
        childObject.tag = parentTransform.gameObject.tag;
        // Child sprites should inherit the rigid body behaviour of their parents
        //Rigidbody2D childRigidBody = childObject.AddComponent<Rigidbody2D>();
        //childRigidBody.mass = parentRigidBody.mass * (childArea / parentArea);
        //childRigidBody.drag = parentRigidBody.drag;
        //childRigidBody.angularDrag = parentRigidBody.angularDrag;
        //childRigidBody.gravityScale = parentRigidBody.gravityScale;
        //childRigidBody.fixedAngle = parentRigidBody.fixedAngle;
        //childRigidBody.isKinematic = parentRigidBody.isKinematic;
        //childRigidBody.interpolation = parentRigidBody.interpolation;
        //childRigidBody.sleepMode = parentRigidBody.sleepMode;
        //childRigidBody.collisionDetectionMode = parentRigidBody.collisionDetectionMode;
        //childRigidBody.velocity = parentRigidBody.velocity;
        //childRigidBody.angularVelocity = parentRigidBody.angularVelocity;

        polygonCollider = childObject.AddComponent<PolygonCollider2D>();
		polygonCollider.points = spriteVertices.ToArray();
		slicedSprite = childObject.AddComponent<SlicedSprite>();
	}

	#region "HELPER_FUNCTIONS"
	static float CalculateDeterminant2x3(Vector2 start, Vector2 end, Vector2 point) 
	{
		return start.x * end.y + end.x * point.y + point.x * start.y - start.y * end.x - end.y * point.x - point.y * start.x;
	}
	
	public static float CalculateDeterminant2x2(Vector2 vectorA, Vector2 vectorB)
	{
		return vectorA.x * vectorB.y - vectorA.y * vectorB.x;
	}

	// Trianglulate the given polygon
	static int[] Triangulate(Vector2[] points) 
	{
		List<int> indices = new List<int>();
		
		int n = points.Length;
		if (n < 3)
			return indices.ToArray();
		
		int[] V = new int[n];
		
		if (Area(points) > 0)
		{
			for (int v = 0; v < n; v++)
				V[v] = v;
		}
		else 
		{
			for (int v = 0; v < n; v++)
				V[v] = (n - 1) - v;
		}
		
		int nv = n;
		int count = 2 * nv;
		for (int m = 0, v = nv - 1; nv > 2; ) {
			if ((count--) <= 0)
				return indices.ToArray();
			
			int u = v;
			if (nv <= u)
				u = 0;
			v = u + 1;
			if (nv <= v)
				v = 0;
			int w = v + 1;
			if (nv <= w)
				w = 0;
			
			if (Snip(points, u, v, w, nv, V)) 
			{
				int a, b, c, s, t;
				a = V[u];
				b = V[v];
				c = V[w];
				indices.Add(a);
				indices.Add(b);
				indices.Add(c);
				m++;
				for (s = v, t = v + 1; t < nv; s++, t++)
					V[s] = V[t];
				nv--;
				count = 2 * nv;
			}
		}
		
		indices.Reverse();
		return indices.ToArray();
	}

	// Get the area of the given polygon
	static float Area (Vector2[] points) 
	{
		int n = points.Length;
		float A = 0.0f;
		
		for (int p = n - 1, q = 0; q < n; p = q++) 
		{
			Vector2 pval = points[p];
			Vector2 qval = points[q];
			A += pval.x * qval.y - qval.x * pval.y;
		}
		
		return (A * 0.5f);
	}
	
	static bool Snip (Vector2[] points, int u, int v, int w, int n, int[] V) 
	{
		int p;
		Vector2 A = points[V[u]];
		Vector2 B = points[V[v]];
		Vector2 C = points[V[w]];
		
		if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
		{
			return false;
		}
		
		for (p = 0; p < n; p++) 
		{
			if ((p == u) || (p == v) || (p == w))
			{
				continue;
			}
			
			Vector2 P = points[V[p]];
			
			if (InsideTriangle(A, B, C, P))
			{
				return false;
			}
		}
		
		return true;
	}

	// Check if a point is inside a given triangle
	static bool InsideTriangle (Vector2 A, Vector2 B, Vector2 C, Vector2 P) 
	{
		float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
		float cCROSSap, bCROSScp, aCROSSbp;
		
		ax = C.x - B.x; ay = C.y - B.y;
		bx = A.x - C.x; by = A.y - C.y;
		cx = B.x - A.x; cy = B.y - A.y;
		apx = P.x - A.x; apy = P.y - A.y;
		bpx = P.x - B.x; bpy = P.y - B.y;
		cpx = P.x - C.x; cpy = P.y - C.y;
		
		aCROSSbp = ax * bpy - ay * bpx;
		cCROSSap = cx * apy - cy * apx;
		bCROSScp = bx * cpy - by * cpx;
		
		return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
	}
	
	// Helper class to sort vertices in ascending X coordinate order
	public class VectorComparer : IComparer<Vector2>
	{
		public int Compare(Vector2 vectorA, Vector2 vectorB)
		{
			if (vectorA.x > vectorB.x) 
			{
				return 1;
			} 
			else if (vectorA.x < vectorB.x) 
			{
				return -1;
			}
			
			return 0; 
		}
	}
	
	/// <summary>
	/// Sort the vertices into a counter clockwise order
	/// </summary>
	static Vector2[] ArrangeVertices(List<Vector2> vertices)
	{
		float determinant;
		int counterClockWiseIndex = 1;
		int clockWiseIndex = vertices.Count - 1;
		
		Vector2[] sortedVertices = new Vector2[vertices.Count];
		vertices.Sort(new VectorComparer());

		Vector2 startPoint = vertices[0];
		Vector2 endPoint = vertices[vertices.Count - 1];
		sortedVertices[0] = startPoint;
		
		for(int vertex = 1; vertex < vertices.Count - 1; vertex++)
		{
			determinant = CalculateDeterminant2x3(startPoint, endPoint, vertices[vertex]);
			
			if (determinant < 0)
			{
				sortedVertices[counterClockWiseIndex++] = vertices[vertex];
			}
			else 
			{
				sortedVertices[clockWiseIndex--] = vertices[vertex];
			}
		}
		
		sortedVertices[counterClockWiseIndex] = endPoint;
		return sortedVertices;
	}
	
	/// <summary>
	/// Work out the area defined by the vertices
	/// </summary>
	static float GetArea(List<Vector2> vertices)
	{
		// Check that the total area isn't stupidly small
		float area = vertices[0].y * (vertices[vertices.Count-1].x- vertices[1].x);
		
		for(int i = 1; i < vertices.Count; i++)
		{
			area += vertices[i].y * (vertices[i-1].x - vertices[(i+1)% vertices.Count].x);
		}
		
		return Mathf.Abs(area * 0.5f);
	}
	
	/// <summary>
	/// Check if this list of points defines a convex shape
	/// </summary>
	public static bool IsConvex(List<Vector2> vertices)
	{
		float determinant;
		Vector3 v1 = vertices[0] - vertices[vertices.Count-1];
		Vector3 v2 = vertices[1] - vertices[0];
		float referenceDeterminant = CalculateDeterminant2x2(v1, v2);
		
		for (int i=1; i< vertices.Count - 1; i++)
		{
			v1 = v2;
			v2 = vertices[i+1] - vertices[i];
			determinant = CalculateDeterminant2x2(v1, v2);
			
			if (referenceDeterminant * determinant < 0.0f)
			{
				return false;
			}
		}
		
		v1 = v2;
		v2 = vertices[0] - vertices[vertices.Count-1];
		determinant = CalculateDeterminant2x2(v1, v2);
		
		if (referenceDeterminant * determinant < 0.0f)
		{
			return false;
		}
		
		return true;
	}
	
	/// <summary>
	/// Verify if the list of vertices are suitable to create a new 2D collider shape
	/// 
	static bool AreVerticesAcceptable(List<Vector2> vertices)
	{
		// Polygons need to at least have 3 vertices, not be convex, and have a vaguely sensible total area
		if (vertices.Count < 3)
		{
			if(s_DebugLoggingEnabled)
			{
				Debug.LogWarning("Vertices rejected - insufficient vertices");
			}

			return false;
		}
		    
		if(GetArea(vertices) < 0.01f)
		{
			if(s_DebugLoggingEnabled)
			{
				Debug.LogWarning("Vertices rejected - below minimum area");
			}

			return false;
		}

		if(!s_AllowConvexSlicing && !IsConvex(vertices))
		{
			if(s_DebugLoggingEnabled)
			{
				Debug.LogWarning("Vertices rejected - shape is not convex");
			}

			return false;
		}
		
		return true;
	}

	/// <summary>
	/// Use the polygon winding algorithm to check whether a point is inside the given polygon
	/// </summary>
	static bool IsPointInsidePolygon(Vector2 pos, Vector2[] polygonPoints)
	{
		int winding = 0;
		
		for(int vertexIndex = 0; vertexIndex < polygonPoints.Length; vertexIndex++)
		{
			int nextIndex = vertexIndex + 1;
			
			if(nextIndex >= polygonPoints.Length)
			{
				nextIndex = 0;
			}
			
			Vector2 thisPoint = polygonPoints[vertexIndex];
			Vector2 nextPoint = polygonPoints[nextIndex];
			
			if(thisPoint.y <= pos.y)
			{
				if(nextPoint.y > pos.y)
				{
					float isLeft = ((nextPoint.x - thisPoint.x) * (pos.y - thisPoint.y) - (pos.x - thisPoint.x) * (nextPoint.y - thisPoint.y));
					
					if(isLeft > 0)
					{
						winding++;
					}
				}
			}
			else
			{
				if(nextPoint.y <= pos.y)
				{
					float isLeft = ((nextPoint.x - thisPoint.x) * (pos.y - thisPoint.y) - (pos.x - thisPoint.x) * (nextPoint.y - thisPoint.y));
					
					if(isLeft < 0)
					{
						winding--;
					}
				}
			}
		}
		
		return winding != 0;
	}
	#endregion
}


