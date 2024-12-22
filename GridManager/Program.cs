using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private readonly Logger _logger;

        private readonly IMyShipController _bridge;
        private readonly IMyDoor _door;
        private readonly IMyShipConnector _connector;

        private double _lastSpeed;

        private readonly Commander _commander = new Commander();
        private int _targetVelocity;
        private readonly List<IMyThrust> _forwardThrusters = new List<IMyThrust>();
        private readonly List<IMyThrust> _reverseThrusters = new List<IMyThrust>();
        private readonly List<IMyInteriorLight> _lights = new List<IMyInteriorLight>();
        private readonly List<Hangar> _hangars = new List<Hangar>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            _logger = new Logger(Me, GridTerminalSystem);
            _logger.CollectTextSurfaces("GridManager", "HangarControl");

            List<IMyShipController> shipControllers = new List<IMyShipController>();
            GridTerminalSystem.GetBlocksOfType(shipControllers);
            _bridge = shipControllers.Find(c => c.IsMainCockpit);

            List<IMyDoor> allDoors = new List<IMyDoor>();
            GridTerminalSystem.GetBlocksOfType(allDoors);
            _door = allDoors.Find(d => d.DisplayNameText == "Bridge Access Door");

            GridTerminalSystem.GetBlocksOfType(_forwardThrusters,
                tr => _bridge.Orientation.TransformDirection(tr.Orientation.Forward) ==
                      Base6Directions.Direction.Forward);
            GridTerminalSystem.GetBlocksOfType(_reverseThrusters,
                tr => _bridge.Orientation.TransformDirection(tr.Orientation.Forward) ==
                      Base6Directions.Direction.Backward);

            _connector = GridTerminalSystem.GetBlockWithName("Connector") as IMyShipConnector;

            List<IMyGravityGenerator> allGravGenerators = new List<IMyGravityGenerator>();
            GridTerminalSystem.GetBlocksOfType(allGravGenerators);
            IMyGravityGenerator gravityGenerator = allGravGenerators.First();

            Vector3I gridV = Me.CubeGrid.Max - Me.CubeGrid.Min;
            float width = gridV.Y * 3;
            float height = gridV.Z * 3;
            float depth = gridV.X * 3;
            gravityGenerator.FieldSize = new Vector3(width, height, depth);

            // Get All Hangars and sort them into their respective Objects
            // find all Hangar doors and sort
            List<IMyDoor> hangarDoors = new List<IMyDoor>();
            GridTerminalSystem.GetBlocksOfType(hangarDoors, door => door.CustomData.Contains("[HangarControl.Config]"));
            Echo("Version 8");
            Echo(hangarDoors.Count.ToString());
            Dictionary<string, List<IMyDoor>> doorsById = new Dictionary<string, List<IMyDoor>>();
            foreach (IMyDoor hangarDoor in hangarDoors)
            {
                string id = hangarDoor.CustomData.Split('\n').Single(s => s.Contains("id")).ToString().Split(':')[1];
                List<IMyDoor> doorList = doorsById.GetValueOrNew(id);
                doorList.Add(hangarDoor);
                doorsById[id] = doorList;
            }

            // find all reflector lights and sort
            List<IMyFunctionalBlock> functionalBlocks = new List<IMyFunctionalBlock>();
            GridTerminalSystem.GetBlocksOfType(functionalBlocks, l => l.CustomData.Contains("[HangarControl.Config]"));
            Echo(functionalBlocks.Count.ToString());
            Dictionary<string, List<IMyFunctionalBlock>> lightsById = new Dictionary<string, List<IMyFunctionalBlock>>();
            foreach (IMyFunctionalBlock functionalBlock in functionalBlocks)
            {
                if (!(functionalBlock is IMyLightingBlock) && !(functionalBlock is IMySearchlight)) continue;
                string id = functionalBlock.CustomData.Split('\n').Single(s => s.Contains("id")).ToString()
                    .Split(':')[1];
                List<IMyFunctionalBlock> lightsList = lightsById.GetValueOrNew(id);
                lightsList.Add(functionalBlock);
                lightsById[id] = lightsList;
            }
            
            // create appropriate Hangar Objects
            foreach (string key in doorsById.Keys)
            {
                Echo(key);
                _hangars.Add(new Hangar(key, doorsById[key],
                    lightsById.GetValueOrDefault(key, new List<IMyFunctionalBlock>())));
            }

            GridTerminalSystem.GetBlocksOfType(_lights, l => !l.CustomData.Contains("[HangarControl.Config]"));
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
                _commander.Read(argument)
                    .Command("connector", () => _connector.ToggleConnect())
                    .Command("dampeners", () => _bridge.DampenersOverride = !_bridge.DampenersOverride)
                    .Command("speedUp", () => _targetVelocity += 10)
                    .Command("speedDown", () => _targetVelocity -= 10);

                UpdateSpeed();
            }

            DisplayFlightInformation();

            UpdateLights();
            DisplayInteriorInformation();

            UpdateHangar();
            DisplayHangarInformation();
            _logger.WriteOutput();
        }

        private void UpdateHangar()
        {
            foreach (Hangar hangar in _hangars)
            {
                if (hangar.PrimaryDoors.First().Status == DoorStatus.Opening)
                {
                    hangar.Open();
                }
                else if (hangar.PrimaryDoors.First().Status == DoorStatus.Closing)
                {
                    hangar.Close();
                }
                else if (hangar.PrimaryDoors.First().Status == DoorStatus.Closed)
                {
                    hangar.Close();
                }
            }
        }

        private void DisplayHangarInformation()
        {
            const string controlSurfaceName = "HangarControl";
            _logger.Log("Hangar Information", true, controlSurfaceName)
                .Log("Hangars: " + _hangars.Count.ToString(), true, controlSurfaceName);
            _hangars.ForEach(d => _logger.Log(d.ID.ToString() + ": " + d.HangarState.ToString(), true,
                controlSurfaceName: controlSurfaceName)
                .Log("Light Count:" + d.HangarLights.Count.ToString(), true, controlSurfaceName));
        }

        private void UpdateLights()
        {
            if (_connector.Status == MyShipConnectorStatus.Connected)
            {
                foreach (IMyInteriorLight light in _lights)
                {
                    light.Color = Color.LightGoldenrodYellow;
                }
            }
            else if (_bridge.IsUnderControl)
            {
                foreach (IMyInteriorLight light in _lights)
                {
                    light.Color = Color.LightCyan;
                }
            }
            else
            {
                foreach (IMyInteriorLight light in _lights)
                {
                    light.Color = Color.White;
                }
            }
        }

        private void DisplayInteriorInformation()
        {
            _logger.Log(_lights.Count.ToString(), true);
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

        class Hangar
        {
            public List<IMyDoor> PrimaryDoors { get; }
            public List<IMyFunctionalBlock> HangarLights { get; }
            public string ID { get; }
            public HangarState HangarState { get; private set; }

            public Hangar(string id, List<IMyDoor> primaryDoors, List<IMyFunctionalBlock> hangarLights)
            {
                ID = id;
                PrimaryDoors = primaryDoors;
                HangarLights = hangarLights;
                HangarState = HangarState.Close;

                foreach (IMyFunctionalBlock light in HangarLights)
                {
                    light.Enabled = false;
                }
            }

            public void Open()
            {
                HangarState = HangarState.Open;
                foreach (IMyDoor door in PrimaryDoors)
                {
                    door.OpenDoor();
                }

                foreach (IMyFunctionalBlock light in HangarLights)
                {
                    if (light.CustomData.Contains("Searchlight"))
                    {
                        light.Enabled = true;
                    }
                    else if (light.CustomData.Contains("RotatingLight"))
                    {
                        light.Enabled = true;
                        ((IMyReflectorLight)light).Color = Color.OrangeRed;
                        light.GetProperty("RotationSpeed").As<Single>().SetValue(light, 15);
                    }
                }
            }

            public void Close()
            {
                HangarState = HangarState.Close;

                if (PrimaryDoors.TrueForAll(d => d.Status == DoorStatus.Closed))
                {
                    foreach (IMyFunctionalBlock light in HangarLights) light.Enabled = false;
                }
                else
                {
                    foreach (IMyDoor door in PrimaryDoors) door.CloseDoor();
                }
            }
        }
    }

    enum HangarState
    {
        Open,
        Close
    }
}