﻿//MIT License

//Copyright (c) 2022-2024 Ezequiel Medina
//Copyright (c) 2024 Peter Kirmeier (Update new HCM interface)

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using HitCounterManager;

namespace AutoSplitterCore
{
    public class AutoSplitterMainModule
    {
        public SekiroSplitter sekiroSplitter = new SekiroSplitter();
        public Ds1Splitter ds1Splitter = new Ds1Splitter();
        public Ds2Splitter ds2Splitter = new Ds2Splitter();
        public Ds3Splitter ds3Splitter = new Ds3Splitter();
        public EldenSplitter eldenSplitter = new EldenSplitter();
        public HollowSplitter hollowSplitter = new HollowSplitter();
        public CelesteSplitter celesteSplitter = new CelesteSplitter();
        public CupheadSplitter cupSplitter = new CupheadSplitter();
        public DishonoredSplitter dishonoredSplitter = new DishonoredSplitter();
        public IGTModule igtModule = new IGTModule();
        public SaveModule saveModule = new SaveModule();
        public UpdateModule updateModule = new UpdateModule();

        private ISplitterControl splitterControl = SplitterControl.GetControl();

        #if !HCMv2
        public Debug debugForm;
        #endif
        public bool DebugMode = false;
        public bool _PracticeMode = false;
        public bool _ShowSettings = false;
        private System.Windows.Forms.Timer _update_timer = new System.Windows.Forms.Timer() { Interval = 500 };

        public List<string> GetGames() { return GameConstruction.GameList; }


        #region Settings
        public void InitDebug()
        {
            splitterControl.SetDebug(true);
        }

        public void AutoSplitterForm(bool darkMode)
        {
            SetShowSettings(true);
            ReaLTaiizor.Forms.PoisonForm form = new AutoSplitter
                   (sekiroSplitter, 
                    hollowSplitter, 
                    eldenSplitter, 
                    ds3Splitter,
                    celesteSplitter, 
                    ds2Splitter, 
                    cupSplitter,
                    ds1Splitter,
                    dishonoredSplitter, 
                    updateModule, 
                    saveModule, 
                    darkMode);
            form.ShowDialog();
            SetShowSettings(false);
        }

        public void RegisterHitCounterManagerInterface(IAutoSplitterCoreInterface interfaceASC)
        {
            //SetPointers
            igtModule.SetSplitterPointers(sekiroSplitter, eldenSplitter, ds3Splitter, celesteSplitter, cupSplitter, ds1Splitter);
            saveModule.SetPointers
                    (sekiroSplitter,
                    ds1Splitter,
                    ds2Splitter,
                    ds3Splitter,
                    eldenSplitter,
                    hollowSplitter,
                    celesteSplitter,
                    cupSplitter,
                    dishonoredSplitter,
                    updateModule,
                    this);
            //LoadSettings
            saveModule.LoadAutoSplitterSettings();

            #if !AutoSplitterCoreDebug
            _update_timer.Tick += (senderT, args) => CheckAutoTimers();
            #endif

            _update_timer.Tick += (senderT, args) => CheckAutoResetSplit();
            _update_timer.Enabled = true;

            updateModule.CheckUpdates(false);

            interfaceASC.GameList.Clear();
            foreach (string game in GetGames())
            {
                interfaceASC.GameList.Add(game);
            }

            //interfaceASC.ActiveGameIndex = GetSplitterEnable(); //Before HCM Interface Change, ASC control mannualy on start the index of ComboBoxGame in Main Program
            interfaceASC.GetActiveGameIndexMethod = () => GetSplitterEnable(); //Afrer HCM Interface Change, HCM ask on Start The index of ComboBoxgame on ASC
            interfaceASC.SetActiveGameIndexMethod = (splitter) =>
            {
                //Disable all games
                EnableSplitting(0);
                //Ask Selected index
                EnableSplitting(splitter);
            };
            interfaceASC.PracticeMode = GetPracticeMode();
            interfaceASC.SetPracticeModeMethod = SetPracticeMode;

            interfaceASC.OpenSettingsMethod = AutoSplitterForm;
            interfaceASC.SaveSettingsMethod = SaveAutoSplitterSettings;

            interfaceASC.GetCurrentInGameTimeMethod = GetCurrentInGameTime;
            bool GetCurrentInGameTime(out long totalTimeMs)
            {
                if (GetIsIGTActive())
                {
                    totalTimeMs = ReturnCurrentIGT();
                    return true;
                }

                totalTimeMs = 0;
                return false;
            }

            interfaceASC.SplitterResetMethod = ResetSplitterFlags;
            splitterControl.SetInterface(interfaceASC);
        }

