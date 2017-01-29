using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using pseudoSinCos;//библиотека, что способна быстро расчитывать углы, без использования тяжёлых синусов и косинусов
#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

/// <summary>
/// Класс, содержащий информацию о вершинах препятствий, которые свет не может преодолеть
/// </summary>
public class verts
{
	public float angle {get;set;}
	public int location {get;set;} // 1 - левый конец, 0 - серединный конец, -1 - правый конец
	public Vector3 pos {get;set;}
	public bool endpoint { get; set;}

}

/// <summary>
/// Источник света, способный отбрасывать 2D-тени в реальном времени
/// </summary>
public class DynamicLight : MonoBehaviour
{

    #region consts

    protected const float magnitudeRange = 0.15f;

    #endregion //consts

    #region fields

    public Material lightMaterial;//Материал, используемый для отрисовки света
    public Texture lightTexture;

	[HideInInspector]public Collider2D[] allMeshes;// Массив всех твердых объектов, мешающих распространению света

	[HideInInspector] public List<verts> allVertices = new List<verts>();//Массив всех вершин в мешах из allMeshes

	[SerializeField]
	public float lightRadius = 20f;

	[Range(4,100)]
	public int lightSegments = 8;//Чем это число больше, тем более круглый источник света
		
	Mesh lightMesh;//Создаваемый меш света

    GameObject cam;

    #endregion //fields

    #region parametres

    public LayerMask layer;//Слои препятствий

    public bool staticLight=false;//Является ли этот свет статичным? Если да, то меш строится в самом начале игры
    bool staticLight1;
    [SerializeField]float reloadTime = 0.1f;
    int renderStage = 0;

    #endregion //parametres

    void Start () {

        staticLight1 = false;
        cam = GameObject.FindGameObjectWithTag("MainCamera");
	//	PseudoSinCos.initPseudoSinCos();
		
		//Шаг 1 - Первоначальная настройка меша света

		MeshFilter meshFilter = (MeshFilter)gameObject.AddComponent(typeof(MeshFilter));//Добавить меш фильтр
		MeshRenderer renderer = gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;// Добавить меш рендерер свету
		renderer.sharedMaterial = lightMaterial;// Добавить материал света
        if (lightTexture != null)
        {
            renderer.sharedMaterial.SetTexture("_MainTex", lightTexture);
        }
		lightMesh = new Mesh();// Создать меш, который будет формировать свет
		meshFilter.mesh = lightMesh;
		lightMesh.name = "Light Mesh";
		lightMesh.MarkDynamic ();

        renderStage = 0;

	}
	
	void LateUpdate()
    {
        if (!staticLight1 && ((Vector2)transform.position - (Vector2)cam.transform.position).sqrMagnitude < lightRadius * lightRadius / 4f && 
            (renderStage == 1 || reloadTime==0))
        {
            RenderLightMesh();
            ResetBounds();
            renderStage++;
            StartCoroutine(ReloadLight());
        }
	}

	void FixedUpdate()
	{
        if (!staticLight1 && ((Vector2)transform.position - (Vector2)cam.transform.position).sqrMagnitude < lightRadius * lightRadius/4f && 
            (renderStage==0 || reloadTime==0))
        {
            GetAllMeshes();
            SetLight();
            renderStage++;
        }
	}

    /// <summary>
    /// Фукция, что возвращает все меши препятствий, на которые падает свет
    /// </summary>
    void GetAllMeshes()
    {
		//allMeshes = FindObjectsOfType(typeof(PolygonCollider2D)) as PolygonCollider2D[];


		Collider2D [] allColl2D = Physics2D.OverlapCircleAll(transform.position, lightRadius/4f, layer);
		allMeshes = new Collider2D[allColl2D.Length];

		for (int i=0; i<allColl2D.Length; i++)
        {
			allMeshes[i] = allColl2D[i];
		}

	}

    /// <summary>
    /// Настроить границы меша света
    /// </summary>
	void ResetBounds()
    {
		Bounds b = lightMesh.bounds;
		b.center = Vector3.zero;
		lightMesh.bounds = b;
        if (staticLight)
            staticLight1 = true;
	}

