﻿//MIT License

//Copyright (c) 2022-2025 Ezequiel Medina

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

using HitCounterManager;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace AutoSplitterCore
{
    public interface ISplitterControl
    {
        // --------------------------------------------------------------------------------
        // Initialization and Configuration
        // --------------------------------------------------------------------------------

        /// For Api Calls initialice ASC without interface or HCM
        // SplitterControl.Initialize(); Initializations 
        // SplitterControl.GetControl(); Get Intance of SplitterControl

        /// <summary>
        /// Sets the AutoSplitter interface to interact with the main HCM program.
        /// </summary>
        /// <param name="interfaceHcm">Instance of IAutoSplitterCoreInterface.</param>
        void SetInterface(IAutoSplitterCoreInterface interfaceHcm);

        /// <summary>
        /// Sets the SaveModule instance.
        /// </summary>
        /// <param name="savemodule">Instance of SaveModule.</param>
        void SetSaveModule(SaveModule savemodule);


        // --------------------------------------------------------------------------------
        // Timer Control
        // --------------------------------------------------------------------------------

        /// <summary>
        /// Starts or stops the main timer of HCM.
        /// </summary>
        /// <param name="stop">True to start the timer, False to stop it.</param>
        void StartStopTimer(bool stop);

        /// <summary>
        /// Forces the internal process of HCM to update timer values to the current value.
        /// </summary>
        void UpdateDuration();


        // --------------------------------------------------------------------------------
        // Splitter State and Debug
        // --------------------------------------------------------------------------------

        /// <summary>
        /// Enables or disables the internal timer that checks the SplitGo value.
        /// Disabled in PracticeMode, when showing a dialog, or when no splitter is selected.
        /// </summary>
        /// <param name="checking">True to enable, False to disable.</param>
        void SetChecking(bool checking);

        /// <summary>
        /// Enables or disables the debug mode.
        /// </summary>
        /// <param name="status">True to enable, False to disable.</param>
        void SetDebug(bool status);

        /// <summary>
        /// Gets the current debug mode status.
        /// </summary>
        /// <returns>True if enabled, False if disabled.</returns>
        bool GetDebug();


        // --------------------------------------------------------------------------------
        // Game Selection and Mode
        // --------------------------------------------------------------------------------

        /// <summary>
        /// Sets the active game index in the HCM ComboBox.
        /// </summary>
        /// <param name="index">Index of the game. Greater than -1 to select a game.</param>
        void SetActiveGameIndex(int index);

        /// <summary>
        /// Enables or disables Practice Mode in HCM.
        /// </summary>
        /// <param name="status">True to enable, False to disable.</param>
        void SetPracticeMode(bool status);


        // --------------------------------------------------------------------------------
        // Splits and Hits
        // --------------------------------------------------------------------------------

        /// <summary>
        /// Processes a split in the main HCM program.
        /// </summary>
        /// <param name="DebugLog">Debug message for the log.</param>
        void SplitCheck(string DebugLog);

        /// <summary>
        /// Processes a hit in the main HCM program.
        /// </summary>
        /// <param name="DebugLog">Debug message for the log.</param>
        void HitCheck(string DebugLog);

        /// <summary>
        /// Resets the current split to the first split and adds a run attempt.
        /// Also stops and resets the timer.
        /// </summary>
        void ProfileReset();


        // --------------------------------------------------------------------------------
        // Profile Management
        // --------------------------------------------------------------------------------

        /// <summary>
        /// Triggered when a profile is changed in HCM.
        /// </summary>
        /// <param name="ProfileTitle">Title of the current profile.</param>
        void ProfileChange(string ProfileTitle);

        /// <summary>
        /// Gets the name of the current profile in HCM.
        /// </summary>
        /// <returns>Name of the current profile.</returns>
        string GetHCMProfileName();

        /// <summary>
        /// Gets a list of all available profiles in HCM.
        /// </summary>
        /// <returns>List of profile names.</returns>
        List<string> GetAllHcmProfile();

        /// <summary>
        /// Returns all splits of the current HCM profile.
        /// </summary>
        /// <returns>List of split names.</returns>
        List<string> GetSplits();

        /// <summary>
        /// Creates a new profile in HCM.
        /// </summary>
        /// <param name="ProfileTitle">Title of the new profile.</param>
        void NewProfile(string ProfileTitle);

        /// <summary>
        /// Inserts a new split into the current HCM profile.
        /// </summary>
        /// <param name="splitTitle">Name of the split.</param>
        void AddSplit(string splitTitle);


        // --------------------------------------------------------------------------------
        // Status Queries
        // --------------------------------------------------------------------------------

        /// <summary>
        /// Returns whether the current split in HCM is the last one.
        /// </summary>
        /// <returns>True if it is the last split, False otherwise.</returns>
        bool CurrentFinalSplit();

        /// <summary>
        /// Gets the current SplitStatus value.
        /// SplitStatus is set to False if Practice Mode is enabled after a flag check has been executed.
        /// </summary>
        /// <returns>True if the status is valid, False otherwise.</returns>
        bool GetSplitStatus();

        /// <summary>
        /// Returns the current running status of the HCM timer.
        /// </summary>
        /// <returns>True if the timer is running, False if it is stopped.</returns>
        bool GetTimerRunning();
    }


    public class SplitterControl : ISplitterControl
    {
        #region Singleton
        private static readonly Lazy<SplitterControl> instance = new Lazy<SplitterControl>(() => new SplitterControl());
        private readonly System.Windows.Forms.Timer updateTimer;

        private SplitterControl()
        {
            updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            updateTimer.Tick += (sender, args) => SplitGo();
        }

        public static ISplitterControl GetControl() => instance.Value;
        #endregion

        #region Initialize

        public static void Initialize() 
        {
            AutoSplitterMainModule autoSplitterMainModule = new AutoSplitterMainModule();
            autoSplitterMainModule.InitDebug();
            autoSplitterMainModule.RegisterHitCounterManagerInterface(null);
        }

        #endregion

        #region Fields
        private bool enableChecking = true;
        private bool debugMode = false;
        private bool splitGo = false;
        private bool splitStatus = false;
        private IAutoSplitterCoreInterface interfaceHCM;
        private SaveModule saveModule;
        private static readonly object splitLock = new object();
        private static readonly object hitLock = new object();
        #endregion

        #region Interface Methods
        public void SetInterface(IAutoSplitterCoreInterface interfaceHcm) => interfaceHCM = interfaceHcm;
        public void SetSaveModule(SaveModule savemodule) => saveModule = savemodule;
        public void SetChecking(bool checking)
        {
            enableChecking = checking;
            updateTimer.Enabled = checking;
        }

        public void SetDebug(bool status) => debugMode = status;
        public bool GetDebug() => debugMode;
        public void StartStopTimer(bool stop) => interfaceHCM.StartStopTimer(stop);
        public void SetActiveGameIndex(int index) => interfaceHCM.ActiveGameIndex = index;
        public void SetPracticeMode(bool status) => interfaceHCM.PracticeMode = status;

        public void SplitCheck(string debugLog)
        {
            lock (splitLock)
            {
                if (debugMode) DebugLog.LogMessage(debugLog);
                if (!enableChecking)
                {
                    splitStatus = false;
                    return;
                }
            
                if (splitGo) Thread.Sleep(2000);
                splitGo = true;
                splitStatus = true;
            }
        }

        public void HitCheck(string debugLog)
        {
            lock (hitLock)
            {
                if (debugMode) DebugLog.LogMessage(debugLog);
                if (enableChecking && !debugMode)
                {
                    interfaceHCM.ProfileHitGo(1, saveModule.generalAS.HitMode == HitMode.Way);           
                }
            }
        }

        public void UpdateDuration() => interfaceHCM.UpdateDuration();
        public void ProfileReset() => interfaceHCM.ProfileReset();
        public bool CurrentFinalSplit() => interfaceHCM.ActiveSplit == interfaceHCM.SplitCount;
        public bool GetSplitStatus() => splitStatus;
        public bool GetTimerRunning() => interfaceHCM.TimerRunning;

        public void ProfileChange(string profileTitle)
        {
            if (!saveModule.ProfileLinkReady()) return;

            var profileLink = saveModule.generalAS.profileLinks.Find(x => x.profileHCM == profileTitle);
            if (profileLink == null) return;

            var selectItem = Path.Combine(saveModule.generalAS.saveProfilePath, profileLink.profileASC);
            if (!File.Exists(selectItem))
            {
                MessageBox.Show("ASC Profile Corrupt or not Exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            const string mainSave = "SaveDataAutoSplitter.xml";
            File.Delete(mainSave);
            File.Copy(selectItem, mainSave);
            saveModule.ReLoadAutoSplitterSettings();

            if (saveModule.generalAS.ResetProfile) saveModule.ResetFlags();
        }

        public string GetHCMProfileName() => interfaceHCM.ProfileName();
        public List<string> GetAllHcmProfile() => interfaceHCM.GetProfiles();
        public List<string> GetSplits() => interfaceHCM.GetSplits();
        public void NewProfile(string profileTitle) => interfaceHCM.NewProfile(profileTitle);
        public void AddSplit(string splitTitle) => interfaceHCM.AddSplit(splitTitle);
        #endregion

        #region Private Methods
        private void SplitGo()
        {
            if (!splitGo || debugMode) return;

            interfaceHCM.ProfileSplitGo(1);
        }
        #endregion
    }
}

