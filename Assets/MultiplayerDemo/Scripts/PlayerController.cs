using System;
using Unity.Netcode;
using UnityEngine;
public class PlayerController : NetworkBehaviour
{
    [SerializeField] Animator animator;
    public float speed = 6.0f;
    public float gravity = 20.0f;
    public float RotateSpeed = 20f;
    int animatorIsWalking;
    float horizontal = 0;
    Vector3 vertical;
    private void Awake()
    {
        GameManager.Instance.MatchFound += OnMatchFound;
    }
    private void Start()
    {
        animatorIsWalking = Animator.StringToHash("IsWalking");
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) Destroy(this);
    }
    void Update()
    {
        if (IsClient && IsOwner)
        {
        }
        PlayerInput();
    }
    private void FixedUpdate()
    {
        if (IsClient && IsOwner)
        {
        }
        PlayerMovementServer();
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        GameManager.Instance.MatchFound -= OnMatchFound;
    }
    private void PlayerInput()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = new Vector3(0, 0, Input.GetAxis("Vertical") * speed);
    }
    //[ServerRpc]
    void PlayerMovementServer()
    {
        Vector3 rotation = horizontal * RotateSpeed * Time.deltaTime * Vector3.up;
        Vector3 translation = vertical * Time.deltaTime;
        bool animationValue = vertical.magnitude > 0;
        PlayerMovementClient(rotation, translation, animationValue);
    }

    //[ClientRpc]
    void PlayerMovementClient(Vector3 rotation, Vector3 translation, bool animationValue)
    {
        transform.Rotate(rotation);
        transform.Translate(translation);
        animator.SetBool(animatorIsWalking, animationValue);
    }


    private void OnMatchFound()
    {
        this.enabled = true;
    }
}
