using System.Collections;
using UnityEngine;
using TMPro;

public class FloatingDamage : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public CanvasGroup canvasGroup; // Inspector에서 연결
    public float floatSpeed = 1.0f;
    public float lifetime = 1.0f;
    public float startScale = 1.0f;
    public float critScale = 1.4f;

    private Coroutine playCoro;

    // 초기화(매니저에서 호출)
    public void Initialize(string text, Color color, Vector3 worldStartPos, float duration = 1f, bool isCrit = false, bool faceCamera = false)
    {
        if (textMesh != null) textMesh.text = text;
        if (textMesh != null) textMesh.color = color;
        lifetime = duration;
        transform.position = worldStartPos;
        transform.localScale = Vector3.one * (isCrit ? critScale : startScale);

        if (playCoro != null) StopCoroutine(playCoro);
        playCoro = StartCoroutine(PlayRoutine(faceCamera));
    }

    private IEnumerator PlayRoutine(bool faceCamera)
    {
        float timer = 0f;
        Vector3 startPos = transform.position;

        while (timer < lifetime)
        {
            float dt = Time.deltaTime;
            timer += dt;

            // 위로 이동 (월드 좌표 기준)
            transform.position = startPos + Vector3.up * (floatSpeed * timer);

            // 스케일(초기 -> 1) 조금 부드럽게
            float tScale = Mathf.SmoothStep(transform.localScale.x, 1f, dt * 5f);
            transform.localScale = Vector3.one * tScale;

            // 페이드 아웃
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
            else if (textMesh != null)
            {
                Color c = textMesh.color;
                c.a = Mathf.Lerp(1f, 0f, timer / lifetime);
                textMesh.color = c;
            }

            // 카메라 바라보기(월드캔버스에서 UI가 카메라 방향 봐야 할 때)
            if (faceCamera && Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            }

            yield return null;
        }

        // 끝나면 비활성화(혹은 Destroy)
        gameObject.SetActive(false); // 풀 사용 시 재사용 가능
    }
}