        public void SaveAutoSplitterSettings() => saveModule.SaveAutoSplitterSettings();

        #endregion
        #region SplitterManagement
        public bool GetPracticeMode() => saveModule._PracticeMode;

        public void SetPracticeMode(bool status)
        {
            _PracticeMode = status;
            saveModule._PracticeMode = status;
            sekiroSplitter._PracticeMode = status;
            ds1Splitter._PracticeMode = status;
            ds2Splitter._PracticeMode = status;
            ds3Splitter._PracticeMode = status;
            eldenSplitter._PracticeMode = status;
            hollowSplitter._PracticeMode = status;
            celesteSplitter._PracticeMode = status;
            cupSplitter._PracticeMode = status;
            dishonoredSplitter._PracticeMode = status;
            splitterControl.SetChecking(!status);
        }

        public void SetShowSettings(bool status)
        {
            _ShowSettings = status;
            sekiroSplitter._ShowSettings = status;
            ds1Splitter._ShowSettings = status;
            ds2Splitter._ShowSettings = status;
            ds3Splitter._ShowSettings = status;
            eldenSplitter._ShowSettings = status;
            hollowSplitter._ShowSettings = status;
            celesteSplitter._ShowSettings = status;
            cupSplitter._ShowSettings = status;
            dishonoredSplitter._ShowSettings = status;
            splitterControl.SetChecking(!status);
        }

        public int GetSplitterEnable()
        {
            if (sekiroSplitter.GetDataSekiro().enableSplitting) { return GameConstruction.SekiroSplitterIndex; }
            if (ds1Splitter.GetDataDs1().enableSplitting) { return GameConstruction.Ds1SplitterIndex; }
            if (ds2Splitter.GetDataDs2().enableSplitting) { return GameConstruction.Ds2SplitterIndex; }
            if (ds3Splitter.GetDataDs3().enableSplitting) { return GameConstruction.Ds3SplitterIndex; }
            if (eldenSplitter.GetDataElden().enableSplitting) { return GameConstruction.EldenSplitterIndex; }
            if (hollowSplitter.GetDataHollow().enableSplitting) { return GameConstruction.HollowSplitterIndex; }
            if (celesteSplitter.GetDataCeleste().enableSplitting) { return GameConstruction.CelesteSplitterIndex; }
            if (dishonoredSplitter.GetDataDishonored().enableSplitting) { return GameConstruction.DishonoredSplitterIndex; }
            if (cupSplitter.GetDataCuphead().enableSplitting) { return GameConstruction.CupheadSplitterIndex; }
            return GameConstruction.NoneSplitterIndex;
        }

        public void EnableSplitting(int splitter)
        {
            gameActive = splitter;
            igtModule.gameSelect = splitter;
            anyGameTime = false;
            if (splitter == GameConstruction.NoneSplitterIndex)
            {
                splitterControl.SetChecking(false);
                sekiroSplitter.SetStatusSplitting(false);
                ds1Splitter.SetStatusSplitting(false);
                ds2Splitter.SetStatusSplitting(false);
                ds3Splitter.SetStatusSplitting(false);
                eldenSplitter.SetStatusSplitting(false);
                hollowSplitter.SetStatusSplitting(false);
                celesteSplitter.SetStatusSplitting(false);
                dishonoredSplitter.SetStatusSplitting(false);
                cupSplitter.SetStatusSplitting(false);
            }
            else
            {
                switch (splitter)
                {
                    case GameConstruction.SekiroSplitterIndex: sekiroSplitter.SetStatusSplitting(true); break;
                    case GameConstruction.Ds1SplitterIndex: ds1Splitter.SetStatusSplitting(true); break;
                    case GameConstruction.Ds2SplitterIndex: ds2Splitter.SetStatusSplitting(true); break;
                    case GameConstruction.Ds3SplitterIndex: ds3Splitter.SetStatusSplitting(true); break;
                    case GameConstruction.EldenSplitterIndex: eldenSplitter.SetStatusSplitting(true); break;
                    case GameConstruction.HollowSplitterIndex: hollowSplitter.SetStatusSplitting(true); break;
                    case GameConstruction.CelesteSplitterIndex: celesteSplitter.SetStatusSplitting(true); break;
                    case GameConstruction.DishonoredSplitterIndex: dishonoredSplitter.SetStatusSplitting(true); break;
                    case GameConstruction.CupheadSplitterIndex: cupSplitter.SetStatusSplitting(true); break;
                    default: EnableSplitting(0); break;
                }
                splitterControl.SetChecking(true);
            }
        }

