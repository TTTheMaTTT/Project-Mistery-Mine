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
