// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2019 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Lypyl (lypyl@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using System;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallConnect.Utility;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Utility;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{

    public class DaggerfallTravelPopUp : DaggerfallPopupWindow
    {
        #region fields
        DaggerfallTravelMapWindow travelWindow = null;

        const string nativeImgName = "TRAV0I04.IMG";

        const float secondsCountdownTickFastTravel = 0.05f; // time used for fast travel countdown for one tick
        TravelTimeCalculator travelTimeCalculator = new TravelTimeCalculator();

        Color32 toggleColor = new Color32(85, 117, 48, 255);
        const string greenCheckboxTextureFilename = "GreenCheckbox";

        Panel travelPanel;
        Panel speedToggleColorPanel;
        Panel transportToggleColorPanel;
        Panel sleepToggleColorPanel;

        Button beginButton;
        Button exitButton;
        Button cautiousToggleButton;
        Button recklessToggleButton;
        Button footHorseToggleButton;
        Button shipToggleButton;
        Button campOutToggleButton;
        Button innToggleButton;
        Texture2D nativeTexture;
        Texture2D greenCheckboxTexture;

        //rects
        Rect nativePanelRect        = new Rect(49, 28, 223, 97);
        Rect exitButtonRect         = new Rect(222, 112, 48, 10);
        Rect beginButtonRect        = new Rect(222, 98, 48, 10);
        Rect cautiousButtonRect     = new Rect(50, 51, 108, 9);
        Rect recklessButtonRect     = new Rect(50, 61, 108, 9);
        Rect footHorseButtonRect    = new Rect(163, 51, 108, 9);
        Rect shipButtonRect         = new Rect(163, 61, 108, 9);
        Rect innsButtonRect         = new Rect(50, 83, 108, 9);
        Rect campoutButtonRect      = new Rect(163, 83, 108, 9);

        Vector2 colorPanelSize      = new Vector2(4.75f, 4.75f);

        Vector2 cautiousPanelPos    = new Vector2(52.25f, 53);
        Vector2 recklessPanelPos    = new Vector2(52.25f, 63.25f);
        Vector2 innPanelPos         = new Vector2(52.25f, 85.5f);
        Vector2 campoutPos          = new Vector2(165, 85.5f);
        Vector2 footPos             = new Vector2(165, 53);
        Vector2 shipPos             = new Vector2(165, 63.25f);
        DFPosition endPos           = new DFPosition(109, 158);

        TextLabel availableGoldLabel;
        TextLabel tripCostLabel;
        TextLabel travelTimeLabel;

        int travelTimeMinutes;
        int countdownValueTravelTimeDays; // used for remaining days in fast travel countdown
        bool doFastTravel = false; // flag used to indicate Update() function that fast travel should happen
        float waitTimer = 0;

        bool speedCautious  = true;
        bool travelShip     = true;
        bool sleepModeInn   = true;

        bool hasHorse = false;
        bool hasCart = false;
        bool hasShip = false;

        #endregion

        #region Properties

        public DFPosition EndPos { get { return endPos; } internal set { endPos = value;} }
        internal DaggerfallTravelMapWindow TravelWindow { get { return travelWindow; } set { travelWindow = value; } }
        public bool SpeedCautious { get { return speedCautious;} set {speedCautious = value; } }
        public bool TravelShip { get { return travelShip;} set { travelShip = value;} }
        public bool SleepModeInn { get { return sleepModeInn; } set { sleepModeInn = value; } }

        #endregion

        #region constructors

        public DaggerfallTravelPopUp(IUserInterfaceManager uiManager, IUserInterfaceWindow previousWindow = null, DaggerfallTravelMapWindow travelWindow = null)
            : base(uiManager, previousWindow)
        {
            this.travelWindow = travelWindow;
        }

        #endregion


        #region User InterFace

        protected override void Setup()
        {
            base.Setup();

            nativeTexture = DaggerfallUI.GetTextureFromImg(nativeImgName);
            if (!nativeTexture)
                throw new System.Exception("DaggerfallTravelMap: Could not load native texture.");

            greenCheckboxTexture = DaggerfallUI.GetTextureFromResources(greenCheckboxTextureFilename);

            ParentPanel.BackgroundColor = Color.clear;

            travelPanel = DaggerfallUI.AddPanel(nativePanelRect, NativePanel);
            travelPanel.BackgroundTexture = nativeTexture;

            availableGoldLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(148, 97), "0", NativePanel);
            availableGoldLabel.MaxCharacters = 12;

            tripCostLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(117, 107), "0", NativePanel);
            tripCostLabel.MaxCharacters = 18;

            travelTimeLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(129, 117), "0", NativePanel);
            travelTimeLabel.MaxCharacters = 16;

            speedToggleColorPanel = DaggerfallUI.AddPanel(new Rect(cautiousPanelPos, colorPanelSize), NativePanel);
            SetToggleLook(speedToggleColorPanel);

            sleepToggleColorPanel = DaggerfallUI.AddPanel(new Rect(innPanelPos, colorPanelSize), NativePanel);
            SetToggleLook(sleepToggleColorPanel);

            transportToggleColorPanel = DaggerfallUI.AddPanel(new Rect(footPos, colorPanelSize), NativePanel);
            SetToggleLook(transportToggleColorPanel);

            SetupButtons();
            Refresh();
        }

        private void SetToggleLook(Panel toggle)
        {
            if (greenCheckboxTexture)
                toggle.BackgroundTexture = greenCheckboxTexture;
            else
                toggle.BackgroundColor = toggleColor;
        }

        void SetupButtons()
        {
            beginButton = DaggerfallUI.AddButton(beginButtonRect, NativePanel );
            beginButton.OnMouseClick += BeginButtonOnClickHandler;

            exitButton = DaggerfallUI.AddButton(exitButtonRect, NativePanel);
            exitButton.OnMouseClick += ExitButtonOnClickHandler;

            cautiousToggleButton = DaggerfallUI.AddButton(cautiousButtonRect, NativePanel);
            cautiousToggleButton.OnMouseClick += SpeedButtonOnClickHandler;
            cautiousToggleButton.OnMouseScrollUp += ToggleSpeedButtonOnScrollHandler;
            cautiousToggleButton.OnMouseScrollDown += ToggleSpeedButtonOnScrollHandler;

            recklessToggleButton = DaggerfallUI.AddButton(recklessButtonRect, NativePanel);
            recklessToggleButton.OnMouseClick += SpeedButtonOnClickHandler;
            recklessToggleButton.OnMouseScrollUp += ToggleSpeedButtonOnScrollHandler;
            recklessToggleButton.OnMouseScrollDown += ToggleSpeedButtonOnScrollHandler;

            footHorseToggleButton = DaggerfallUI.AddButton(footHorseButtonRect, NativePanel);
            footHorseToggleButton.OnMouseClick += TransportModeButtonOnClickHandler;
            footHorseToggleButton.OnMouseScrollUp += ToggleTransportModeButtonOnScrollHandler;
            footHorseToggleButton.OnMouseScrollDown += ToggleTransportModeButtonOnScrollHandler;

            shipToggleButton = DaggerfallUI.AddButton(shipButtonRect, NativePanel);
            shipToggleButton.OnMouseClick += TransportModeButtonOnClickHandler;
            shipToggleButton.OnMouseScrollUp += ToggleTransportModeButtonOnScrollHandler;
            shipToggleButton.OnMouseScrollDown += ToggleTransportModeButtonOnScrollHandler;

            innToggleButton = DaggerfallUI.AddButton(innsButtonRect, NativePanel);
            innToggleButton.OnMouseClick += SleepModeButtonOnClickHandler;
            innToggleButton.OnMouseScrollUp += ToggleSleepModeButtonOnScrollHandler;
            innToggleButton.OnMouseScrollDown += ToggleSleepModeButtonOnScrollHandler;

            campOutToggleButton = DaggerfallUI.AddButton(campoutButtonRect, NativePanel);
            campOutToggleButton.OnMouseClick += SleepModeButtonOnClickHandler;
            campOutToggleButton.OnMouseScrollUp += ToggleSleepModeButtonOnScrollHandler;
            campOutToggleButton.OnMouseScrollDown += ToggleSleepModeButtonOnScrollHandler;
        }


        public override void OnPush()
        {
            base.OnPush();

            Items.ItemCollection inventory = GameManager.Instance.PlayerEntity.Items;
            hasHorse = inventory.Contains(Items.ItemGroups.Transportation, (int)Items.Transportation.Horse);
            hasCart = inventory.Contains(Items.ItemGroups.Transportation, (int)Items.Transportation.Small_cart);
            hasShip = Banking.DaggerfallBankManager.OwnsShip || GameManager.Instance.GuildManager.FreeShipTravel();

            if (base.IsSetup)
                Refresh();
        }

        #endregion

        #region Overrides

        public override void Update()
        {
            base.Update();

            if (doFastTravel)
            {
                if (countdownValueTravelTimeDays > 0)
                {
                    TickCountdown();
                }
                else
                {
                    doFastTravel = false;
                    DaggerfallUI.Instance.FadeBehaviour.SmashHUDToBlack();
                    performFastTravel();
                }

            }
        }

        #endregion


        #region Methods

        //Update when player pushes buttons etc.
        void Refresh()
        {
            UpdateTogglePanels();
            UpdateLabels();
        }

        //Updates the positions for the panels to indicate which button is selected
        void UpdateTogglePanels()
        {
            if (speedCautious)
                speedToggleColorPanel.Position = cautiousPanelPos;
            else
                speedToggleColorPanel.Position = recklessPanelPos;
            if (sleepModeInn)
                sleepToggleColorPanel.Position = innPanelPos;
            else
                sleepToggleColorPanel.Position = campoutPos;
            if (travelShip)
                transportToggleColorPanel.Position = shipPos;
            else
                transportToggleColorPanel.Position = footPos;
        }

        //Updates text labels
        void UpdateLabels()
        {
            availableGoldLabel.Text = GameManager.Instance.PlayerEntity.GoldPieces.ToString();
            travelTimeMinutes = travelTimeCalculator.CalculateTravelTime(endPos, speedCautious, sleepModeInn, travelShip, hasHorse, hasCart);

            // Players can have fast travel benefit from guild memberships
            travelTimeMinutes = GameManager.Instance.GuildManager.FastTravel(travelTimeMinutes);

            int travelTimeDaysTotal = (travelTimeMinutes / 1440);

            // Classic always adds 1. For DF Unity, only add 1 if there is a remainder to round up.
            if ((travelTimeMinutes % 1440) > 0)
                travelTimeDaysTotal += 1;

            travelTimeCalculator.CalculateTripCost(
                travelTimeMinutes,
                sleepModeInn,
                hasShip,
                travelShip
                );

            travelTimeLabel.Text = string.Format("{0}", travelTimeDaysTotal);
            tripCostLabel.Text = travelTimeCalculator.TotalCost.ToString();

            countdownValueTravelTimeDays = travelTimeDaysTotal;
        }

        bool TickCountdown()
        {
            bool finished = false;

            if (Time.realtimeSinceStartup > waitTimer + secondsCountdownTickFastTravel)
            {
                waitTimer = Time.realtimeSinceStartup;

                countdownValueTravelTimeDays--;
                travelTimeLabel.Text = string.Format("{0}", countdownValueTravelTimeDays);
                travelTimeLabel.Update();

                finished = true;
            }

            return finished;
        }

        // perform fast travel actions
        private void performFastTravel()
        {
            RaiseOnPreFastTravelEvent();

            // Cache scene first, if fast travelling while on ship.
            if (GameManager.Instance.TransportManager.IsOnShip())
                SaveLoadManager.CacheScene(GameManager.Instance.StreamingWorld.SceneName);
            GameManager.Instance.StreamingWorld.TeleportToCoordinates((int)endPos.X, (int)endPos.Y, StreamingWorld.RepositionMethods.DirectionFromStartMarker);

            if (speedCautious)
            {
                GameManager.Instance.PlayerEntity.CurrentHealth = GameManager.Instance.PlayerEntity.MaxHealth;
                GameManager.Instance.PlayerEntity.CurrentFatigue = GameManager.Instance.PlayerEntity.MaxFatigue;
                if (!GameManager.Instance.PlayerEntity.Career.NoRegenSpellPoints)
                    GameManager.Instance.PlayerEntity.CurrentMagicka = GameManager.Instance.PlayerEntity.MaxMagicka;
            }

            DaggerfallUnity.WorldTime.DaggerfallDateTime.RaiseTime(travelTimeMinutes * 60);

            // Halt random enemy spawns for next playerEntity update so player isn't bombarded by spawned enemies at the end of a long trip
            GameManager.Instance.PlayerEntity.PreventEnemySpawns = true;

            // Vampires and characters with Damage from Sunlight disadvantage always arrive just after 6pm regardless of travel type
            // Otherwise raise arrival time to just after 7am if cautious travel would arrive at night
            if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
            {
                DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.RaiseTime((DaggerfallDateTime.DuskHour - DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.Hour) * 3600);
            }
            else if (speedCautious)
            {
                if ((DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour < 7)
                    || ((DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour == 7) && (DaggerfallUnity.WorldTime.DaggerfallDateTime.Minute < 10)))
                {
                    float raiseTime = (((7 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour) * 3600)
                                        + ((10 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Minute) * 60)
                                        - DaggerfallUnity.WorldTime.DaggerfallDateTime.Second);
                    DaggerfallUnity.WorldTime.DaggerfallDateTime.RaiseTime(raiseTime);
                }
                else if (DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour > 17)
                {
                    float raiseTime = (((31 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour) * 3600)
                    + ((10 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Minute) * 60)
                    - DaggerfallUnity.WorldTime.DaggerfallDateTime.Second);
                    DaggerfallUnity.WorldTime.DaggerfallDateTime.RaiseTime(raiseTime);
                }
            }

            DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
            travelWindow.CloseTravelWindows(true);
            GameManager.Instance.PlayerEntity.RaiseSkills();
            DaggerfallUI.Instance.FadeBehaviour.FadeHUDFromBlack();

            RaiseOnPostFastTravelEvent();
        }

        // Return whether player has enough gold for the selected travel options
        // Taverns only accept gold pieces
        bool enoughGoldCheck()
        {
            return (GameManager.Instance.PlayerEntity.GetGoldAmount() >= travelTimeCalculator.TotalCost) &&
                   (GameManager.Instance.PlayerEntity.GoldPieces >= travelTimeCalculator.PiecesCost);
        }

        void showNotEnoughGoldPopup()
        {
            const int notEnoughGoldTextId = 454;

            TextFile.Token[] tokens = DaggerfallUnity.TextProvider.GetRSCTokens(notEnoughGoldTextId);
            if (tokens != null && tokens.Length > 0)
            {
                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, this);
                messageBox.SetTextTokens(tokens);
                messageBox.ClickAnywhereToClose = true;
                messageBox.Show();
            }
        }

        #endregion


        #region events

        public void BeginButtonOnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            Refresh();

            // Warns player if they have a disease
            if (GameManager.Instance.PlayerEffectManager.DiseaseCount > 0 || GameManager.Instance.PlayerEffectManager.PoisonCount > 0)
            {
                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, this);
                TextFile.Token[] tokens = DaggerfallUnity.Instance.TextProvider.GetRandomTokens(1010);
                messageBox.SetTextTokens(tokens);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
                messageBox.OnButtonClick += ConfirmTravelPopupDiseasedButtonClick;
                uiManager.PushWindow(messageBox);
            }
            else
            {
                CallFastTravelGoldCheck();
            }
        }

        public override void CancelWindow()
        {
            doFastTravel = false;
            base.CancelWindow();
        }

        /// <summary>
        /// Button handler for travel-with-incubating-disease confirmation pop up.
        /// </summary>
        void ConfirmTravelPopupDiseasedButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            sender.CloseWindow();

            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                CallFastTravelGoldCheck();
            }
            else
                return;
        }

        void CallFastTravelGoldCheck()
        {
            if (!enoughGoldCheck())
            {
                showNotEnoughGoldPopup();
                return;
            }
            else
            {
                GameManager.Instance.PlayerEntity.GoldPieces -= travelTimeCalculator.PiecesCost;
                GameManager.Instance.PlayerEntity.DeductGoldAmount(travelTimeCalculator.TotalCost - travelTimeCalculator.PiecesCost);
            }

            doFastTravel = true; // initiate fast travel (Update() function will perform fast travel when this flag is true)
        }

        public void ExitButtonOnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            doFastTravel = false;
            DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
        }

        public void SpeedButtonOnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            speedCautious = (sender == cautiousToggleButton);
            Refresh();
        }

        public void ToggleSpeedButtonOnScrollHandler(BaseScreenComponent sender)
        {
            speedCautious = !speedCautious;
            Refresh();
        }

        public void TransportModeButtonOnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            travelShip = (sender == shipToggleButton);
            Refresh();
        }

        public void ToggleTransportModeButtonOnScrollHandler(BaseScreenComponent sender)
        {
            travelShip = !travelShip;
            Refresh();
        }

        public void SleepModeButtonOnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            sleepModeInn = (sender == innToggleButton);
            Refresh();
        }

        public void ToggleSleepModeButtonOnScrollHandler(BaseScreenComponent sender)
        {
            sleepModeInn = !sleepModeInn;
            Refresh();
        }

        /// <summary>
        /// Raised before a fast travel is performed.
        /// </summary>
        public static event Action<DaggerfallTravelPopUp> OnPreFastTravel;
        void RaiseOnPreFastTravelEvent()
        {
            if (OnPreFastTravel != null)
                OnPreFastTravel(this);
        }

        // OnPostFastTravel
        public delegate void OnOnPostFastTravelEventHandler();
        public static event OnOnPostFastTravelEventHandler OnPostFastTravel;
        void RaiseOnPostFastTravelEvent()
        {
            if (OnPostFastTravel != null)
                OnPostFastTravel();
        }

        #endregion

    }
}