        public void ResetSplitterFlags()
        {
            sekiroSplitter.ResetSplited();
            ds1Splitter.ResetSplited();
            ds2Splitter.ResetSplited();
            ds3Splitter.ResetSplited();
            eldenSplitter.ResetSplited();
            hollowSplitter.ResetSplited();
            celesteSplitter.ResetSplited();
            cupSplitter.ResetSplited();
        }
        #endregion
        #region IGT & Timmer 
        public long ReturnCurrentIGT()
        {
            long igtTime;
            if (GameOn())
            {
                try
                {
                    igtTime = (long)igtModule.ReturnCurrentIGT();
                }
                catch (Exception) { igtTime = -1; }
            }else { igtTime = -1; }

            return igtTime;
        }

        public bool GetIsIGTActive()
        {
            return this.anyGameTime && ReturnCurrentIGT() > 0;
        }

        public int gameActive = 0;
        private bool anyGameTime = false;
        private bool autoTimer = false;
        private bool profileResetDone = false;
        private long _lastCelesteTime;
        private long _lastTime;

        public void CheckAutoTimers()
        {
            anyGameTime = false;
            autoTimer = false;        
            switch (gameActive)
            {
                case GameConstruction.SekiroSplitterIndex: //Sekiro
                    if (sekiroSplitter.GetDataSekiro().autoTimer && !_PracticeMode)
                    {
                        autoTimer = true;
                        if (sekiroSplitter.GetDataSekiro().gameTimer)
                        {
                            anyGameTime = true;
                        }
                    }
                    break;
                case GameConstruction.Ds1SplitterIndex: //DS1
                    if (ds1Splitter.GetDataDs1().autoTimer && !_PracticeMode)
                    {
                        autoTimer = true;
                        if (ds1Splitter.GetDataDs1().gameTimer)
                        {
                            anyGameTime = true;
                        }
                    }
                    break;
                case GameConstruction.Ds3SplitterIndex: //Ds3
                    if (ds3Splitter.GetDataDs3().autoTimer && !_PracticeMode)
                    {
                        autoTimer = true;
                        if (!ds3Splitter.GetDataDs3().gameTimer)
                        {
                            anyGameTime = true;
                        }
                    }
                    break;
                case GameConstruction.EldenSplitterIndex: //Elden
                    if (eldenSplitter.GetDataElden().autoTimer && !_PracticeMode)
                    {
                        autoTimer = true;
                        if (eldenSplitter.GetDataElden().gameTimer)
                        {
                            anyGameTime = true;
                        }
                    }
                    break;

                //Special Case
                case GameConstruction.CelesteSplitterIndex: //Celeste
                    if (celesteSplitter.GetDataCeleste().autoTimer && !_PracticeMode)
                    {
                        if (!celesteSplitter.GetDataCeleste().gameTimer)
                        {                           
                            if (!celesteSplitter._runStarted && celesteSplitter.IsInGame())
                            {
                                if (!splitterControl.GetTimerRunning() && GameOn())
                                    splitterControl.StartStopTimer(true);
                                celesteSplitter._runStarted = true;
                            }
                        }
                        else
                        {
                            anyGameTime = true;
                            var currentCelesteTime = celesteSplitter.GetTimeInGame();
                            if (currentCelesteTime > 0 && currentCelesteTime != _lastCelesteTime && celesteSplitter.IsInGame() && GameOn())
                            {
                                if (!splitterControl.GetTimerRunning())
                                    splitterControl.StartStopTimer(true);
                            }
                            else
                            {
                                if (splitterControl.GetTimerRunning())
                                    splitterControl.StartStopTimer(false);
                            }
                                

                            if (currentCelesteTime > 0)
                                _lastCelesteTime = currentCelesteTime;
                        }
                    }
                    break;
                case GameConstruction.CupheadSplitterIndex: //Cuphead
                    if (cupSplitter.GetDataCuphead().autoTimer && !_PracticeMode)
                    {
                        if (!cupSplitter.GetDataCuphead().gameTimer)
                        {
                            if (!cupSplitter.IsInGame() || !GameOn())
                            {
                                if (splitterControl.GetTimerRunning())
                                    splitterControl.StartStopTimer(false);
                            }
                            else
                            {
                                if (!splitterControl.GetTimerRunning())
                                    splitterControl.StartStopTimer(true);
                            }
                        }
                        else
                        {
                            anyGameTime = true;
                            if (!cupSplitter.IsInGame() || cupSplitter.LevelCompleted() || !GameOn())
                            {
                                if (splitterControl.GetTimerRunning())
                                    splitterControl.StartStopTimer(false);
                            }
                            else
                            {
                                if (!splitterControl.GetTimerRunning())
                                    splitterControl.StartStopTimer(true);
                            }
                        }                     
                    }
                    break;

                //Manual Controller with Loading Events           
                case GameConstruction.Ds2SplitterIndex: //DS2
                    if (ds2Splitter.GetDataDs2().autoTimer && !_PracticeMode)
                    {
                        if (ds2Splitter._runStarted && !splitterControl.GetTimerRunning() && GameOn())
                            splitterControl.StartStopTimer(true);

                        if ((!ds2Splitter._runStarted || !GameOn()) && splitterControl.GetTimerRunning())
                            splitterControl.StartStopTimer(false);
                    }
                    break;
                case GameConstruction.HollowSplitterIndex: //Hollow
                    if (hollowSplitter.GetDataHollow().autoTimer && !_PracticeMode)
                    {
                        if (hollowSplitter._runStarted && !splitterControl.GetTimerRunning() && GameOn())
                            splitterControl.StartStopTimer(true);

                        if ((!hollowSplitter._runStarted || !GameOn()) && splitterControl.GetTimerRunning())
                            splitterControl.StartStopTimer(false);
                    }
                    break;
                case GameConstruction.DishonoredSplitterIndex:
                    if (dishonoredSplitter.GetDataDishonored().autoTimer && !_PracticeMode)
                    {
                        if (!dishonoredSplitter.GetDataDishonored().gameTimer)
                        {
                            if (dishonoredSplitter._runStarted && !splitterControl.GetTimerRunning() && GameOn())
                                splitterControl.StartStopTimer(true);
                            
                            if ((!dishonoredSplitter._runStarted || !GameOn()) && splitterControl.GetTimerRunning())
                                splitterControl.StartStopTimer(false);
                        }
                        else
                        {
                            if (dishonoredSplitter._runStarted && !dishonoredSplitter.isLoading && !splitterControl.GetTimerRunning() && GameOn())
                                splitterControl.StartStopTimer(true);

                            if ((!dishonoredSplitter._runStarted || dishonoredSplitter.isLoading || !GameOn()) && splitterControl.GetTimerRunning())
                                splitterControl.StartStopTimer(false);
                        }
                    }
                    break;


                case GameConstruction.NoneSplitterIndex:               
                default: anyGameTime = false; autoTimer = false; break;
            }


            //AutoTimerReset Checking
            if (autoTimer && anyGameTime)
            {                
                int inGameTime = -1;
                if (GameOn()) {
                    try {
                        inGameTime = igtModule.ReturnCurrentIGT(); 
                    } catch (Exception) { inGameTime = -1; }
                }

                if (inGameTime > 0 && _lastTime != inGameTime && !splitterControl.GetTimerRunning() && !splitterControl.CurrentFinalSplit() && GameOn())
                {
                    splitterControl.UpdateDuration();
                    splitterControl.StartStopTimer(true);
                    splitterControl.UpdateDuration();
                }
                    
                if ((inGameTime <= 0 || (inGameTime > 0 && _lastTime == inGameTime) || splitterControl.CurrentFinalSplit() || !GameOn()) && splitterControl.GetTimerRunning())
                {
                    splitterControl.UpdateDuration();
                    splitterControl.StartStopTimer(false);
                    splitterControl.UpdateDuration();
                }

                if (inGameTime > 0)
                    _lastTime = inGameTime;
            }

            if (autoTimer && !anyGameTime)
            {
                int inGameTime = -1;
                if (GameOn())
                {
                    try
                    {
                        inGameTime = igtModule.ReturnCurrentIGT();
                    }
                    catch (Exception) { inGameTime = -1; }
                }

                if (inGameTime > 0 && !splitterControl.GetTimerRunning() && !splitterControl.CurrentFinalSplit() && GameOn())
                    splitterControl.StartStopTimer(true);

                if ((inGameTime <= 0 || splitterControl.CurrentFinalSplit() || !GameOn()) && splitterControl.GetTimerRunning())
                    splitterControl.StartStopTimer(false);
            }
        }

