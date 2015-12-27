using UnityEngine;

using Baku.Quma.Pdk;

/// <summary>
/// QUMARIONから取得出来る各ボーンのローカルな並進、回転情報を用いてボーンの可視化を行います。
/// 基本的にTransformコンポーネントのみを持った空のオブジェクトにアタッチして用いてください。
/// </summary>
public class PdkBoneVisualizer : MonoBehaviour
{

    private StandardCharacterModel _model;

    private bool _useAccelFilter = false;
    //加速度センサでフィルタ処理を行う場合はtrueに設定。基本false推奨。
    public bool UseAccelFilter = false;

    private bool _useAccelerometer = false;
    //加速度センサを使う場合はtrueに設定。加速度センサを使うと体全体が傾いた状態も表現可能。
    public bool UseAccelerometer = true;
   
    //描画処理を担当するボーンのルート(ルート以下は木構造で保持)
    private BoneForPdkTreeVisualizer _rootBone;
    
    /// <summary>Qumarionの動作を適用するモデルの生成と、デバイスとの接続を行います。</summary>
    void Start ()
    {
        _model = PdkManager.CreateStandardModelPS();
        //NOTE: ルート以下については再帰的に生成する感じのアレ
        _rootBone = new BoneForPdkTreeVisualizer(_model.Root, null);
        _rootBone.BoneObject.transform.parent = transform;

        if (PdkManager.ConnectedDeviceCount == 0)
        {
            Debug.LogWarning("QUMARION was not found");
        }
        else
        {
            _model.AttachQumarion(PdkManager.GetDefaultQumarion());
            _model.AccelerometerRestrictMode = AccelerometerRestrictMode.None;
        }
    }
	
    /// <summary>デバイスの姿勢情報を更新し、適用先モデルにも更新が必要であることを通知します。</summary>
	void Update ()
    {
        if (_model.AttachedQumarion == null)
        {
            return;
        }

        UpdateAccelerometerSetting();

        _model.Update();
        _rootBone.Update();
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
}

