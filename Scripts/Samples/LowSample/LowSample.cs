using UnityEngine;
using Baku.Quma.Low;

/// <summary>Low APIの角度情報を用いる簡単な例を表したサンプルスクリプトです。</summary>
public class LowSample : MonoBehaviour
{
    private Qumarion _qumarion;

    /// <summary>ハードウェアを取得して初期化を行います。</summary>
    void Start()
    {
        bool hardwareExists = QumarionManager.CheckConnectionToHardware();
        if(!hardwareExists)
        {
            Debug.LogWarning("Failed to find Qumarion hardware device");
        }

        if(hardwareExists)
        {
            _qumarion = QumarionManager.GetDefaultDevice();
        }

    }

    /// <summary>
    /// 例として、QUMARIONの右肩関節にある3つの角度センサの値を
    /// アタッチされたオブジェクトの角度に適用します。
    /// 座標の整合性は特に取っていないので「おっ動いた!」程度の確認に使ってください。
    /// </summary>
    void Update()
    {
        if (_qumarion == null)
        {
            return;
        }

        _qumarion.Update();
        var angle = new UnityEngine.Vector3(
            _qumarion.Sensors[Sensors.R_Shoulder_MX1].Angle,
            _qumarion.Sensors[Sensors.R_Shoulder_MZ].Angle,
            _qumarion.Sensors[Sensors.R_Shoulder_MX2].Angle
            );

        transform.eulerAngles = angle;
    }
}
