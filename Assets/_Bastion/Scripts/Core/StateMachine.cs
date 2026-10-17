using System;

namespace Bastion.Core
{
    /// <summary>
    /// 플레이어, 적, 보스가 공통으로 사용하는 단순 상태 머신.
    /// </summary>
    public sealed class StateMachine
    {
        public IState CurrentState { get; private set; }

        public event Action<IState, IState> StateChanged;

        public void Initialize(IState startState)
        {
            CurrentState = startState ?? throw new ArgumentNullException(nameof(startState));
            CurrentState.Enter();
        }

        public void ChangeState(IState nextState)
        {
            if (nextState == null || nextState == CurrentState)
                return;

            IState previous = CurrentState;
            previous?.Exit();
            CurrentState = nextState;
            CurrentState.Enter();
            StateChanged?.Invoke(previous, CurrentState);
        }

        public void Tick() => CurrentState?.Tick();

        public void FixedTick() => CurrentState?.FixedTick();
    }
}