    /// <summary>
    /// Найти точки, что составляют меша света
    /// </summary>
	void SetLight ()
    {

		bool sortAngles = false;

		allVertices.Clear();

		//layer = 1 << 8;

		//Шаг 2: Обработать все вершины мешей препятствий
	
		bool quad4 = false;
		bool quad3 = false;
		float magRange = magnitudeRange;

		List <verts> tempVerts = new List<verts>();

		for (int m = 0; m < allMeshes.Length; m++)
        {
			tempVerts.Clear();
			Collider2D mf = allMeshes[m];

            //Булевы переменные, которые указывают на принадлежность меша к квадрантам
			quad4 = false;//Есть ли у меша точки, принадлежащие 4-ому квадранту (x>0, y<0)
			quad3 = false;//Есть ли у меша точки, принадлежащие 3-ему квадранту (x<0, y<0)

            if (((1 << mf.transform.gameObject.layer) & layer) != 0)
            {
                int pointCount = 0;
                Vector2[] points = null;
                if (mf is PolygonCollider2D)
                {
                    PolygonCollider2D pCol = (PolygonCollider2D)mf;
                    pointCount = pCol.GetTotalPointCount();
                    points = pCol.points;
                }
                else if (mf is BoxCollider2D)
                {
                    pointCount = 4;
                    points = GetBoxColPoints((BoxCollider2D)mf);
                }
                else
                    continue;
				for (int i = 0; i < pointCount; i++)
                {
					
					verts v = new verts();
					// Перейти к мировым координатам
					Vector3 worldPoint = mf.transform.TransformPoint(points[i]);

					RaycastHit2D ray = Physics2D.Raycast(transform.position, worldPoint - transform.position, (worldPoint - transform.position).magnitude, layer);
			
					if(ray)
                    {
						v.pos = ray.point;
						if( worldPoint.sqrMagnitude >= (ray.point.sqrMagnitude - magRange) && worldPoint.sqrMagnitude <= (ray.point.sqrMagnitude + magRange) )
							v.endpoint = true;
					}
                    else
                    {
						v.pos =  worldPoint;
						v.endpoint = true;
					}
					
					Debug.DrawLine(transform.position, v.pos, Color.white);	
					
					//Перейти к локальной системе координат
					v.pos = transform.InverseTransformPoint(v.pos); 
					//Расчитать углы
					v.angle = GetVectorAngle(true,v.pos.x, v.pos.y);
					
					if(v.angle < 0f )
						quad4 = true;
					if(v.angle > 2f)
						quad3 = true;
					
					
					//Добавить обработанные вершины в общий список
					if((v.pos).sqrMagnitude <= lightRadius*lightRadius)
                    {
						tempVerts.Add(v);	
					}
					
					if(sortAngles == false)
						sortAngles = true;
					
				}
  
			}

			// Установить типы конечных точек
			if(tempVerts.Count > 0){

                tempVerts.Sort((item1, item2) => (item2.angle.CompareTo(item1.angle)));

                int posLowAngle = 0; // save the indice of left ray
				int posHighAngle = 0; // same last in right side

				//Debug.Log(lows + " " + his);

				if(quad3 && quad4)
                {
					float lowestAngle = -1f;
					float highestAngle = tempVerts[0].angle;

					for(int i=0; i<tempVerts.Count; i++){

						if(tempVerts[i].angle < 1f && tempVerts[i].angle > lowestAngle)
                        {
							lowestAngle = tempVerts[i].angle;
							posLowAngle = i;
						}

						if(tempVerts[i].angle > 2f && tempVerts[i].angle < highestAngle){
							highestAngle = tempVerts[i].angle;
							posHighAngle = i;
						}
					}
				}
                else
                {
					posLowAngle = 0; 
					posHighAngle = tempVerts.Count-1;
				}

				tempVerts[posLowAngle].location = 1; // правая точка
				tempVerts[posHighAngle].location = -1; // левая точка

				//Запомнить отсортированные точки для главного меша света
				allVertices.AddRange(tempVerts); 
				//allVertices.Add(tempVerts[0]);
				//allVertices.Add(tempVerts[tempVerts.Count - 1]);
                
				for(int r = 0; r<2; r++)
                {
                    //найти крайнюю точку меша тени, созданную мешем препятствия под светом источника

					Vector3 fromCast = transform.TransformPoint(tempVerts[r==0? posLowAngle: posHighAngle].pos);
					bool isEndpoint = tempVerts[r == 0 ? posLowAngle : posHighAngle].endpoint;

                    if (isEndpoint)
                    {
						Vector2 from = (Vector2)fromCast;
						Vector2 dir = from - (Vector2)transform.position;
                        
						float mag = lightRadius;
						const float checkPointLastRayOffset= 0.005f; 
						
						from += (dir * checkPointLastRayOffset);						

						RaycastHit2D rayCont = Physics2D.Raycast(from, dir, mag, layer);
						Vector3 hitp;
						if(rayCont)
                        {
							hitp = rayCont.point;
						}
                        else
                        {
                            Vector2 newDir = transform.InverseTransformDirection(dir);
							hitp = (Vector2)transform.TransformPoint( newDir.normalized * mag);
						}

						if(((Vector2)hitp - (Vector2)transform.position ).sqrMagnitude > lightRadius * lightRadius)
                        {
							dir = (Vector2)transform.InverseTransformDirection(dir);	//local p
							hitp = (Vector2)transform.TransformPoint( dir.normalized * mag);
						}

						Debug.DrawLine(fromCast, hitp, Color.green);	

						verts vL = new verts();
						vL.pos = transform.InverseTransformPoint(hitp);

						vL.angle = GetVectorAngle(true,vL.pos.x, vL.pos.y);
						allVertices.Add(vL);
					}
				}
			}	
		}

		//Шаг 3: Добавить вектора, что обрамляют меш света
        
		int theta = 0;
		//float amount = (Mathf.PI * 2) / lightSegments;
		int amount = 360 / lightSegments;

		for (int i = 0; i < lightSegments; i++)
        {

			theta =amount * (i);
			if(theta == 360)
                theta = 0;

			verts v = new verts();
			v.pos = new Vector3((Mathf.Sin(theta)), (Mathf.Cos(theta)), 0); // реализация в радианах (медленно, но точно)
			//v.pos = new Vector3((PseudoSinCos.SinArray[theta]), (PseudoSinCos.CosArray[theta]), 0); // реализация в градусах (быстро, но с погрешностями)

			v.angle = GetVectorAngle(true,v.pos.x, v.pos.y);
			v.pos *= lightRadius;
			v.pos += transform.position;

			RaycastHit2D ray = Physics2D.Raycast(transform.position,v.pos - transform.position,lightRadius, layer);

			if (!ray)
            {
				v.pos = transform.InverseTransformPoint(v.pos);
				allVertices.Add(v);
			}
		}

		//Шаг 4: отсортировать массив вершин по углам
		if (sortAngles)
        {
			allVertices.Sort((item1, item2) => (item2.angle.CompareTo(item1.angle)));
        }


        //Дополнительный шаг изменить порядок вершин с учетом тех из них, что имеют одно и то же направление от источника света
		float rangeAngleComparision = 0.00001f;
		for(int i = 0; i< allVertices.Count-1; i++)
        {	
			verts point1 = allVertices[i];
			verts point2 = allVertices[i +1];

			if(Mathf.Abs(point1.angle - point2.angle)<=rangeAngleComparision)
            {	
				if(point2.location == -1)// Крайняя правая точка от меша тени препятствия
                {
					if(point1.pos.sqrMagnitude > point2.pos.sqrMagnitude)
                    {
						allVertices[i] = point2;
						allVertices[i+1] = point1;
					}
				}
				
                //Бесполезен ли этот блок кода? может и нет)
				if(point1.location == 1)// Крайняя левая точка от меша тени препятствия
                {
					if(point1.pos.sqrMagnitude < point2.pos.sqrMagnitude)
                    {	
						allVertices[i] = point2;
						allVertices[i+1] = point1;
					}
				}
			}
		}
	}

