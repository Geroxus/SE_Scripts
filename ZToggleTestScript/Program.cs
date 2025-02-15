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
using Sandbox.ModAPI.Interfaces.Terminal;
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
        private readonly Logger _logger;
        private readonly GridScan _scan;
        private readonly Commander _commander;


        public Program()
        {
            _logger = new Logger(Me, GridTerminalSystem);
            _logger.CollectTextSurfaces("ToggleTest");

            _scan = new GridScan(GridTerminalSystem);

            _commander = new Commander();
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
            _commander.Read(argument);

            
            
            _logger.To("ToggleTest").Consume(b => Echo(b.ToString())).Write().Clear();
        }
    }
}