using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject target;
    public float damage;
    public float speed;
    public float destroyTime;

    public bool homing = false;
    public float rotateSpeed = 200f;

    private Rigidbody2D rigid;

    public virtual void Initialize(GameObject target, float damage, float speed, float destroyTime, bool homing)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
        this.destroyTime = destroyTime;
        this.homing = homing;
    }

    private void Start()
    {
        rigid = GetComponent<Rigidbody2D>();

        LaunchMethod();
    }

    protected virtual void LaunchMethod()
    {
        

        rigid.velocity = transform.right * speed;
        Destroy(gameObject, destroyTime);
    }

    private void FixedUpdate()
    {
        if(homing)
        {
            Vector2 direction = (target.transform.position - transform.position).normalized;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            float newAngle = Mathf.LerpAngle(rigid.rotation, angle, rotateSpeed * Time.fixedDeltaTime);

            rigid.MoveRotation(newAngle);

            rigid.velocity = -transform.right * speed;
        }
    }
}
