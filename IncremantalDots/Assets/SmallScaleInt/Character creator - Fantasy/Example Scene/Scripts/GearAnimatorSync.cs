using UnityEngine;

namespace SmallScaleInc.CharacterCreatorFantasy
{
    [RequireComponent(typeof(Animator))]
    public class GearAnimatorSync : MonoBehaviour
    {
        [Tooltip("Drag your player (with TopDownCharacterController) here.")]
        public TopDownCharacterController playerController;

        Animator _gearAnim;
        Animator _playerAnim;

        void Awake()
        {
            _gearAnim = GetComponent<Animator>();

            if (playerController != null)
            {
                _playerAnim = playerController.GetComponent<Animator>();

                // subscribe to the player's trigger event:
                playerController.OnTriggerFired += OnPlayerTrigger;
            }
        }

        void OnDestroy()
        {
            if (playerController != null)
                playerController.OnTriggerFired -= OnPlayerTrigger;
        }

        // void LateUpdate()
        // {
        //     if (_playerAnim == null) return;

        //     // copy floats & ints
        //     _gearAnim.SetFloat(  "Direction", _playerAnim.GetFloat("Direction"));
        //     _gearAnim.SetInteger("DirIndex",  _playerAnim.GetInteger("DirIndex"));

        //     // copy all bools
        //     foreach (var p in new[]{
        //         "IsRun","IsWalk","IsStrafeLeft","IsStrafeRight","IsRunBackwards",
        //         "UseIdle2","UseIdle3","UseIdle4",
        //         "IsCrouching","IsMounted",
        //         "Speed1x","Speed2x",
        //         // "IsCrouchMoving","IsRideMoving"
        //     })
        //         _gearAnim.SetBool(p, _playerAnim.GetBool(p));

        //     // copy playback speed
        //     _gearAnim.speed = _playerAnim.speed;
        // }

        public void SyncFromPlayer(Animator playerAnimator)
        {
            if (_gearAnim == null || playerAnimator == null) return;

            _gearAnim.SetFloat("Direction", playerAnimator.GetFloat("Direction"));
            _gearAnim.SetInteger("DirIndex", playerAnimator.GetInteger("DirIndex"));

            foreach (var p in new[]{
                "IsRun","IsWalk","IsStrafeLeft","IsStrafeRight","IsRunBackwards",
                "UseIdle2","UseIdle3","UseIdle4",
                "IsCrouching","IsMounted",
                "Speed1x","Speed2x"
            })
                _gearAnim.SetBool(p, playerAnimator.GetBool(p));

            _gearAnim.speed = playerAnimator.speed;
        }


        // this is called whenever the player fires a trigger
        void OnPlayerTrigger(string triggerName)
        {
            _gearAnim.SetTrigger(triggerName);
        }
    }
}