    /// <summary>
    /// Сформировать и отрендерить меш света
    /// </summary>
	void RenderLightMesh()
    {
		//Шаг 5: Заполнить меш найденными вершинами
        		
		//interface_touch.vertexCount = allVertices.Count; // Передать количество точек меша
		
		Vector3 []initVerticesMeshLight = new Vector3[allVertices.Count+1];
		
		initVerticesMeshLight [0] = Vector3.zero;
		
		for (int i = 0; i < allVertices.Count; i++)
        { 
			//Debug.Log(allVertices[i].angle);
			initVerticesMeshLight [i+1] = allVertices[i].pos;
			
			//if(allVertices[i].endpoint == true)
			//Debug.Log(allVertices[i].angle);	
		}
		
		lightMesh.Clear ();
		lightMesh.vertices = initVerticesMeshLight;//Заполним вершины меша
		
		Vector2 [] uvs = new Vector2[initVerticesMeshLight.Length];
		for (int i = 0; i < initVerticesMeshLight.Length; i++)
        {
			uvs[i] = new Vector2(initVerticesMeshLight[i].x, initVerticesMeshLight[i].y);		
		}
		lightMesh.uv = uvs;//Заполним текстурные координаты меша (?)
		
		int idx = 0;
		int [] triangles = new int[(allVertices.Count * 3)];
		for (int i = 0; i < (allVertices.Count*3); i+= 3) {
			
			triangles[i] = 0;
			triangles[i+1] = idx+1;
			
			if(i == (allVertices.Count*3)-3)
            {
				//замнуть меш
				triangles[i+2] = 1;	
			}
            else
            {
				triangles[i+2] = idx+2;	
			}
			
			idx++;
		}
		lightMesh.triangles = triangles;//Заполнить полигоны (треугольники) меша

        //lightMesh.RecalculateNormals();
		GetComponent<Renderer>().sharedMaterial = lightMaterial;//Заполнить материал меша
	}

