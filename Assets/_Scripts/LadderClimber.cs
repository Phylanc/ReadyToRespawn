using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class LadderClimber : MonoBehaviour
{
    [Header("Ladder")]
    [SerializeField] private string ladderTag = "Ladder";
    [SerializeField] private float climbSpeed = 3f;

    [Header("References")]
    [SerializeField] private PlayerController playerController;

    CharacterController _cc;
    bool _inLadderZone;
    bool _isClimbing;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!_inLadderZone)
        {
            StopClimbIfNeeded();
            return;
        }

        float v = Input.GetAxisRaw("Vertical");

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            float stickY = gamepad.leftStick.ReadValue().y;
            if (Mathf.Abs(stickY) > 0.1f)
                v = stickY;
        }

        if (Mathf.Abs(v) > 0.01f)
        {
            StartClimb();
            Vector3 move = Vector3.up * v * climbSpeed * Time.deltaTime;
            _cc.Move(move);
        }
    }

    void StartClimb()
    {
        if (_isClimbing) return;
        _isClimbing = true;
        if (playerController != null) playerController.enabled = false;
    }

    void StopClimbIfNeeded()
    {
        if (!_isClimbing) return;
        _isClimbing = false;
        if (playerController != null) playerController.enabled = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ladderTag)) return;
        _inLadderZone = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(ladderTag)) return;
        _inLadderZone = false;
        StopClimbIfNeeded();
    }
}
