namespace CLD.HFSM
{
    public readonly struct StateHandlersAsync
    {
        public readonly StateEnterActionAsync? Enter;
        public readonly StateExitActionAsync? Exit;

        public StateHandlersAsync(
            StateEnterActionAsync? enter = null,
            StateExitActionAsync? exit = null)
        {
            Enter = enter;
            Exit = exit;
        }

        public void OnEnter()
        {
            Enter?.Invoke();
        }

        public void OnExit()
        {
            Exit?.Invoke();
        }
    }
}
