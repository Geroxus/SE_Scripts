using System;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    public class Commander<T> where T : IMyTerminalBlock
    {
        private bool _parsed;
        private readonly T _entity;
        private readonly MyCommandLine _commandLine = new MyCommandLine();

        public Commander(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _entity = entity;
        }

        public Commander<T> Read(string arguments)
        {
            _parsed = _commandLine.TryParse(arguments);
            return this;
        }
        
        public Commander<T> Command(string flag, Action<T> action)
        {
            if (_parsed && _commandLine.Switches.Contains(flag)) action(_entity);
            return this;
        }
    }
}