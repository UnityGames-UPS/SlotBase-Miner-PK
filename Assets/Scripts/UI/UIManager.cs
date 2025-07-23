using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;


public class UIManager : MonoBehaviour
{






    [SerializeField]
    private Button Paytable_Button;


    [SerializeField] private TMP_Text[] SymbolsText;
    [SerializeField] private TMP_Text Jackpot_Text;
    // [SerializeField] private Button exitButton;

    [Header("Popus UI")]
    [SerializeField]
    private GameObject MainPopup_Object;


    [Header("Paytable Popup")]
    [SerializeField]
    private GameObject PaytablePopup_Object;
    [SerializeField]
    private Button PaytableExit_Button;

    [Header("menu popup")]
    [SerializeField] private Button Menu_button;
    [SerializeField] private Transform Menu_button_grp;
    [SerializeField] private Sprite MenuOpenSprite;
    [SerializeField] private Sprite MenuCloseSprite;

    [Header("Settings popup")]
    [SerializeField] private Button Setting_button;
    [SerializeField] private GameObject settingObject;
    [SerializeField] private Slider SoundSlider;
    [SerializeField] private Slider MusicSlider;
    [SerializeField] private Button Setting_exit_button;

    [Header("Quit popup")]
    [SerializeField] private GameObject QuitPopupObject;
    [SerializeField] private Button yes_button;
    [SerializeField] private Button no_button;
    [SerializeField] private Button cancel_button;
    [SerializeField] private Button Quit_button;

    [Header("LowBalance Popup")]
    [SerializeField]
    private Button LBExit_Button;
    [SerializeField]
    private GameObject LBPopup_Object;

    [Header("Disconnection Popup")]
    [SerializeField] private Button CloseDisconnect_Button;
    [SerializeField] private GameObject DisconnectPopup_Object;

    [Header("Scripts")]
    [SerializeField] private AudioController audioController;
    [SerializeField] private SlotBehaviour slotBehaviour;
    [SerializeField] private SocketIOManager socketManager;

    [Header("All wins popup")]
    [SerializeField] private GameObject WinPopup_Object;
    [SerializeField] private GameObject jackpot_Object;
    [SerializeField] private GameObject normalWin_Object;
    [SerializeField] private TMP_Text Win_Text;
    [SerializeField] private TMP_Text jackpot_Text;
    [SerializeField] private Image Win_Image;
    [SerializeField] private Sprite HugeWin_Sprite;
    [SerializeField] private Sprite BigWin_Sprite;
    [SerializeField] private Sprite MegaWin_Sprite;
    [SerializeField] private Button SkipWinAnimation;

    [Header("Splash Screen")]
    [SerializeField] private GameObject spalsh_screen;
    [SerializeField] private Image progressbar;
    [SerializeField] private RectTransform progressbarHandle;
    [SerializeField] private TMP_Text loadingText;

    [Header("AnotherDevice Popup")]
    [SerializeField] private Button CloseAD_Button;
    [SerializeField] private GameObject ADPopup_Object;

    [Header("Free Spins")]
    [SerializeField] private Image freeSpinBar;
    [SerializeField] private RectTransform freeSpinBarHandle;
    [SerializeField] private TMP_Text freeSpinCount;
    [SerializeField] private TMP_Text FreeSpin_Text;
    [SerializeField] private TMP_Text Wild_Text;
    [SerializeField] protected internal GameObject FreeSpinPopup_Object;
    //[SerializeField] private Button FreeSpin_Button;

    [SerializeField] private Button m_AwakeGameButton;


    private bool isOpen;
    private bool isMusic = true;
    private bool isSound = true;
    private bool isExit = false;

    private int FreeSpins = 0;
    private int PopupActivatedCount = 0;

    private Tween WinPopupTextTween;
    private Tween ClosePopupTween;

    //private void Awake()
    //{
    //    if (spalsh_screen) spalsh_screen.SetActive(true);
    //    StartCoroutine(LoadingRoutine());
    //}

    private void Awake()
    {
        SimulateClickByDefault();
    }

    private IEnumerator LoadingRoutine()
    {
        StartCoroutine(LoadingTextAnimate());
        float fillAmount = 0.7f;
        progressbar.DOFillAmount(fillAmount, 1f).SetEase(Ease.Linear);
        progressbarHandle.DOAnchorPosX(20 + (fillAmount * (510 - 20)), 2f, true).SetEase(Ease.Linear);
        yield return new WaitUntil(() => !socketManager.isLoading);
        progressbar.DOFillAmount(1, 1f).SetEase(Ease.Linear);
        progressbarHandle.DOAnchorPosX(510, 1f, true).SetEase(Ease.Linear);
        yield return new WaitForSecondsRealtime(1f);
        if (spalsh_screen) spalsh_screen.SetActive(false);
        StopCoroutine(LoadingTextAnimate());
    }

