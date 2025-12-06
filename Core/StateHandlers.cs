namespace CLD.HFSM
{
    public readonly struct StateHandlers
    {

        public readonly StateEnterAction? Enter;
        public readonly StateExitAction? Exit;

        public StateHandlers(StateEnterAction? enter = null, StateExitAction? exit = null)
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
