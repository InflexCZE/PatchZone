using System;

namespace PatchZone.Hatch.Utils
{
    public abstract class Singleton<T>
        where T : class
    {
        public static T Instance { get; private set; }

        protected Singleton()
        {
            if (Instance != null)
            {
                throw new Exception(GetType().FullName + " is singleton");
            }

            Instance = (T) (object) this;
        }
    }
}