    private IEnumerator LoadingTextAnimate()
    {
        while (true)
        {
            if (loadingText) loadingText.text = "Loading.";
            yield return new WaitForSeconds(0.5f);
            if (loadingText) loadingText.text = "Loading..";
            yield return new WaitForSeconds(0.5f);
            if (loadingText) loadingText.text = "Loading...";
            yield return new WaitForSeconds(0.5f);
        }
    }
    private void Start()
    {

        if (Quit_button) Quit_button.onClick.RemoveAllListeners();
        if (Quit_button) Quit_button.onClick.AddListener(delegate { OpenPopup(QuitPopupObject); if (audioController) audioController.PlayButtonAudio(); });

        if (no_button) no_button.onClick.RemoveAllListeners();
        if (no_button) no_button.onClick.AddListener(delegate { if (!isExit) ClosePopup(QuitPopupObject); if (audioController) audioController.PlayButtonAudio(); });

        if (cancel_button) cancel_button.onClick.RemoveAllListeners();
        if (cancel_button) cancel_button.onClick.AddListener(delegate { if (!isExit) ClosePopup(QuitPopupObject); if (audioController) audioController.PlayButtonAudio(); });

        if (yes_button) yes_button.onClick.RemoveAllListeners();
        if (yes_button) yes_button.onClick.AddListener(CallOnExitFunction);

        if (Paytable_Button) Paytable_Button.onClick.RemoveAllListeners();
        if (Paytable_Button) Paytable_Button.onClick.AddListener(delegate { OpenPopup(PaytablePopup_Object); if (audioController) audioController.PlayButtonAudio(); });

        if (PaytableExit_Button) PaytableExit_Button.onClick.RemoveAllListeners();
        if (PaytableExit_Button) PaytableExit_Button.onClick.AddListener(delegate { ClosePopup(PaytablePopup_Object); if (audioController) audioController.PlayButtonAudio(); });

        if (Setting_button) Setting_button.onClick.RemoveAllListeners();
        if (Setting_button) Setting_button.onClick.AddListener(delegate { OpenPopup(settingObject); if (audioController) audioController.PlayButtonAudio(); });

        if (LBExit_Button) LBExit_Button.onClick.RemoveAllListeners();
        if (LBExit_Button) LBExit_Button.onClick.AddListener(delegate { ClosePopup(LBPopup_Object); if (audioController) audioController.PlayButtonAudio(); });

        if (Setting_exit_button) Setting_exit_button.onClick.RemoveAllListeners();
        if (Setting_exit_button) Setting_exit_button.onClick.AddListener(delegate { ClosePopup(settingObject); if (audioController) audioController.PlayButtonAudio(); });

        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener(()=>{CallOnExitFunction(); socketManager.closeSocketReactnativeCall();});

        // if (audioController) audioController.ToggleMute(false);

        isMusic = true;
        isSound = true;

        if (Menu_button) Menu_button.onClick.RemoveAllListeners();
        if (Menu_button) Menu_button.onClick.AddListener(OnMenuClick);


        if (SoundSlider) SoundSlider.onValueChanged.AddListener(delegate { ToggleSound(); });
        if (MusicSlider) MusicSlider.onValueChanged.AddListener(delegate { ToggleMusic(); });

        if (SkipWinAnimation) SkipWinAnimation.onClick.RemoveAllListeners();
        if (SkipWinAnimation) SkipWinAnimation.onClick.AddListener(SkipWin);

    }

    //HACK: Something To Do Here
    private void SimulateClickByDefault()
    {

        m_AwakeGameButton.onClick.AddListener(() => { Debug.Log("Called The Game..."); });
        m_AwakeGameButton.onClick.Invoke();
    }

    internal void LowBalPopup()
    {
        OpenPopup(LBPopup_Object);
    }

    internal void InitialiseUIData(Paylines symbolsText)
    {
        //if (Support_Button) Support_Button.onClick.RemoveAllListeners();
        //if (Support_Button) Support_Button.onClick.AddListener(delegate { UrlButtons(SupportUrl); });

        //if (Terms_Button) Terms_Button.onClick.RemoveAllListeners();
        //if (Terms_Button) Terms_Button.onClick.AddListener(delegate { UrlButtons(TermsUrl); });

        //if (Privacy_Button) Privacy_Button.onClick.RemoveAllListeners();
        //if (Privacy_Button) Privacy_Button.onClick.AddListener(delegate { UrlButtons(PrivacyUrl); });

        //StartCoroutine(DownloadImage(AbtImgUrl));

        PopulateSymbolsPayout(symbolsText);
    }