        public void CheckAutoResetSplit()
        {
            //AutoResetSplits Checking
            if (saveModule.dataAS.AutoResetSplit)
            {
                int inGameTime = -1;
                bool SpecialCaseReset = false;
                if (GameOn())
                {
                    try
                    {
                        inGameTime = igtModule.ReturnCurrentIGT();
                    }
                    catch (Exception) { inGameTime = -1; }

                    //Ds2 and Hollow Cases
                    if (gameActive == GameConstruction.Ds2SplitterIndex)
                    {
                        SpecialCaseReset = true;
                        var loading = ds2Splitter.Ds2IsLoading();
                        if (!loading)
                        {
                            var position = ds2Splitter.GetCurrentPosition();
                            if (
                                position.Y < -322.0f && position.Y > -323.0f &&
                                position.X < -213.0f && position.X > -214.0f)
                            {
                                if (!profileResetDone)
                                {
                                    profileResetDone = true;
                                    ResetSplitterFlags();
                                    if (!DebugMode)
                                    {
                                        splitterControl.StartStopTimer(false);
                                        splitterControl.ProfileReset();
                                    }                                   
                                }
                            }
                            else
                                profileResetDone = false;
                        }
                    }

                    if (gameActive == GameConstruction.HollowSplitterIndex)
                    {
                        SpecialCaseReset = true;
                        if (hollowSplitter.IsNewgame())
                        {
                            if (!profileResetDone)
                            {
                                profileResetDone = true;
                                ResetSplitterFlags();

                                if (!DebugMode)
                                {
                                    splitterControl.StartStopTimer(false);
                                    splitterControl.ProfileReset();
                                }
                            }                           
                        }
                        else
                            profileResetDone = false;
                    }
                }


                if (!SpecialCaseReset)
                {
                    if (inGameTime > 0 && inGameTime <= 10000)
                    {
                        if (!profileResetDone)
                        {
                            profileResetDone = true;
                            ResetSplitterFlags();

                            if (!DebugMode)
                            {
                                splitterControl.StartStopTimer(false);
                                splitterControl.ProfileReset();
                            }
                        }
                    }
                    else
                    {
                        profileResetDone = false;
                    }
                }
            }
        }

