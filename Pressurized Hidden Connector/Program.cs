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
        IMyPistonBase piston = null;
        List<IMyAirtightHangarDoor> airlockBlocks = new List<IMyAirtightHangarDoor>();
        MyCommandLine _commandLine = new MyCommandLine();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
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
            if (updateSource == UpdateType.Trigger || updateSource == UpdateType.Terminal)
            {
                if (_commandLine.TryParse(argument))
                {
                    piston = GridTerminalSystem.GetBlockWithName(_commandLine.Argument(0)) as IMyPistonBase;
                    GridTerminalSystem.GetBlockGroupWithName(_commandLine.Argument(1)).GetBlocksOfType(airlockBlocks);
                }

                if (piston == null || airlockBlocks.Count == 0)
                {
                    Echo("Please enter a valid piston or airlock block group");
                    return;
                }

                if (airlockBlocks.TrueForAll(door => door.Status == DoorStatus.Closed))
                {
                    Echo("The airlock block group is closed");
                    OpenHangarDoors();
                }
                else if (airlockBlocks.TrueForAll(door => door.Status != DoorStatus.Closed))
                {
                    Echo("The airlock block group is open");
                    CloseHangarDoors();
                }

                foreach (IMyAirtightHangarDoor door in airlockBlocks)
                {
                    Echo(door.OpenRatio.ToString());
                    Echo(door.Closed ? "closed" : "open");
                    Echo(door.Status.ToString());
                }
            } else if (updateSource == UpdateType.Update100)
            {
                if (airlockBlocks.TrueForAll(door => door.OpenRatio >= 0.6f && door.Status == DoorStatus.Opening))
                {
                    foreach (IMyAirtightHangarDoor door in airlockBlocks)
                    {
                        door.Enabled = false;
                    }
                }
            }
        }

        private void CloseHangarDoors()
        {
            foreach (IMyAirtightHangarDoor hangarDoor in airlockBlocks)
            {
                hangarDoor.Enabled = true;
                hangarDoor.CloseDoor();
            }
        }

        private void OpenHangarDoors()
        {
            foreach (IMyAirtightHangarDoor hangarDoor in airlockBlocks)
            {
                hangarDoor.OpenDoor();
            }
        }
    }
}