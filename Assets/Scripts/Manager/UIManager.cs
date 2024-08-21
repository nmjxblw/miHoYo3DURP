using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UIManager : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> UIList = new List<GameObject>();
    public Dictionary<string, GameObject> UI = new Dictionary<string, GameObject>();
    private static UIManager _instance;
    public static UIManager Instance
    {

        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
            }
            return _instance;
        }
    }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(this.gameObject);
        }
        foreach (var ui in UIList)
        {
            UI.Add(ui.name, ui);
        }
    }

    public void UpdateTraceUIPosition(Transform traceTargetTransform)
    {
        Vector3 targetViewportPoint = Camera.main.WorldToViewportPoint(traceTargetTransform.position);
        Vector3 screenPosition = new Vector3(targetViewportPoint.x * Screen.width, targetViewportPoint.y * Screen.height, 0);

        // 将屏幕坐标转换为 Canvas 的局部坐标
        // Vector2 localPoint;
        // RectTransformUtility.ScreenPointToLocalPointInRectangle(UI["TraceUI"].transform.parent as RectTransform, screenPosition, Camera.main, out localPoint);

        // 设置 UI 元素的位置
        UI["TraceUI"].transform.position = screenPosition;
    }
}
