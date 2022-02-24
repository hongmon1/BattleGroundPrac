using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//state가 특정 조건을 만족하면 state가 바뀐다
[CreateAssetMenu(menuName = "PluggableAI/State")]
public class State : ScriptableObject
{
    public Action[] actions;
    public Transition[] transitions;

    public Color sceneGizmoColor = Color.gray;

    public void DoActions(StateController controller)
    {
        for(int i = 0; i < actions.Length; i++)
        {
            actions[i].Act(controller);
        }
    }

    //스테이트 바뀔 때 호출
    //준비용 
    public void OnEnableActions(StateController controller)
    {

        for(int i=0; i < actions.Length; i++)
        {
            actions[i].OnReadyAction(controller);
        }
        for(int i=transitions.Length - 1; i >= 0; i--)
        {
            transitions[i].decision.OnEnableDecision(controller);
        }
    }

    public void CheckTrasitions(StateController controller)
    {
        for(int i = 0; i < transitions.Length; i++)
        {
            bool decision =transitions[i].decision.Decide(controller);
            if (decision)
            {
                controller.TransitionToState(transitions[i].trueState, transitions[i].decision);
            }
            else
            {
                controller.TransitionToState(transitions[i].falseState, transitions[i].decision);
            }

            //스테이트가 바뀌면
            if(controller.currentState != this)
            {
                controller.currentState.OnEnableActions(controller);
                break;
            }
        }
    }
}
