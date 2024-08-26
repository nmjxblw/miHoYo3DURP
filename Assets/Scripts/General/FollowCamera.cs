using System;
using System.ComponentModel;
using UnityEngine;


//使用时需要自己调整面片/特效的Position（位于相机正前方）
//调整面片/特效的大小以覆盖全屏
//此脚本用于让面片/特效始终跟随相机，而不必在相机上添加太多累赘的东西。
[ExecuteInEditMode]
public class FollowCamera : MonoBehaviour
{
    public Camera targetCamera; // 插槽接收的外来物体的Transform

    private GameObject emptyObject; // 用于模拟 B 变换的空物体
    private bool successed = false;

    // 记录特效的初始缩放
    // 保证FOV在变化时，面片也会覆盖全屏
    public Vector3 initialScale = Vector3.zero;
    public float initialFocalLength = 13f;
    public float initialFOV = 60f;

    void Start() {
        // 记录特效的初始缩放
        initialFOV = targetCamera.fieldOfView;
    }


    void LateUpdate()
    {
        if (targetCamera != null)
        {
            if (transform.parent && !transform.parent.name.StartsWith("Camera_")) successed = false;
            if (!successed)
            {
                CreateEmptyObject(targetCamera.transform);
            }
            // 将空物体的变换设置为 B 的变换
            if(transform.parent != null)
            {
                transform.parent.position = targetCamera.transform.position;
                transform.parent.rotation = targetCamera.transform.rotation;
            }
            // 根据FOV调整特效的缩放
            if (initialFOV != targetCamera.fieldOfView)
            {
                float scaleValue = 1 / (targetCamera.focalLength / initialFocalLength);
                Vector3 scaleFactor = initialScale * scaleValue; // 60是透视摄像机的默认FOV值
                transform.localScale = new Vector3(scaleFactor.x, scaleFactor.y, transform.localScale.z);
                initialFOV = targetCamera.fieldOfView;
            }
            
        }
    }

    // 创建空物体来模拟 B 的变换
    void CreateEmptyObject(Transform slot)
    {
        string objName = "Camera_" + slot.name;
        if (transform.parent)
        {
            Transform child = transform.parent.Find(objName);
            if (child != null) 
            {
                transform.SetParent(transform.parent);
                successed = true;
            }
            else
            {
                if (transform.parent.name.StartsWith("Camera_")) 
                {
                    successed = true;
                    return;
                }
                emptyObject = new GameObject(objName);
                emptyObject.transform.position = Vector3.zero;
                emptyObject.transform.rotation = Quaternion.identity;
                emptyObject.transform.localScale = Vector3.one;
                emptyObject.transform.SetParent(transform.parent);
                transform.SetParent(emptyObject.transform);
                successed = true;
            }
        }
        else
        {
            emptyObject = new GameObject(objName);
            emptyObject.transform.position = Vector3.zero;
            emptyObject.transform.rotation = Quaternion.identity;
            emptyObject.transform.localScale = Vector3.one;
            emptyObject.transform.SetParent(transform.parent);
            transform.SetParent(emptyObject.transform);
            successed = true;
        }

        
    }
    [ContextMenu("Reset Scale & FocalLength")]
    void ResetScale()
    {
        initialScale = transform.localScale;
        initialFocalLength = targetCamera.focalLength;
    }

}

