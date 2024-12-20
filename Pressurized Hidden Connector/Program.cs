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

        IMyPistonBase _piston = null;
        List<IMyAirtightHangarDoor> _airlockBlocks = new List<IMyAirtightHangarDoor>();
        MyCommandLine _commandLine = new MyCommandLine();

        List<IMyTextSurface> _textSurfaces = new List<IMyTextSurface>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            _textSurfaces.Add(Me.GetSurface(0));

            CollectTextSurfaces();
        }

        private void CollectTextSurfaces()
        {
            List<IMyTextSurfaceProvider> textSurfaceProviders = new List<IMyTextSurfaceProvider>();
            GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(textSurfaceProviders);
            foreach (IMyTextSurfaceProvider textSurfaceProvider in textSurfaceProviders)
            {
                var myTerminalBlock = textSurfaceProvider as IMyTerminalBlock;
                if (myTerminalBlock != null && myTerminalBlock.CustomData.Contains("PistonControlScreen"))
                {
                    foreach (string dataLine in myTerminalBlock.CustomData.Split('\n'))
                    {
                        if (dataLine.StartsWith("PistonControlScreen"))
                        {
                            int index = Convert.ToInt32(dataLine.Split(':')[1].Trim());
                            _textSurfaces.Add(textSurfaceProvider.GetSurface(index));
                        }
                    }
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

            debug.Append("State: ").AppendLine(_state.ToString());
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
            }
            else if (updateSource == UpdateType.Terminal)
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
                TogglePiston();
                StatusCheck(debug);
            }

            WriteOutput(debug);
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

        private void ToggleDoors()
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
                    foreach (IMyAirtightHangarDoor hangarDoor in _airlockBlocks)
                    {
                        hangarDoor.OpenDoor();
                        if (hangarDoor.OpenRatio >= OpenRatio) hangarDoor.Enabled = false;
                    }

                    break;
                }
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

        enum State
        {
            TowardsOpen,
            TowardsClose
        }
    }
}