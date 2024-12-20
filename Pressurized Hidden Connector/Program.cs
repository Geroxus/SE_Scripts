﻿using Sandbox.Game.EntityComponents;
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
        private const float CloseRatio = 0.1f;

        private State _state = State.TowardsClose;

        IMyPistonBase _piston = null;
        List<IMyAirtightHangarDoor> _airlockBlocks = new List<IMyAirtightHangarDoor>();
        MyCommandLine _commandLine = new MyCommandLine();

        List<IMyTextSurface> _textSurfaces = new List<IMyTextSurface>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            _textSurfaces.Add(Me.GetSurface(0));

            List<IMyTerminalBlock> allBlocksWithName = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName("LCD piston control", allBlocksWithName);
            foreach (IMyTerminalBlock block in allBlocksWithName)
            {
                var provider = block as IMyTextSurfaceProvider;
                if (provider != null)
                {
                    _textSurfaces.Add(provider.GetSurface(0));
                }
            }
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

            debug.Append("piston: ").AppendLine(_piston != null ? _piston.DisplayNameText : "-");
            debug.Append("airlock: ").AppendLine(_airlockBlocks.Count.ToString());
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
                ToggleDoors();
            } else if (updateSource == UpdateType.Terminal)
            {
                // Initblock is needed once.
                if (_commandLine.TryParse(argument))
                {
                    _piston = GridTerminalSystem.GetBlockWithName(_commandLine.Argument(0)) as IMyPistonBase;
                    GridTerminalSystem.GetBlockGroupWithName(_commandLine.Argument(1)).GetBlocksOfType(_airlockBlocks);
                }

                if (_piston == null || _airlockBlocks.Count == 0)
                {
                    debug.AppendLine("Please enter a valid piston and airlock block group");
                    WriteOutput(debug);
                    return;
                }
                StatusCheck(debug);
            }
            else if (updateSource == UpdateType.Update100 && _airlockBlocks.Count > 0 && _piston != null)
            {
                ToggleDoors();
                // Stop Doors (I think...wtf I do here?)
                bool doorsInRestrictedOpenState = _airlockBlocks.TrueForAll(door =>
                    door.OpenRatio >= OpenRatio && door.Status == DoorStatus.Opening);
                if (_state == State.TowardsOpen && _piston.Status == PistonStatus.Retracted && doorsInRestrictedOpenState)
                {
                    foreach (IMyAirtightHangarDoor door in _airlockBlocks)
                    {
                        door.Enabled = false;
                    }
                    _piston.Extend();
                }
                else if (_state == State.TowardsClose && _piston.Status == PistonStatus.Retracting && doorsInRestrictedOpenState)
                {
                    CloseHangarDoors();
                }

                StatusCheck(debug);
            }

            WriteOutput(debug);
        }

        private void ToggleDoors()
        {
            if (_state == State.TowardsClose)
            {
                CloseHangarDoors();
            } else if (_state == State.TowardsOpen)
            {
                OpenHangarDoors();
            }
        }

        private void StatusCheck(StringBuilder output)
        {
            foreach (IMyAirtightHangarDoor door in _airlockBlocks)
            {
                output.Append(door.DisplayNameText)
                    .Append(": ")
                    .Append(Math.Round(door.OpenRatio, 2).ToString())
                    .Append(" | ")
                    .AppendLine(door.Status.ToString());
            }

            output.Append("Piston: ")
                .AppendLine(_piston.Status.ToString());
        }

        private void WriteOutput(StringBuilder output)
        {
            Echo(output.ToString());
            foreach (IMyTextSurface surface in _textSurfaces)
            {
                surface.ContentType = ContentType.TEXT_AND_IMAGE;
                surface.Font = "Green";
                surface.WriteText(output.ToString());
            }
        }

        private void CloseHangarDoors()
        {
            if (_piston.Status == PistonStatus.Retracted)
            {
                foreach (IMyAirtightHangarDoor hangarDoor in _airlockBlocks)
                {
                    hangarDoor.Enabled = true;
                    hangarDoor.CloseDoor();
                }
            }
            else
            {
                _piston.Retract();
            }
        }

        private void OpenHangarDoors()
        {
            foreach (IMyAirtightHangarDoor hangarDoor in _airlockBlocks)
            {
                hangarDoor.OpenDoor();
            }
        }

        enum State
        {
            TowardsOpen,
            TowardsClose
        }
    }
}