using UnityEngine;

using Baku.Quma.Pdk;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// QUMARIONのボーン情報をヒューマノイドに投影します。
/// </summary>
public class PdkToHumanoid : MonoBehaviour
{

    private StandardCharacterModel _model;

    //適用先ヒューマノイドにおける初期状態の回転値をキャッシュします。
    private Dictionary<HumanBodyBones, Quaternion> _initialRotations;

    /// <summary>適用先のヒューマノイドに対応するアニメーターです。</summary>
    public Animator animator;

    private bool _useAccelFilter = false;
    //加速度センサでフィルタ処理を行う場合はtrueに設定。基本false推奨。
    public bool UseAccelFilter = false;

    private bool _useAccelerometer = false;
    //加速度センサを使う場合はtrueに設定。加速度センサを使うと体全体が傾いた状態も表現可能。
    public bool UseAccelerometer = false;

    //NOTE: ルートボーン以下は木構造状に保持されている
    private QumaBone2Humanoid _rootBone;

    /// <summary>Qumarionの動作を適用するモデルの生成と、デバイスとの接続を行います。</summary>
    void Start()
    {
        _model = PdkManager.CreateStandardModelPS();

        //NOTE: QmBoneOnUnityのコンストラクタが再帰的に子要素のインスタンスを生成
        _rootBone = new QumaBone2Humanoid(_model.Root, StandardPSBones.Hips, null);

        if (PdkManager.ConnectedDeviceCount == 0)
        {
            Debug.LogWarning("QUMARION was not found");
        }
        else
        {
            _model.AttachQumarion(PdkManager.GetDefaultQumarion());
            _model.AccelerometerRestrictMode = AccelerometerRestrictMode.None;
        }

        if(animator == null)
        {
            animator = GetComponent<Animator>();
        }

        //キャラをTポーズにするために必要な回転の情報をキャッシュします。
        _initialRotations = _targetBones.ToDictionary(
            b => b,
            b => animator.GetBoneTransform(b).localRotation
            );

        InitializePseudAxis();
    }

    /// <summary>デバイスの姿勢情報を更新し、ヒューマノイドの姿勢に適用します。</summary>
    void Update()
    {
        if (_model.AttachedQumarion == null || animator == null)
        {
            return;
        }

        UpdateAccelerometerSetting();

        _model.Update();
        _rootBone.Update();

        ApplyQumaLocalRotations();
    }

    //加速度センサの設定更新を行います。
    private void UpdateAccelerometerSetting()
    {
        if (UseAccelFilter != _useAccelFilter)
        {
            _useAccelFilter = UseAccelFilter;
            _model.AccelerometerMode = UseAccelFilter ?
                AccelerometerMode.Relative :
                AccelerometerMode.Direct;
            Debug.Log(string.Format("Accelerometer mode was set to {0}", _model.AccelerometerMode));
        }
        if (UseAccelerometer != _useAccelerometer)
        {
            _useAccelerometer = UseAccelerometer;
            _model.AttachedQumarion.EnableAccelerometer = UseAccelerometer;
            Debug.Log(string.Format("Accelerometer Enable state was changed to {0}", UseAccelerometer));
        }
    }

    //Qumaの各ボーンのローカル回転をヒューマノイドへ適用します。
    private void ApplyQumaLocalRotations()
    {
        var childBones = _rootBone.ChildBones;
        foreach(var target in _targetBones)
        {
            //対応するQuma側のボーンを取得
            var sourceBone = childBones.FirstOrDefault(
                bone => bone.IsValidHumanBodyBone && bone.HumanBodyBone == target
                );

            if(sourceBone == null)
            {
                continue;
            }


            //仮想軸 -> ローカル軸への変換をやって正しい回転を取得
            Quaternion localRotation = GetRotation(sourceBone.InitialToCurrentOnUnityAxis, target);

            //Tポーズへの初期回転 + そこからの回転、という形で設定
            var targetTransform = animator.GetBoneTransform(target);
            targetTransform.localRotation = _initialRotations[target] * localRotation;
        }
    }


    private readonly HumanBodyBones[] _targetBones = new HumanBodyBones[]
    {
        #region Qumarionから転写するボーンの一覧
        HumanBodyBones.Hips,
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.Neck,
        HumanBodyBones.Head,
        HumanBodyBones.LeftShoulder,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.LeftHand,
        HumanBodyBones.RightShoulder,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.RightHand,
        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.LeftFoot,
        HumanBodyBones.LeftToes,
        HumanBodyBones.RightUpperLeg,
        HumanBodyBones.RightLowerLeg,
        HumanBodyBones.RightFoot,
        HumanBodyBones.RightToes
        #endregion
    };

    //追加: Perception Neuronでも使った座標変換の小技
    //仮想的な「Unity XYZ軸の向きを向いたボーンの座標軸」のキャッシュ
    private Dictionary<HumanBodyBones, Vector3> pseudXaxis;
    private Dictionary<HumanBodyBones, Vector3> pseudYaxis;
    private Dictionary<HumanBodyBones, Vector3> pseudZaxis;

    //キャラのヒューマノイドボーンに仮想的な「UnityのワールドXYZ軸に沿った軸」を割り当てます。
    private void InitializePseudAxis()
    {
        //このrootTの軸がUnityのXYZ軸に一致してればOK(ダメな場合は頑張って工夫してください)
        //またキャラがTポーズ取ってることも必要。これについてはPrefabから出した後でTポーズ取らせれば(たぶん)OK
        var rootT = animator.GetBoneTransform(HumanBodyBones.Hips).root;

        pseudXaxis = _targetBones.ToDictionary(
            b => b,
            b =>
            {
                var t = animator.GetBoneTransform(b);
                return new Vector3(
                    Vector3.Dot(t.right, rootT.right),
                    Vector3.Dot(t.up, rootT.right),
                    Vector3.Dot(t.forward, rootT.right)
                    );
            });

        pseudYaxis = _targetBones.ToDictionary(
            b => b,
            b =>
            {
                var t = animator.GetBoneTransform(b);
                return new Vector3(
                    Vector3.Dot(t.right, rootT.up),
                    Vector3.Dot(t.up, rootT.up),
                    Vector3.Dot(t.forward, rootT.up)
                    );
            });

        pseudZaxis = _targetBones.ToDictionary(
            b => b,
            b =>
            {
                var t = animator.GetBoneTransform(b);
                return new Vector3(
                    Vector3.Dot(t.right, rootT.forward),
                    Vector3.Dot(t.up, rootT.forward),
                    Vector3.Dot(t.forward, rootT.forward)
                    );
            });

    }

    /// <summary>「UnityのワールドXYZ軸に沿った軸で表したTポーズからの回転」をローカル用の回転に直します。</summary>
    /// <param name="q">UnityのワールドXYZ軸に沿った軸で表したTポーズからの回転</param>
    /// <returns>ローカル座標用に修正された回転</returns>
    private Quaternion GetRotation(Quaternion q, HumanBodyBones bone)
    {
        //ボーンがWorldのXYZに沿ってる場合の回転を表すパラメタをまず拾う
        float angle;
        Vector3 axis;
        q.ToAngleAxis(out angle, out axis);

        //回転軸をローカル座標上の値に直す: Matrix使っても書けそうだけど原始的に。
        Vector3 axisInLocalCoordinate =
            axis.x * pseudXaxis[bone] +
            axis.y * pseudYaxis[bone] +
            axis.z * pseudZaxis[bone];

        return Quaternion.AngleAxis(angle, axisInLocalCoordinate);
    }

}

