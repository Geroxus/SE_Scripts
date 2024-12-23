using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private readonly GridScan _gridScan;
        private readonly Logger _logger;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            if (Me.CubeGrid.GridSizeEnum != MyCubeSize.Small) throw new Exception("Grid size is not small");
            _logger = new Logger(Me, GridTerminalSystem);
            _logger.CollectTextSurfaces("GridManager");
            _gridScan = new GridScan(GridTerminalSystem);
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            foreach (Base6Directions.Direction direction in Enum.GetValues(typeof(Base6Directions.Direction)))
            {
                _logger.Log(direction.ToString() + ": " + _gridScan.ThrusterByDirection[direction].Count.ToString(), true);
            }
            _logger.WriteOutput();
        }
    }
}