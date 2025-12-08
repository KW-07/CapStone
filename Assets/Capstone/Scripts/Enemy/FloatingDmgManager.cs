using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingDmgManager : MonoBehaviour
{
    public static FloatingDmgManager Instance { get; private set; }

    [Header("Prefab & Canvas")]
    public GameObject floatingTextPrefab; // FloatingText.prefab
    public Canvas worldCanvas;            // World Space Canvas (optional)
    public Canvas uiCanvas;               // Screen Space Canvas (optional)
    public Camera uiCamera;               // Screen Space - Camera 일 때 할당 (optional)

    [Header("Default Settings")]
    public float defaultDuration = 1.0f;
    public Color defaultColor = Color.white;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 월드 좌표에 플로팅 텍스트 표시 (월드캔버스 사용)
    /// </summary>
    public void ShowFloatingTextAtWorld(float damage, Vector3 worldPos, Color? color = null, float duration = -1f, bool isCrit = false)
    {
        if (floatingTextPrefab == null) return;
        if (duration <= 0) duration = defaultDuration;
        Color col = color ?? defaultColor;

        // 월드 캔버스가 있으면 그 하위에 생성(월드 캔버스가 없으면 그냥 Scene에 생성)
        Transform parent = (worldCanvas != null) ? worldCanvas.transform : null;
        GameObject obj = Instantiate(floatingTextPrefab, worldPos, Quaternion.identity, parent);
        obj.SetActive(true);

        var ft = obj.GetComponentInChildren<FloatingDamage>();
        if (ft != null)
        {
            ft.Initialize(damage.ToString("0"), col, worldPos, duration, isCrit, faceCamera: true);
        }
    }

    /// <summary>
    /// Screen Space (UI 캔버스)용: worldPos -> 캔버스 좌표로 변환 후 표시
    /// </summary>
    public void ShowFloatingTextAtScreen(float damage, Vector3 worldPos, Color? color = null, float duration = -1f, bool isCrit = false)
    {
        if (floatingTextPrefab == null || uiCanvas == null) return;
        if (duration <= 0) duration = defaultDuration;
        Color col = color ?? defaultColor;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos);
        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out localPoint);

        GameObject obj = Instantiate(floatingTextPrefab, uiCanvas.transform);
        obj.SetActive(true);

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = localPoint;

        var ft = obj.GetComponentInChildren<FloatingDamage>();
        if (ft != null)
        {
            // Screen space: faceCamera = false
            ft.Initialize(damage.ToString("0"), col, obj.transform.position, duration, isCrit, faceCamera: false);
        }
    }
}
