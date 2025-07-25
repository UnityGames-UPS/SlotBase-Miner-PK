using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;

public class SlotBehaviour : MonoBehaviour
{

    [Header("Sprites")]
    [SerializeField]
    private Sprite[] myImages;  //images taken initially

    [Header("Slot Images")]
    [SerializeField]
    private List<SlotImage> images;     //class to store total images
    [SerializeField]
    private List<SlotImage> Tempimages;     //class to store the result matrix


    [Header("Slots Elements")]
    [SerializeField]
    private LayoutElement[] Slot_Elements;

    [Header("Slots Transforms")]
    [SerializeField]
    private Transform[] Slot_Transform;

    private Dictionary<int, string> y_string = new Dictionary<int, string>();

    [Header("Buttons")]
    [SerializeField]
    private Button SlotStart_Button;
    [SerializeField]
    private Button AutoSpin_Button;
    [SerializeField] private Button AutoSpinStop_Button;

    [Header("Miscellaneous UI")]
    [SerializeField]
    private TMP_Text Balance_text;
    [SerializeField]
    private TMP_Text TotalBet_text;
    [SerializeField]
    private TMP_Text BetPerLine_text;
    [SerializeField]
    private TMP_Text Lines_text;
    [SerializeField]
    private TMP_Text TotalWin_text;
    [SerializeField]
    private Button MaxBet_Button;
    [SerializeField]
    private Button BetPlus_Button;
    [SerializeField]
    private Button BetMinus_Button;
    [SerializeField]
    private Button LinePlus_Button;
    [SerializeField]
    private Button LineMinus_Button;

    [Header("Audio Management")]
    [SerializeField] private AudioController audioController;
    [SerializeField] private UIManager uiManager;

    int tweenHeight = 0;  //calculate the height at which tweening is done

    [SerializeField]
    private GameObject Image_Prefab;    //icons prefab

    [SerializeField]
    private PayoutCalculation PayCalculator;

    private List<Tweener> alltweens = new List<Tweener>();


    [SerializeField]
    private List<Transform> TempList = new List<Transform>();  //stores the sprites whose animation is running at present 

    [SerializeField]
    private int IconSizeFactor = 100;       //set this parameter according to the size of the icon and spacing

    private int numberOfSlots = 5;          //number of columns

    [SerializeField]
    int verticalVisibility = 3;

    [SerializeField]
    private SocketIOManager SocketManager;
    [SerializeField] private Button Turbo_Button;
    [SerializeField] private Sprite TurboToggleSprite;
    [SerializeField] private Button StopSpin_Button;
    Coroutine AutoSpinRoutine = null;
    Coroutine tweenroutine = null;
    Coroutine FreeSpinRoutine = null;

    private Tweener WinTween = null;
    private Tween BalanceTween;
    bool IsAutoSpin = false;
    bool IsSpinning = false;
    bool IsFreeSpin = false;
    private bool CheckSpinAudio = false;
    internal bool CheckPopups = false;
    private int BetCounter = 0;
    private int LineCounter = 0;
    private double currentBalance = 0;
    private double currentTotalBet = 0;
    protected int Lines = 20;
    private int FreeSpins = 0;
    private bool StopSpinToggle;
    private bool IsTurboOn;
    private float SpinDelay = 0.2f;
    private bool WasAutoSpinOn;
    private void Awake()
    {
        OnApplicationFocus(true);
    }