    void SkipWin()
    {
        Debug.Log("Skip win called");
        if (ClosePopupTween != null)
        {
            ClosePopupTween.Kill();
            ClosePopupTween = null;
        }
        if (WinPopupTextTween != null)
        {
            WinPopupTextTween.Kill();
            WinPopupTextTween = null;
        }
        ClosePopup(WinPopup_Object);
        slotBehaviour.CheckPopups = false;
    }
    private void PopulateSymbolsPayout(Paylines paylines)
    {
        for (int i = 0; i < SymbolsText.Length; i++)
        {
            string text = null;

            if (paylines.symbols[i].multiplier[0] != 0)
            {
                text += "5x - " + paylines.symbols[i].multiplier[0] + "X";
            }
            if (paylines.symbols[i].multiplier[1] != 0)
            {
                text += "\n4x - " + paylines.symbols[i].multiplier[1] + "X";
            }
            if (paylines.symbols[i].multiplier[2] != 0)
            {
                text += "\n3x - " + paylines.symbols[i].multiplier[2] + "X";
            }

            Debug.Log(string.Concat("<color=blue><b>", text, "</b></color>"));
            if (SymbolsText[i]) SymbolsText[i].text = text;
        }


        for (int i = 0; i < paylines.symbols.Count; i++)
        {
            switch (paylines.symbols[i].name.ToUpper())
            {
                case "JACKPOT":
                    if (Jackpot_Text) Jackpot_Text.text = paylines.symbols[i].description.ToString();
                    Debug.Log(string.Concat("<color=blue><b>", "JACKPOT", "</b></color>"));
                    break;
                case "FREESPIN":
                    if (FreeSpin_Text) FreeSpin_Text.text = paylines.symbols[i].description.ToString();
                    Debug.Log(string.Concat("<color=blue><b>", "FREESPIN", "</b></color>"));
                    break;
                case "WILD":
                    if (Wild_Text) Wild_Text.text = paylines.symbols[i].description.ToString();
                    Debug.Log(string.Concat("<color=blue><b>", "WILD", "</b></color>"));
                    break;
            }
        }
    }

    internal void DisconnectionPopup(bool isReconnection)
    {
        //if (isReconnection)
        //{
        //    OpenPopup(ReconnectPopup_Object);
        //}
        //else
        //{
        //ClosePopup(ReconnectPopup_Object);
        if (isReconnection)
        {
            ClosePopup(DisconnectPopup_Object);
        }
        else
        {
            if (!isExit)
            {
                OpenPopup(DisconnectPopup_Object);
            }
        }
        //}
    }

    internal void ADfunction()
    {
        OpenPopup(ADPopup_Object);
    }

    private void CallOnExitFunction()
    {
        isExit = true;
        print("close");
        slotBehaviour.CallCloseSocket();
        //Application.ExternalCall("window.parent.postMessage", "onExit", "*");
    }

    internal void PopulateWin(int value, double amount)
    {
        switch (value)
        {
            case 1:
                if (Win_Image) Win_Image.sprite = BigWin_Sprite;
                break;
            case 2:
                if (Win_Image) Win_Image.sprite = HugeWin_Sprite;
                break;
            case 3:
                if (Win_Image) Win_Image.sprite = MegaWin_Sprite;
                break;

        }
        if (value == 4)
            StartPopupAnim(amount, true);
        else
            StartPopupAnim(amount, false);

    }

    internal void UpdateFreeSpinData(float fillAmount, int count)
    {
        if (fillAmount < 0)
            fillAmount = 0;
        if (fillAmount > 1)
            fillAmount = 1;

        freeSpinBar.DOFillAmount(fillAmount, 0.5f).SetEase(Ease.Linear);
        freeSpinBarHandle.DOAnchorPosX(20 + (fillAmount * (510 - 20)), 0.5f).SetEase(Ease.Linear);

        freeSpinCount.text = count.ToString();
    }

    //internal void SetFreeSpinData(int count)
    //{

    //    freeSpinBar.DOFillAmount(1, 0.2f).SetEase(Ease.Linear);
    //    freeSpinBarHandle.DOAnchorPosX(510, 0.2f).SetEase(Ease.Linear);
    //    freeSpinCount.text = count.ToString();
    //}

