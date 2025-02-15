using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        private class Hangar
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
}