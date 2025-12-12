namespace CLD.HFSM
{
    public readonly struct StateHandlers
    {
        public readonly StateEnterAction? Enter;
        public readonly StateEnterActionAsync? EnterAsync;
        public readonly StateExitAction? Exit;
        public readonly StateExitActionAsync? ExitAsync;

        public StateHandlers(StateEnterAction? enter = null, StateEnterActionAsync? enterAsync = null, StateExitAction? exit = null, StateExitActionAsync? exitAsync = null)
        {
            Enter = enter;
            EnterAsync = enterAsync;
            Exit = exit;
            ExitAsync = exitAsync;
        }

        public void OnEnter()
        {
            Enter?.Invoke();
        }

        public void OnExit()
        {
            Exit?.Invoke();
        }

        public void OnEnterAsync()
        {
            EnterAsync?.Invoke();
        }

        public void OnExitAsync()
        {
            ExitAsync?.Invoke();
        }

        public static bool EmptyGuard() => true;
    }
}
