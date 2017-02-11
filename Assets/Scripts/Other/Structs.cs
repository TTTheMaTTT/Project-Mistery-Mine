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