using System;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    public class Commander
    {
        private bool _parsed;
        private readonly MyCommandLine _commandLine = new MyCommandLine();

        public Commander()
        {
        }

        public Commander Read(string arguments)
        {
            _parsed = _commandLine.TryParse(arguments);
            return this;
        }
        public Commander Command(string flag, Action action)
        {
            if (_parsed && _commandLine.Switches.Contains(flag)) action();
            return this;
        }
    }
}