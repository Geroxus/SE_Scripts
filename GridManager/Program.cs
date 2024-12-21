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
        private readonly Logger _logger;

        private readonly IMyShipController _bridge;
        private IMyDoor _door;

        private double _lastSpeed;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            
            _logger = new Logger(Me, GridTerminalSystem);
            _logger.CollectTextSurfaces("GridManager");

            List<IMyShipController> shipControllers = new List<IMyShipController>();
            GridTerminalSystem.GetBlocksOfType(shipControllers);
            _bridge = shipControllers.Find(c => c.IsMainCockpit);

            List<IMyDoor> allDoors = new List<IMyDoor>();
            GridTerminalSystem.GetBlocksOfType(allDoors);
            _door = allDoors.Find(d => d.DisplayNameText == "Bridge Access Door");
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
            if (_bridge.IsUnderControl)
            {
                _logger.Log("Bridge is under control", true);
                CloseDownBridge();
            }
            else
            {
                _logger.Log("Bridge is not controlled", true);
                OpenBridge();
            }
            DisplayFlightInformation();
            _logger.WriteOutput();
        }

        private void DisplayFlightInformation()
        {
            double speed = Math.Round(_bridge.GetShipSpeed(), 2);
            double acceleration = speed - _lastSpeed;
            _lastSpeed = speed;
            
            _logger.Log("Speed: ")
                .Log(speed.ToString(), true);
            // _bridge.MoveIndicator.ToString()
            _logger.Log("Acceleration: ")
                .Log(acceleration.ToString(), true);
            _logger.Log("Dampeners: ")
                .Log(_bridge.DampenersOverride.ToString(), true);
        }

        private void OpenBridge()
        {
            if (_door.Status == DoorStatus.Open) return;
            _door.Enabled = true;
            _door.OpenDoor();
        }

        private void CloseDownBridge()
        {
            if (!_door.Enabled) return;
            _door.CloseDoor();
            if (_door.Status == DoorStatus.Closed) _door.Enabled = false;
        }
    }
}