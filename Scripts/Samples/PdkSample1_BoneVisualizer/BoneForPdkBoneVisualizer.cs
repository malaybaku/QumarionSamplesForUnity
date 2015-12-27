using System.Linq;

using UnityEngine;
using Baku.Quma.Pdk;
using Baku.Quma.Unity;

/// <summary>
/// <see cref="PdkBoneVisualizer"/>で使うためにラップされたQumarionのボーンです。
/// </summary>
public class BoneForPdkTreeVisualizer
{
    public BoneForPdkTreeVisualizer(Bone bone, BoneForPdkTreeVisualizer parent)
    {
        _bone = bone;
        _parent = parent;

        //Transformを正しく保持するために用いる実際の骨オブジェクト
        BoneObject = new GameObject();
        BoneObject.name = _bone.Name;

        //見た目としてBoneObjectの位置を表す球
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "renderSphere";
        sphere.transform.parent = BoneObject.transform;
        sphere.transform.localScale = new Vector3(.05f, .05f, .05f);
        sphere.transform.localPosition = Vector3.zero;

        if (_parent != null)
        {
            BoneObject.transform.parent = _parent.BoneObject.transform;
            _lineRenderer = BoneObject.AddComponent<LineRenderer>();
            _lineRenderer.SetWidth(0.05f, 0.0f);
            _lineRenderer.SetColors(Color.blue, Color.green);
        }

        //再帰的な子ボーンの初期化。
        _childs = bone
            .Childs
            .Select(b => new BoneForPdkTreeVisualizer(b, this))
            .ToArray();
    }

    //対応するQumarionのボーン
    private readonly Bone _bone;

    //親要素。ルート要素ではnull
    private readonly BoneForPdkTreeVisualizer _parent;

    //子要素一覧。末端部では要素数0の配列
    private readonly BoneForPdkTreeVisualizer[] _childs;

    //親子間をつなぐ線
    private readonly LineRenderer _lineRenderer;

    /// <summary>ボーンの位置を表すオブジェクトを取得します。</summary>
    public GameObject BoneObject { get; private set; }

    /// <summary>ボーンの姿勢を更新します。</summary>
    public void Update()
    {
        //子要素の更新
        foreach (var child in _childs)
        {
            child.Update();
        }

        //姿勢の更新
        var lmat = _bone.LocalMatrix;

        //lmatは[左-上-前」という右手系座標で定義されてんので「右-上-前」系に修正してから用いる
        //1. 成分表記が逆なので反転。
        lmat[0, 0] *= -1f;
        lmat[1, 0] *= -1f;
        lmat[2, 0] *= -1f;
        //2.x軸の向きそのものが逆なので反転。結果としてlmat[0, 0]は数値的には元の値といっしょ。
        lmat[0, 0] *= -1f;
        lmat[0, 1] *= -1f;
        lmat[0, 2] *= -1f;

        var q = MatrixToQuaternion.CreateFrom(lmat);
        Vector3 axis;
        float angle;
        q.ToAngleAxis(out angle, out axis);

        //NOTE: ここ、あまり理解してないのだけど
        //      右手系と左手系ではデフォルト回転正向きが逆なので補正が要るらしい？
        BoneObject.transform.localRotation = Quaternion.AngleAxis(-angle, axis);


        var translate = _bone.InitialLocalMatrix.Translate;
        //NOTE: Qumarionでは長さがcm単位らしいのでm単位に修正
        BoneObject.transform.localPosition = new Vector3(-translate.X * 0.01f, translate.Y * 0.01f, translate.Z * 0.01f);

        //線の描画内容を更新
        if (_lineRenderer != null && _parent != null)
        {
            _lineRenderer.SetPosition(0, _parent.BoneObject.transform.position);
            _lineRenderer.SetPosition(1, BoneObject.transform.position);
        }

    }

}