        public bool GameOn()
        {
            switch (gameActive)
            {
                case GameConstruction.SekiroSplitterIndex: 
                    return sekiroSplitter._StatusSekiro;
                case GameConstruction.Ds1SplitterIndex: 
                    return ds1Splitter._StatusDs1;
                case GameConstruction.Ds2SplitterIndex:
                    return ds2Splitter._StatusDs2;
                case GameConstruction.Ds3SplitterIndex:
                    return ds3Splitter._StatusDs3;
                case GameConstruction.EldenSplitterIndex:
                    return eldenSplitter._StatusElden;
                case GameConstruction.HollowSplitterIndex:
                    return hollowSplitter._StatusHollow;
                case GameConstruction.CelesteSplitterIndex:
                    return celesteSplitter._StatusCeleste;
                case GameConstruction.DishonoredSplitterIndex:
                    return dishonoredSplitter._StatusDish;
                case GameConstruction.CupheadSplitterIndex:
                    return cupSplitter._StatusCuphead;  
                case GameConstruction.NoneSplitterIndex:
                default: return false;
            }
        }
        #endregion
        #region Common
        /// <summary>
        /// Tries to open an URI with the system's registered default browser.
        /// (See: https://github.com/dotnet/runtime/issues/21798)
        /// </summary>
        /// <param name="uri">URI that shall be opened</param>
        public static void OpenWithBrowser(Uri uri) => Process.Start(new ProcessStartInfo("cmd", $"/c start {uri.OriginalString.Replace("&", "^&")}") { CreateNoWindow = true });
        #endregion
    }
}
