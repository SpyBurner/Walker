using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class StateManager<EState> : MonoBehaviour where EState : Enum
{
    protected Dictionary<EState, BaseState<EState>> States = new Dictionary<EState, BaseState<EState>>();
    protected BaseState<EState> CurrentState;

    protected bool IsTransitingState = false;
    private void Awake() { }
    private void Start() {

        CurrentState.EnterState();
    }
    private void Update() {
        CurrentState.UpdateState();
    }

    protected void TransitionToState(EState stateKey) {
        if (stateKey.Equals(CurrentState.StateKey)) return;

        IsTransitingState = true;
        CurrentState.ExitState();
        CurrentState = States[stateKey];
        CurrentState.EnterState();
        IsTransitingState = false;
    }

}