    private void StartFreeSpins(int spins)
    {
        //if (MainPopup_Object) MainPopup_Object.SetActive(false);
        //if (FreeSpinPopup_Object) FreeSpinPopup_Object.SetActive(false);
        freeSpinBar.DOFillAmount(1, 0.2f).SetEase(Ease.Linear);
        freeSpinBarHandle.DOAnchorPosX(510, 0.2f).SetEase(Ease.Linear);
        freeSpinCount.text = spins.ToString();
        slotBehaviour.FreeSpin(spins);
    }

    internal void FreeSpinProcess(int spins)
    {
        FreeSpins = spins;
        //if (FreeSpinPopup_Object) FreeSpinPopup_Object.SetActive(true);
        //if (Free_Text) Free_Text.text = spins.ToString() + " Free spins awarded.";
        //if (MainPopup_Object) MainPopup_Object.SetActive(true);
        StartFreeSpins(spins);
    }

    private void StartPopupAnim(double amount, bool jackpot = false)
    {
        double initAmount = 0;
        OpenPopup(WinPopup_Object);
        if (jackpot)
        {
            if (jackpot_Object) jackpot_Object.SetActive(true);
            // OpenPopup(jackpot_Object);
        }
        else
        {
            if (normalWin_Object) normalWin_Object.SetActive(true);
        }


        //if (MainPopup_Object) MainPopup_Object.SetActive(true);

        WinPopupTextTween = DOTween.To(() => initAmount, (val) => initAmount = val, amount, 3f).OnUpdate(() =>
        {
            // if (jackpot)
            // {
            //     if (jackpot_Text) jackpot_Text.text = initAmount.ToString("f3");

            // }
            // else
            // {

            if (Win_Text) Win_Text.text = initAmount.ToString("f3");

            // }
        });

        ClosePopupTween = DOVirtual.DelayedCall(4f, () =>
        {
            ClosePopup(WinPopup_Object);
            if (jackpot_Object.activeSelf)
                jackpot_Object.SetActive(false);
            if (normalWin_Object.activeSelf)
                normalWin_Object.SetActive(false);


            slotBehaviour.CheckPopups = false;
        });
    }
    void OnMenuClick()
    {
        if (audioController) audioController.PlayButtonAudio();
        isOpen = !isOpen;

        if (isOpen)
        {
            if (Menu_button) Menu_button.image.sprite = MenuCloseSprite;
            for (int i = 0; i < Menu_button_grp.childCount - 1; i++)
            {
                Menu_button_grp.GetChild(i).DOLocalMoveY(-150 * (i + 1), 0.1f * (i + 1));
            }
        }
        else
        {

            if (Menu_button) Menu_button.image.sprite = MenuOpenSprite;

            for (int i = 0; i < Menu_button_grp.childCount - 1; i++)
            {
                Menu_button_grp.GetChild(i).DOLocalMoveY(0, 0.1f * (i + 1));
            }
        }


    }
    private void OpenPopup(GameObject Popup)
    {
        //if (audioController) audioController.PlayButtonAudio();
        if (Popup) Popup.SetActive(true);
        PopupActivatedCount++;
        Debug.Log(string.Concat("<color=red><b>", PopupActivatedCount, "</b></color>"));
        if (MainPopup_Object) MainPopup_Object.SetActive(true);
    }

    //private void ClosePopup(GameObject Popup)
    //{
    //    if (audioController) audioController.PlayButtonAudio();
    //    if (Popup) Popup.SetActive(false);
    //    if (!DisconnectPopup_Object.activeSelf)
    //    {
    //        if (MainPopup_Object) MainPopup_Object.SetActive(false);
    //    }
    //}

    private void ClosePopup(GameObject Popup)
    {
        //if (audioController) audioController.PlayButtonAudio();
        if (Popup) Popup.SetActive(false);
        if (PopupActivatedCount > 0)
        {
            PopupActivatedCount--;
            Debug.Log(string.Concat("<color=cyan><b>", PopupActivatedCount, "</b></color>"));
        }

        if (PopupActivatedCount <= 0)
        {
            if (!DisconnectPopup_Object.activeSelf)
            {
                if (MainPopup_Object) MainPopup_Object.SetActive(false);
            }
        }
    }

    private void ToggleMusic()
    {
        float value = MusicSlider.value;
        audioController.ToggleMute(value, "bg");

    }

    private void ToggleSound()
    {

        float value = SoundSlider.value;
        if (audioController) audioController.ToggleMute(value, "button");
        if (audioController) audioController.ToggleMute(value, "wl");
    }
}
