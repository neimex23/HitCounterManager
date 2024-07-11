﻿//MIT License

//Copyright (c) 2022 Ezequiel Medina

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

namespace AutoSplitterCore
{
    public class IGTModule
    {
        public int gameSelect = 0;
        SekiroSplitter sekiroSplitter;
        EldenSplitter eldenSplitter;
        Ds3Splitter ds3Splitter;
        Ds1Splitter ds1Splitter;
        CelesteSplitter celesteSplitter;
        CupheadSplitter cupSplitter;
        public int ReturnCurrentIGT()
        {
            switch (gameSelect)
            {
                case GameConstruction.SekiroSplitterIndex:
                    return sekiroSplitter.getTimeInGame();
                case GameConstruction.Ds1SplitterIndex:
                    return ds1Splitter.getTimeInGame();
                case GameConstruction.Ds3SplitterIndex:
                    return ds3Splitter.getTimeInGame();
                case GameConstruction.EldenSplitterIndex:
                    return eldenSplitter.getTimeInGame();
                case GameConstruction.CelesteSplitterIndex:
                    return celesteSplitter.getTimeInGame();
                case GameConstruction.CupheadSplitterIndex:
                    return cupSplitter.getTimeInGame();

                case GameConstruction.Ds2SplitterIndex:
                case GameConstruction.HollowSplitterIndex:
                case GameConstruction.DishonoredSplitterIndex:
                case GameConstruction.NoneSplitterIndex:
                default:
                    return -1;
            }
        }

        public void setSplitterPointers(SekiroSplitter sekiroSplitter, EldenSplitter eldenSplitter, Ds3Splitter ds3Splitter, CelesteSplitter celesteSplitter, CupheadSplitter cupSplitter, Ds1Splitter ds1Splitter)
        {
            this.sekiroSplitter = sekiroSplitter;
            this.eldenSplitter = eldenSplitter;
            this.ds3Splitter = ds3Splitter;
            this.ds1Splitter = ds1Splitter;
            this.celesteSplitter = celesteSplitter;
            this.cupSplitter = cupSplitter;
        }
    }
}