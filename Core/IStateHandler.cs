using System.Threading.Tasks;

namespace CLD.HFSM
{
    public interface IStateHandler
    {
        void OnEnter();
        void OnExit();
        ValueTask OnEnterAsync();
        ValueTask OnExitAsync();
    }
}
