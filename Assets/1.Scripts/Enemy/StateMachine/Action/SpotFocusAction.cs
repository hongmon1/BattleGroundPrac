using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PluggableAI/Actions/Spot Focus")]
public class SpotFocusAction : Action
{
    public override void Act(StateController controller)
    {
        controller.nav.destination = controller.personalTarget;
        controller.nav.speed = 0f; //이동하지 않고 타겟을 바라봄
    }
}
