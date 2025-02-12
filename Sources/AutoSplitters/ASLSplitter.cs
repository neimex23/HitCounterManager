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

using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveSplit;
using LiveSplit.Model;
using System.Windows.Forms;
using LiveSplit.UI;
using LiveSplit.Model.Comparisons;
using LiveSplit.Options;
using System.Diagnostics;
using System.Threading;
using System.Xml;

namespace AutoSplitterCore 
{ 
    public class ListenerASL : TraceListener
    {
        public override void Write(string message) => RegisterLog(message);

        public override void WriteLine(string message) => RegisterLog(message);

        private void RegisterLog(string message) => DebugLog.LogMessage($"Trace ASL: {message}");

        public static void Initialize()
        {
            var listener = new ListenerASL();
            listener.Filter = new EventTypeFilter(SourceLevels.All);
            Trace.Listeners.Add(listener);
        }
    }

    public class ASLSplitter
    {
        public bool _PracticeMode = false;

        LiveSplitState state = null;
        ASLComponent asl;
        ISplitterControl splitterControl = SplitterControl.GetControl();
        System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer() { Interval = 100 };

        #region ASLConstructor
        private static ASLSplitter _instance = new ASLSplitter();
        public static ASLSplitter GetInstance() => _instance;

        private ASLSplitter()
        {
            state = GeneratorState();
            asl = new ASLComponent(state);
            _timer.Tick += ASCHandlerSetters;        
        }

        private LiveSplitState GeneratorState() {
            Form liveSplitForm = new Form
            {
                Text = "LiveSplit Form",
                Width = 300,
                Height = 200
            };

            var layoutSettings = new LiveSplit.Options.LayoutSettings();
            var layout = new Layout();

            IComparisonGeneratorsFactory comparisonFactory = new StandardComparisonGeneratorsFactory();
            var run = new Run(comparisonFactory)
            {
                GameName = "Example Game",
                CategoryName = "Any%"
            };
            run.AddSegment("Start");
            run.AddSegment("Middle");
            run.AddSegment("End");

            ISettings settings = new Settings();

            var state = new LiveSplitState(run, liveSplitForm, layout, layoutSettings, settings);
            ITimerModel timerModel = new TimerModel() { CurrentState = state};
            timerModel.InitializeGameTime();
            state.RegisterTimerModel(timerModel);

            return state;
        }

        private bool ASCHandlerSetted = false;
        private void ASCHandlerSetters(object sender, EventArgs e)
        {
            if (ASCHandlerSetted)
            {
                _timer.Stop(); return;
            }

            if (asl.Script != null)
            {
                asl.Script.ShouldSplit += ASCOnSplit;
                asl.Script.StartRun += ASCOnStart;
                asl.Script.ResetRun += ASCOnReset;
                ASCHandlerSetted = true;
                Log.Info("Handlers ASL Setted");
            }                
        }

        public Control AslControl 
        { 
            get 
            { 
                return asl != null ? asl.GetSettingsControl(LayoutMode.Vertical) : null; 
            } 
        }

        #endregion
        #region Control Management
        public void setData(XmlNode node)
        {
            if (node != null) { asl.SetSettings(node); } 
        }

        public XmlNode getData(XmlDocument doc)
        {
            return asl.GetSettings(doc);
        }

        public bool _AslActive { get; private set; } = false;
        public void SetStatusSplitting(bool status)
        {
            _AslActive = status;
            if (!ASCHandlerSetted) _timer.Start();
        }

        #endregion
        public bool IGTEnable { get; set; } = false;
        public bool GetStatusGame() => asl.Script != null ? asl.Script.ProccessAtached() : false;


        private void ASCOnSplit(object sender, EventArgs e)
        {
            if (!_PracticeMode && _AslActive)
                splitterControl.SplitCheck("Trace ASL: SplitFlag Produced on ASL");
        }

        private void ASCOnStart(object sender, EventArgs e)
        {
           if (splitterControl.GetDebug())
           {
                DebugLog.LogMessage("Trace ASL: Start Run Trigger Produced on ASL");
                return; //StartStopTimer not implemented on debugmode
           }

            if (!_PracticeMode && _AslActive && !IGTEnable)
            {
                splitterControl.StartStopTimer(true);
            }
        }

        private void ASCOnReset(object sender, EventArgs e) 
        {
            if (splitterControl.GetDebug())
            {
                DebugLog.LogMessage("Trace ASL: Reset Run Trigger Produced on ASL");
                return; //ProfileReset not implemented on debugmode
            }

            if (!_PracticeMode && _AslActive)
                splitterControl.ProfileReset();
        }

        public long GetIngameTime() => state != null ? (long)state.CurrentTime.GameTime.Value.TotalMilliseconds : -1;
        

    }
}
