using UnityEngine;
using System.Collections;

/// <summary>
/// Из всех объектов, в которых расположен этот компонент, будет извлечён коллайдер и сам этот компонент. Такие коллайдеры нужны для работы редактора уровней, но они не нужны для самой игры
/// </summary>
public class EditorCollider : MonoBehaviour {

	void Start ()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            Destroy(cols[i]);
        }
        Destroy(this);

	}
	
}
