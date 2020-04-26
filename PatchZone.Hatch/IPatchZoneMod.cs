using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatchZone.Hatch
{
    public interface IPatchZoneMod
    {
        void Init(IPatchZoneContext context);

        void OnBeforeGameStart();
    }
}
