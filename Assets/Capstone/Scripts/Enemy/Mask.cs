using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Mask : MonoBehaviour, LivingEntity
{
    public Transform playerTransform;
    public GameObject healthBar; // 몬스터 방향전환시 HP바가 회전하지 않게 하기 위해 받아옴

    [Header("HP")]
    public Image currentHealthBar;
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Ranges")]
    public float detectRange = 6f;
    public float dashRange = 2f;

    [Header("Movement Speeds")]
    public float moveSpeed = 1.5f;
    public float approachSpeed = 2.5f;
    public float dashSpeed = 12f;

    [Header("Patrol Movement")]
    public float amplitude = 0.5f; // 위아래로 움직일 거리
    public float updownSpeed = 2f;       // 움직이는 속도
    private float startY;

    [Header("Reattach Control")]
    public float reattachCooldown = 2f;      // 다시 붙기까지 걸리는 시간
    private float lastDetachTime = -999f;     // 마지막으로 떨어진 시간 기록
    public float minReattachDistance = 6f;   // 플레이어와 이 거리 이상 떨어져야 다시 대쉬 가능
    public float escapeSpeed = 5f;          // 분리 후 도망 속도

    [Header("Think")]
    public int nextThinkTime = 1;
    private int nextMove;

    [Header("Detach Control")]
    private int directionSwitchCount = 0;
    public float directionCheckTime = 2.0f; // 2초 안에 입력해야 함
    private float directionTimer = 0f;
    private int lastDirection = 0;

    private PlayerInput playerInput; // Input System 접근

    private Rigidbody2D rb;
    private Vector2 dashTarget;
    private bool isDashing = false;
    private bool isAttached = false;

    private Animator animator;
    private BTSelector root;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        playerInput = playerTransform.GetComponent<PlayerInput>();
        startY = transform.position.y;
        Invoke("Think", nextThinkTime);
        InitialSet();

        root = new BTSelector();

        // 부착 상태이면 아무것도 안함
        BTSequence attachSeq = new BTSequence();
        attachSeq.AddChild(new BTCondition(() => isAttached));
        attachSeq.AddChild(new BTAction(Attaching));

        // 공격 범위에 들어오면 대쉬
        BTSequence dashSeq = new BTSequence();
        dashSeq.AddChild(new BTCondition(IsInDashRange));
        dashSeq.AddChild(new BTAction(DashToPlayer));

        BTSequence chaseSeq = new BTSequence();
        chaseSeq.AddChild(new BTCondition(IsInDetectRange));
        chaseSeq.AddChild(new BTAction(Chase));

        BTSequence patrolSeq = new BTSequence();
        patrolSeq.AddChild(new BTAction(Patrol));

        root.AddChild(attachSeq);
        root.AddChild(dashSeq);
        root.AddChild(chaseSeq);
        root.AddChild(patrolSeq);
    }

    private void Update()
    {
        root.Evaluate();
        if (healthBar != null)
        {
            healthBar.transform.rotation = Quaternion.identity;

/*            Transform barVisual = healthBar.transform.Find("BarVisual");
            if (barVisual != null)
            {
                Vector3 scale = barVisual.localScale;
                scale.x = Mathf.Abs(scale.x); // 항상 양수
                barVisual.localScale = scale;
            }*/
        }
        if (isAttached)
        {
            HandleDetachInput();
        }
    }
    private void FixedUpdate()
    {
        if(!isAttached)
        {
            transform.position = new Vector3(
                transform.position.x,
                startY + Mathf.Sin(Time.time * updownSpeed) * amplitude,
                transform.position.z
            );
            DetectingGround();
        }
    }

    private bool IsInDetectRange()
    {
        return Vector2.Distance(transform.position, playerTransform.position) <= detectRange;
    }

    private bool IsInDashRange()
    {
        return Vector2.Distance(transform.position, playerTransform.position) <= dashRange;
    }
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

    private BTNodeState Attaching()
    {
        rb.velocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;  // 충돌 제거
        return BTNodeState.Running;
    }

    private BTNodeState Patrol()
    {
        if (isDashing || isAttached) return BTNodeState.Failure;
        rb.velocity = new Vector2(nextMove * moveSpeed, rb.velocity.y);
        return BTNodeState.Running;
    }
    private void DetectingGround()
    {
        Vector2 direction = rb.velocity.normalized;
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, 1f, direction, 1.5f, LayerMask.GetMask("Ground"));

        if (hit.collider != null)
        {
            rb.velocity = -rb.velocity;
        }
    }

    private BTNodeState Chase()
    {
        if (isDashing || isAttached) return BTNodeState.Failure;
        LookAtPlayer();
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);

        return BTNodeState.Running;
    }

    private BTNodeState DashToPlayer()
    {
        if (isDashing || isAttached) 
        { 
            return BTNodeState.Failure; 
        }
        isDashing = true;
        dashTarget = playerTransform.position;
        StartCoroutine(DashRoutine());

        return BTNodeState.Success;
    }

    private IEnumerator DashRoutine()
    {
        Vector2 direction = (dashTarget - (Vector2)transform.position).normalized;

        while (!isAttached)
        {
            rb.velocity = direction * dashSpeed;

            if (!IsInDashRange())
            {
                break; // 공격 범위에서 벗어나면 멈춤
            }

            yield return null;
        }

        isDashing = false;
    }
    //private void OnColliderEnter2D(Collision2D collision)
    //{
    //    Debug.Log(collision.gameObject.name + "On OnTriggerEnter");
    //    if (isDashing && collision.collider.CompareTag("Player"))
    //    {
    //        AttachToPlayer(collision.transform);
    //    }
    //}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("동작중");
        if (isDashing && collision.CompareTag("Player"))
        {
            Debug.Log(collision.gameObject.name + "On OnTriggerEnter");
            AttachToPlayer(collision.transform);
        }
    }
    private void AttachToPlayer(Transform player)
    {
        if (Time.time < lastDetachTime + reattachCooldown)
        {
            Debug.Log("아직 부착할 수 없습니다.");
            return;
        }
        isAttached = true;
        isDashing = false;
        rb.velocity = Vector2.zero;
        animator.SetBool("Attach", true);
        CancelInvoke();
        // 부착 위치 받아오기
        Transform attachPoint = player.GetComponent<Player>()?.GetMaskAttachPoint();

        rb.isKinematic = true;
        rb.simulated = false;

        if (attachPoint != null)
        {
            transform.SetParent(attachPoint);
            transform.localPosition = Vector3.zero; // 부착 지점에 정확히 고정
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("부착 위치가 정의되어 있지 않습니다! 기본 위치에 부착됩니다.");
            transform.SetParent(player);
            transform.localPosition = new Vector3(0, 0, 0);
        }

        Debug.Log("MaskMonster 부착됨");
    }
    private void HandleDetachInput()
    {
        float moveInput = playerInput.actions["Move"].ReadValue<float>();
        int currentDir = Mathf.RoundToInt(moveInput);  // -1, 0, 1 중 하나

        if (currentDir != 0 && currentDir != lastDirection)
        {
            lastDirection = currentDir;
            directionSwitchCount++;
            directionTimer = 0f;
        }

        directionTimer += Time.deltaTime;

        if (directionSwitchCount >= 5)
        {
            Detach();
            ResetDetachInput();
        }

        if (directionTimer > directionCheckTime)
        {
            ResetDetachInput();
        }
    }

    private void ResetDetachInput()
    {
        directionSwitchCount = 0;
        directionTimer = 0f;
        lastDirection = 0;
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
        if (playerTransform == null) return;
        transform.localScale = new Vector3(
            playerTransform.position.x < transform.position.x ? -1 : 1,
            1, 1
        );
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dashRange);
    }

    public void Detach()
    {
        isAttached = false;
        transform.SetParent(null);
        rb.isKinematic = false;
        rb.simulated = true;
        GetComponent<Collider2D>().enabled = true;
        animator.SetBool("Attach", false);

        lastDetachTime = Time.time; // 떨어진 시간 저장

        StopAllCoroutines(); // 혹시 다른 이동 중이면 중단
        StartCoroutine(EscapeFromPlayer());
        Invoke("Think", nextThinkTime);
    }
    private IEnumerator EscapeFromPlayer()
    {
        if (playerTransform == null) yield break;

        Vector2 dir = (transform.position - playerTransform.position).normalized; // 플레이어 반대 방향
        rb.velocity = dir * escapeSpeed;

        // 거리가 충분히 멀어질 때까지 계속 이동
        while (Vector2.Distance(transform.position, playerTransform.position) < minReattachDistance)
        {
            yield return null;
        }

        // 충분히 멀어지면 멈춤
        rb.velocity = Vector2.zero;
    }
}