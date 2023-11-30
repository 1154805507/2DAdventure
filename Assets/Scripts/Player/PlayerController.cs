using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public PlayerInputControl inputControl;
    private Rigidbody2D rb;
    private CapsuleCollider2D coll;
    private PhysicsCheck physicsCheck;
    private PlayerAnimation playerAnimation;
    public Vector2 inputDirction;
    [Header("基本参数")]
    public float speed;
    private float runSpeed;
    private float walkSpeed => speed / 2.5f;
    public float jumpForce;
    public float hurtForce;
    private Vector2 originalOffset;
    private Vector2 originalSize;

    [Header("物理材质")]
    public PhysicsMaterial2D normal;
    public PhysicsMaterial2D wall;

    [Header("状态")]
    public bool isCrouch;
    public bool isHurt;
    public bool isDead;
    public bool isAttack;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        physicsCheck = GetComponent<PhysicsCheck>();
        coll = GetComponent<CapsuleCollider2D>();
        playerAnimation = GetComponent<PlayerAnimation>();
        originalOffset = coll.offset;
        originalSize = coll.size;

        inputControl = new PlayerInputControl();

        inputControl.Gameplay.Jump.started += Jump;

        #region 强制走路
        runSpeed = speed;
        inputControl.Gameplay.WalkButton.performed += ctx =>
        {
            if (physicsCheck.isGround)
                speed = walkSpeed;
        };

        inputControl.Gameplay.WalkButton.canceled += ctx =>
        {
            if (physicsCheck.isGround)
                speed = runSpeed;
        };
        #endregion

        //攻击
        inputControl.Gameplay.Attack.started += PlayerAttack;
    }

    private void OnEnable()
    {
        inputControl.Enable();
    }

    private void OnDisable()
    {
        inputControl.Disable();
    }

    private void Update()
    {
        inputDirction = inputControl.Gameplay.Move.ReadValue<Vector2>();

        CheckState();
    }

    private void FixedUpdate()
    {
        if (!isHurt && !isAttack)
            Move();
    }

    public void Move()
    {
        if (!isCrouch)
            rb.velocity = new Vector2(inputDirction.x * speed * Time.deltaTime, rb.velocity.y);

        int faceDir = (int)transform.localScale.x;

        if (inputDirction.x > 0)
            faceDir = 1;
        if (inputDirction.x < 0)
            faceDir = -1;

        //人物翻转
        transform.localScale = new Vector3(faceDir, 1, 1);

        //下蹲
        isCrouch = inputDirction.y < -0.5f && physicsCheck.isGround;
        if (isCrouch)
        {
            coll.offset = new Vector2(-0.05f, 0.75f);
            coll.size = new Vector2(0.8f, 1.5f);
        }
        else
        {
            coll.offset = originalOffset;
            coll.size = originalSize;
        }
    }

    private void Jump(InputAction.CallbackContext context)
    {
        //Debug.Log("Jump");
        if (physicsCheck.isGround)
            rb.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
    }

    private void PlayerAttack(InputAction.CallbackContext context)
    {
        if (!physicsCheck.isGround)
            return;
        playerAnimation.PlayerAttack();
        isAttack = true;
    }

    public void GetHurt(Transform attacker)
    {
        isHurt = true;
        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2((transform.position.x - attacker.position.x), 0).normalized;

        rb.AddForce(dir * hurtForce, ForceMode2D.Impulse);
    }

    public void PlayerDead()
    {
        isDead = true;
        inputControl.Gameplay.Disable();
    }

    private void CheckState()
    {
        if (isDead)
            gameObject.layer = LayerMask.NameToLayer("Enemy");
        else
            gameObject.layer = LayerMask.NameToLayer("Player");

        coll.sharedMaterial = physicsCheck.isGround ? normal : wall;
    }
}
