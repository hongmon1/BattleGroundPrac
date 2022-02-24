using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PluggableAI/Actions/Reload")]
public class ReloadAction : Action
{

    public override void Act(StateController controller)
    {
        if(!controller.reloading && controller.bullets <= 0)
        {
            controller.enemyAnimation.anim.SetTrigger(FC.AnimatorKey.Reload);
            controller.reloading = true;
            SoundManager.Instance.PlayOneShotEffect((int)SoundList.reloadWeapon, controller.enemyAnimation.gunMuzzle.position, 2f);
        }
        //decision에서 총알 채워줌
        //action에서는 재장전 모션 사운드 재생
    }
}
