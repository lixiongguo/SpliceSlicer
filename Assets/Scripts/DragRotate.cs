using UnityEngine;
using System.Collections;

public class DragRotate : MonoBehaviour {

    public GameObject sprite;
    bool start;
    private Vector3 mouseStart; //鼠标点击的初始点//
    void Update () {
        if (Input.GetMouseButtonDown(0)){
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitStart;
            if (Physics.Raycast(ray.origin, ray.direction, out hitStart))
            {
              //  Debug.Log("start : "  + hitStart.point);
                mouseStart = hitStart.point;
                start = true;
            }
        }
        if (Input.GetMouseButton(0) && start)
        {
            Ray rayRun = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitRun;
            if (Physics.Raycast(rayRun.origin, rayRun.direction, out hitRun))
            {
                Debug.DrawLine(mouseStart,hitRun.point, Color.blue);
            }
            Debug.DrawLine(sprite.transform.position, mouseStart, Color.green);
            Vector3 v1 = mouseStart - sprite.transform.position;
            Vector3 v2 = Vector3.back;
            Vector3 v3 = Vector3.Normalize(Vector3.Cross(v1,v2));
            Vector3 v4 = hitRun.point - mouseStart;
            float f = Vector3.Dot(v3,v4);
            Vector3 v5 = f * v3;
            Debug.DrawRay(mouseStart, v5, Color.red);
            Vector3 endPoint = mouseStart + v5;
            Vector3 v6 = endPoint - transform.position;
            Debug.DrawLine(sprite.transform.position,endPoint,Color.black);

            Quaternion q = Quaternion.LookRotation(Vector3.back,v6);
            sprite.transform.localRotation = q;
          //  Debug.Log("drag .... ") ;
        }
        if (Input.GetMouseButtonUp(0))
        {
            //Debug.Log("end " + Input.mousePosition);
            start = false;
        }
    }
}
