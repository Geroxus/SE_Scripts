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
        private const float OpenRatio = 0.6f;

        private State _state = State.TowardsClose;
        private Logger _logger;

        IMyPistonBase _piston = null;
        IMyDoor _door = null;
        List<IMyAirtightHangarDoor> _airlockBlocks = new List<IMyAirtightHangarDoor>();
        MyCommandLine _commandLine = new MyCommandLine();

        List<IMyTextSurface> _textSurfaces = new List<IMyTextSurface>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            _logger = new Logger(me: Me, terminalSystem: GridTerminalSystem);
            _logger.CollectTextSurfaces("PistonControlScreen");
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
            StringBuilder debug = new StringBuilder();

            _logger.Log("State: ").Log(_state.ToString(), true);
            _logger.Log("piston: ").Log(_piston != null ? _piston.DisplayNameText : "-", true);
            _logger.Log("airlock: ").Log(_airlockBlocks.Count.ToString(), true);
            if (updateSource == UpdateType.Trigger)
            {
                switch (_state)
                {
                    case State.TowardsClose:
                        _state = State.TowardsOpen;
                        break;
                    case State.TowardsOpen:
                        _state = State.TowardsClose;
                        break;
                }

                StatusCheck();
                ToggleHangarDoors();
            }
            else if (updateSource == UpdateType.Terminal)
            {
                // Initblock is needed once.
                if (_commandLine.TryParse(argument))
                {
                    _piston = GridTerminalSystem.GetBlockWithName(_commandLine.Argument(0)) as IMyPistonBase;
                    GridTerminalSystem.GetBlockGroupWithName(_commandLine.Argument(1)).GetBlocksOfType(_airlockBlocks);
                    _door = GridTerminalSystem.GetBlockWithName(_commandLine.Argument(2)) as IMyDoor;
                }

                if (_piston == null || _door == null || _airlockBlocks.Count == 0)
                {
                    _logger.Log("Please enter a valid piston, door and airlock block group", true);
                    _logger.WriteOutput();
                    return;
                }

                StatusCheck();
            }
            else if (updateSource == UpdateType.Update100 && _airlockBlocks.Count > 0 && _piston != null)
            {
                ToggleDoor();
                ToggleHangarDoors();
                TogglePiston();
                StatusCheck();
            }

            _logger.WriteOutput();
        }

        private void ToggleDoor()
        {
            switch (_state)
            {
                case State.TowardsOpen:
                    _door.CloseDoor();
                    if (_door.Status == DoorStatus.Closed) _door.Enabled = false;
                    break;
                case State.TowardsClose:
                    if (_airlockBlocks.TrueForAll(door => door.Status == DoorStatus.Closed))
                    {
                        _door.Enabled = true;
                        _door.OpenDoor();
                    }

                    break;
            }
        }

        private void TogglePiston()
        {
            switch (_state)
            {
                case State.TowardsOpen:
                    bool doorsInRestrictedOpenState = _airlockBlocks.TrueForAll(door =>
                        door.OpenRatio >= OpenRatio && door.Status == DoorStatus.Opening);
                    if (doorsInRestrictedOpenState) _piston.Extend();
                    break;
                case State.TowardsClose:
                    _piston.Retract();
                    break;
            }
        }

        private void ToggleHangarDoors()
        {
            switch (_state)
            {
                case State.TowardsClose:
                    if (_piston.Status == PistonStatus.Retracted)
                    {
                        foreach (IMyAirtightHangarDoor hangarDoor in _airlockBlocks)
                        {
                            hangarDoor.Enabled = true;
                            hangarDoor.CloseDoor();
                        }
                    }

                    break;
                case State.TowardsOpen:
                {
                    if (_door.Status != DoorStatus.Closed) break;
                    foreach (IMyAirtightHangarDoor hangarDoor in _airlockBlocks)
                    {
                        hangarDoor.OpenDoor();
                        if (hangarDoor.OpenRatio >= OpenRatio) hangarDoor.Enabled = false;
                    }

                    break;
                }
            }
        }

        private void StatusCheck()
        {
            foreach (IMyAirtightHangarDoor door in _airlockBlocks)
            {
                _logger.Log(door.DisplayNameText)
                    .Log(": ")
                    .Log(Math.Round(door.OpenRatio, 2).ToString())
                    .Log(" | ")
                    .Log(door.Status.ToString(), true);
            }

            _logger.Log("Piston: ")
                .Log(_piston.Status.ToString(), true);

            _logger.Log("Door: ")
                .Log(_door.Status.ToString(), true);
        }

        enum State
        {
            TowardsOpen,
            TowardsClose
        }
    }
}