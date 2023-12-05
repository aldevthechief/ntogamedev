using UnityEngine;
using System.Collections;

public class Movement : MonoBehaviour
{
    [SerializeField] private InputHandler InputHandler;

    [Header("moving and jumping properties")]
    public Rigidbody rb;
    [SerializeField] private float speed;
    private float velocityMagnitude;

    private bool isGrounded;
    public Transform gc;
    public float groundDistance = 0.4f;
    public LayerMask groundMask; 

    public float groundvelmult;
    public float airmult;
    private float velocitymult;
    private Vector3 velocityChange;

    public float jumpheight;
    public float jumpgrace;
    private float? lastgrounded;
    private float? jumppress;

    [Header("slope movement")]
    [SerializeField] float slopeDamp = 6f;
    [SerializeField] float slopeDistance = 5f;

    [Header("current movement state")]
    public MovementState PlayerState;
    public enum MovementState
    {
        grounded,
        air
    }

    private bool isLanding = true;
    private float landedLastTime;

    [Header("animations")]
    [SerializeField] private Animator SpriteAnim;

    [Header("particles")]
    [SerializeField] GameObject PlayerTrail;
    [SerializeField] GameObject JumpParticles;
    [SerializeField] GameObject LandParticles;
    [SerializeField] float TrailTime;

    void Start()
    {
        StartCoroutine(InstantiateTrail());
    }

    void Update()
    {
        if(!GameManager.InputAllowed)
            return;

        isGrounded = Physics.CheckSphere(gc.position, groundDistance, groundMask); 

        if(isGrounded)
        {
            PlayerState = MovementState.grounded;
            velocitymult = groundvelmult;
            lastgrounded = Time.time;
        }
        else
        {
            PlayerState = MovementState.air;
            velocitymult = groundvelmult * airmult;
            if(Time.time - landedLastTime > 0.5f)
                isLanding = true;
        }

        float x = InputHandler._InputAllowed ? InputManager.GetAxis("Horizontal") : 0;
        float z = InputHandler._InputAllowed ? InputManager.GetAxis("Vertical") : 0;

        float inputmagnitude = new Vector2(x, z).magnitude;
        velocityMagnitude = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;

        SpriteAnim.SetBool("isWalking", inputmagnitude > 0);

        Vector3 move = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1) * speed;
        Vector3 newmove = new Vector3(move.x, rb.velocity.y, move.z);

        CalculateMovementVector(newmove);

        if (InputManager.GetButtonDown("Jump") && InputHandler._InputAllowed)
        {
            jumppress = Time.time;
        }

        if(Time.time - lastgrounded <= jumpgrace && Time.time - jumppress <= jumpgrace)
        {
            SpriteAnim.SetBool("isJumping", true);
            Instantiate(JumpParticles, gc.position, Quaternion.identity);
            rb.AddForce(Vector3.up * jumpheight, ForceMode.Impulse);
            jumppress = null;
            lastgrounded = null;
        }

        // slopedir = transform.up - slopehit.normal * Vector3.Dot(transform.up, slopehit.normal);
    }

    void FixedUpdate()
    {
        if(!GameManager.InputAllowed)
            return;

        rb.AddForce(velocityChange * velocitymult, ForceMode.Force);


        // RaycastHit slopehit;
        // if(Physics.Raycast(rb.position, Vector3.down, out slopehit, slopeDistance, groundMask))
        // {
        //     Quaternion sloperot = Quaternion.FromToRotation(transform.up, slopehit.normal);
        //     rb.MoveRotation(Quaternion.Slerp(rb.rotation, sloperot * rb.rotation, Time.fixedDeltaTime * slopeDamp));
        // }
    }


    void CalculateMovementVector(Vector3 dir)
    {
        velocityChange = dir - rb.velocity;
    }

    public void AddForceToThePlayer(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
    }

    IEnumerator InstantiateTrail()
    {
        if(!GameManager.IsDead || GameManager.InputAllowed)
        {
            if(isGrounded && velocityMagnitude > 0.1f)
                Instantiate(PlayerTrail, gc.position, Quaternion.identity);
            yield return new WaitForSeconds(TrailTime);
            yield return InstantiateTrail();
        }
        else yield return null;
    }

    void OnCollisionEnter(Collision other)
    {
        if(GameManager.IsDead || !GameManager.InputAllowed)
            return;

        if(other.gameObject.layer == 3 && isLanding) //layermask � ���� ������ ���� ������ ���
        {
            SpriteAnim.SetBool("isJumping", false);
            Instantiate(LandParticles, gc.position, Quaternion.identity);
            isLanding = false;
            landedLastTime = Time.time;
        }
    }
}
