using UnityEngine;

public class JoystickAnimatorController : MonoBehaviour
{
    [Header("Animator Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string horizontalParam = "MoveX";   // имя параметра для горизонтального наклона
    [SerializeField] private string verticalParam = "MoveY";     // имя параметра для вертикального наклона
    
    [Header("Input Settings")]
    [SerializeField] private bool useNormalizedDirection = true; // нормализовать ли диагональные движения
    [SerializeField] private float deadZone = 0.1f;              // зона нечувствительности для ввода
    
    [Header("Smoothing")]
    [SerializeField] private bool smoothMovement = true;         // плавное изменение наклона
    [SerializeField] private float smoothTime = 0.1f;            // время сглаживания
    
    // текущий целевой вектор наклона
    private Vector2 targetDirection;
    private Vector2 currentVelocity;
    private Vector2 currentDirection;
    
    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
            
        if (animator == null)
            Debug.LogError("Animator component not found on " + gameObject.name);
    }
    
    private void Update()
    {
        // Считываем ввод с клавиатуры (стрелки или WASD)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        Vector2 input = new Vector2(horizontal, vertical);
        
        // Применяем зону нечувствительности
        if (input.magnitude < deadZone)
            input = Vector2.zero;
        
        // Нормализуем вектор, чтобы диагональные движения были такой же длины
        if (useNormalizedDirection && input.magnitude > 1f)
            input.Normalize();
        
        targetDirection = input;
        
        // Плавное изменение или мгновенное
        if (smoothMovement)
        {
            currentDirection = Vector2.SmoothDamp(currentDirection, targetDirection, ref currentVelocity, smoothTime);
        }
        else
        {
            currentDirection = targetDirection;
        }
        
        // Применяем значения к аниматору
        if (animator != null)
        {
            animator.SetFloat(horizontalParam, currentDirection.x);
            animator.SetFloat(verticalParam, currentDirection.y);
        }
    }
    
    // Опционально: сброс джойстика в ноль по событию (например, при деактивации объекта)
    private void OnDisable()
    {
        if (animator != null)
        {
            animator.SetFloat(horizontalParam, 0f);
            animator.SetFloat(verticalParam, 0f);
        }
    }
}