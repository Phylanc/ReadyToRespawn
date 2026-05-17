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
    Transform _currentLadder; // Ссылка на текущую лестницу, чтобы притягивать к ней игрока

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

        float v = GetVerticalInput();

        if (_isClimbing && Mathf.Abs(v) <= 0.01f)
        {
            StopClimbIfNeeded();
            return;
        }

        // Начинаем лезть, если приложено усилие
        if (Mathf.Abs(v) > 0.01f)
        {
            StartClimb();
        }

        // Двигаемся только если мы в состоянии лазания
        if (_isClimbing)
        {
            // Рассчитываем движение вверх/вниз
            Vector3 move = Vector3.up * (v * climbSpeed);

            // Если у нас есть ссылка на лестницу, слегка "притягиваем" персонажа к ее центру (или оси),
            // чтобы CharacterController не "соскальзывал" и не выпадал при движении вниз
            if (_currentLadder != null)
            {
                Vector3 directionToLadder = _currentLadder.position - transform.position;
                directionToLadder.y = 0; // Игнорируем разницу по высоте, нас интересует только центр (X, Z)
                
                // Добавляем вектор притяжения к центру лестницы, чтобы персонаж не вылетал из коллайдера
                if (directionToLadder.sqrMagnitude > 0.05f) 
                {
                    move += directionToLadder.normalized * 2f; 
                }
            }

            _cc.Move(move * Time.deltaTime);
        }
    }

    private float GetVerticalInput()
    {
        // Поддержка старой системы
        float v = Input.GetAxisRaw("Vertical");

        // Поддержка новой системы (геймпады)
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            float stickY = gamepad.leftStick.ReadValue().y;
            if (Mathf.Abs(stickY) > 0.1f)
            {
                v = stickY;
            }
        }

        return v;
    }

    void StartClimb()
    {
        if (_isClimbing) return;
        _isClimbing = true;
        if (playerController != null) playerController.SetClimbing(true);
    }

    void StopClimbIfNeeded()
    {
        if (!_isClimbing) return;
        _isClimbing = false;
        if (playerController != null) playerController.SetClimbing(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ladderTag)) return;
        _inLadderZone = true;
        _currentLadder = other.transform;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(ladderTag)) return;
        _inLadderZone = false;
        _currentLadder = null;
        StopClimbIfNeeded();
    }
}