    private void Start()
    {
        IsAutoSpin = false;
        if (SlotStart_Button) SlotStart_Button.onClick.RemoveAllListeners();
        if (SlotStart_Button) SlotStart_Button.onClick.AddListener(delegate { StartSlots(); });

        if (BetPlus_Button) BetPlus_Button.onClick.RemoveAllListeners();
        if (BetPlus_Button) BetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); });
        if (BetMinus_Button) BetMinus_Button.onClick.RemoveAllListeners();
        if (BetMinus_Button) BetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); });

        if (LinePlus_Button) LinePlus_Button.onClick.RemoveAllListeners();
        if (LinePlus_Button) LinePlus_Button.onClick.AddListener(delegate { ChangeLine(true); });
        if (LineMinus_Button) LineMinus_Button.onClick.RemoveAllListeners();
        if (LineMinus_Button) LineMinus_Button.onClick.AddListener(delegate { ChangeLine(false); });

        if (MaxBet_Button) MaxBet_Button.onClick.RemoveAllListeners();
        if (MaxBet_Button) MaxBet_Button.onClick.AddListener(MaxBet);

        if (AutoSpin_Button) AutoSpin_Button.onClick.RemoveAllListeners();
        if (AutoSpin_Button) AutoSpin_Button.onClick.AddListener(AutoSpin);

        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.RemoveAllListeners();
        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.AddListener(StopAutoSpin);

        if (StopSpin_Button) StopSpin_Button.onClick.RemoveAllListeners();
        if (StopSpin_Button) StopSpin_Button.onClick.AddListener(() => { audioController.PlayButtonAudio(); StopSpinToggle = true; StopSpin_Button.gameObject.SetActive(false); });

        if (Turbo_Button) Turbo_Button.onClick.RemoveAllListeners();
        if (Turbo_Button) Turbo_Button.onClick.AddListener(TurboToggle);

        tweenHeight = (myImages.Length * IconSizeFactor) - 280;
    }

    void TurboToggle()
    {
        audioController.PlayButtonAudio();
        if (IsTurboOn)
        {
            IsTurboOn = false;
            Turbo_Button.GetComponent<ImageAnimationOrigin>().StopAnimation();
            Turbo_Button.image.sprite = TurboToggleSprite;
            // Turbo_Button.image.color=new Color(0.86f,0.86f,0.86f,1);
        }
        else
        {
            IsTurboOn = true;
            Turbo_Button.GetComponent<ImageAnimationOrigin>().StartAnimation();
            // Turbo_Button.image.color=new Color(1,1,1,1);
        }
    }
    private void AutoSpin()
    {
        if (!IsAutoSpin)
        {

            IsAutoSpin = true;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(true);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);

            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                AutoSpinRoutine = null;
            }
            AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());

        }
    }

    private void StopAutoSpin()
    {
        if (IsAutoSpin)
        {
            IsAutoSpin = false;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
            StartCoroutine(StopAutoSpinCoroutine());
        }

    }

    private IEnumerator AutoSpinCoroutine()
    {
        while (IsAutoSpin)
        {
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
            yield return new WaitForSeconds(SpinDelay);
        }
        WasAutoSpinOn = false;
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        if (!IsFreeSpin)
            ToggleButtonGrp(true);
        if (AutoSpinRoutine != null || tweenroutine != null)
        {
            StopCoroutine(AutoSpinRoutine);
            StopCoroutine(tweenroutine);
            tweenroutine = null;
            AutoSpinRoutine = null;
            StopCoroutine(StopAutoSpinCoroutine());
        }
    }

    private void CompareBalance()
    {
        if (currentBalance < currentTotalBet)
        {
            uiManager.LowBalPopup();
            //if (AutoSpin_Button) AutoSpin_Button.interactable = false;
            //if (SlotStart_Button) SlotStart_Button.interactable = false;
        }
        //else
        //{
        //    if (AutoSpin_Button) AutoSpin_Button.interactable = true;
        //    if (SlotStart_Button) SlotStart_Button.interactable = true;
        //}
    }

    internal void FetchLines(string LineVal, int count)
    {
        y_string.Add(count + 1, LineVal);

        //HACK: Line Equation To Be Implemented
        //StaticLine_Texts[count].text = (count + 1).ToString();
        //StaticLine_Objects[count].SetActive(true);
    }

    #region FreeSpin
    internal void FreeSpin(int spins)
    {
        if (!IsFreeSpin)
        {
            //if (FSnum_text) FSnum_text.text = spins.ToString();
            //if (FSBoard_Object) FSBoard_Object.SetActive(true);

            if (uiManager.FreeSpinPopup_Object) uiManager.FreeSpinPopup_Object.SetActive(true);
            IsFreeSpin = true;
            ToggleButtonGrp(false);

            if (FreeSpinRoutine != null)
            {
                StopCoroutine(FreeSpinRoutine);
                FreeSpinRoutine = null;
            }
            FreeSpinRoutine = StartCoroutine(FreeSpinCoroutine(spins));
        }
    }

    private IEnumerator FreeSpinCoroutine(int spinchances)
    {
        int i = 0;

        yield return new WaitForSeconds(0.6f);

        while (i <= spinchances)
        {
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
            yield return new WaitForSeconds(0.6f);
            i++;
            uiManager.UpdateFreeSpinData(1 - ((float)i / (float)spinchances), spinchances - i);
            //if (FSnum_text) FSnum_text.text = (spinchances - i).ToString();
        }
        //if (FSBoard_Object) FSBoard_Object.SetActive(false);
        if (uiManager.FreeSpinPopup_Object) uiManager.FreeSpinPopup_Object.SetActive(false);
        if (WasAutoSpinOn)
        {
            AutoSpin();
        }
        else
        {
            ToggleButtonGrp(true);
        }
        IsFreeSpin = false;
    }
    #endregion

    internal void GenerateStaticLine(TMP_Text LineID_Text)
    {
        DestroyStaticLine();
        int LineID = 1;
        try
        {
            LineID = int.Parse(LineID_Text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Exception while parsing " + e.Message);
        }
        List<int> y_points = null;
        y_points = y_string[LineID]?.Split(',')?.Select(Int32.Parse)?.ToList();
        PayCalculator.GeneratePayoutLinesBackend(y_points, y_points.Count, true);
    }

    //Destroy Static Lines from button hovers
    internal void DestroyStaticLine()
    {
        PayCalculator.ResetStaticLine();
    }

    private void MaxBet()
    {
        if (audioController) audioController.PlayButtonAudio();
        BetCounter = SocketManager.initialData.bets.Count - 1;
        if (BetPerLine_text) BetPerLine_text.text = SocketManager.initialData.bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.bets[BetCounter] * SocketManager.initialData.lines.Count).ToString();
        //if (TotalBet_text) TotalBet_text.text = "99999";
        currentTotalBet = SocketManager.initialData.bets[BetCounter] * Lines;
    }

    internal void CallCloseSocket()
    {
       StartCoroutine(SocketManager.CloseSocket());
    }
    private void ChangeLine(bool IncDec)
    {
        if (audioController) audioController.PlayButtonAudio();

        if (IncDec)
        {
            if (LineCounter < SocketManager.initialData.LinesCount.Count - 1)
            {
                LineCounter++;
            }
        }
        else
        {
            if (LineCounter > 0)
            {
                LineCounter--;
            }
        }

        if (Lines_text) Lines_text.text = SocketManager.initialData.LinesCount[LineCounter].ToString();


    }


    private void ChangeBet(bool IncDec)
    {
        if (audioController) audioController.PlayButtonAudio();
        if (IncDec)
        {
            BetCounter++;
            if (BetCounter > SocketManager.initialData.bets.Count - 1)
            {
                BetCounter = 0;
            }
        }
        else
        {
            BetCounter--;
            if (BetCounter < 0)
            {
                BetCounter = SocketManager.initialData.bets.Count - 1;
            }
        }

        currentTotalBet = SocketManager.initialData.bets[BetCounter] * SocketManager.initialData.lines.Count;
        if (BetPerLine_text) BetPerLine_text.text = SocketManager.initialData.bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.bets[BetCounter] * SocketManager.initialData.lines.Count).ToString();
        // CompareBalance();
    }

    internal void shuffleInitialMatrix()
    {
        for (int i = 0; i < Tempimages.Count; i++)
        {
            for (int j = 0; j < Tempimages[i].slotImages.Count; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, myImages.Length);
                Tempimages[i].slotImages[j].sprite = myImages[randomIndex];
            }
        }
    }

    internal void SetInitialUI()
    {
        try
        {
            //BetCounter = SocketManager.initialData.Bets.Count - 1;
            BetCounter = 0;
            LineCounter = SocketManager.initialData.lines.Count - 1;
            if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.bets[BetCounter] * SocketManager.initialData.lines.Count).ToString();
            if (Lines_text) Lines_text.text = SocketManager.initialData.lines.Count.ToString();
            //if (TotalWin_text) TotalWin_text.text = SocketManager.playerdata.currentWining.ToString();
            if (Balance_text) Balance_text.text = SocketManager.playerdata.balance.ToString();
            if (BetPerLine_text) BetPerLine_text.text = SocketManager.initialData.bets[BetCounter].ToString();
            currentBalance = SocketManager.playerdata.balance;
            currentTotalBet = SocketManager.initialData.bets[BetCounter] * Lines;
            //_bonusManager.PopulateWheel(SocketManager.bonusdata);
            CompareBalance();
            uiManager.InitialiseUIData(
                SocketManager.initUIData.paylines);
        }
        catch (Exception e)
        {
            Debug.Log(string.Concat("Something Went Wrong in SetInitialUI ", "<color=cyan><b>", e, "</b></color>"));
        }
    }

    //reset the layout after populating the slots
    internal void LayoutReset(int number)
    {
        if (Slot_Elements[number]) Slot_Elements[number].ignoreLayout = true;
        if (SlotStart_Button) SlotStart_Button.interactable = true;
    }

    private void OnApplicationFocus(bool focus)
    {
        Debug.Log(string.Concat("<color=cyan><b>", focus, "</b></color>"));
        audioController.CheckFocusFunction(focus, CheckSpinAudio);
    }
    private void StartSlots(bool autoSpin = false)
    {
        if (audioController) audioController.PlayButtonAudio("spin");

        if (!autoSpin)
        {
            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                StopCoroutine(tweenroutine);
                tweenroutine = null;
                AutoSpinRoutine = null;
            }
        }

        if (SlotStart_Button) SlotStart_Button.interactable = false;
        if (TempList.Count > 0)
        {
            StopGameAnimation();
        }
        PayCalculator.ResetLines();
        tweenroutine = StartCoroutine(TweenRoutine());
    }

    //manage the Routine for spinning of the slots
    private IEnumerator TweenRoutine()
    {
        WinningsAnim(false);

        if (currentBalance < currentTotalBet && !IsFreeSpin)
        {
            CompareBalance();
            StopAutoSpin();
            yield return new WaitForSeconds(1);
            ToggleButtonGrp(true);
            yield break;
        }

        if (audioController) audioController.PlaySpinAudio();
        if (TotalWin_text) TotalWin_text.text = "0.00";
        IsSpinning = true;
        CheckSpinAudio = true;

        ToggleButtonGrp(false);
        if (!IsTurboOn && !IsFreeSpin && !IsAutoSpin)
        {
            StopSpin_Button.gameObject.SetActive(true);
        }

        for (int i = 0; i < numberOfSlots; i++)
        {
            InitializeTweening(Slot_Transform[i]);
            yield return new WaitForSeconds(0.1f);
        }

        if (!IsFreeSpin)
        {
            BalanceDeduction();
        }

        SocketManager.AccumulateResult(BetCounter);

        yield return new WaitUntil(() => SocketManager.isResultdone);


        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                int resultNum = int.Parse(SocketManager.resultData.matrix[i][j]);
                // print("resultNum: " + resultNum);
                // print("image loc: " + j + " " + i);
                // PopulateAnimationSprites(Tempimages[j].slotImages[i].GetComponent<ImageAnimation>(), resultNum);
                Tempimages[j].slotImages[i].sprite = myImages[resultNum];
            }
        }

        // if (IsTurboOn || IsFreeSpin)
        // {
        //     yield return new WaitForSeconds(0.1f);
        // }
        // else
        if (!(IsTurboOn || IsFreeSpin))
        {
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(0.3f);
                if (StopSpinToggle)
                {
                    break;
                }
            }
            StopSpin_Button.gameObject.SetActive(false);
        }

        for (int i = 0; i < numberOfSlots; i++)
        {
            yield return StopTweening(5, Slot_Transform[i], i, StopSpinToggle);
        }

        StopSpinToggle = false;

        yield return alltweens[^1].WaitForCompletion();
        KillAllTweens();

        if (SocketManager.resultData.payload.winAmount > 0)
        {
            SpinDelay = 1.2f;
        }
        else
        {
            SpinDelay = 0.2f;
        }

        if (audioController) audioController.StopSpinAudio();
        if (TotalWin_text) TotalWin_text.text = SocketManager.resultData.payload.winAmount.ToString("f2");
        if (Balance_text) Balance_text.text = SocketManager.playerdata.balance.ToString("f2");

        if (SocketManager.resultData.payload.winAmount > 0)
        {
            List<int> winLine = new();
            foreach (var item in SocketManager.resultData.payload.wins)
            {
                winLine.Add(item.line);
            }
            CheckPayoutLineBackend(winLine);
        }

        // CheckPayoutLineBackend(SocketManager.resultData.linesToEmit, SocketManager.resultData.FinalsymbolsToEmit, SocketManager.resultData.jackpot);

        currentBalance = SocketManager.playerdata.balance;

        CheckPopups = true;

        CheckForFeaturesAnimation();

        if (SocketManager.resultData.jackpot.isTriggered)
        {
            CheckPopups = true;
            uiManager.PopulateWin(4, SocketManager.resultData.jackpot.amount);
            yield return new WaitUntil(() => !CheckPopups);
        }
        else
        {
            CheckPopups = true;
            CheckWinPopups();
        }

        yield return new WaitUntil(() => !CheckPopups);
        BalanceTween?.Kill();
        if (TotalWin_text) TotalWin_text.text = SocketManager.resultData.payload.winAmount.ToString("f2");
        if (Balance_text) Balance_text.text = SocketManager.playerdata.balance.ToString("f2");

        if (!IsAutoSpin && !IsFreeSpin)
        {
            ToggleButtonGrp(true);
            IsSpinning = false;
        }
        else
        {
            IsSpinning = false;
        }

        if (SocketManager.resultData.freeSpin.isFreeSpin)
        {
            if (IsAutoSpin)
            {

                StopAutoSpin();
                // yield return new WaitForSeconds(0.1f);
                WasAutoSpinOn = true;
            }
            if (IsFreeSpin)
            {
                IsFreeSpin = false;
                if (FreeSpinRoutine != null)
                {
                    StopCoroutine(FreeSpinRoutine);
                    FreeSpinRoutine = null;
                }
            }
            StartCoroutine(uiManager.FreeSpinProcess((int)SocketManager.resultData.freeSpin.count));
        }
    }

    private void CheckWinPopups()
    {
        if (SocketManager.resultData.payload.winAmount >= currentTotalBet * 10 && SocketManager.resultData.payload.winAmount < currentTotalBet * 15 && SocketManager.resultData.jackpot.amount == 0)
        {
            uiManager.PopulateWin(1, SocketManager.resultData.payload.winAmount);

        }
        else if (SocketManager.resultData.payload.winAmount >= currentTotalBet * 15 && SocketManager.resultData.payload.winAmount < currentTotalBet * 20 && SocketManager.resultData.jackpot.amount == 0)
        {
            uiManager.PopulateWin(2, SocketManager.resultData.payload.winAmount);

        }
        else if (SocketManager.resultData.payload.winAmount >= currentTotalBet * 20 && SocketManager.resultData.jackpot.amount == 0)
        {
            uiManager.PopulateWin(3, SocketManager.resultData.payload.winAmount);

        }
        else
        {
            CheckPopups = false;
        }
    }

    private void BalanceDeduction()
    {
        double bet = 0;
        double balance = 0;
        try
        {
            bet = double.Parse(TotalBet_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }

        try
        {
            balance = double.Parse(Balance_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }
        double initAmount = balance;

        balance = balance - bet;

        BalanceTween = DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
        {
            if (Balance_text) Balance_text.text = initAmount.ToString("f2");
        });
    }


    internal void CheckBonusGame()
    {
        //_bonusManager.StartBonus((int)SocketManager.resultData.BonusStopIndex);
        // if (SocketManager.resultData.isBonus)
        // {
        //     //_bonusManager.StartBonus((int)SocketManager.resultData.BonusStopIndex);
        // }
        // else
        // {
        //     CheckPopups = false;
        // }
    }

    void ToggleButtonGrp(bool toggle)
    {

        if (SlotStart_Button) SlotStart_Button.interactable = toggle;
        if (MaxBet_Button) MaxBet_Button.interactable = toggle;
        if (AutoSpin_Button) AutoSpin_Button.interactable = toggle;
        if (LinePlus_Button) LinePlus_Button.interactable = toggle;
        if (LineMinus_Button) LineMinus_Button.interactable = toggle;
        if (BetMinus_Button) BetMinus_Button.interactable = toggle;
        if (BetPlus_Button) BetPlus_Button.interactable = toggle;

    }

    //start the icons animation
    private void StartGameAnimation(Transform animObjects)
    {
        animObjects.DOScale(1.15f, 0.5f).SetLoops(-1, LoopType.Yoyo);
        //ImageAnimation temp = animObjects.GetComponent<ImageAnimation>();
        //temp.StartAnimation();
        TempList.Add(animObjects);
    }

    //stop the icons animation
    private void StopGameAnimation()
    {
        for (int i = 0; i < TempList.Count; i++)
        {
            DOTween.Kill(TempList[i]);
            TempList[i].localScale = Vector3.one;
        }
        TempList.Clear();
    }

    private void CheckPayoutLineBackend(List<int> LineId, double jackpot = 0)
    {
        List<int> y_points = null;
        if (LineId.Count > 0)
        {
            if (audioController) audioController.PlayWLAudio("win");

            for (int i = 0; i < LineId.Count; i++)
            {
                y_points = y_string[LineId[i] + 1]?.Split(',')?.Select(Int32.Parse)?.ToList();
                PayCalculator.GeneratePayoutLinesBackend(y_points, y_points.Count);
            }

            if (jackpot > 0)
            {
                // if (audioController.m_Player_Listener.enabled) audioController.m_Win_Audio.Play();
                for (int i = 0; i < Tempimages.Count; i++)
                {
                    for (int k = 0; k < Tempimages[i].slotImages.Count; k++)
                    {
                        StartGameAnimation(Tempimages[i].slotImages[k].transform);
                    }
                }
            }
            else
            {
                List<KeyValuePair<int, int>> coords = new();
                for (int j = 0; j < LineId.Count; j++)
                {
                    for (int k = 0; k < SocketManager.resultData.payload.wins[j].positions.Count; k++)
                    {
                        int rowIndex = SocketManager.initialData.lines[LineId[j]][k];
                        int columnIndex = k;
                        coords.Add(new KeyValuePair<int, int>(rowIndex, columnIndex));
                    }
                }

                foreach (var coord in coords)
                {
                    int rowIndex = coord.Key;
                    int columnIndex = coord.Value;
                    StartGameAnimation(Tempimages[columnIndex].slotImages[rowIndex].gameObject.transform);
                }
            }
            //  WinningsAnim(true);               //change it here ashu
        }
        else
        {

            if (audioController) audioController.StopWLAaudio();
        }

    }

    private void CheckForFeaturesAnimation()
    {
        bool playScatter = false;
        bool playJackpot = false;
        bool playFreespin = false;
        // if (SocketManager.resultData.scatter.amount > 0)
        // {
        //     playScatter = true;
        // }
        if (SocketManager.resultData.jackpot.amount > 0)
        {
            playJackpot = true;
        }
        if (SocketManager.resultData.freeSpin.isFreeSpin)
        {
            playFreespin = true;
        }
        PlayFeatureAnimation(playScatter, playJackpot, playFreespin);
    }
    private void PlayFeatureAnimation(bool scatter = false, bool jackpot = false, bool freeSpin = false)
    {
        for (int i = 0; i < SocketManager.resultData.matrix.Count; i++)
        {
            for (int j = 0; j < SocketManager.resultData.matrix[i].Count; j++)
            {

                if (int.TryParse(SocketManager.resultData.matrix[i][j], out int parsedNumber))
                {
                    // if (scatter && parsedNumber == 12)
                    // {
                    //     StartGameAnimation(Tempimages[j].slotImages[i].transform);
                    // }
                    if (jackpot && parsedNumber == 10)
                    {
                        StartGameAnimation(Tempimages[j].slotImages[i].transform);
                    }
                    if (freeSpin && parsedNumber == 11)
                    {
                        StartGameAnimation(Tempimages[j].slotImages[i].transform);
                    }
                }

            }
        }
    }
    private void GenerateMatrix(int value)
    {
        for (int j = 0; j < 4; j++)
        {
            Tempimages[value].slotImages.Add(images[value].slotImages[images[value].slotImages.Count - 5 + j]);
        }
    }

    #region TweeningCode
    private void InitializeTweening(Transform slotTransform)
    {
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        Tweener tweener = slotTransform.DOLocalMoveY(-tweenHeight, 0.2f).SetLoops(-1, LoopType.Restart).SetDelay(0);
        tweener.Play();
        alltweens.Add(tweener);
    }

    private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, bool isStop)
    {
        alltweens[index].Pause();
        int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        alltweens[index] = slotTransform.DOLocalMoveY(-tweenpos + 45, 0.5f).SetEase(Ease.OutElastic);
        if (!isStop)
        {
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            yield return null;
        }
    }


    private void KillAllTweens()
    {
        for (int i = 0; i < numberOfSlots; i++)
        {
            alltweens[i].Kill();
        }
        alltweens.Clear();

    }
    #endregion

    private void WinningsAnim(bool IsStart)
    {
        if (IsStart)
        {
            WinTween = TotalWin_text.transform.DOScale(new Vector2(1.5f, 1.5f), 1f).SetLoops(-1, LoopType.Yoyo).SetDelay(0);
        }
        else
        {
            WinTween.Kill();
            TotalWin_text.gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
        }
    }
}

[Serializable]
public class SlotImage
{
    public List<Image> slotImages = new List<Image>(10);
}

