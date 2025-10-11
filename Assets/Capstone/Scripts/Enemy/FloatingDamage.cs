using UnityEngine;
using TMPro;

public class FloatingDamage : MonoBehaviour
{
    public TextMeshProUGUI damageText;
    public float floatSpeed = 2f;
    public float lifetime = 1f;

    private Vector3 moveDirection = new Vector3(0, 1, 0);

    public void SetText(float damage)
    {
        damageText.text = damage.ToString("0");
    }

    private void Update()
    {
        transform.position += moveDirection * floatSpeed * Time.deltaTime;
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}