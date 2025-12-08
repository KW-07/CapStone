using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossDaru : MonoBehaviour, LivingEntity
{
    public Transform playerTransform;

    [Header("HP")]
    public Image currentHealthBar;
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Range")]
    public float attackRange = 1.5f;    // 공격 가능 거리
    public float detectionRange = 5.0f; // 플레이어 감지 거리

    [Header("Move")]
    public float moveSpeed = 2.0f;

    [Header("PatrolJump")]  // 이동 중 점프 관련 조정
    public float jumpForce = 5.0f;
    public float jumpInterval = 2.0f;
    private float nextJumpTime = 0f;

    [Header("AttackBase")]
    public Transform attackPoint;
    public float attackCooldown = 2.0f;
    private float nextAttackTime = 0f;
    private bool isattack = false;

    [Header("Attack1")]
    public Vector2 attack1BoxSize;
    public float attack1damage = 10f;
    public float attackDelay = 1.0f;  // 공격 1의 대기 시간

    [Header("Attack2")]
    public Transform SlamPoint;
    public Vector2 attack2BoxSize;    // 공격 범위
    public float attack2damage = 10f;
    public float jumpHeight = 3.0f;      // 점프 높이
    public float slamSpeed = 10.0f;      // 내려찍기 속도
    public float groundCheckDistance = 1f;  // 지면 감지 거리
    private bool isSlamming = false;     // 내려찍기 상태 여부
    private bool isJumping = false;      // 점프 상태 여부

    [Header("Attack3")]
    public Transform leftHorn;
    public Transform rightHorn;
    public GameObject fireballPrefab;
    public float fireballSpeed = 5.0f;
    public float fireballAngle = 45.0f;
    public int fireballCount = 3;           // 발사할 불꽃 개수
    public float fireballInterval = 0.2f;

    [Header("Teleport")]
    public float teleportHeight = 10f;   // 플레이어 머리 위로 어느정도 뜰 것인지
    public float teleportCooldown = 5.0f; // 텔레포트 재사용 대기시간
    private float nextTeleportTime = 0f;

    public int nextMoveTime = 3;
    private int nextMove;

    private Rigidbody2D rb;
    private BTSelector root;
    private Animator animator;

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

        Invoke("Think", nextMoveTime);

        root = new BTSelector();

        BTSequence attackSequence = new BTSequence();
        BTSequence chaseSequence = new BTSequence();
        BTSequence patrolSequence = new BTSequence();

        BTRandomSelector normalAttackSelector = new BTRandomSelector();

        BTAction attack1 = new BTAction(Attack1);
        BTAction attack2 = new BTAction(Attack2);

        BTAction chaseAction = new BTAction(Chase);
        BTAction patrolAction = new BTAction(Patrol);

        BTCondition playerInRange = new BTCondition(IsPlayerInRange);
        BTCondition playerDetected = new BTCondition(IsPlayerDetected);

        BTCondition canAttack = new BTCondition(CanAttack);
        BTCondition canTeleport = new BTCondition(CanTeleport);

        normalAttackSelector.AddChild(attack1);
        normalAttackSelector.AddChild(attack2);

        attackSequence.AddChild(playerInRange);
        attackSequence.AddChild(canAttack);
        attackSequence.AddChild(normalAttackSelector);

        chaseSequence.AddChild(playerDetected);
        chaseSequence.AddChild(chaseAction);

        patrolSequence.AddChild(patrolAction);

        root.AddChild(attackSequence);
        root.AddChild(chaseSequence);
        root.AddChild(patrolSequence);
    }

    private void Update()
    {
        // root가 없을 경우 안전하게 스킵
        if (root != null)
            root.Evaluate();
    }
    private float Get2DDistance(Vector3 a, Vector3 b) // 아오 z값
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
        //Debug.Log($"IsPlayerDetected? Distance: {dist} / DetectionRange: {detectionRange} / Result: {dist <= detectionRange}");
        return dist <= detectionRange;
    }

    private bool IsPlayerInRange()
    {
        if (!EnsurePlayerTransform()) return false;
        float dist = Get2DDistance(transform.position, playerTransform.position);
        //Debug.Log($"IsPlayerInRange? Distance: {dist} / AttackRange: {attackRange} / Result: {dist <= attackRange}");
        return dist <= attackRange;
    }
    private bool CanAttack() => Time.time >= nextAttackTime;
    private void SetNextAttackTime() => nextAttackTime = Time.time + attackCooldown;
    private bool CanTeleport() => Time.time >= nextTeleportTime;

    public void InitialSet()
    {
        currentHealth = maxHealth;
        isattack = false;
        isSlamming = false;
        isJumping = false;
    }

    public void CheckHp()
    {
        // 데미지 공식 어쩌구... 난 귀찮아 저쩌구...
        if (currentHealthBar != null)
            currentHealthBar.fillAmount = currentHealth / maxHealth;
        Debug.Log($"체력바 갱신 fillAmount : {currentHealthBar.fillAmount}");
    }

    #region Attack1 Code
    private BTNodeState Attack1()
    {
        LookAtPlayer();
        Debug.Log("Preparing Attack 1...");
        // 공격 애니메이션 트리거 넣고, 공격 애니메이션에서 공격1 함수 호출
        animator.SetTrigger("Attack");
        //Invoke(nameof(PerformAttack1), attackDelay);
        isattack = false;
        SetNextAttackTime();
        return BTNodeState.Success;
    }
    private void PerformAttack1()
    {
        isattack = true;
        Collider2D[] hitPlayer = Physics2D.OverlapBoxAll(attackPoint.position, attack1BoxSize, 0);
        foreach (var player in hitPlayer)
        {
            if (player.CompareTag("Player"))
            {
                Debug.Log("Hit Player!");
                player.GetComponent<LivingEntity>().OnDamage(attack1damage);
            }
        }
    }
    #endregion
    #region Attack2 Code
    private BTNodeState Attack2()
    {
        LookAtPlayer();
        if (currentHealth <= maxHealth * 0.4f)
        {
            if (IsPlayerDetected() && CanTeleport())
            {
                Debug.Log("teleporting...");
                TeleportAbovePlayer();
                Debug.Log("Slaming Attack!!");
                isattack = true;                
                StartCoroutine(AttackSlam(1f));
            }
        }
        else
        {
            Debug.Log("Jumping for Attack 2...");
            StartCoroutine(Attack2Flow());
        }
       
        SetNextAttackTime();
        return BTNodeState.Success;
    }
    private IEnumerator Attack2Flow()
    {
        isattack = true;
        yield return StartCoroutine(AttackJump());
        yield return StartCoroutine(AttackSlam(0));
    }
    private IEnumerator AttackJump()
    {
        isJumping = true;
        animator.SetTrigger("Jump"); // 애니메이션 트리거

        // 위로 점프
        rb.velocity = new Vector2(rb.velocity.x, Mathf.Sqrt(2 * jumpHeight * Mathf.Abs(Physics2D.gravity.y * rb.gravityScale)));

        // 점프한 후 일정 높이에 도달할 때까지 대기
        while (rb.velocity.y > 0)
        {
            yield return null;
        }
        isJumping = false;
    }
    private IEnumerator AttackSlam(float delay)
    {
        yield return new WaitForSeconds(delay);

        isSlamming = true;
        animator.SetTrigger("Slam"); // 애니메이션 트리거

        Debug.Log("Slamming Down!");
        rb.velocity = new Vector2(0, -slamSpeed);

        // 지면에 도착할 때까지 대기
        //yield return new WaitForSeconds(0.2f);
        while (!IsGrounded())
        {
            yield return null;
        }
        // 충격파 공격 실행
        animator.SetTrigger("AfterSlam");
        Debug.Log("after..");
        isSlamming = false;
        isattack = false;
    }
    private bool IsGrounded()
    {
        Debug.DrawRay(SlamPoint.position, Vector2.down * groundCheckDistance, Color.blue);
        return Physics2D.Raycast(SlamPoint.position, Vector2.down, groundCheckDistance, LayerMask.GetMask("Ground"));
    }
    private void PerformSlamAttack()
    {
        Debug.Log("Performing Ground Slam Attack!");
        // 주변 범위 공격
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(SlamPoint.position, attack2BoxSize, 0);
        foreach (var enemy in hitEnemies)
        {
            if (enemy.CompareTag("Player"))
            {
                Debug.Log("Hit Player with Slam Attack!");
                enemy.GetComponent<LivingEntity>().OnDamage (attack2damage);
            }
        }
        // 체력이 40% 이하라면 Attack3 연계
        if (currentHealth <= maxHealth * 0.4f)
        {
            Attack3();
        }
    }
    #endregion
    #region Attack3 Code
    private void Attack3()
    {
        Debug.Log("Performing Attack 3");
        StartCoroutine(FireballBurst());
        SetNextAttackTime();
    }
    private IEnumerator FireballBurst()
    {
        for (int i = 0; i < fireballCount; i++)
        {
            SpawnFireball(leftHorn);
            SpawnFireball(rightHorn);
            yield return new WaitForSeconds(fireballInterval);
        }
    }
    private void SpawnFireball(Transform spawnPoint)
    {
        GameObject fireball = Instantiate(fireballPrefab, spawnPoint.position, Quaternion.identity);
        Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float angleRad = fireballAngle * Mathf.Deg2Rad;
            Vector2 force = new Vector2(Mathf.Cos(angleRad) * fireballSpeed, Mathf.Sin(angleRad) * fireballSpeed);
            rb.velocity = force;
        }
    }
    #endregion

    private void TeleportAbovePlayer()
    {
        if (Time.time < nextTeleportTime)
        {
            Debug.Log("Not ready to teleport...");
            return;
        }
        LookAtPlayer();
        Debug.Log("Teleporting above player!");
        transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y + teleportHeight, transform.position.z);

        nextTeleportTime = Time.time + teleportCooldown; // 다음 텔레포트 시간 설정
    }

    private BTNodeState Chase()
    {
        // 플레이어가 없으면 추격 실패
        if (!EnsurePlayerTransform()) return BTNodeState.Failure;

        if (IsPlayerInRange())  // 플레이어가 공격 범위 안에 있다면 추격을 멈춤
        {
            //Debug.Log("Player is in attack range, stopping chase.");
            return BTNodeState.Failure;
        }

        //Debug.Log("Chasing Player");
        LookAtPlayer();
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
        return BTNodeState.Running;
    }

    private BTNodeState Patrol()
    {
        if (IsPlayerDetected())
        {
            return BTNodeState.Failure;  // 플레이어가 감지되면 순찰을 멈춤
        }

        rb.velocity = new Vector2(nextMove * moveSpeed, rb.velocity.y);

        if (Time.time >= nextJumpTime)
        {
            Jump();
            nextJumpTime = Time.time + jumpInterval;
        }
        return BTNodeState.Running;
    }

    private void Jump() // 패트롤 시 점프하는 함수
    {
        //Debug.Log("Monster is Jumping!");
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }
    private void Think()
    {
        nextMove = Random.Range(-1, 2);
        float yRotation = nextMove == -1 ? 180f : 0f;
        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        Invoke("Think", nextMoveTime);
    }

    public void OnDamage(float damage)
    {
        currentHealth -= damage;
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
        transform.rotation = Quaternion.Euler(0, lookLeft ? 180f : 0f, 0);
    }

    private void OnDrawGizmos()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attackPoint.position, attack1BoxSize);
            Gizmos.DrawWireCube(SlamPoint.position, attack2BoxSize);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}