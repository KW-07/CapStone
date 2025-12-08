using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour, LivingEntity
{
    public static Player instance { get; private set; }

    [Header("스탯 UI")]
    public Image currentHealthBar;
    public float maxHealth = 100f;
    private float currentHealth;
    public Image currentStaminaBar;
    public float maxStamina = 100f;
    private float currentStamina;

    [Header("얼굴위치")]
    public Transform maskAttachPoint;

    [Header("이동")]
    public float dir;
    public float originalMoveSpeed = 1f;
    private float moveSpeed;
    private Vector3 stopPosition;
    public bool facingRight = true;

    [Header("점프")]
    public float jumpPower = 1f;
    [SerializeField] private int _jumpCount = 0;
    public int jumpCount
    {
        get
        {
            return _jumpCount;
        }
        set
        {
            _jumpCount = value;
        }
    }

    [Header("대쉬")]
    public float teleportdis;

    [Header("버프")]
    [SerializeField] private List<float> activeSpeedMultipliers = new List<float>();

    [Header("커맨드")]
    public int[] pCommand = new int[8];
    [SerializeField] private float limitCommandTime;
    [SerializeField] private float inCommandingTimeScale;
    float initTime = 0;
    public float commandingTime = 0;
    [SerializeField] private int commandCount;
    private bool movePossible; // 합치고 이동관련 스크립트에 넣을 예정
    private SkillSystem skillSystem;

    [Header("UI")]
    public GameObject commandTimeUI;
    private Vector2 currentPCommandSize = new Vector2(0, 0);
    public GameObject pCommandUI;
    public GameObject pCommandUIGrid;
    public Sprite[] commandIcon;
    [SerializeField] private Image[] pCommandIcon;
    public CommandData[] usableCommandList;

    [Header("공격")]
    public Transform shootPoint;
    [SerializeField] private MeleeAttackData normalAttack;
    [SerializeField] private RangeAttackData normalProjectile;
    private bool canRangeAttack = true;
    [SerializeField] private float rangeCooldown;
    [SerializeField] private float initTimeMultipleAttack;
    float cooldownTimer;
    float rangeCooldownTimer;
    [SerializeField] private float attackCountInitTime;

    public int attackStackCount = 0;
    public float stackDamage;

    [Header("데미지")]
    [SerializeField]private float _playerDamage;

    [SerializeField] private float dieTime;
    public float playerDamage
    {
        get
        {
            return _playerDamage;
        }
        private set
        {
            _playerDamage = value;
        }
    }

    [Header("적 탐지")] // 추후 private으로 변경 예정
    // 맵에 잡히는 전체 적 수
    public GameObject[] allEnemyArray;
    // 플레이어 기준 전방에 존재하는 적 수
    public List<GameObject> visibleEnemy;
    [Space(10f)]
    // 전방에 존재하며 가장 가까운 적
    public GameObject neareastEnemy;

    [Header("버프(or 디버프)SO")]
    public Buff PoisonBuff;
    
    Rigidbody2D rb;
    CapsuleCollider2D capsule;
    Transform playerTransform;
    Animator animator;

    private void Awake()
    {
        if (instance != null)
            Destroy(instance);
        else instance = this;

        rb = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        playerTransform = GetComponent<Transform>();
        animator = GetComponent<Animator>();
        skillSystem = gameObject.GetComponent<SkillSystem>();
        
        animator.SetFloat("attackCount", normalAttack.multipleAttack);
    }

    private void Start()
    {
        moveSpeed = originalMoveSpeed;

        initTime = 0;
        commandCount = 0;
        GameManager.instance.isCommand = false;
        commandTimeUI.SetActive(false);
        pCommandUI.SetActive(false);
        CommandInitialization(pCommand);

        InitialSet();
    }

    private void Update()
    {
        if (GameManager.instance.isCommand)
            playerTransform.position = new Vector3(stopPosition.x, playerTransform.position.y, -5);
        else
            playerTransform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -5);

        FindEnemy();

        cooldownTimer -= Time.deltaTime;
        rangeCooldownTimer -= Time.deltaTime;

        if (cooldownTimer < 0)
        {
            // Multiple Attack
            cooldownTimer = initTimeMultipleAttack;
            normalAttack.multipleAttack = 0;
            animator.SetFloat("attackCount", normalAttack.multipleAttack);
        }

        if (rangeCooldownTimer < 0)
        {
            canRangeAttack = true;
        }

        if (GameManager.instance.isCommand)
        {
            InputCommand();
        }
    }

    private void FixedUpdate()
    {
        Move();
        animator.SetFloat("yVelocity", rb.velocity.y);
    }

    public void InitialSet()
    {
        currentHealth = maxHealth;
    }
    public void CheckHp()
    {
        if (currentHealthBar != null)
            currentHealthBar.fillAmount = currentHealth / maxHealth;
        Debug.Log($"플레이어 현재 체력 : {currentHealthBar.fillAmount}");
    }
    public void OnDamage(float damage)
    {
        Debug.Log($"플레이어가 {damage}의 피해를 입었습니다.");   
        currentHealth -= damage;
        animator.SetTrigger("damaged");
        CheckHp();
        if (currentHealth <= 0)
        {
           Die();
        }
    }
    public void Die()
    {
        animator.SetTrigger("playerDie");
        Destroy(this.gameObject, dieTime);
    }

    public Transform GetMaskAttachPoint()   //Mask에서 사용할 얼굴위치 넘겨주는 함수
    {
        return maskAttachPoint;
    }

    #region Move

    public void OnMove(InputAction.CallbackContext context)
    {
        dir = context.ReadValue<float>();
    }

    // 이동
    void Move()
    {
        // 대화 중 움직임 제어
        if (!GameManager.instance.isUI && !GameManager.instance.isCommand)
        {
            // spriteFlip
            if (dir < 0 && facingRight)
            {
                Flip();
            }
            else if (dir > 0 && !facingRight)
            {
                Flip();
            }

            rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);

            animator.SetFloat("xVelocity", dir);
        }
    }

    // 움직임 멈춤
    public void moveStop()
    {
        stopPosition = new Vector3(transform.position.x, transform.position.y, -5);
    }

    // 방향 뒤집기
    void Flip()
    {
        facingRight = !facingRight;
        transform.Rotate(0, 180, 0);

        commandTimeUI.GetComponent<Transform>().Rotate(0, 180, 0);
        pCommandUIGrid.GetComponent<Transform>().Rotate(0, 180, 0);
    }

    // 버프 사용
    public void ApplySpeedBuff(float multiplier, float duration)
    {
        if (multiplier <= 0) return;

        // List에 버프 효과 추가
        activeSpeedMultipliers.Add(multiplier);
        // 가장 높은 배율의 버프효과 적용
        UpdateMoveSpeed();

        StartCoroutine(RemoveSpeedBuffAfterDelay(multiplier, duration));
    }

    private IEnumerator RemoveSpeedBuffAfterDelay(float multiplier, float duration)
    {
        // 버프 시간 초과 후 적용중이던 버프 삭제
        yield return new WaitForSeconds(duration);
        activeSpeedMultipliers.Remove(multiplier);

        // 기존 버프가 해제되고 다음 순위의 가장 높은 버프 사용
        UpdateMoveSpeed();
    }

    // 이동속도 증감
    private void UpdateMoveSpeed()
    {
        if (activeSpeedMultipliers.Count > 0)
        {
            float maxMultiplier = Mathf.Max(activeSpeedMultipliers.ToArray());
            moveSpeed = moveSpeed * maxMultiplier;
        }
        else
        {
            //moveSpeed = moveSpeed;
        }

        Debug.Log($"현재 이동 속도: {moveSpeed}");
    }

    // 점프
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!GameManager.instance.isUI && !GameManager.instance.isCommand)
            {
                if (jumpCount < 2)
                {
                    rb.velocity = new Vector2(rb.velocity.x, jumpPower);
                    GameManager.instance.isGrounded = false;

                    jumpCount += 1;
                    animator.SetFloat("jumpCount", jumpCount);

                    animator.SetBool("isJumping", !GameManager.instance.isGrounded);
                }
            }
        }
    }

    // 아랫점프
    public void OnDownJump(InputAction.CallbackContext context)
    {
        if (context.performed && GameObject.FindWithTag("Platform").GetComponent<Platform>().isPlayer == true)
        {
            if (!GameManager.instance.isUI && GameManager.instance.isCommand)
            {
                StartCoroutine("coDownJump");
            }
        }
    }
    IEnumerator coDownJump()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        capsule.isTrigger = true;
        float y = transform.position.y;
        while (transform.position.y > y - 1.6f && transform.position.y <= y)
        {
            yield return wait;
        }
        capsule.isTrigger = false;
    }

    // 대쉬
    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if(!GameManager.instance.isUI && !GameManager.instance.isCommand)
            {
                // 대쉬의 방향 및 거리
                Vector2 dashDirection = facingRight ? Vector2.right : Vector2.left;
                float dashDistance = teleportdis;

                // 플레이어의 콜라이더 크기 체크
                Vector2 playerColliderSize = gameObject.GetComponent<BoxCollider2D>().size;

                // 박스형 Raycast
                RaycastHit2D hit = Physics2D.BoxCast(rb.position, playerColliderSize, 0f, dashDirection, dashDistance, LayerMask.GetMask("Ground"));

                // Ground의 레이어를 가진 무언가가 잡힌다면 해당 오브젝트 앞까지 대쉬
                if (hit.collider != null)
                {
                    float safeDistance = hit.distance - 0.1f;
                    rb.MovePosition(rb.position + dashDirection * safeDistance);
                    Debug.Log("Dash - 충돌 감지!");
                }
                // 없다면 정상적으로 대쉬
                else
                {
                    rb.MovePosition(rb.position + dashDirection * dashDistance);
                    Debug.Log("Dash - 정상");
                }
            }
        }
    }
    #endregion

    #region Save&Load
    public void Save(ref PlayerMoveSavaData data)
    {
        data.position = transform.position;
    }

    public void Load(PlayerMoveSavaData data)
    {
        transform.position = data.position;
    }

    #endregion

    #region Collision&Trigger
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Ground" || collision.gameObject.tag == "Platform")
        {
            GameManager.instance.isGrounded = true;
            animator.SetBool("isJumping", !GameManager.instance.isGrounded);
            animator.SetBool("jumpCommanding", false);

            jumpCount = 0;
            animator.SetFloat("jumpCount", jumpCount);
        }
    }
    #endregion

    #region Attack
    // Melee
    public void OnMeleeAttack(InputAction.CallbackContext context)
    {

        if (context.performed)
        {

            if (!GameManager.instance.isUI && GameManager.instance.isGrounded && !GameManager.instance.isCommand)
            {
                if (attackStackCount > 0)
                {
                    SkillSystem.instance.command.damage = stackDamage;
                }
                else
                {
                    SkillSystem.instance.command.damage = SkillSystem.instance.command.baseDamage;

                    SkillSystem.instance.command = normalAttack;

                    cooldownTimer = initTimeMultipleAttack;
                    normalAttack.multipleAttack++;
                    if (normalAttack.multipleAttack > 3)
                        normalAttack.multipleAttack = 1;

                    skillSystem.UseSkill(shootPoint.gameObject, neareastEnemy);

                    Debug.Log(normalAttack.multipleAttack);

                    animator.SetTrigger("isMeleeAttack");
                    animator.SetFloat("attackCount", normalAttack.multipleAttack);
                }
            }
        }
    }

    // Range
    public void RangeAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!GameManager.instance.isUI && canRangeAttack && !GameManager.instance.isCommand)
            {
                rangeCooldownTimer = rangeCooldown;
                canRangeAttack = false;

                animator.SetTrigger("isRangeAttack");

                SkillSystem.instance.command = normalProjectile;

                skillSystem.UseSkill(shootPoint.gameObject, neareastEnemy);
            }
        }
    }

    void FindEnemy()
    {
        allEnemyArray = GameObject.FindGameObjectsWithTag("Enemy");

        float distance = 0;

        visibleEnemy.Clear();
        neareastEnemy = null;
        foreach (GameObject enemy in allEnemyArray)
            enemy.GetComponent<SpriteRenderer>().color = Color.white;

        // 우측을 보고있다면 오른쪽 적을 좌측을 보고있다면 왼쪽적을 '볼 수 있는' 적으로 판단
        if (facingRight)
        {
            foreach (GameObject enemy in allEnemyArray)
            {
                if (enemy.transform.position.x > this.gameObject.transform.position.x)
                {
                    visibleEnemy.Add(enemy);
                }
            }
        }
        else
        {
            foreach (GameObject enemy in allEnemyArray)
            {
                if (enemy.transform.position.x < this.gameObject.transform.position.x)
                {
                    visibleEnemy.Add(enemy);
                }
            }
        }

        // '볼 수 있는' 적 중 가장 가까운 적을 판별
        for (int i = 0; i < visibleEnemy.Count; i++)
        {
            if (i == 0)
            {
                distance = Vector2.Distance(this.gameObject.transform.position, visibleEnemy[i].transform.position);
                neareastEnemy = visibleEnemy[i];
            }
            else
            {
                if (Vector2.Distance(this.gameObject.transform.position, visibleEnemy[i].transform.position) < distance)
                {
                    distance = Vector2.Distance(this.gameObject.transform.position, visibleEnemy[i].transform.position);
                    neareastEnemy = visibleEnemy[i];
                }
            }
        }
    }
    #endregion

    #region RepeatAttack
    // 반복공격 실행
    public void RepeatAttack(GameObject enemy, int repeatCount, float repeatDelay, float damage)
    {
        if (repeatCount <= 0)
            return;

        StartCoroutine(RepeatAttackCoroutine(enemy, repeatCount, repeatDelay, damage));
    }
    // 반복공격 coroutine
    private IEnumerator RepeatAttackCoroutine(GameObject enemy, int repeatCount, float repeatDelay, float damage)
    {
        for (int i = 0; i < repeatCount; i++)
        {
            Debug.Log($"{enemy.name}에게 {playerDamage + damage}의 피해를 입힘!");

            yield return new WaitForSeconds(repeatDelay);
        }
    }
    #endregion

    #region DelaySpawn
    // n초 뒤 오브젝트 생성
    public void DelayInstantiate(GameObject obj, Vector2 position, float delayTIme)
    {
        if (delayTIme <= 0)
            return;

        StartCoroutine(DelayInstantiateCoroutine(obj, position, delayTIme));
    }

    private IEnumerator DelayInstantiateCoroutine(GameObject obj, Vector2 position, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        Instantiate(obj, position, Quaternion.identity);
    }
    #endregion

    #region Command

    private void InputCommand()
    {
        if (initTime == 0)
        {
            GameManager.instance.isCommand = true;

            // 기존 pCommand 값 초기화
            CommandInitialization(pCommand);

            currentPCommandSize = new Vector2(0, 0);
            // pCommandUI 사이즈 초기값 설정
            pCommandUI.GetComponent<RectTransform>().sizeDelta = currentPCommandSize;

            // pCommandGrid 사이즈 초기값 설정
            pCommandUIGrid.GetComponent<RectTransform>().sizeDelta = pCommandUI.GetComponent<RectTransform>().sizeDelta;

            UIManager.instance.deleteCommandCandidate();

            initTime++;
        }
        else if (initTime > 0)
        {

            animator.SetBool("isCommanding", true);
            if (!GameManager.instance.isGrounded && GameManager.instance.isCommand)
                animator.SetBool("jumpCommanding", true);

            commandingTime -= Time.unscaledDeltaTime;
            movePossible = BooleanOnOff(movePossible);

            PCommandCandidate();

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                for (int i = 0; i < pCommand.Length; i++)
                {
                    if (pCommand[i] == 0)
                    {
                        pCommand[i] = 1;
                        commandCount++;
                        ShowPCommand(i);

                        UIManager.instance.deleteCommandCandidate();
                        break;
                    }
                }
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                for (int i = 0; i < pCommand.Length; i++)
                {
                    if (pCommand[i] == 0)
                    {
                        pCommand[i] = 2;
                        commandCount++;
                        ShowPCommand(i);

                        UIManager.instance.deleteCommandCandidate();
                        break;
                    }
                }
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                for (int i = 0; i < pCommand.Length; i++)
                {
                    if (pCommand[i] == 0)
                    {
                        pCommand[i] = 3;
                        commandCount++;
                        ShowPCommand(i);

                        UIManager.instance.deleteCommandCandidate();
                        break;
                    }
                }
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                for (int i = 0; i < pCommand.Length; i++)
                {
                    if (pCommand[i] == 0)
                    {
                        pCommand[i] = 4;
                        commandCount++;
                        ShowPCommand(i);

                        UIManager.instance.deleteCommandCandidate();
                        break;
                    }
                }
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                for (int i = 0; i < pCommand.Length; i++)
                {
                    if (pCommand[i] == 0)
                    {
                        pCommand[i] = 5;
                        commandCount++;
                        ShowPCommand(i);

                        UIManager.instance.deleteCommandCandidate();
                        break;
                    }
                }
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                for (int i = 0; i < pCommand.Length; i++)
                {
                    if (pCommand[i] == 0)
                    {
                        pCommand[i] = 6;
                        commandCount++;
                        ShowPCommand(i);

                        UIManager.instance.deleteCommandCandidate();
                        break;
                    }
                }
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                for (int i = 0; i < pCommand.Length; i++)
                {
                    if (pCommand[i] == 0)
                    {
                        pCommand[i] = 7;
                        commandCount++;
                        ShowPCommand(i);

                        UIManager.instance.deleteCommandCandidate();
                        break;
                    }
                }
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                for (int i = 0; i < pCommand.Length; i++)
                {
                    if (pCommand[i] == 0)
                    {
                        pCommand[i] = 8;
                        commandCount++;
                        ShowPCommand(i);

                        UIManager.instance.deleteCommandCandidate();
                        break;
                    }
                }
            }
            UIManager.instance.CommandCandidate();

            // 커맨드 시간 초과 시
            if (commandingTime <= 0)
            {
                bool bCommandCount = false;

                animator.SetBool("isCommanding", false);

                animator.SetBool("jumpCommanding", false);
                GameManager.instance.isCommand = BooleanOnOff(GameManager.instance.isCommand);

                commandTimeUI.SetActive(false);
                pCommandUI.SetActive(false);

                if (commandCount <= 0)
                {

                }
                // 커맨드의 합이 0 초과하면 즉, 무언가가 눌렸다면
                else
                {

                    // 커맨드 리스트 탐색
                    for (int i = 0; i < CommandManager.instance.commandList.Length; i++)
                    {
                        // 커맨드가 존재한다면
                        if (Enumerable.SequenceEqual(pCommand, CommandManager.instance.commandList[i].command))
                        {
                            Debug.Log("커맨드 : " + CommandManager.instance.commandList[i].commandNameKor);
                            bCommandCount = true;

                            SkillSystem.instance.command = CommandManager.instance.commandList[i];

                            // 스킬 사용
                            // 버프일 경우 플레이어 위치에서 생성
                            if (CommandManager.instance.commandList[i].castPlayerPosition)
                            {
                                skillSystem.UseSkill(gameObject, neareastEnemy);
                            }
                            // 버프가 아닐 경우 플레이어 전방에서 생성
                            else
                            {
                                skillSystem.UseSkill(shootPoint.gameObject, neareastEnemy);
                            }

                            // 커맨드 타이머 삭제
                            commandTimeUI.SetActive(false);
                            pCommandUI.SetActive(false);

                            CommandInitialization(usableCommandList);
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    if (!bCommandCount)
                    {

                        Debug.Log("Nothing Command!");

                        CommandInitialization(usableCommandList);
                    }
                }
                // End
                GameManager.instance.isCommand = false;
                initTime = 0;
            }
        }
    }

    public void PlayerCommanding(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // 아무 상황이 아닐 경우에만 커맨드 실행
            if (!GameManager.instance.isUI)
            {
                GameManager.instance.isCommand = BooleanOnOff(GameManager.instance.isCommand);
                GameManager.instance.isCommand = true;

                moveStop();

                if (GameManager.instance.isCommand)
                {
                    commandTimeUI.SetActive(true);
                    pCommandUI.SetActive(true);
                    commandingTime = limitCommandTime;
                    //Time.timeScale = inCommandingTimeScale;
                }
                else
                {
                    commandingTime = limitCommandTime;
                    //Time.timeScale = 1.0f;
                    commandTimeUI.SetActive(false);
                    pCommandUI.SetActive(false);

                }
            }
        }
    }

    private void CommandInitialization(int[] command)
    {
        for (int i = 0; i < command.Length; i++)
        {
            command[i] = 0;
        }
        commandCount = 0;
    }
    private void CommandInitialization(CommandData[] command)
    {
        for (int i = 0; i < command.Length; i++)
        {
            command[i] = null;
        }
        commandCount = 0;
    }
    #endregion

    #region pCommandUI
    // 초기 사이즈 변환
    private void PCommandInitSize()
    {
        currentPCommandSize = new Vector2(
            pCommandUIGrid.GetComponent<GridLayoutGroup>().cellSize.x + pCommandUIGrid.GetComponent<GridLayoutGroup>().padding.left + pCommandUIGrid.GetComponent<GridLayoutGroup>().padding.right,
            pCommandUIGrid.GetComponent<GridLayoutGroup>().cellSize.y + pCommandUIGrid.GetComponent<GridLayoutGroup>().padding.top + pCommandUIGrid.GetComponent<GridLayoutGroup>().padding.bottom);

        // pCommand값 초기값 조정
        pCommandUI.GetComponent<RectTransform>().sizeDelta = currentPCommandSize;

        // pCommandGrid값 초기값 조정
        pCommandUIGrid.GetComponent<RectTransform>().sizeDelta = new Vector2(
            pCommandUI.GetComponent<RectTransform>().sizeDelta.x,
            pCommandUI.GetComponent<RectTransform>().sizeDelta.y);
    }

    private void ShowPCommand(int i)
    {
        // 첫번째 크기은 기본값으로 설정
        if (commandCount == 1)
        {
            PCommandInitSize();
        }
        // 이후 크기는 일정 값에 따른 조정
        else
        {
            // 현재UI 길이값 + 다음 셀의 크기값 + 사이 간격
            pCommandUI.GetComponent<RectTransform>().sizeDelta = new Vector2(
                pCommandUI.GetComponent<RectTransform>().sizeDelta.x + pCommandUIGrid.GetComponent<GridLayoutGroup>().cellSize.x + pCommandUIGrid.GetComponent<GridLayoutGroup>().spacing.x,
                pCommandUI.GetComponent<RectTransform>().sizeDelta.y);
            // 현재UIGrid 길이값 + 다음 셀의 크기값 + 사이 간격
            pCommandUIGrid.GetComponent<RectTransform>().sizeDelta = new Vector2(
                pCommandUI.GetComponent<RectTransform>().sizeDelta.x,
                pCommandUI.GetComponent<RectTransform>().sizeDelta.y);
        }

        // 이미지 삽입
        pCommandIcon[i].sprite = commandIcon[pCommand[i] - 1];
    }
    #endregion

    private bool BooleanOnOff(bool boolean)
    {
        if (boolean)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    #region Candidate
    private void PCommandCandidate()
    {
        // 커맨드리스트의 개수만큼 할당
        usableCommandList = new CommandData[CommandManager.instance.commandList.Length];
        int j = 0;

        if (commandCount > 0)
        {
            for (int i = 0; i < CommandManager.instance.commandList.Length; i++)
            {
                if (pCommand.Take(commandCount).SequenceEqual(CommandManager.instance.commandList[i].command.Take(commandCount)))
                {
                    usableCommandList[j] = CommandManager.instance.commandList[i];
                    j++;
                }
            }
        }

        // 필요없는 부분은 삭제
        Array.Resize(ref usableCommandList, j);
    }
    #endregion
}

[System.Serializable]
public struct PlayerMoveSavaData
{
    public Vector3 position;
}
