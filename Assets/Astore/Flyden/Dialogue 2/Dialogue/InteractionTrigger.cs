using UnityEngine;
using Yarn.Unity;
using TMPro; // Добавляем для работы с TextMeshPro

public class InteractionTrigger : MonoBehaviour
{
    [Header("Yarn Settings")]
    public string nodeToStart = "Start";

    [Header("Interaction")]
    public KeyCode interactionKey = KeyCode.LeftShift; // Меняем на Shift

    [Header("UI Prompt")]
    public TextMeshProUGUI promptText; // Ссылка на текст подсказки

    private DialogueRunner dialogueRunner;
    private bool playerInRange = false;

    void Start()
    {
        dialogueRunner = FindObjectOfType<DialogueRunner>();

        // На всякий случай скрываем текст при старте
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && !dialogueRunner.IsDialogueRunning && Input.GetKeyDown(interactionKey))
        {
            dialogueRunner.StartDialogue(nodeToStart);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (promptText != null)
                promptText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (promptText != null)
                promptText.gameObject.SetActive(false);
        }
    }
}