using Sandbox.Game.Entities.Blocks;
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
        List<IMyBatteryBlock> myBatteries = new List<IMyBatteryBlock>();
        float maxStoredPower = 0;
        float maxInput = 0;

        List<IMyPowerProducer> myPowerProducers = new List<IMyPowerProducer>();

        List<IMyGasTank> myGasTanks = new List<IMyGasTank>();

        IMyTextSurface mySurface;

        MyCommandLine _commandLine = new MyCommandLine();
        List<string> textSurfaceNameList = new List<string>();
        List<IMyTextSurface> textSurfaceList = new List<IMyTextSurface>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            GridTerminalSystem.GetBlocksOfType(myBatteries);
            GridTerminalSystem.GetBlocksOfType(myPowerProducers);
            GridTerminalSystem.GetBlocksOfType(myGasTanks);

            myBatteries = myBatteries.Where(block => block.IsSameConstructAs(Me)).ToList();
            myPowerProducers = myPowerProducers.Where(block => !(block is IMyBatteryBlock) && block.IsSameConstructAs(Me)).ToList();
            myGasTanks = myGasTanks.Where(block => block.DisplayNameText.Contains("Hydrogen") && block.IsSameConstructAs(Me)).ToList();

            mySurface = Me.GetSurface(0) as IMyTextSurface;
            mySurface.ContentType = ContentType.TEXT_AND_IMAGE;

            foreach (IMyBatteryBlock block in myBatteries)
            {
                maxStoredPower += block.MaxStoredPower;
                maxInput += block.MaxInput;
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
            StringBuilder output = new StringBuilder();

            if (_commandLine.TryParse(argument))
            {
                for (int i = 0; i < _commandLine.ArgumentCount; i++)
                {
                    string name = _commandLine.Argument(i);
                    if (!textSurfaceNameList.Contains(name))
                    {
                        IMyTextSurfaceProvider block = GridTerminalSystem.GetBlockWithName(name) as IMyTextSurfaceProvider;
                        IMyTextSurface surface = block.GetSurface(0);
                        surface.ContentType = ContentType.TEXT_AND_IMAGE;
                        surface.Font = "Green";
                        textSurfaceList.Add(surface);
                    }
                }
            }
            if (textSurfaceList.Count > 0)
            {
                debug.Append("Text Surfaces Found:");
                foreach (IMyTextSurface surface in textSurfaceList)
                {
                    debug.Append(surface.Name);
                }
            }


            /*
             * Batteries
             */
            output.AppendLine("Batteries:");
            debug.AppendLine("Batteries");

            float currentStoredPower = 0;
            float currentInput = 0;
            float currentOutput = 0;
            bool charging = false;
            foreach (IMyBatteryBlock block in myBatteries)
            {
                debug.AppendLine($"Mode:{block.ChargeMode} on {block.DisplayNameText}");
                debug.AppendLine($"Charging:{block.IsCharging} Stored:{Math.Round(block.CurrentStoredPower, 2)} MWh");

                currentStoredPower += block.CurrentStoredPower;
                currentInput += block.CurrentInput;
                currentOutput += block.CurrentOutput;

                charging |= block.IsCharging;
            }

            output.AppendLine($"Stored: {Math.Round(currentStoredPower, 2)}/{maxStoredPower} MWh");
            if ( charging )
            {
                output.AppendLine($"Charging: {Math.Round(currentInput, 2)}/{maxInput} MW");
            }
            else
            {
                output.AppendLine("Charging: False");
            }
            output.AppendLine($"Output:   {Math.Round(currentOutput, 2)} MW");
            output.AppendLine();

            /*
             * Power Producers
             */
            output.AppendLine("Power Production:");
            debug.AppendLine("Power Production");

            float currentProducerOutput = 0;
            foreach (IMyPowerProducer producer in myPowerProducers)
            {
                debug.AppendLine($"{producer.DisplayNameText}");

                currentProducerOutput += producer.CurrentOutput;
            }
            output.AppendLine($"Produced: {Math.Round(currentProducerOutput, 2)} MW");
            output.AppendLine();

            /*
             * Total
             */
            output.AppendLine($"Total Poweroutput: {Math.Round(currentProducerOutput + currentOutput, 2)} MW");
            output.AppendLine();

            /*
             * Hydrogen Fill Percentage
             */
            debug.AppendLine("Hydro Tanks");

            double currentHydroFillAcummulated = 0;
            foreach (IMyGasTank tank in myGasTanks)
            {
                debug.AppendLine(tank.DisplayNameText);

                currentHydroFillAcummulated += tank.FilledRatio;
            }
            double currentHydroFillPercent = currentHydroFillAcummulated / myGasTanks.Count;
            output.AppendLine($"Hydrogen Fill status: {Math.Round(currentHydroFillPercent, 2)}%");

            writeTextToSurfaces(output.ToString());
            Echo(debug.ToString());
        }

        private void writeTextToSurfaces(String output)
        {
            mySurface.WriteText(output.ToString());
            foreach (IMyTextSurface surface in textSurfaceList)
            {
                surface.WriteText(output);
            }
        }
    }
}
