using System;
using Microsoft.Win32;

namespace IonMobility
{
    /// <summary>
    /// RegistryEZ will provide classes with the means to store variables by initializing RegistryEZ with a
    /// location in the registry. It will also do the appropriate exception handling.
    /// </summary>
    public class RegistryEZ
    {
        private RegistryKey _key;

        /// <summary>
        /// UserAppDataRegistry should be Application.UserAppDataRegistry for forms.
        /// </summary>
        /// <param name="UserAppDataRegistry"></param>
        /// <param name="subkey"></param>
        public RegistryEZ(RegistryKey UserAppDataRegistry, string subkey)
        {
            _key = UserAppDataRegistry.CreateSubKey(subkey);
        }

        public void Persist(string name, string val)
        {
            _key.SetValue(name, val);
        }

        public double RestoreDouble(string name)
        {
            try
            {
                double d = Convert.ToDouble(_key.GetValue(name));
                return d;
            }
            catch(Exception ex)
            {
                return 0.0;
            }
        }

        public string RestoreString(string name)
        {
            try
            {
                string d = Convert.ToString(_key.GetValue(name));
                return d;
            }
            catch(Exception ex)
            {
                return String.Empty;
            }
        }

        public int RestoreInt32(string name)
        {
            try
            {
                int d = Convert.ToInt32(_key.GetValue(name));
                return d;
            }
            catch(Exception ex)
            {
                return 0;
            }
        }
    }
}
