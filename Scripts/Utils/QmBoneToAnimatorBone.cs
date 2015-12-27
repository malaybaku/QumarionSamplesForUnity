using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Baku.Quma.Pdk;

namespace Baku.Quma.Unity
{
    /// <summary>QUMARION SDKのボーンをUnityの標準人型ボーンに対応づけます。</summary>
    public static class QmBoneToAnimatorBone
    {
        public static HumanBodyBones GetHumanBodyBone(StandardPSBones qmBone)
        {
            return _qm2human[qmBone];
        }


        //NOTE: とりあえず指については対応しない、というかQUMARION側のモデルが取得不能な指ボーンを持ってる理由は割と不明である。
        private static readonly Dictionary<StandardPSBones, HumanBodyBones> _qm2human = new Dictionary<StandardPSBones, HumanBodyBones>()
        {
            { StandardPSBones.Hips, HumanBodyBones.Hips },
            { StandardPSBones.Spine1, HumanBodyBones.Spine },
            { StandardPSBones.Spine2, HumanBodyBones.Chest },
            { StandardPSBones.Neck, HumanBodyBones.Neck },
            { StandardPSBones.Head, HumanBodyBones.Head },

            { StandardPSBones.LeftShoulder, HumanBodyBones.LeftShoulder },
            { StandardPSBones.LeftArm, HumanBodyBones.LeftUpperArm },
            { StandardPSBones.LeftForeArm, HumanBodyBones.LeftLowerArm },
            { StandardPSBones.LeftHand, HumanBodyBones.LeftHand },

            { StandardPSBones.RightShoulder, HumanBodyBones.RightShoulder },
            { StandardPSBones.RightArm, HumanBodyBones.RightUpperArm },
            { StandardPSBones.RightForeArm, HumanBodyBones.RightLowerArm },
            { StandardPSBones.RightHand, HumanBodyBones.RightHand },

            { StandardPSBones.LeftUpLeg, HumanBodyBones.LeftUpperLeg },
            { StandardPSBones.LeftLeg, HumanBodyBones.LeftLowerLeg },
            { StandardPSBones.LeftFoot, HumanBodyBones.LeftFoot },
            { StandardPSBones.LeftToeBase, HumanBodyBones.LeftToes },

            { StandardPSBones.RightUpLeg, HumanBodyBones.RightUpperLeg },
            { StandardPSBones.RightLeg, HumanBodyBones.RightLowerLeg },
            { StandardPSBones.RightFoot, HumanBodyBones.RightFoot },
            { StandardPSBones.RightToeBase, HumanBodyBones.RightToes }
        };
    }


}

