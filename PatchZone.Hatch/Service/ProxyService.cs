using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PatchZone.Hatch.Utils;

namespace PatchZone.Hatch
{
    public class ProxyService<TImpl, TService> : Singleton<TImpl>
        where TImpl : class
    {
        public TService Vanilla { get; internal set; }
    }
}