    /*
    /// <summary>
    /// Отобразить линии, составляющие меш света
    /// </summary>
	void DrawLinePerVertex(){
		for (int i = 0; i < allVertices.Count; i++)
		{
			if (i < (allVertices.Count -1))
			{
				Debug.DrawLine(allVertices [i].pos , allVertices [i+1].pos, new Color(i*0.02f, i*0.02f, i*0.02f));
			}
			else
			{
				Debug.DrawLine(allVertices [i].pos , allVertices [0].pos, new Color(i*0.2f, i*0.02f, i*0.02f));
			}
		}
	}*/

    /// <summary>
    /// Найти угол для объекта типа vert
    /// </summary>
	float GetVectorAngle(bool pseudo, float x, float y)
    {
		float ang = 0;
		if(pseudo == true)
        {
			ang = PseudoAngle(x, y);
		}
        else
        {
			ang = Mathf.Atan2(y, x);
		}
		return ang;
	}
	
    /// <summary>
    /// Приблизительные значения углов, которые легко вычислаются
    /// </summary>
	float PseudoAngle(float dx, float dy)
    {
		float ax = Mathf.Abs (dx);
		float ay = Mathf.Abs (dy);
		float p = dy / (ax + ay);
		if (dx < 0)
        {
			p = 2 - p;
		}
		return p;
	}

    /// <summary>
    /// Определить, какие точки составляют коллайдер
    /// </summary>
    Vector2[] GetBoxColPoints(BoxCollider2D col)
    {
        float angle = Mathf.Repeat(col.transform.eulerAngles.z, 90f) * Mathf.PI / 180f;

        Vector2 e = col.bounds.extents;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        float cos2 = Mathf.Cos(2 * angle);
        float b = 2 * (e.x * sin - e.y * cos) / -cos2;

        Vector3 b1 = new Vector3(e.x - b * sin, e.y),
                b2 = new Vector3(e.x, e.y - b * cos);

        Transform bTrans = col.transform;
        Vector3 vect = col.transform.position;
        Vector2[] points = new Vector2[] {bTrans.InverseTransformPoint(vect+b1), bTrans.InverseTransformPoint(vect+b2),
                                          bTrans.InverseTransformPoint(vect-b1),bTrans.InverseTransformPoint(vect-b2) };
        return points;
    }

    /// <summary>
    /// Сразу учесть все препятствия света и отрисовать меш света
    /// </summary>
    public void GenerateLight()
    {
        bool static1 = staticLight;
        staticLight = true;
        Start();
        staticLight = static1;
    }

    /// <summary>
    /// Функция, обеспечивающая рендер света через определённые промежутки времени
    /// </summary>
    IEnumerator ReloadLight()
    {
        yield return new WaitForSeconds(reloadTime);
        renderStage = 0;
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(DynamicLight))]
[CanEditMultipleObjects]
public class DynamicLight_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();  //вызов функции отрисовки базового класса для показа публичных полей компонента
        DynamicLight dLight = (DynamicLight)target;
        if (GUILayout.Button("Generate Light Mesh"))
        {
            dLight.GenerateLight();
        } 
    }
}
#endif //UNITY_EDITOR