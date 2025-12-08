using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Daru : MonoBehaviour, LivingEntity
{
    public Transform playerTransform;
    public GameObject healthBar; // 몬스터 방향전환시 HP바가 회전하지 않게 하기 위해 받아옴

    [Header("HP")]
    public Image currentHealthBar;
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Range")]
    public float attackRange = 1.5f;
    public float detectionRange = 5.0f;

    [Header("MoveSpeed")]
    public float moveSpeed = 2.0f;

    [Header("Attack")]
    public Transform attackPoint;
    public Vector2 attackBoxSize;
    public float damage = 10f;
    public float attackCooldown = 2.0f;
    private float nextAttackTime = 0f;

    [Header("Think")]
    public int nextThinkTime = 3;
    private int nextMove;

    private Animator animator;
    private Rigidbody2D rb;
    private BTSelector root;
    private void Awake()
    {
        InitialSet();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // 안전한 플레이어 참조 획득 (파괴된 경우를 대비)
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        playerTransform = playerObj != null ? playerObj.transform : null; 
        
        Invoke("Think", nextThinkTime);


        root = new BTSelector();

        BTSequence attackSequence = new BTSequence();
        attackSequence.AddChild(new BTCondition(IsPlayerInRange));
        attackSequence.AddChild(new BTCondition(CanAttack));
        attackSequence.AddChild(new BTAction(Attack));

        BTSequence chaseSequence = new BTSequence();
        chaseSequence.AddChild(new BTCondition(IsPlayerDetected));
        chaseSequence.AddChild(new BTAction(Chase));

        BTSequence patrolSequence = new BTSequence();
        patrolSequence.AddChild(new BTAction(Patrol));

        root.AddChild(attackSequence);
        root.AddChild(chaseSequence);
        root.AddChild(patrolSequence);
    }

    private void Update()
    {
        if (root != null)
            root.Evaluate();

        if (healthBar != null)
        {
            healthBar.transform.rotation = Quaternion.identity;

            /*        Transform barVisual = healthBar.transform.Find("BarVisual");
                    if (barVisual != null)
                    {
                        Vector3 scale = barVisual.localScale;
                        scale.x = Mathf.Abs(scale.x); // 항상 양수
                        barVisual.localScale = scale;
                    }*/
        }
    }
    private void FixedUpdate()
    {
        // rb가 없을 경우 안전하게 획득 또는 스킵
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        Vector2 frontVec = new Vector2(rb.position.x + nextMove * 0.5f, rb.position.y);
        RaycastHit2D rayHit = Physics2D.Raycast(frontVec, Vector3.down, 1, LayerMask.GetMask("Ground"));
        if (rayHit.collider == null)
        {
            nextMove *= -1;
            float yRotation = nextMove == -1 ? 180f : 0f;
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            CancelInvoke();
            Invoke("Think", nextThinkTime);
        }
    }
    private float Get2DDistance(Vector3 a, Vector3 b)
    {
        a.z = 0;
        b.z = 0;
        return Vector3.Distance(a, b);
    }

    // 플레이어 참조가 null이면 재탐색을 시도합니다. 성공하면 true, 실패하면 false 반환.
    private bool EnsurePlayerTransform()
    {
        if (playerTransform != null) return true;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            return true;
        }

        return false;
    }

    private bool IsPlayerDetected()
    {
        if (!EnsurePlayerTransform()) return false;
        float dist = Get2DDistance(transform.position, playerTransform.position);
        return dist <= detectionRange;
    }

    private bool IsPlayerInRange()
    {
        if (!EnsurePlayerTransform()) return false;
        float dist = Get2DDistance(transform.position, playerTransform.position);
        return dist <= attackRange;
    }

    // private bool IsPlayerInRange() => Vector3.Distance(transform.position, playerTransform.position) <= attackRange;
    // private bool IsPlayerDetected() => Vector3.Distance(transform.position, playerTransform.position) <= detectionRange;
    private bool CanAttack() => Time.time >= nextAttackTime;
    private void SetNextAttackTime() => nextAttackTime = Time.time + attackCooldown;
    public void InitialSet()
    {
        currentHealth = maxHealth;
    }

    public void CheckHp()
    {
        if (currentHealthBar != null)
            currentHealthBar.fillAmount = currentHealth / maxHealth;
        Debug.Log($"체력바 갱신 fillAmount : {currentHealthBar.fillAmount}");
    }
    #region attack
    private BTNodeState Attack()
    {
        LookAtPlayer();
        animator.SetTrigger("Attack");
        SetNextAttackTime();
        return BTNodeState.Success;
    }

    private void PerformAttack() //애니메이션에서 호출중
    {
        Collider2D[] hitTargets = Physics2D.OverlapBoxAll(attackPoint.position, attackBoxSize, 0f);
        foreach (var target in hitTargets)
        {
            if (target.CompareTag("Player"))
            {
                Debug.Log("Hit Player!");
                target.GetComponent<LivingEntity>().OnDamage(damage);
            }
        }
    }
    #endregion

    private BTNodeState Chase()
    {
        if (!EnsurePlayerTransform()) return BTNodeState.Failure;

        if (IsPlayerInRange()) return BTNodeState.Failure;
        LookAtPlayer();
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
        return BTNodeState.Running;
    }
    private BTNodeState Patrol()
    {
        if (IsPlayerDetected())
        {
            return BTNodeState.Failure;  // 플레이어가 감지되면 순찰을 멈춘다.
        }
        rb.velocity = new Vector2(nextMove * moveSpeed, rb.velocity.y);
        return BTNodeState.Running;
    }
    private void Think()
    {
        nextMove = Random.Range(-1, 2);
        animator.SetInteger("Think", nextMove);
        if (nextMove != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = nextMove == -1 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        Invoke("Think", nextThinkTime);
    }
    /*    private void Think()
        {
            nextMove = Random.Range(-1, 2);
            animator.SetInteger("Think", nextMove);
            //Debug.Log(nextMove);
            if (nextMove != 0)
            {
                float yRotation = nextMove == -1 ? 180f : 0f;
                transform.rotation = Quaternion.Euler(0, yRotation, 0);
            }
            Invoke("Think", nextThinkTime);
        }*/
    public void OnDamage(float damage)
    {
        currentHealth -= damage;
        animator.SetTrigger("Hit");
        CheckHp();
        Debug.Log(gameObject.name + " took damage! Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            animator.SetTrigger("Die");
        }
    }

    private void Die()
    {
        Debug.Log("Monster is Dead!");
        CancelInvoke();
        rb.velocity = Vector2.zero;  // 움직임 정지
        GetComponent<Collider2D>().enabled = false;  // 충돌 제거
        Destroy(this.gameObject);
    }
    private void LookAtPlayer()
    {
        // 널 체크: 호출부에서 EnsurePlayerTransform을 사용하거나 이 함수 자체에서 처리
        if (playerTransform == null)
        {
            if (!EnsurePlayerTransform()) return;
        }
        bool lookLeft = playerTransform.position.x < transform.position.x;
        Vector3 scale = transform.localScale;
        scale.x = lookLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
    /*    private void LookAtPlayer()
        {
            if (playerTransform == null) return;

            bool lookLeft = playerTransform.position.x < transform.position.x;
            transform.rotation = Quaternion.Euler(0, lookLeft ? 180f : 0f, 0);
        }*/
    private void OnDrawGizmos()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attackPoint.position, attackBoxSize);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
