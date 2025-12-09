using System.Threading.Tasks;

namespace CLD.HFSM
{
    public interface IStateHandler
    {
        void OnEnter();
        void OnExit();
    }

    public interface IStateHandlerAsync
    {
        ValueTask OnEnter();
        ValueTask OnExit();
    }
}
