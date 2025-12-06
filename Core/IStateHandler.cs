using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CLD.HFSM
{
    public interface IStateHandler
    {
        void OnEnter();
        void OnExit();
    }
}
