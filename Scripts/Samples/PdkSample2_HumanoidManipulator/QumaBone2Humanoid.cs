using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Baku.Quma.Pdk;
using Baku.Quma.Unity;

/// <summary>
/// <see cref="PdkToHumanoid"/>で使うためにラップされたQumarionのボーンです。
/// </summary>
public class QumaBone2Humanoid
{
    //インスタンスに対応するQumarionのボーン
    private readonly Bone _bone;

    //親のボーン(ルート要素ではnull)
    private readonly QumaBone2Humanoid _parent;

    //子要素一覧(末端部では要素数0の配列)
    private readonly QumaBone2Humanoid[] _childs;

    //疑似的な座標軸として「UnityのXYZ軸に平行なローカル座標軸」を仮想的に割り当てるための3つの軸
    private readonly Vector3 xAxis;
    private readonly Vector3 yAxis;
    private readonly Vector3 zAxis;
    //上記メンバに関する補足: 仮想的な座標を挟む小技は以前モーションキャプチャ系を使ったときの流用。
    //参考: http://www.baku-dreameater.net/archives/5741
    //本クラスでは上記記事に書いてる変換の逆変換に相当する処理を行う

    /// <summary>Qumarionで割り当てられているボーンの種類を取得します。</summary>
    public StandardPSBones QumaBoneType { get; private set; }

    /// <summary><see cref="QumaBoneType"/>に対応したヒューマノイド上のボーンの種類を取得します。</summary>
    public HumanBodyBones HumanBodyBone { get; private set; }

    /// <summary><see cref="HumanBodyBone"/>が適切に割り当てられているかどうかを取得します。</summary>
    public bool IsValidHumanBodyBone { get; private set; }

    /// <summary>このボーンを含む末端の子ボーンまでを列挙したものを再帰的に取得します。</summary>
    public IEnumerable<QumaBone2Humanoid> ChildBones
    {
        get
        {
            return _childs.SelectMany(child => child.ChildBones).Concat(new QumaBone2Humanoid[] { this });
        }
    }


    /// <summary>初期状態(Tポーズ)での回転を取得します。</summary>
    public Quaternion InitialRotation { get; private set; }

    /// <summary>現在の回転を取得します。</summary>    
    public Quaternion LocalRotation { get; private set; }

    /// <summary><see cref="InitialRotation"/>から<see cref="LocalRotation"/>への移動を表す回転を取得します。</summary>
    public Quaternion InitialToCurrent { get; private set; }

    /// <summary><see cref="InitialToCurrent"/>について回転軸を修正したものを取得します。</summary>
    public Quaternion InitialToCurrentOnUnityAxis { get; private set; }


    /// <summary>Qumarion側のボーン情報と親ボーンを指定してインスタンスを初期化します。</summary>
    /// <param name="bone">Qumarion側のボーン</param>
    /// <param name="boneType">Qumarion側のボーンが標準ボーンのどれに該当するか</param>
    /// <param name="parent">親ボーン(ルートのボーンを生成する場合nullを指定)</param>
    public QumaBone2Humanoid(Bone bone, StandardPSBones boneType, QumaBone2Humanoid parent)
    {
        _bone = bone;
        _parent = parent;

        QumaBoneType = boneType;
        //StandardPSBones -> HumanBodyBoneの対応付けがあれば登録し、無い場合は無いことを確認。
        try
        {
            HumanBodyBone = QmBoneToAnimatorBone.GetHumanBodyBone(boneType);
            IsValidHumanBodyBone = true;
        }
        catch (KeyNotFoundException)
        {
            HumanBodyBone = HumanBodyBones.Hips;
            IsValidHumanBodyBone = false;
        }

        InitialRotation = MatrixToQuaternionWithCoordinateModify(_bone.InitialLocalMatrix);

        //ゼロ回転状態での固定座標軸を参照するため親のワールド座標を確認: ルート(Hips)はワールド座標直下。
        Matrix4f initMat = (_bone.Parent != null) ?
            _bone.Parent.InitialWorldMatrix :
            Matrix4f.Unit;


        //疑似座標系の初期化: Qumarion側の行列は右手系(左-上-前)なので正負補正が必要な事に注意。
        //NOTE: Qumarionの場合ルートが原点、回転は無しとみなすと次のように簡単化される
        xAxis = new Vector3(initMat.M11, -initMat.M21, -initMat.M31);
        yAxis = new Vector3(-initMat.M12, initMat.M22, initMat.M32);
        zAxis = new Vector3(-initMat.M13, initMat.M23, initMat.M33);

        //再帰的に子ボーンを初期化。
        _childs = bone
            .Childs
            .Select(b => new QumaBone2Humanoid(b, StandardPSBonesUtil.GetStandardPSBone(b.Name), this))
            .ToArray();
    }

    /// <summary>ボーンの姿勢を更新します。</summary>
    public void Update()
    {
        //子要素の更新
        foreach (var child in _childs)
        {
            child.Update();
        }

        //このボーン自体の姿勢更新
        var lmat = _bone.LocalMatrix;

        //ゼロ回転からではなくTポーズからの変化を知りたいので差分を求める
        var initialToCurrent = MatrixRotationDif.CreateDifFrom(lmat, _bone.InitialLocalMatrix);

        //右手/左手系の差を吸収しながらクォータニオン表現に修正
        InitialToCurrent = MatrixToQuaternionWithCoordinateModify(initialToCurrent);

        //回転軸を取得し、疑似座標系に投影してヒューマノイドから拾いやすいよう修正
        Vector3 axis;
        float angle;
        InitialToCurrent.ToAngleAxis(out angle, out axis);

        Vector3 axisOnUnity =
            axis.x * xAxis +
            axis.y * yAxis +
            axis.z * zAxis;

        InitialToCurrentOnUnityAxis = Quaternion.AngleAxis(angle, axisOnUnity);

        LocalRotation = MatrixToQuaternionWithCoordinateModify(lmat);
    }

    //Qumarionから得た姿勢行列(左-上-前の右手系)をUnityで利用可能な左手系回転表現に直す
    private Quaternion MatrixToQuaternionWithCoordinateModify(Matrix4f lmat)
    {
        //lmatは[左-上-前」という右手系座標で定義されてんので「右-上-前」系に修正してから用いる
        //1. 成分表記が逆なので反転。
        //lmat[0, 0] *= -1f;
        lmat[1, 0] *= -1f;
        lmat[2, 0] *= -1f;
        //2.x軸の向きそのものが逆なので反転。結果としてlmat[0, 0]は数値的には元の値といっしょ。
        //lmat[0, 0] *= -1f;
        lmat[0, 1] *= -1f;
        lmat[0, 2] *= -1f;

        var q = MatrixToQuaternion.CreateFrom(lmat);
        Vector3 axis;
        float angle;
        q.ToAngleAxis(out angle, out axis);

        //NOTE: ここ、あまり理解してないのだけど
        //      右手系と左手系ではデフォルト回転正向きが逆なので補正が要るということ？
        return Quaternion.AngleAxis(-angle, axis);
    }


}
