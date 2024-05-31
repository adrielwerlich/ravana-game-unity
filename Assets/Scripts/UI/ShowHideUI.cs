using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RavanaGame;

namespace InventoryExample.UI
{
    public class ShowHideUI : MonoBehaviour
    {
        [SerializeField] KeyCode toggleKey = KeyCode.Escape;
        [SerializeField] GameObject uiContainer = null;

        private RavanaPlayerController playerController;

        // Start is called before the first frame update
        void Start()
        {
            uiContainer.SetActive(false);

            playerController = GameObject.Find("RavanaPlayer").GetComponent<RavanaPlayerController>(); 
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                uiContainer.SetActive(!uiContainer.activeSelf);
                Cursor.lockState = uiContainer.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = uiContainer.activeSelf;
                Time.timeScale = uiContainer.activeSelf ? 0 : 1;
                playerController.playerControllerPublicProperties.LockCameraPosition = uiContainer.activeSelf;
            }
        }
    }
}