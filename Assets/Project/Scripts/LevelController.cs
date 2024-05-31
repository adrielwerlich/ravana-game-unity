using RavanaGame;
using Scene_Teleportation_Kit.Scripts.player;
using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }
    public int currentLevel = 1;
    [SerializeField] private GameObject userMessages;
    public GameObject missionsTextPanel;
    private Text messageTitle;
    private Text messageBody;
    private Button userMessageButton;
    private Text buttonText;
    public static event Action<Transform> Mission1_FindBrahma;

    private Transform LordBrahma;
    private Transform portalToBrahma;

    private const string HIDE_WARNING_TEXT = "Hide Warning";
    private const string PRESS_ENTER = "Press Enter";
    private const string RESTART = "RESTART";
    public bool hideWarning = false;

    [SerializeField] private PlayerScoreEvolutionController playerScoreEvolutionController;

    private RavanaPlayerController playerController;


    void Start()
    {
        LordBrahma = this.transform.Find("Brahma");
        portalToBrahma = GameObject.Find("PortalToBrahma").transform;

        playerController = GameObject.Find("RavanaPlayer").GetComponent<RavanaPlayerController>();
        // LordBrahma.gameObject.SetActive(false);

        userMessages = GameObject.Find("UserMessages"); 
        missionsTextPanel = userMessages.transform.Find("MissionsTextPanel").gameObject;

        userMessageButton = missionsTextPanel.transform.FindChildByRecursive("ButtonStart").GetComponent<Button>();
        buttonText = userMessageButton.transform.Find("ButtonText").GetComponent<Text>();


        messageTitle = missionsTextPanel.transform.FindChildByRecursive("EditorTitle").GetComponent<Text>();
        messageBody = missionsTextPanel.transform.FindChildByRecursive("EditorText").GetComponent<Text>();

        userMessageButton.onClick.AddListener(HideMessagePanel);

        // SetStartMissionText
        SetMissionText(
            "Your Mission",
            "You need to achieve level hundred to achieve next mission. Go destroying all your enemies to raise your level",
            PRESS_ENTER,
            52
        );

        // remove debugging - REMEMBER
        //missionsTextPanel.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerScoreEvolutionController.ScoreHundredReached += OnScoreHundredReached;
        RavanaPlayerController.EnterKeyPressed += OnEnterKeyPressed;
        MountMeruUserMessage.ShowKillAllMonstersMessage += OnShowKillAllMonstersMessage;
        RavanaCollisionController.PlayerIsDead += ShowDeadMessage;

        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    private void OnDisable()
    {
        PlayerScoreEvolutionController.ScoreHundredReached -= OnScoreHundredReached;
        RavanaPlayerController.EnterKeyPressed -= OnEnterKeyPressed;
        MountMeruUserMessage.ShowKillAllMonstersMessage -= OnShowKillAllMonstersMessage;
        RavanaCollisionController.PlayerIsDead -= ShowDeadMessage;

    }

    private void ShowDeadMessage()
    {
        SetMissionText(
           "Your were killed",
           "Restart and try again.",
           RESTART,
           45
       );
        missionsTextPanel.SetActive(true);
    }

    private void OnShowKillAllMonstersMessage()
    {
        SetMissionText(
            "Kill All Monsters",
            "You need to kill all monsters to open the portal.",
            PRESS_ENTER,
            45
        );
        missionsTextPanel.SetActive(true);
    }

    private void OnScoreHundredReached()
    {
        currentLevel++;
        switch (currentLevel)
        {
            case 2:
                SetMissionText(
                    "Find Brahma",
                    "He will give you power to be invencible. Follow the arrow to find him. Jump to climb the mountains. Now you can press Z or right click to throw spell.",
                    PRESS_ENTER,
                    40
                );
                break;
            default:
                break;
        }
        missionsTextPanel.SetActive(true);
    }

    private void OnEnterKeyPressed()
    {
        if (buttonText.text == HIDE_WARNING_TEXT || buttonText.text == PRESS_ENTER)
        {
            HideMessagePanel();
            hideWarning = true;
            return;
        }
        if (buttonText.text == RESTART)
        {
            HideMessagePanel();
            hideWarning = true;
            playerController.GoToMainMenu();
            return;
        }
        switch (currentLevel)
        {
            case 1:
                HideMessagePanel();
                break;
            case 2:
                HideMessagePanel();
                Mission1_FindBrahma?.Invoke(portalToBrahma);
                break;
            default:
                break;
        }
    }

    public void HideMessagePanel()
    {
        missionsTextPanel.SetActive(false);
    }

    public void SetAvoidSunlightText()
    {
        SetMissionText(
         "Avoid Sunlight",
         "You need to avoid sunlight. It will damage you. You can see the sunlight in the sky. If you are in sunlight, you will loose strength and see some particles from your player.",
         HIDE_WARNING_TEXT
        );
    }

    private void SetMissionText(string titleText, string bodyText, string btnText, int fontSize = 36)
    {
        messageTitle.text = titleText;
        messageBody.text = bodyText;
        messageBody.fontSize = fontSize;
        buttonText.text = btnText;
    }

}
