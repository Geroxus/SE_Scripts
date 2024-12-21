using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private readonly Logger _logger;

        private readonly IMyShipController _bridge;
        private IMyDoor _door;

        private double _lastSpeed;
        
        private readonly Commander<IMyShipController> _bridgeCommander;
        private int _targetVelocity;
        private readonly List<IMyThrust> _forwardThrusters = new List<IMyThrust>();
        private readonly List<IMyThrust> _reverseThrusters = new List<IMyThrust>();

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
            
            GridTerminalSystem.GetBlocksOfType(_forwardThrusters, tr => _bridge.Orientation.TransformDirection(tr.Orientation.Forward) == Base6Directions.Direction.Forward);
            GridTerminalSystem.GetBlocksOfType(_reverseThrusters, tr => _bridge.Orientation.TransformDirection(tr.Orientation.Forward) == Base6Directions.Direction.Backward);
            
            _bridgeCommander = new Commander<IMyShipController>(_bridge);
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

                ResetThrusters();
            }
            else
            {
                _logger.Log("Bridge is not controlled", true);
                OpenBridge();
                
                // Can only execute these commands, when bridge is not controlled
                _bridgeCommander.Read(argument)
                    .Command("dampeners", c => c.DampenersOverride = !c.DampenersOverride)
                    .Command("speedUp", _ => _targetVelocity += 10)
                    .Command("speedDown", _ => _targetVelocity -= 10);

                UpdateSpeed();
            }
            DisplayFlightInformation();
            _logger.WriteOutput();
        }

        private void UpdateSpeed()
        {
            if (_targetVelocity == 0)
            {
                ResetThrusters();
                _bridge.DampenersOverride = true;
                return;
            }
            int variance = 5;
            if (_bridge.GetShipSpeed() + variance < _targetVelocity)
            {
                _bridge.DampenersOverride = false;
                Accelerate();
            }
            else if (_bridge.GetShipSpeed() - variance > _targetVelocity)
            {
                _bridge.DampenersOverride = false;
                Decelerate();
            }
            else
            {
                ResetThrusters();
            }
        }

        private void ResetThrusters()
        {
            foreach (IMyThrust thruster in _forwardThrusters)
            {
                thruster.ThrustOverride = 0f;
            }
            foreach (IMyThrust thruster in _reverseThrusters)
            {
                thruster.ThrustOverride = 0f;
            }
        }

        private void Accelerate()
        {
            foreach (IMyThrust thruster in _forwardThrusters)
            {
                thruster.ThrustOverride = 150000f;
            }

            foreach (IMyThrust thruster in _reverseThrusters)
            {
                thruster.ThrustOverride = 0f;
            }
        }

        private void Decelerate()
        {
            
            foreach (IMyThrust thruster in _forwardThrusters)
            {
                thruster.ThrustOverride = 0f;
            }

            foreach (IMyThrust thruster in _reverseThrusters)
            {
                thruster.ThrustOverride = 150000f;
            }
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
            _logger.Log("Target Velocity: ")
                .Log(_targetVelocity.ToString(), true);
            _logger.Log("Dampeners: ")
                .Log(_bridge.DampenersOverride.ToString(), true);
        }

        private void OpenBridge()
        {
            if (_door == null) return;
            if (_door.Status == DoorStatus.Open) return;
            _door.Enabled = true;
            _door.OpenDoor();
        }

        private void CloseDownBridge()
        {
            if (_door == null) return;
            if (!_door.Enabled) return;
            _door.CloseDoor();
            if (_door.Status == DoorStatus.Closed) _door.Enabled = false;
        }
    }
}