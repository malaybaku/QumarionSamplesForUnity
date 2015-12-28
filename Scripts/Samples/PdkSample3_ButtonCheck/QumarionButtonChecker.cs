using UnityEngine;
//ポイント1: Baku.Quma.Low名前空間にもQumarionクラスがあるけど使わないように！
using Baku.Quma.Pdk;

public class QumarionButtonChecker : MonoBehaviour
{
    //ボタンはデバイスの情報であるのでモデルを介さず状態が取得できる
    private Qumarion _qumarion;

	void Start ()
    {
        //PCに接続中のQUMARIONがあるかどうかチェックし、存在する場合は接続
	    if(PdkManager.ConnectedDeviceCount > 0)
        {
            _qumarion = PdkManager.GetDefaultQumarion();
        }
        else
        {
            Debug.LogWarning("Qumarion was not found");
        }
	}
	
	void Update ()
    {
	    if(_qumarion == null)
        {
            return;
        }

        //ButtonStateプロパティをチェックすると現在の状態が取得可能
        //レスポンスがそこまで速くないのでゆっくり押すような用途で使うこと！
        bool isButtonDown = (_qumarion.ButtonState == QumaButtonState.Down);
        Debug.Log(string.Format("ButtonState is Down ? : {0}", isButtonDown));
	}
}
