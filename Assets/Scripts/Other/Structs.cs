using UnityEngine;
using System.Collections;

/// <summary>
/// Трхмерный вектор,у которого появляются состояния "существует"/"не существует"
/// </summary>
public struct EVector3
{
    public float x, y, z;
    public bool exists;

    public EVector3(float _x, float _y, float _z, bool _exists)
    {
        x = _x;
        y = _y;
        z = _z;
        exists = _exists;
    }

    public EVector3(float _x, float _y, float _z)
    {
        x = _x;
        y = _y;
        z = _z;
        exists = true;
    }

    public EVector3(Vector3 _vect, bool _exists)
    {
        x = _vect.x;
        y = _vect.y;
        z = _vect.z;
        exists = _exists;
    }

    public EVector3(Vector3 _vect)
    {
        x = _vect.x;
        y = _vect.y;
        z = _vect.z;
        exists = false;
    }

    public static implicit operator Vector3(EVector3 e1)
    {
        return new Vector3(e1.x, e1.y, e1.z);
    }

    public static implicit operator Vector2(EVector3 e1)
    {
        return new Vector2(e1.x, e1.y);
    }

    public static EVector3 zero { get { return new EVector3(Vector3.zero); } }
    

}

/// <summary>
/// Специальная структура, используемая ИИ для хранения информации об их целях. Может закрепляться за игровым объектом и давать нужную информацию, если объект сменил своё местоположение
/// </summary>
public struct ETarget
{
    public float x, y;
    public Transform transform;
    public bool exists;

    public ETarget(float _x, float _y, bool _exists, Transform _transform)
    {
        x = _x;
        y = _y;
        exists = _exists;
        transform = _transform;
    }

    public ETarget(float _x, float _y)
    {
        x = _x;
        y = _y;
        exists = true;
        transform = null;
    }

    public ETarget(Vector2 _vect)
    {
        x = _vect.x;
        y = _vect.y;
        exists = true;
        transform = null;
    }

    public ETarget(Transform _transform)
    {
        x = 0; y = 0;
        transform = _transform;
        exists = true;
    }

    public static ETarget zero { get { return new ETarget(0f, 0f, false, null); } }

    public bool Exists { get { return exists; } set { exists = value; if (!exists) transform = null; } }

    public static implicit operator Vector2(ETarget e1)
    {
        return e1.transform==null? new Vector2(e1.x, e1.y): (Vector2)e1.transform.position;
    }

    public static implicit operator Vector3(ETarget e1)
    {
        return e1.transform == null ? new Vector3(e1.x, e1.y,0f) : e1.transform.position;
    }

    public static bool operator ==(ETarget e1, ETarget e2)
    {
        return (e1.exists && e2.exists)?((e1.transform==e2.transform && e1.transform!=null)? true : (e1.x == e2.x && e1.y == e2.y)):false;
    }

    public static bool operator !=(ETarget e1, ETarget e2)
    {
        return (!e1.exists || !e2.exists)?true:((e1.transform!=null || e2.transform!= null) && e1.transform!=e2.transform)?true:(e1.x!=e2.x || e1.y!=e2.y);
    }

}

/// <summary>
/// Структура, хранящая информацию о простой кривой Безье
/// </summary>
[System.Serializable]
public struct BezierSimpleCurve
{
    public Vector2 p0;
    public Vector2 p1;
    public Vector2 p2;
    public Vector2 p3;

    public bool draw;

    public BezierSimpleCurve(Vector2 _p0, Vector2 _p1, Vector2 _p2, Vector2 _p3)
    {
        p0 = _p0;
        p1 = _p1;
        p2 = _p2;
        p3 = _p3;
        draw = false;
    }

    public BezierSimpleCurve(BezierSimpleCurve _curve, Vector2 offset)
    {
        p0 = offset+_curve.p0;
        p1 = offset+_curve.p1;
        p2 = offset+_curve.p2;
        p3 = offset+_curve.p3;
        draw = _curve.draw;
    }

    public BezierSimpleCurve(BezierSimpleCurve _curve, Vector2 offset, float direction)
    {
        p0 = offset + new Vector2(_curve.p0.x * direction, _curve.p0.y);
        p1 = offset + new Vector2(_curve.p1.x * direction, _curve.p1.y);
        p2 = offset + new Vector2(_curve.p2.x * direction, _curve.p2.y);
        p3 = offset + new Vector2(_curve.p3.x * direction, _curve.p3.y);
        draw = _curve.draw;
    }

    public BezierSimpleCurve(BezierSimpleCurve _curve, bool _draw)
    {
        p0 = _curve.p0;
        p1 = _curve.p1;
        p2 = _curve.p2;
        p3 = _curve.p3;
        draw = _draw;
    }

    public BezierSimpleCurve(BezierSimpleCurve _curve, Vector2 offset, bool _draw)
    {
        p0 = offset + _curve.p0;
        p1 = offset + _curve.p1;
        p2 = offset + _curve.p2;
        p3 = offset + _curve.p3;
        draw = _draw;
    }

    public BezierSimpleCurve(BezierSimpleCurve _curve, Vector2 offset, float direction, bool _draw)
    {
        p0 = offset + new Vector2(_curve.p0.x * direction, _curve.p0.y);
        p1 = offset + new Vector2(_curve.p1.x * direction, _curve.p1.y);
        p2 = offset + new Vector2(_curve.p2.x * direction, _curve.p2.y);
        p3 = offset + new Vector2(_curve.p3.x * direction, _curve.p3.y);
        draw = _draw;
    }

    public BezierSimpleCurve(Vector2 _p0, Vector2 _p1, Vector2 _p2, Vector2 _p3, bool _draw)
    {
        p0 = _p0;
        p1 = _p1;
        p2 = _p2;
        p3 = _p3;
        draw = _draw;
    }

    public static BezierSimpleCurve zero { get { return new BezierSimpleCurve(Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero); } }

    // Вернуть точку данной кривой Безье, используя параметр t
    public Vector2 GetBezierPoint(float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);

        float t1 = 1 - t;
        return t1 * t1 * t1 * p0 + 3 * t * t1 * t1 * p1 +
               3 * t * t * t1 * p2 + t * t * t * p3;
    }

    public static bool operator ==(BezierSimpleCurve e1, BezierSimpleCurve e2)
    {
        return (e1.p0 == e2.p0)&&(e1.p1==e2.p1) && (e1.p2 == e2.p2) && (e1.p3 == e2.p3);
    }

    public static bool operator !=(BezierSimpleCurve e1, BezierSimpleCurve e2)
    {
        return (e1.p0 != e2.p0) || (e1.p1 != e2.p1) || (e1.p2 != e2.p2) || (e1.p3 != e2.p3);
    }

}