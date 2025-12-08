using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEngine.TextCore.LowLevel;

public class GameManager : MonoBehaviour
{
    // ΩÃ±€≈Ê
    public static GameManager instance { get; private set; }

    public bool isFirstPlay;

    [Header("State")]
    public bool isGrounded = false;
    public bool isMeleeAttack = false;
    public bool isRangeAttack = false;
    public bool isCommandAction = false;

    [Header("UIState")]
    public bool isConversation = false;
    public bool isCommand = false;
    public bool isUI = false;

    [Header("Event")]
    [SerializeField] private Dialogue testDialogue;

    private void Awake()
    {
        if (instance != null)
            Destroy(instance);
        else instance = this;
    }

    private void Start()
    {
        isFirstPlay = true;

        //if (isFirstPlay)
        //{
        //    UIManager.instance.showDialogue(testDialogue);
        //    isFirstPlay = false;
        //}
    }

    private void Update()
    {
        if (isFirstPlay)
        {
            //UIManager.instance.showDialogue(testDialogue);
            isFirstPlay = false;
        }
    }

    public void Save(ref GameManagerSaveData data)
    {
        data.isFirstPlay = this.isFirstPlay;
    }

    public void Load(GameManagerSaveData data)
    {
        this.isFirstPlay = data.isFirstPlay;
    }
}

[System.Serializable]
public struct GameManagerSaveData
{
    public bool isFirstPlay;
}
