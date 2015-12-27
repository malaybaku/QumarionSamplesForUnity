using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Baku.Quma.Pdk;
using Baku.Quma.Unity;

/// <summary>
/// (参考程度に同梱)
/// QUMARIONから取得出来る各ボーンの終端座標を用いてボーンの可視化を行います。
/// このスクリプトは<see cref="PdkBoneVisualizer"/>より実装がシンプルなため同梱していますが
/// ボーンの階層構造を使っていないためあまり参考にならない可能性が高いです。
/// 
/// 基本的に、Transformコンポーネントのみを持った空のオブジェクトにアタッチして用いてください。
/// </summary>
public class PdkPositionBasedBoneVisualizer : MonoBehaviour
{
    private StandardCharacterModel _model;

    //ジョイントの位置を示す球の集まり
    private Dictionary<StandardPSBones, GameObject> _spheres;

    //ジョイントの繋がりを示す線の集まり
    private DrawableBone[] _drawableBones;

    private bool _useAccelFilter = false;
    //加速度センサでフィルタ処理を行う場合はtrueに設定。基本falseで良さそう。
    public bool UseAccelFilter = false;

    private bool _useAccelerometer = false;
    //加速度センサを使う場合はtrueに設定。加速度センサを使うと体全体が傾いた状態も表現可能。
    public bool UseAccelerometer = true;

    //描画ターゲットの初期化とデバイスへの接続を行います。
    void Start()
    {
        _model = PdkManager.CreateStandardModelPS();

        //ボーンを一つ残らず初期配置
        //NOTE: ここではボーンの階層構造を作らずすべてRoot直下に置いてる事に注意！
        _spheres = _model.Bones
            .ToDictionary(
            bone => bone.Key,
            bone =>
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = bone.Value.Name;
                s.transform.parent = this.transform;
                s.transform.localScale = new Vector3(.05f, .05f, .05f);

                var t = bone.Value.InitialWorldMatrix.Translate;
            //NOTE: QUMARIONのボーンはcm単位らしいので0.01倍に縮める
            s.transform.localPosition = 0.01f * new Vector3(-t.X, t.Y, t.Z);
                return s;
            });

        //ボーン間の線を書くためのオブジェクトを用意
        _drawableBones = _model.Bones
            .Where(kvp => kvp.Key != StandardPSBones.Hips)
            .Select(kvp =>
            {
                var childSphere = _spheres[kvp.Key];
                var parentSphere = _spheres[StandardPSBonesUtil.GetStandardPSBone(_model.Bones[kvp.Key].Parent.Name)];

                return new DrawableBone(parentSphere, childSphere);
            })
            .ToArray();

        if (PdkManager.ConnectedDeviceCount == 0)
        {
            Debug.LogWarning("QUMARION was not found");
        }
        else
        {
            _model.AttachQumarion(PdkManager.GetDefaultQumarion());
            _model.AttachedQumarion.EnableAccelerometer = false;
            _model.AccelerometerMode = AccelerometerMode.Direct;
            _model.AccelerometerRestrictMode = AccelerometerRestrictMode.None;
        }
    }

    //姿勢情報とそれに応じた描画内容の更新を行います。
    void Update()
    {
        if (_model.AttachedQumarion == null)
        {
            return;
        }

        UpdateAccelerometerSetting();

        //主要な処理部分: 本体情報を更新し、結果のうちWorld位置情報を特に用いて各ボーンの位置を指定
        _model.Update();
        foreach (var kvp in _spheres)
        {
            var boneType = kvp.Key;
            var sphere = kvp.Value;
            var t = _model.Bones[boneType].WorldMatrix.Translate;
            sphere.transform.localPosition = 0.01f * new Vector3(-t.X, t.Y, t.Z);

            var r = MatrixToQuaternion.CreateFrom(_model.Bones[boneType].WorldMatrix);
            sphere.transform.localRotation = r;
        }

        foreach (var bone in _drawableBones)
        {
            bone.Update();
        }

    }

    //加速度センサの設定を確認
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

}

/// <summary>親子要素どうしを繋ぐ線を表します。</summary>
public class DrawableBone
{
    public DrawableBone(GameObject parent, GameObject child)
    {
        _parent = parent;
        _child = child;

        _renderer = _child.AddComponent<LineRenderer>();
        if (_renderer == null)
        {
            Debug.LogWarning("Failed to add renderer. Is there already existing LineRenderer??");
        }
        _renderer.SetWidth(0.05f, 0.0f);
        Update();
    }

    private readonly GameObject _parent;
    private readonly GameObject _child;
    private readonly LineRenderer _renderer;

    public void Update()
    {
        _renderer.SetPosition(0, _parent.transform.position);
        _renderer.SetPosition(1, _child.transform.position);
    }
}



