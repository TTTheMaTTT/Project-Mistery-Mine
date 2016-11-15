using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
//using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Редактор уровней
/// </summary>
public class LevelEditor : EditorWindow
{

    #region consts

    private const string iconPath = "Assets/Editor/LevelEditor/EditorIcons/";//в этой папке находятся все нужные иконки редактора
    private const string groundBrushesPath= "Assets/Editor/LevelEditor/GroundBrushes/";//в этой папке находятся все нужные кисти
    private const string waterBrushesPath = "Assets/Editor/LevelEditor/WaterBrushes/";//в этой папке находятся все нужные кисти
    private const string databasePath= "Assets/Editor/LevelEditor/Database/";//в этой папке находится база данных

    private const float maxCtr = 200f;

    #endregion //consts

    #region fields

    private static Sprite selectIcon, drawIcon, dragIcon, eraseIcon;//Иконки, используемые при отрисовки меню редактора
    private static Sprite groundIcon, waterIcon, plantIcon, ladderIcon, spikesIcon, usualDrawIcon, lightPointIcon;//Иконки, используемые в меню рисования

    #endregion //fields

    #region parametres

    private static EditorModEnum editorMod;//Режим, в котором находится редактор

    private static bool isGrid, isEnable;//Включено ли отображение сетки, включен ли режим рисования, включен ли редактор, активен ли редактор?
    private static Vector2 gridSize = new Vector2(0.16f, 0.16f);//Размер сетки

    private static float zPosition;//Задаём ось z, по которой происходит отрисовка и установка объектов
    private static string tagName="Untagged";//Тег, по которому мы создаём объекты
    private static string sortingLayer;//Sorting Layer, в котором отрисовываются спрайты

    private static GameObject parentObj;//Какой объект ставится как родительский по отношению к создаваемым объектам

    #region draw

    private static DrawModEnum drawMod;//Режим рисования

    #region groundBrush

    private static List<GroundBrush> groundBrushes=new List<GroundBrush>();
    private static GroundBrush grBrush;//Земляная кисть, которой мы рисуем поверхность в данный момент
    private static string groundBrushName;

    private static int groundLayer = LayerMask.NameToLayer("ground");//По дефолту у земли будет layer на ground
    private static bool groundCollider = true;
    private static int groundSortingLayer;
    private static bool groundAngle=false;
    private static string grParentObjName;//Имя объекта, который станет родительским по отношению к создаваемым объектам.

    #endregion //groundBrush

    #region plantBrush

    private static PlantBase plantBase;//База данных по растительности
    private static float plantOffset;//Насколько сильно дольжно отклониться растение от центра сетки
    private static List<Sprite> currentPlants=new List<Sprite>();//Выборка из растений которыми мы будем украшать уровень в данный момент, взятая из базы данных

    private static int plantLayer = LayerMask.NameToLayer("plant");
    private static Sprite nextPlant;
    private static string plantParentObjName;//Имя объекта, который станет родительским по отношению к создаваемым объектам.

    #endregion //plantBrush

    #region waterBrush

    private static List<WaterBrush> waterBrushes = new List<WaterBrush>();
    private static WaterBrush wBrush;//Водная кисть
    private static string waterBrushName;
    private static Material waterMaterial;

    private static int waterLayer = LayerMask.NameToLayer("Water");
    private static float maxWaterHeight = 100f, maxWaterWidth = 100f;//Максимальные размеры водной области
    private static string waterParentObjName;//Имя объекта, который станет родительским по отношению к создаваемым объектам.

    #endregion //waterBrush

    #region ladderBrush

    private static LadderBase ladderBase;//База данных по лестницам
    private static GameObject currentLadder;//Какая лестница используется в данный момент

    private static int ladderLayer = LayerMask.NameToLayer("ladder");
    private static string ladderTag = "ladder";
    private static GameObject nextLadder;
    private static string ladderParentObjName;//Имя объекта, который станет родительским по отношению к создаваемым объектам.
    private static bool isLiana = false;//Мы рисуем лиану или лестницу?

    #endregion //ladderBrush

    #region spikesBrush

    private static string obstacleName;//Как называется создаваемое препятствие
    private static string obstacleParentName;//Как называется родительский по отношению к создаваемому препятствию объект?
    private static ObstacleBase obstacleBase;//База данных по препятствиям
    private static GameObject currentObstacle;//Какой вид препятствия используется в данный момент

    private static float obstacleDamage;//Какой урон наносит данный вид препятствия
    private static float damageBoxSize=.16f;//Размер области атаки по оси Y
    private static float damageBoxOffset = 0f;//Насколько сдвинут хитбокс по оси Y
    private static float obstacleOffset;//Смещение по вертикальной оси при расположении препятствий

    private static int obstacleLayer = LayerMask.NameToLayer("obstacle");
    private static GameObject nextObstacle;
    private static ObstacleEnum obstacleType;//Тип создаваемого препятствия

    #endregion //spikesBrush

    #region usualBrush
    
    private static SpriteBase spriteBase;//База данных по спрайтам
    private static List<Sprite> currentSprites = new List<Sprite>();//Выборка из спрайтов которыми мы будем рисовать уровень, взятая из базы данных

    private static bool hasCollider=true, isTrigger=false;//Определяем твёрдость объектов
    private static bool overpaint = false;//Есть ли возможность перерисовывать объекты?

    private static int spriteLayer = LayerMask.NameToLayer("ground");
    private static Sprite nextSprite;
    private static string spriteParentObjName;//Имя объекта, который станет родительским по отношению к создаваемым объектам.

    #endregion //usualBrush

    #region lightObstaclePointer

    private static string lightObstacleName;//Название для препятствия света

    private static int lightObstacleLayer = LayerMask.NameToLayer("lightObstacle");
    private static string lightObstacleParentObjName;//Имя объекта, который станет родительским по отношению к создаваемым объектам.

    private static bool createMargin = true;//Если true, то препятствие будет создано с неким отступом с целью созданию эффекта подсвеченного края
    private static float lightMarginOffset=0.05f;//Ширина края твёрдого объекта (препятствия света), который ещё освещается

    private static float lightPointerPrecision = 0.02f;//Точность определения границ коллайдеров твёрдых объектов 

    private static bool sliceObstacle = true;//Если true, то препятствие света разделяется прямоуголниками
    private static Vector2 maxLightObstacleSize = new Vector2(2f, 2f);//Максимальный рзме прямоугольника, что должен обрамлять препятствие

    private static List<GameObject> lightObstacles = new List<GameObject>();//Список последних созданных препятствий света

    #endregion //lightObstaclePointer

    private Sprite[] sprites;
    private static Sprite activeSprite;//Спрайт, что мы используем для отрисовки

    private static bool addBoxCollider;//Станет ли добавляемый объект твёрдым после добавления

    private static int drawIndex=0;
    private static int sLayerIndex = 0;

    private string[] drawOptions;
    private string[] drawFiles;
    private string[] sortingLayers;

    #endregion //draw

    #region erase

    private static int eraseLayer;//Какие лэйеры будут стираться

    #endregion //erase

    public GUIStyle textureStyle;
    public GUIStyle textureStyleAct;

    #region scrollVectors

    private static Vector2 drawScrollPos, groundBrushScrollPos;

    #endregion //scrollVectors

    #endregion //parametres

    public void OnInspectorUpdate()
    {
        Repaint();
    }

    
    void OnEnable()
    {
        isEnable = true;

        selectIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath+"Select.png");
        drawIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + "Draw.png");
        dragIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + "Drag.png");
        eraseIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + "Erase.png");

        groundIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + "rockIcon.png");
        plantIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + "plantIcon.png");
        waterIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + "waterIcon.png");
        ladderIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + "ladderIcon.png");
        spikesIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + "spikeIcon.png");
        usualDrawIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + "brushIcon.png");
        lightPointIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + "lightObstacleIcon.png");

        if (!Directory.Exists(groundBrushesPath))
        {
            AssetDatabase.CreateFolder("Assets/Editor/LevelEditor", "GroundBrushes");
            AssetDatabase.Refresh();
            Debug.Log("Created Ground Brush Directory");
        }
        string[] brushNames = Directory.GetFiles(groundBrushesPath, "*.asset");
        groundBrushes = new List<GroundBrush>();
        foreach (string brushName in brushNames)
        {
            groundBrushes.Add(AssetDatabase.LoadAssetAtPath<GroundBrush>(brushName));
        }
        groundAngle = false;

        if (!Directory.Exists(waterBrushesPath))
        {
            AssetDatabase.CreateFolder("Assets/Editor/LevelEditor", "WaterBrushes");
            AssetDatabase.Refresh();
            Debug.Log("Created Water Brush Directory");
        }
        brushNames = Directory.GetFiles(waterBrushesPath, "*.asset");
        waterBrushes = new List<WaterBrush>();
        foreach (string brushName in brushNames)
        {
            waterBrushes.Add(AssetDatabase.LoadAssetAtPath<WaterBrush>(brushName));
        }

        if (!File.Exists(databasePath + "PlantBase.asset"))
        {
            PlantBase _plantBase = new PlantBase();
            AssetDatabase.CreateAsset(_plantBase, databasePath + "PlantBase" + ".asset");
            AssetDatabase.SaveAssets();
            _plantBase.plants = new List<Sprite>();  
        }
        plantBase = AssetDatabase.LoadAssetAtPath<PlantBase>(databasePath + "PlantBase.asset");

        if (!File.Exists(databasePath + "LadderBase.asset"))
        {
            LadderBase _ladderBase = new LadderBase();
            AssetDatabase.CreateAsset(_ladderBase, databasePath + "LadderBase" + ".asset");
            AssetDatabase.SaveAssets();
            _ladderBase.ladders = new List<GameObject>();
        }
        ladderBase = AssetDatabase.LoadAssetAtPath<LadderBase>(databasePath + "LadderBase.asset");

        if (!File.Exists(databasePath + "ObstacleBase.asset"))
        {
            ObstacleBase _obstacleBase = new ObstacleBase();
            AssetDatabase.CreateAsset(_obstacleBase, databasePath + "ObstacleBase" + ".asset");
            AssetDatabase.SaveAssets();
            _obstacleBase.obstacles = new List<GameObject>();
        }
        obstacleBase = AssetDatabase.LoadAssetAtPath<ObstacleBase>(databasePath + "ObstacleBase.asset");

        if (!File.Exists(databasePath + "SpriteBase.asset"))
        {
            SpriteBase _spriteBase = new SpriteBase();
            AssetDatabase.CreateAsset(_spriteBase, databasePath + "SpriteBase" + ".asset");
            AssetDatabase.SaveAssets();
            _spriteBase.sprites = new List<Sprite>();
        }
        spriteBase = AssetDatabase.LoadAssetAtPath<SpriteBase>(databasePath + "SpriteBase.asset");

        lightObstacles = new List<GameObject>();

        System.Type internalEditorUtilityType = typeof(InternalEditorUtility);
        PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);

        this.sortingLayers = (string[])sortingLayersProperty.GetValue(null, new object[0]);

        Editor.CreateInstance(typeof(SceneViewEventHandler));
    }

    void OnDestroy()
    {
        isEnable = false;
    }

    /// <summary>
    /// Класс, что задаёт правила ввода в редакторе
    /// </summary>
    public class SceneViewEventHandler : Editor
    {
        static SceneViewEventHandler()
        {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        /// <summary>
        /// Действия, производимые на сцене при работающем редакторе уровней
        /// </summary>
        static void OnSceneGUI(SceneView sView)
        {
            Event e = Event.current;
            if (e.type == EventType.keyDown)
            {
                if (e.keyCode == KeyCode.V)
                    editorMod = EditorModEnum.select;
                else if (e.keyCode == KeyCode.B)
                    editorMod = EditorModEnum.draw;
                else if (e.keyCode == KeyCode.C)
                    editorMod = EditorModEnum.erase;
            }
            switch (editorMod)
            {
                case EditorModEnum.select:
                    {
                        SelectHandler();
                        break;
                    }
                case EditorModEnum.draw:
                    {
                        DrawHandler();
                        break;
                    }
                case EditorModEnum.drag:
                    {
                        DragHandler();
                        break;
                    }
                case EditorModEnum.erase:
                    {
                        EraseHandler();
                        break;
                    }       
            }
        }

        /// <summary>
        /// Что можно сделать на сцене в режиме выбора
        /// </summary>
        static void SelectHandler()
        {

        }

        #region draw

        /// <summary>
        /// Что можно сделать на сцене при включенном режиме рисования
        /// </summary>
        static void DrawHandler()
        {

            Event hotkey_e = Event.current;

            if (isEnable)
            {

                switch (drawMod)
                {
                    case DrawModEnum.ground:
                        {
                            GroundHandler();
                            break;
                        }
                    case DrawModEnum.plant:
                        {
                            PlantHandler();
                            break;
                        }
                    case DrawModEnum.water:
                        {
                            WaterHandler(false);
                            break;
                        }
                    case DrawModEnum.ladder:
                        {
                            LadderHandler();
                            break;
                        }
                    case DrawModEnum.spikes:
                        {
                            ObstacleHandler();
                            break;
                        }
                    case DrawModEnum.usual:
                        {
                            UsualHandler();
                            break;
                        }
                    case DrawModEnum.lightObstacle:
                        {
                            LightPointHandler();
                            break;
                        }
                }
            }
        }

        #region ground

        /// <summary>
        /// Здесь происходит отрисовка земных поверхностей
        /// </summary>
        static void GroundHandler()
        {
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (e.type == EventType.keyDown)
            {
                if (e.keyCode == KeyCode.A)
                    groundAngle = !groundAngle;
            }
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && (grBrush!=null? !grBrush.Incomplete :false))
            {
                Camera camera = SceneView.currentDrawingSceneView.camera;

                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = camera.pixelHeight - mousePos.y;
                Vector3 mouseWorldPos = camera.ScreenPointToRay(mousePos).origin;
                mouseWorldPos.z = zPosition;
                if (gridSize.x > 0.05f && gridSize.y > 0.05f)
                {
                    mouseWorldPos.x = Mathf.Floor(mouseWorldPos.x / gridSize.x) * gridSize.x + gridSize.x / 2.0f;
                    mouseWorldPos.y = Mathf.Ceil(mouseWorldPos.y / gridSize.y) * gridSize.y - gridSize.y / 2.0f;
                }
                Ray ray = camera.ScreenPointToRay(mouseWorldPos);

                Vector2 pos = gridSize;
                int grLayer = groundLayer;
                string lName = LayerMask.LayerToName(grLayer);

                if (!Physics2D.Raycast(mouseWorldPos, Vector2.down, Mathf.Min(gridSize.x,gridSize.y)/4f, LayerMask.GetMask(lName)))
                {
                    string objName = (parentObj != null) ? parentObj.name + "0" : grBrush.outGround.name;
                    GameObject newGround = new GameObject(objName, typeof(SpriteRenderer));
                    newGround.transform.position = mouseWorldPos;
                    newGround.GetComponent<SpriteRenderer>().sprite = groundAngle? grBrush.angleGround:grBrush.outGround;
                    newGround.GetComponent<SpriteRenderer>().sortingLayerName = sortingLayer;
                    newGround.tag = tagName;
                    newGround.layer = groundLayer;
                    if (parentObj != null)
                        newGround.transform.parent = parentObj.transform;
                    if (groundAngle)
                    {
                        Sprite gSprite = newGround.GetComponent<SpriteRenderer>().sprite;
                        Vector2 texSize = gSprite.textureRect.size;
                        PolygonCollider2D col = newGround.AddComponent<PolygonCollider2D>();
                        col.points = new Vector2[3];
                        col.points = new Vector2[]{new Vector2(texSize.x, texSize.y) / 2f / gSprite.pixelsPerUnit, 
                                        new Vector2(-texSize.x, -texSize.y) / 2f / gSprite.pixelsPerUnit,
                                        new Vector2(texSize.x, -texSize.y) / 2f / gSprite.pixelsPerUnit};
                        col.isTrigger = !groundCollider;
                    }
                    else
                    {
                        newGround.AddComponent<BoxCollider2D>();
                        newGround.GetComponent<BoxCollider2D>().isTrigger = !groundCollider;
                    }
                    GameObject[] groundBlocks = new GameObject[9];
                    groundBlocks[4] = newGround;
                    RaycastHit2D hit=new RaycastHit2D();
                    groundBlocks[0] = (hit=Physics2D.Raycast(mouseWorldPos + new Vector3(-gridSize.x,gridSize.y*1.1f,0f),Vector2.down,gridSize.x/4f, LayerMask.GetMask(lName)))?  hit.collider.gameObject : null;
                    groundBlocks[1] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(0f, gridSize.y*1.1f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(lName))) ? hit.collider.gameObject : null;
                    groundBlocks[2] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(gridSize.x, gridSize.y*1.1f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(lName))) ? hit.collider.gameObject : null;
                    groundBlocks[3] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(-gridSize.x, gridSize.y * 0.1f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(lName))) ? hit.collider.gameObject : null;
                    groundBlocks[5] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(gridSize.x, gridSize.y * 0.1f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(lName))) ? hit.collider.gameObject : null;
                    groundBlocks[6] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(-gridSize.x, gridSize.y * -0.9f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(lName))) ? hit.collider.gameObject : null;
                    groundBlocks[7] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(0f, gridSize.y * -0.9f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(lName))) ? hit.collider.gameObject : null;
                    groundBlocks[8] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(gridSize.x, gridSize.y * -0.9f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(lName))) ? hit.collider.gameObject : null;

                    for (int i = 0; i < 9; i++)
                        if (groundBlocks[i] != null)
                            CorrectGround(groundBlocks[i]);
                }
            }
        }

        /// <summary>
        /// Откорректировать землю и привести её в нормальный вид
        /// </summary>
        static void CorrectGround(GameObject _ground)
        {
            GroundBrush _groundBrush;
            if (_ground.GetComponent<SpriteRenderer>() == null)
                return;
            if (groundBrushes == null)
                return;

            Sprite _gSprite = _ground.GetComponent<SpriteRenderer>().sprite;
            _groundBrush = groundBrushes.Find(x => x.ContainsSprite(_gSprite));
            if (_groundBrush == null)
                return;

            SpriteRenderer sRenderer = _ground.GetComponent<SpriteRenderer>();
            Vector2 pos = _ground.transform.position;
            float radius = Mathf.Min(gridSize.x, gridSize.y) / 8f;

            if (sRenderer == null)
                return;

            LayerMask grLayer = LayerMask.GetMask(LayerMask.LayerToName(groundLayer));
            //Сначала определим, что окружает блок земли
            bool b11, b12, b13, b21, b23, b31, b32, b33;//Матричные элементы, позволяющие определить, как данный блок земли окружён другими блоками
            b11 = (Physics2D.OverlapCircle(pos+new Vector2(-gridSize.x,gridSize.y), radius, grLayer));
            b12 = (Physics2D.OverlapCircle(pos + new Vector2(0f, gridSize.y), radius, grLayer));
            b13 = (Physics2D.OverlapCircle(pos + new Vector2(gridSize.x, gridSize.y), radius, grLayer));
            b21 = (Physics2D.OverlapCircle(pos + new Vector2(-gridSize.x, 0f), radius, grLayer));
            b23 = (Physics2D.OverlapCircle(pos + new Vector2(gridSize.x, 0f), radius, grLayer));
            b31 = (Physics2D.OverlapCircle(pos + new Vector2(-gridSize.x, -gridSize.y), radius, grLayer));
            b32 = (Physics2D.OverlapCircle(pos + new Vector2(0f, -gridSize.y), radius, grLayer));
            b33 = (Physics2D.OverlapCircle(pos + new Vector2(gridSize.x, -gridSize.y), radius, grLayer));

            if (_gSprite == _groundBrush.angleGround)
            {
                if (!b11 && !b12 && !b21)
                {
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                }
                else if (!b21 && !b31 && !b32)
                {
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
                }
                else if (!b32 && !b33 && !b23)
                {
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
                }
                else if (!b13 && !b12 && !b23)
                {
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
                }
                else
                {
                    sRenderer.sprite = _groundBrush.defGround;
                    _ground.AddComponent<BoxCollider2D>();
                    _ground.GetComponent<BoxCollider2D>().isTrigger = _ground.GetComponent<PolygonCollider2D>().isTrigger;
                    DestroyImmediate(_ground.GetComponent<PolygonCollider2D>());
                    CorrectGround(_ground);
                }
            }
            else
            {
                if (!b12 && !b21 && !b23 && !b32)
                {
                    sRenderer.sprite = _groundBrush.defGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                }
                else if (!b11 && b12 && !b13 && !b23 && !b33 && b32 && !b31 && !b21)
                {
                    sRenderer.sprite = _groundBrush.marginGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                }
                else if (!b11 && !b12 && !b13 && b23 && !b33 && !b32 && !b31 && b21)
                {
                    sRenderer.sprite = _groundBrush.marginGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
                }
                else if (!b11 && !b12 && !b23 && !b32 && !b31 && b21)
                {
                    sRenderer.sprite = _groundBrush.edgeGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
                }
                else if (!b33 && !b32 && !b21 && !b13 && !b12 && b23)
                {
                    sRenderer.sprite = _groundBrush.edgeGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
                }
                else if (!b33 && !b23 && !b12 && !b31 && !b21 && b32)
                {
                    sRenderer.sprite = _groundBrush.edgeGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                }
                else if (!b11 && !b21 && !b32 && !b13 && !b23 && b12)
                {
                    sRenderer.sprite = _groundBrush.edgeGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
                }
                else if (b33 && b32 && b23 && !b12 && !b21 && !b13 && !b31)
                {
                    sRenderer.sprite = _groundBrush.outAngleGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                }
                else if (b31 && b32 && b21 && !b12 && !b23 && !b11 && !b33)
                {
                    sRenderer.sprite = _groundBrush.outAngleGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
                }
                else if (b13 && b23 && b12 && !b11 && !b21 && !b33 && !b32)
                {
                    sRenderer.sprite = _groundBrush.outAngleGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
                }
                else if (b11 && b21 && b12 && !b13 && !b31 && !b32 && !b23)
                {
                    sRenderer.sprite = _groundBrush.outAngleGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
                }
                else if ((b31 && b32 && b33 && (b21 || b23) && !b12) || (!b12 && (b31 || b33)))
                {
                    sRenderer.sprite = _groundBrush.outGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                }
                else if ((b13 && b23 && b33 && (b12 || b32) && !b21) || (!b21 && (b13 || b33)))
                {
                    sRenderer.sprite = _groundBrush.outGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
                }
                else if ((b11 && b12 && b13 && (b21 || b23) && !b32) || (!b32 && (b11 || b13)))
                {
                    sRenderer.sprite = _groundBrush.outGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
                }
                else if ((b11 && b21 && b31 && (b12 || b32) && !b23) || (!b23 && (b11 || b31)))
                {
                    sRenderer.sprite = _groundBrush.outGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
                }
                else if (b21 && b31 && b12 && b32 && !b13 && b23 && b33)
                {
                    sRenderer.sprite = _groundBrush.inAngleGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                }
                else if (b21 && b31 && b12 && b32 && !b11 && b23 && b33)
                {
                    sRenderer.sprite = _groundBrush.inAngleGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
                }
                else if (b21 && b11 && b12 && b32 && !b31 && b23 && b13)
                {
                    sRenderer.sprite = _groundBrush.inAngleGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
                }
                else if (b21 && b11 && b12 && b32 && !b33 && b23 && b13)
                {
                    sRenderer.sprite = _groundBrush.inAngleGround;
                    _ground.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
                }
                else
                {
                    sRenderer.sprite = _groundBrush.inGround;
                }
            }
        }

        #endregion //ground
        
        /// <summary>
        /// Отрисовка растений
        /// </summary>
        static void PlantHandler()
        {
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && (currentPlants != null))
            {
                Camera camera = SceneView.currentDrawingSceneView.camera;

                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = camera.pixelHeight - mousePos.y;
                Vector3 mouseWorldPos = Vector3.zero;
                Vector3 mouseWorldPos1 = camera.ScreenPointToRay(mousePos).origin;
                mouseWorldPos1.z = mouseWorldPos.z = zPosition;
                if (gridSize.x > 0.05f && gridSize.y > 0.05f)
                {
                    mouseWorldPos.x = Mathf.Floor(mouseWorldPos1.x / gridSize.x) * gridSize.x + gridSize.x / 2.0f;
                    mouseWorldPos.y = Mathf.Ceil(mouseWorldPos1.y / gridSize.y) * gridSize.y - gridSize.y / 2.0f;
                }
                Ray ray = camera.ScreenPointToRay(mouseWorldPos);

                Vector2 pos = gridSize;
                Vector2 dif = mouseWorldPos1 - mouseWorldPos;
                Vector3 offsetVect = (Mathf.Abs(dif.x) > Mathf.Abs(dif.y) ? new Vector3(gridSize.x * Mathf.Sign(dif.x), 0f) : new Vector3(0f, gridSize.y * Mathf.Sign(dif.y)));
                int grLayer = groundLayer;
                string glName = LayerMask.LayerToName(grLayer), plName=LayerMask.LayerToName(plantLayer);

                float step = Mathf.Min(gridSize.x, gridSize.y);
                bool b1 = Physics2D.Raycast(mouseWorldPos+Vector3.up*step/10f, Vector2.up, Mathf.Min(gridSize.x, gridSize.y) / 4f, LayerMask.GetMask(glName));
                bool b2 = Physics2D.Raycast(mouseWorldPos + Vector3.right * step/10f, Vector2.right, Mathf.Min(gridSize.x, gridSize.y) / 4f, LayerMask.GetMask(glName));
                bool b3 = Physics2D.Raycast(mouseWorldPos + Vector3.down * step/10f, Vector2.down, Mathf.Min(gridSize.x, gridSize.y) / 4f, LayerMask.GetMask(glName));
                bool b4 = Physics2D.Raycast(mouseWorldPos + Vector3.left * step/10f, Vector2.left, Mathf.Min(gridSize.x, gridSize.y) / 4f, LayerMask.GetMask(glName));

                Vector2 plantUp = Vector2.zero;


                if (b3 && b4 && !b1 && !b2)
                    plantUp = new Vector2(1f, 1f).normalized;
                else if (b4 && b1 && !b2 && !b3)
                    plantUp = new Vector2(1f, -1f).normalized;
                else if (b1 && b2 && !b3 && !b4)
                    plantUp = new Vector2(-1f, -1f).normalized;
                else if (b2 && b3 && !b4 && !b1)
                    plantUp = new Vector2(-1f, 1f).normalized;
                else if (b1 && b2 && b3 && b4)
                    plantUp = offsetVect.normalized;
                bool a1 = (b1 || b2 || b3 || b4);
                bool a2 = !(b1 & b2 && b3 && b4);
                bool a3 = !Physics2D.Raycast(mouseWorldPos + new Vector3(plantUp.x, plantUp.y) * step / 5f, plantUp, step / 4f, LayerMask.GetMask(glName));
                bool a4 = !Physics2D.Raycast(mouseWorldPos + new Vector3(plantUp.x, plantUp.y) * plantOffset, plantUp, step / 4f, LayerMask.GetMask(glName, LayerMask.LayerToName(plantLayer)));
                if ((b1 && b2 && b3 && b4 &&
                         !Physics2D.Raycast(mouseWorldPos + offsetVect,
                                       plantUp, step/ 4f, LayerMask.GetMask(glName)) &&
                         !Physics2D.Raycast(mouseWorldPos,plantUp,
                                        offsetVect.magnitude+plantOffset, LayerMask.GetMask(plName))) ||
                    ((b1||b2||b3||b4) && !(b1&b2&&b3&&b4) && 
                    !Physics2D.Raycast(mouseWorldPos+new Vector3(plantUp.x,plantUp.y)*step/10f,plantUp,step/4f,LayerMask.GetMask(glName))&&
                    !Physics2D.Raycast(mouseWorldPos-new Vector3(plantUp.x,plantUp.y)*step/4f,plantUp,plantOffset + step / 4f, LayerMask.GetMask(plName))))
                {
                    Sprite loadedPlant = currentPlants[Random.Range(0, currentPlants.Count)];
                    string plantName = (parentObj != null) ? parentObj.name + "0" :"plant";
                    GameObject newPlant = new GameObject(plantName, typeof(SpriteRenderer));
                    newPlant.transform.position = mouseWorldPos + ((b1&&b2&&b3&&b4)? (offsetVect / 2f):Vector3.zero) +new Vector3(plantUp.x,plantUp.y) * plantOffset;
                    newPlant.transform.eulerAngles=new Vector3(0f,0f,plantUp.x<0? Vector2.Angle(Vector2.up,plantUp): 360f-Vector2.Angle(Vector2.up, plantUp));
                    newPlant.transform.localScale += new Vector3((plantUp.x * plantUp.y != 0f ? Mathf.Sqrt(2)-1f : 0f),0f,0f);
                    newPlant.GetComponent<SpriteRenderer>().sprite = loadedPlant;
                    newPlant.GetComponent<SpriteRenderer>().sortingLayerName = sortingLayer;
                    newPlant.tag = tagName;
                    newPlant.layer = plantLayer;
                    if (parentObj != null)
                        newPlant.transform.parent = parentObj.transform;
                    BoxCollider2D col=newPlant.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(col.size.x, col.size.y /5f);
                    newPlant.AddComponent<EditorCollider>();

                }
            }
        }


        #region water

        /// <summary>
        /// Отрисовка воды 
        /// </summary>
        static void WaterHandler(bool wErase)
        {
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && (wBrush != null ? !wBrush.Incomplete : false))
            {
                Camera camera = SceneView.currentDrawingSceneView.camera;

                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = camera.pixelHeight - mousePos.y;
                Vector3 mouseWorldPos = camera.ScreenPointToRay(mousePos).origin;
                mouseWorldPos.z = zPosition;
                if (gridSize.x > 0.05f && gridSize.y > 0.05f)
                {
                    mouseWorldPos.x = Mathf.Floor(mouseWorldPos.x / gridSize.x) * gridSize.x + gridSize.x / 2.0f;
                    mouseWorldPos.y = Mathf.Ceil(mouseWorldPos.y / gridSize.y) * gridSize.y - gridSize.y / 2.0f;
                }
                Ray ray = camera.ScreenPointToRay(mouseWorldPos);

                Vector2 pos = gridSize;
                int wLayer = waterLayer;
                string wlName = LayerMask.LayerToName(wLayer), glName = LayerMask.LayerToName(groundLayer);
                float step = Mathf.Min(gridSize.x, gridSize.y);


                bool b1 = (!Physics2D.Raycast(mouseWorldPos + Vector3.up * gridSize.y / 10f, Vector2.up, gridSize.y / 4f, LayerMask.GetMask(wlName)));
                bool b2 = (!Physics2D.Raycast(mouseWorldPos + Vector3.right * gridSize.x / 10f, Vector2.right, gridSize.x / 4f, LayerMask.GetMask(wlName)));
                bool b3 = (!Physics2D.Raycast(mouseWorldPos + Vector3.down * gridSize.y / 10f, Vector2.down, gridSize.y / 4f, LayerMask.GetMask(wlName)));
                bool b4 = (!Physics2D.Raycast(mouseWorldPos + Vector3.left * gridSize.x / 10f, Vector2.left, gridSize.x / 4f, LayerMask.GetMask(wlName)));
                bool a1 = (!Physics2D.Raycast(mouseWorldPos + Vector3.up * gridSize.y / 10f, Vector2.up, gridSize.y / 4f, LayerMask.GetMask(glName)));
                bool a2 = (!Physics2D.Raycast(mouseWorldPos + Vector3.right * gridSize.x / 10f, Vector2.right, gridSize.x / 4f, LayerMask.GetMask(glName)));
                bool a3 = (!Physics2D.Raycast(mouseWorldPos + Vector3.down * gridSize.y / 10f, Vector2.down, gridSize.y / 4f, LayerMask.GetMask(glName)));
                bool a4 = (!Physics2D.Raycast(mouseWorldPos + Vector3.left * gridSize.x / 10f, Vector2.left, gridSize.x / 4f, LayerMask.GetMask(glName)));

                if (b1 && b2 && b3 && b4 && !(!a1 && !a2 && !a3 && !a4)&&!wErase)
                {
                    List<Vector2> waterBorder = FormWaterPoints(mouseWorldPos);
                    FillAreaWithWater(waterBorder);
                }
                else if (!b1 || !b2 || !b3 || !b4)
                {
                    while (Physics2D.Raycast(mouseWorldPos + Vector3.up * gridSize.y * 0.55f, Vector2.up, gridSize.y / 2f, LayerMask.GetMask(wlName)))
                    {
                        List<Vector2> waterBorder = GetWaterAreaByPoint(mouseWorldPos);
                        if (waterBorder != null ? waterBorder.Count > 0 : false)
                            ReduceWaterLevel(waterBorder);
                        else
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Определить края области, в которых следует разместить спрайты воды
        /// </summary>
        static List<Vector2> FormWaterPoints(Vector3 beginPoint)
        {
            Vector2 movVect = new Vector2(0f, -1f);//Вектор направления обхода контура (движения по контуру)
            Vector2 colVect = new Vector2(-1f, 0f);//Вектор, что всегда направлен в сторону контура (направлен в твёрдый объект)
            List<Vector2> border = new List<Vector2>();//Искомая граница
            float step = Mathf.Abs(gridSize.x * movVect.x + gridSize.y * movVect.y);
            string glName = LayerMask.LayerToName(groundLayer), wlName = LayerMask.LayerToName(waterLayer);
            Vector2 leftPoint = beginPoint;
            Vector2 rightPoint = leftPoint+Vector2.up*gridSize.y;
            while (!((Mathf.Abs(rightPoint.y - leftPoint.y)<gridSize.y/10f)&&(Physics2D.Raycast(rightPoint+Vector2.right*gridSize.x*.1f,Vector2.right,gridSize.x*.7f,LayerMask.GetMask(glName)))))
            {
                if (rightPoint.y - leftPoint.y > gridSize.y / 10f)
                    rightPoint = leftPoint;
                if (!Physics2D.Raycast(leftPoint + gridSize.x * .1f * Vector2.right, Vector2.left, gridSize.x * .7f, LayerMask.GetMask(glName)))
                {
                    while (!Physics2D.Raycast(leftPoint + gridSize.x * .1f * Vector2.right, Vector2.left, gridSize.x * .7f, LayerMask.GetMask(glName)))
                    {
                        leftPoint -= new Vector2(gridSize.x, 0f);
                        if (Mathf.Abs((rightPoint - leftPoint).x) > maxWaterWidth)  
                            return null;
                    }
                    rightPoint = leftPoint;
                }
                border.Add(rightPoint);

                if (!Physics2D.Raycast(rightPoint + step * .1f * colVect, colVect, step * .7f, LayerMask.GetMask(glName)))
                {
                    Vector2 vect = -movVect;
                    movVect = colVect;
                    colVect = vect;
                    step = Mathf.Abs(gridSize.x * movVect.x + gridSize.y * movVect.y);
                }

                int turnCount = 0;
                while (turnCount<4 && Physics2D.Raycast(rightPoint + step*.1f*movVect, movVect, step * .7f, LayerMask.GetMask(glName)))
                {
                    turnCount++;
                    Vector2 vect = -colVect;
                    colVect = movVect;
                    movVect = vect;
                    step = Mathf.Abs(gridSize.x * movVect.x + gridSize.y * movVect.y);
                }
                if (turnCount != 4)
                {
                    rightPoint += movVect * step;
                    if (Mathf.Abs((rightPoint - leftPoint).x) >= maxWaterWidth || Mathf.Abs((rightPoint - leftPoint).y) >= maxWaterHeight)
                    {
                        border = null;
                        return border;
                    }
                }
                else
                {
                    return border;
                }
                if (Mathf.Approximately(rightPoint.y, leftPoint.y) && (rightPoint.x < leftPoint.x))
                {
                    leftPoint = rightPoint;
                    border = new List<Vector2>();
                    movVect = new Vector2(0f, -1f);
                    colVect = new Vector2(-1f, 0f);
                    rightPoint = leftPoint + Vector2.up * gridSize.y;
                    if (Physics2D.Raycast(leftPoint + gridSize.x * .1f * Vector2.right, Vector2.left, gridSize.x * .7f, LayerMask.GetMask(glName)))
                    {
                        movVect = Vector2.down;
                        colVect = Vector2.left;
                        step = Mathf.Abs(gridSize.x * movVect.x + gridSize.y * movVect.y);
                    }
                }
                else if (Mathf.Approximately(rightPoint.y, leftPoint.y))
                    border.Add(rightPoint);
            }
            // Учтём внутренние контуры и замыкнём нужный нам
            bool onBorder = false;
            movVect = Vector2.left;
            colVect = Vector2.up;
            step = gridSize.y;
            rightPoint = border[border.Count - 1];
            while ((Mathf.Abs(leftPoint.x - rightPoint.x) > gridSize.x / 10f) || (Mathf.Abs(leftPoint.y - rightPoint.y) > gridSize.y / 10f))
            {
                int turnCount = 0;
                if (onBorder)
                {
                    if ((Mathf.Abs(leftPoint.y - rightPoint.y) <= gridSize.y / 10f) &&
                        (!Physics2D.Raycast(rightPoint+Vector2.left*step*.1f, Vector2.left, .7f * gridSize.x, LayerMask.GetMask(glName)))&&
                        (Physics2D.Raycast(rightPoint + Vector2.right * step * .1f, Vector2.right, .7f * gridSize.x, LayerMask.GetMask(glName))))
                    {
                        onBorder = false;
                        movVect = Vector2.left;
                        colVect = Vector2.up;
                        step = gridSize.x;
                    }
                }
                else
                {
                    if (Physics2D.Raycast(rightPoint + Vector2.left * step * .1f, Vector2.left, .7f * gridSize.x, LayerMask.GetMask(glName)))
                    {
                        onBorder = true;
                        movVect = Vector2.down;
                        colVect = Vector2.left;
                        step = gridSize.y;
                    }
                }
                if (onBorder)
                {
                    if (!Physics2D.Raycast(rightPoint + step * .1f * colVect, colVect, step * .7f, LayerMask.GetMask(glName)))
                    {
                        Vector2 vect = -movVect;
                        movVect = colVect;
                        colVect = vect;
                        step = Mathf.Abs(gridSize.x * movVect.x + gridSize.y * movVect.y);
                    }
                    while (turnCount < 4 && Physics2D.Raycast(rightPoint + step * .1f * movVect, movVect, step * .7f, LayerMask.GetMask(glName)))
                    {
                        turnCount++;
                        Vector2 vect = -colVect;
                        colVect = movVect;
                        movVect = vect;
                        step = Mathf.Abs(gridSize.x * movVect.x + gridSize.y * movVect.y);
                    }
                }
                if (turnCount != 4)
                {
                    rightPoint += movVect * step;
                    border.Add(rightPoint);
                }
                else
                {
                    return border;
                }
                if (Mathf.Approximately(rightPoint.y, leftPoint.y) && Physics2D.Raycast(rightPoint+Vector2.right*gridSize.x*.1f,Vector2.right,gridSize.x*.7f,LayerMask.GetMask(glName)))
                {
                    onBorder = false;
                    movVect = Vector2.left;
                    colVect = Vector2.up;
                    step = gridSize.x;
                }
            }
            return border;
        }

        /// <summary>
        /// Заполнить область с заданной границей водой
        /// </summary>
        static void FillAreaWithWater(List<Vector2> areaBorder)
        {
            if (areaBorder == null? true : areaBorder.Count == 0)
                return;
            Vector2 leftPoint = Vector2.zero, rightPoint = Vector2.zero;
            string glName = LayerMask.LayerToName(groundLayer), wlName = LayerMask.LayerToName(waterLayer);
            float depth = Mathf.Infinity;
            foreach (Vector2 vect in areaBorder)
            {
                if (vect.y < depth)
                    depth = vect.y;
            }
            while (Mathf.Abs(depth-areaBorder[0].y)<gridSize.y/10f || depth<areaBorder[0].y)
            {
                float leftPos = Mathf.Infinity, rightPos = Mathf.NegativeInfinity;
                List<Vector2> border = areaBorder.FindAll(x => Mathf.Abs(x.y-depth)<gridSize.y/10f);
                border.Sort((x,y)=> { return x.x.CompareTo(y.x); });

                for (int i=0;i< border.Count;i++)
                {
                    leftPos = border[i].x;
                    if (i < border.Count - 1 ? !(Physics2D.Raycast(border[i] + Vector2.right*gridSize.x/10f, border[i + 1] - border[i], gridSize.x * .7f, LayerMask.GetMask(glName)) && 
                                                (Physics2D.Raycast(border[i+1] + Vector2.left * gridSize.x / 10f, Vector2.left, gridSize.x * .7f, LayerMask.GetMask(glName)))): false)
                    {
                        rightPos = border[i + 1].x;
                        while (leftPos < rightPos)
                        {
                            Vector2 cellPosition = new Vector2(leftPos, depth);
                            FillCellWithWater(cellPosition, (Mathf.Abs(depth - areaBorder[0].y) < gridSize.y / 10f));
                            leftPos += gridSize.x;
                        }
                    }
                    else
                    {
                        Vector2 cellPosition = new Vector2(leftPos, depth);
                        FillCellWithWater(cellPosition, (Mathf.Abs(depth - areaBorder[0].y) < gridSize.y / 10f));
                    }
                }
                depth += gridSize.y;
            }
        }

        /// <summary>
        /// Заполнить одну клетку сетки водой правильным образом
        /// </summary>
        static void FillCellWithWater(Vector2 cellPosition, bool surface)
        {
            string glName = LayerMask.LayerToName(groundLayer);
            string wlName = LayerMask.LayerToName(waterLayer);
            bool a1 = (Physics2D.Raycast(cellPosition + Vector2.up * gridSize.y / 10f, Vector2.up, gridSize.y / 4f, LayerMask.GetMask(glName)));
            bool a2 = (Physics2D.Raycast(cellPosition + Vector2.right * gridSize.x / 10f, Vector2.right, gridSize.x / 4f, LayerMask.GetMask(glName)));
            bool a3 = (Physics2D.Raycast(cellPosition + Vector2.down * gridSize.y / 10f, Vector2.down, gridSize.y / 4f, LayerMask.GetMask(glName)));
            bool a4 = (Physics2D.Raycast(cellPosition + Vector2.left * gridSize.x / 10f, Vector2.left, gridSize.x / 4f, LayerMask.GetMask(glName)));

            if (!(a1 && a2 && a3 && a4))
            {
                bool angle = false;
                string objName = (parentObj != null) ? parentObj.name + "0" : grBrush.outGround.name;
                GameObject prevWater = null;
                if (Physics2D.Raycast(cellPosition, Vector2.left, gridSize.x / 4f, LayerMask.GetMask(wlName)))
                    prevWater = (Physics2D.Raycast(cellPosition, Vector2.left, gridSize.x / 4f, LayerMask.GetMask(wlName)).collider.gameObject);
                if (prevWater == null && Physics2D.Raycast(cellPosition, Vector2.right, gridSize.x / 4f, LayerMask.GetMask(wlName)))
                    prevWater = (Physics2D.Raycast(cellPosition, Vector2.right, gridSize.x / 4f, LayerMask.GetMask(wlName)).collider.gameObject);
                GameObject newWater = new GameObject(objName, typeof(SpriteRenderer));
                newWater.transform.position = new Vector3(cellPosition.x, cellPosition.y, zPosition);
                newWater.GetComponent<SpriteRenderer>().sortingLayerName = sortingLayer;
                if (waterMaterial != null)
                    newWater.GetComponent<SpriteRenderer>().material = waterMaterial;
                newWater.tag = tagName;
                newWater.layer = waterLayer;
                if (parentObj != null)
                    newWater.transform.parent = parentObj.transform;
                angle = (a1 || a2 || a3 || a4);
                if (prevWater != null)
                {
                    newWater.transform.eulerAngles = prevWater.transform.eulerAngles;
                    DestroyImmediate(prevWater);
                }
                if (angle)
                {
                    Sprite wSprite = newWater.GetComponent<SpriteRenderer>().sprite = wBrush.waterAngleSprite;
                    Vector2 texSize = wSprite.textureRect.size;
                    PolygonCollider2D col = newWater.AddComponent<PolygonCollider2D>();
                    col.points = new Vector2[3];
                    col.points = new Vector2[]{new Vector2(texSize.x, texSize.y) / 2f / wSprite.pixelsPerUnit,
                                    new Vector2(-texSize.x, -texSize.y) / 2f / wSprite.pixelsPerUnit,
                                    new Vector2(texSize.x, -texSize.y) / 2f / wSprite.pixelsPerUnit};
                    col.isTrigger = true;
                    newWater.transform.eulerAngles = new Vector3(0f, 0f, a1 && a2 ? -90f : a2 && a3 ? 180f : a3 && a4 ? 90f : 0f);
                }
                else
                {
                    newWater.GetComponent<SpriteRenderer>().sprite = wBrush.waterSprite;
                    newWater.AddComponent<BoxCollider2D>();
                    newWater.GetComponent<BoxCollider2D>().isTrigger = true;
                }
                if (!a1 && surface && (wBrush.waterObjects != null ? wBrush.waterObjects.Count > 0 : false))
                {
                    GameObject wObject = GameObject.Instantiate(wBrush.waterObjects[Random.Range(0, wBrush.waterObjects.Count - 1)], newWater.transform.position + Vector3.up * gridSize.y / 2f, Quaternion.identity) as GameObject;
                    wObject.transform.parent = newWater.transform;
                    if (wObject.GetComponent<SpriteRenderer>() != null)
                        wObject.GetComponent<SpriteRenderer>().sortingLayerName = sortingLayer;
                }
            }
        }

        /// <summary>
        /// Убрать всю воду из клетки
        /// </summary>
        static void RemoveWaterFromCell(Vector2 cellPosition)
        {
            string wlName = LayerMask.LayerToName(waterLayer);
            GameObject destrWater = null;
            if (Physics2D.Raycast(cellPosition + Vector2.left * gridSize.x * .1f, Vector2.left, gridSize.x * .35f, LayerMask.GetMask(wlName)))
            {
                destrWater = Physics2D.Raycast(cellPosition + Vector2.left * gridSize.x * .1f, Vector2.left, gridSize.x * .35f, LayerMask.GetMask(wlName)).collider.gameObject;
            }
            if ((destrWater == null) && (Physics2D.Raycast(cellPosition + Vector2.right * gridSize.x * .1f, Vector2.right, gridSize.x * .35f, LayerMask.GetMask(wlName))))
                destrWater = Physics2D.Raycast(cellPosition + Vector2.right * gridSize.x * .1f, Vector2.right, gridSize.x * .35f, LayerMask.GetMask(wlName)).collider.gameObject;
            if (destrWater != null)
                DestroyImmediate(destrWater);

            GameObject newWaterObj = null;
            Vector2 downCellPosition = new Vector2(cellPosition.x, cellPosition.y - gridSize.y);
            if (Physics2D.Raycast(downCellPosition + Vector2.up * gridSize.y * .1f, Vector2.up, gridSize.x * .35f, LayerMask.GetMask(wlName)))
                newWaterObj = Physics2D.Raycast(downCellPosition+ Vector2.up * gridSize.y * .1f, Vector2.up, gridSize.y * .35f, LayerMask.GetMask(wlName)).collider.gameObject;
            if (newWaterObj != null && wBrush != null ? wBrush.waterObjects.Count > 0 : false)
            {
                GameObject wObject = GameObject.Instantiate(wBrush.waterObjects[Random.Range(0, wBrush.waterObjects.Count - 1)], newWaterObj.transform.position + Vector3.up * gridSize.y / 2f, Quaternion.identity) as GameObject;
                wObject.transform.parent = newWaterObj.transform;
                if (wObject.GetComponent<SpriteRenderer>() != null)
                    wObject.GetComponent<SpriteRenderer>().sortingLayerName = sortingLayer;
            }

        }

        /// <summary>
        /// Уменьшить уровень воды на размер сетки
        /// </summary>
        static void ReduceWaterLevel(List<Vector2> areaBorder)
        {
            if (areaBorder != null ? areaBorder.Count == 0 : true)
                return;
            List<Vector2> border1 = areaBorder.FindAll(x => Mathf.Approximately(x.y, areaBorder[0].y));
            border1.Sort((x, y) => { return x.x.CompareTo(y.x); });
            float leftPos = areaBorder[0].x;
            float y1 = areaBorder[0].y;
            string wlName = LayerMask.LayerToName(waterLayer);
            string glName = LayerMask.LayerToName(groundLayer);
            for (int i = 0; i < border1.Count; i++)
            {
                leftPos = border1[i].x;
                if (i < border1.Count - 1 ? !(Physics2D.Raycast(new Vector2(leftPos, y1), Vector2.right, gridSize.x * .75f, LayerMask.GetMask(glName))&&
                                              (Physics2D.Raycast(new Vector2(border1[i+1].x, y1), Vector2.left, gridSize.x * .75f, LayerMask.GetMask(glName)))) : false)
                {
                    float rightPos = border1[i + 1].x;
                    while (leftPos < rightPos || Mathf.Abs(rightPos - leftPos) < gridSize.x / 10f)
                    {
                        Vector2 cellPosition = new Vector2(leftPos, y1);
                        RemoveWaterFromCell(cellPosition);
                        leftPos += gridSize.x;
                    }
                }
                else
                {
                    Vector2 cellPosition = new Vector2(leftPos, y1);
                    RemoveWaterFromCell(cellPosition);
                }
            }
        }

        /// <summary>
        /// Указать границы области воды, в которой находится данная точка
        /// </summary>
        static List<Vector2> GetWaterAreaByPoint(Vector2 waterPoint)
        {
            //Сначала найдём самую высокую точку водной области
            bool findBorder = false;
            Vector2 currentPosition = waterPoint;
            Vector2 movVect = Vector2.up;
            Vector2 colVect = Vector2.right;
            Vector2 rightPoint = currentPosition;
            float step = gridSize.y;
            string glName = LayerMask.LayerToName(groundLayer), wlName=LayerMask.LayerToName(waterLayer);
            while (!(findBorder && Mathf.Approximately(rightPoint.y,currentPosition.y) && Mathf.Approximately(rightPoint.x, currentPosition.x)) && 
                   (Physics2D.Raycast(currentPosition+Vector2.up*gridSize.y*.55f,Vector2.up,gridSize.y*.3f,LayerMask.GetMask(glName,wlName))))
            {
                int turnCount = 0;
                if (!findBorder)
                {
                    if (Physics2D.Raycast(currentPosition + Vector2.up * gridSize.y * .1f, Vector2.up, gridSize.y*.7f, LayerMask.GetMask(glName)))
                    {
                        findBorder = true;
                        rightPoint = currentPosition;
                        movVect = Vector2.left;
                        colVect = Vector2.up;
                        step = gridSize.x;
                    }
                }
                else if (!Physics2D.Raycast(currentPosition + Vector2.up * gridSize.y * .1f, Vector2.up, gridSize.y*.7f, LayerMask.GetMask(glName)) && currentPosition.y >= rightPoint.y)
                {
                    findBorder = false;
                    step = gridSize.y;
                    movVect = Vector2.up;
                    colVect = Vector2.right;
                }
                if (currentPosition.y > rightPoint.y)
                    rightPoint = currentPosition;
                if (findBorder)
                {
                    if (!Physics2D.Raycast(currentPosition + colVect * step * .1f, colVect, step * .7f, LayerMask.GetMask(glName)))
                    {
                        Vector2 vect = -movVect;
                        movVect = colVect;
                        colVect=vect;
                        step = movVect.x != 0 ? gridSize.x : gridSize.y;
                    }
                    while (turnCount<4 && Physics2D.Raycast(currentPosition+movVect*step*.1f,movVect,step*.7f,LayerMask.GetMask(glName)))
                    {
                        Vector2 vect= -colVect;
                        colVect = movVect;
                        movVect = vect;
                        step = movVect.x!=0? gridSize.x : gridSize.y;
                        turnCount++;
                    }
                }
                if (turnCount < 4)
                    currentPosition += movVect * step;
            }
            bool k = Physics2D.Raycast(currentPosition + Vector2.up * gridSize.y * .55f, Vector2.up, gridSize.y * .3f, LayerMask.GetMask(glName, wlName));
            rightPoint = currentPosition;
            List<Vector2> border = FormWaterPoints(rightPoint);
            return border;
        }

        #endregion //water

        #region ladder

        /// <summary>
        /// Функция, создающая лестницы при нажатии на кнопку мыши
        /// </summary>
        static void LadderHandler()
        {
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && currentLadder != null)
            {
                Camera camera = SceneView.currentDrawingSceneView.camera;

                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = camera.pixelHeight - mousePos.y;
                Vector3 mouseWorldPos = camera.ScreenPointToRay(mousePos).origin;
                if (gridSize.x > 0.05f && gridSize.y > 0.05f)
                {
                    mouseWorldPos.x = Mathf.Floor(mouseWorldPos.x / gridSize.x) * gridSize.x + gridSize.x / 2.0f;
                    mouseWorldPos.y = Mathf.Ceil(mouseWorldPos.y / gridSize.y) * gridSize.y - gridSize.y / 2.0f;
                }
                mouseWorldPos.z = zPosition;
                Ray ray = camera.ScreenPointToRay(mouseWorldPos);

                string glName = LayerMask.LayerToName(groundLayer), llName = LayerMask.LayerToName(ladderLayer);

                float step = Mathf.Min(gridSize.x, gridSize.y);

                if (!Physics2D.Raycast(mouseWorldPos,Vector2.down, gridSize.y*.45f, LayerMask.GetMask(glName, llName)) && 
                    (!isLiana || Physics2D.Raycast(mouseWorldPos+Vector3.up*gridSize.y*.5f,Vector2.up,gridSize.y*.05f,LayerMask.GetMask(glName,llName))))
                {
                    if (parentObj == null && ladderParentObjName != string.Empty)
                    {
                        parentObj = new GameObject(ladderParentObjName);
                        parentObj.transform.position = mouseWorldPos;
                    }
                    string ladderName = (parentObj != null) ? parentObj.name + "0" : (isLiana? "liana" : "ladder");
                    GameObject newLadder = GameObject.Instantiate(currentLadder,mouseWorldPos,Quaternion.identity) as GameObject;
                    newLadder.transform.position = mouseWorldPos;
                    newLadder.tag = ladderTag;
                    newLadder.layer = ladderLayer;
                    newLadder.name = ladderName;
                    newLadder.GetComponent<SpriteRenderer>().sortingLayerName=sortingLayer;
                    
                    if (parentObj != null)
                        newLadder.transform.parent = parentObj.transform;
                }
            }
        }

        #endregion //ladder

        #region obstacles

        /// <summary>
        /// Функция, создающая препятствия при нажатии кнопки мыши
        /// </summary>
        static void ObstacleHandler()
        {
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && (currentObstacle != null))
            {
                Camera camera = SceneView.currentDrawingSceneView.camera;

                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = camera.pixelHeight - mousePos.y;
                Vector3 mouseWorldPos = camera.ScreenPointToRay(mousePos).origin;
                if (gridSize.x > 0.05f && gridSize.y > 0.05f)
                {
                    mouseWorldPos.x = Mathf.Floor(mouseWorldPos.x / gridSize.x) * gridSize.x + gridSize.x / 2.0f;
                    mouseWorldPos.y = Mathf.Ceil(mouseWorldPos.y / gridSize.y) * gridSize.y - gridSize.y / 2.0f;
                }
                Ray ray = camera.ScreenPointToRay(mouseWorldPos);
                mouseWorldPos.z = zPosition;

                string glName = LayerMask.LayerToName(groundLayer), olName = LayerMask.LayerToName(obstacleLayer);

                if (!Physics2D.Raycast(mouseWorldPos, Vector2.down, gridSize.y * .45f, LayerMask.GetMask(glName, olName)) &&
                    Physics2D.Raycast(mouseWorldPos + Vector3.down * gridSize.y * .5f, Vector2.down, gridSize.y * .05f, LayerMask.GetMask(glName)))
                {
                    GameObject obstacle = null;
                    if (Physics2D.Raycast(mouseWorldPos + Vector3.up*(obstacleOffset + damageBoxOffset + (damageBoxSize - gridSize.y) / 2f) + Vector3.left * gridSize.x * .55f, 
                        Vector2.left, gridSize.x * .15f, LayerMask.GetMask(olName)))
                    {
                        obstacle = Physics2D.Raycast(mouseWorldPos + Vector3.up * (obstacleOffset + damageBoxOffset + (damageBoxSize - gridSize.y) / 2f) + Vector3.left * gridSize.x * .55f,
                                                    Vector2.left, gridSize.x * .15f, LayerMask.GetMask(olName)).collider.gameObject;
                        if (!((obstacleType == ObstacleEnum.plants && obstacle.GetComponent<ObstacleScript>() != null) ||
                             ((obstacleType == ObstacleEnum.spikes && obstacle.GetComponent<SpikesScript>() != null))))
                            obstacle = null;
                    }
                    if ((obstacle == null) && Physics2D.Raycast(mouseWorldPos + Vector3.up * (obstacleOffset + damageBoxOffset +  (damageBoxSize - gridSize.y) / 2f) + Vector3.right * gridSize.x * .55f, 
                                                                Vector2.right, gridSize.x * .15f, LayerMask.GetMask(olName)))
                    {
                        obstacle = Physics2D.Raycast(mouseWorldPos + Vector3.up * (obstacleOffset + damageBoxOffset + (damageBoxSize - gridSize.y) / 2f) + Vector3.right * gridSize.x * .55f,
                                                     Vector2.right, gridSize.x * .15f, LayerMask.GetMask(olName)).collider.gameObject;
                        if (!((obstacleType == ObstacleEnum.plants && obstacle.GetComponent<ObstacleScript>() != null) ||
                             ((obstacleType == ObstacleEnum.spikes && obstacle.GetComponent<SpikesScript>() != null))))
                            obstacle = null;
                    }
                    if (obstacle == null)
                    {
                        obstacle = new GameObject(obstacleName);
                        obstacle.transform.position = mouseWorldPos+Vector3.up*obstacleOffset;
                        BoxCollider2D col = obstacle.AddComponent<BoxCollider2D>();
                        col.size = new Vector2(gridSize.x, damageBoxSize);
                        col.offset = new Vector2(0f, (damageBoxSize-gridSize.y) / 2f + damageBoxOffset);
                        col.isTrigger = true;
                        if (obstacleType == ObstacleEnum.plants)
                        {
                            ObstacleScript obScript = obstacle.AddComponent<ObstacleScript>();
                            obScript.HitData = new HitClass(obstacleDamage, -1f, col.size, col.offset,0f);
                            obScript.HitData.damage = obstacleDamage;
                            obScript.HitData.hitSize = col.size;
                            obScript.HitData.hitPosition = col.offset;
                            obScript.Enemies = new List<string>() { "player" };
                            obstacle.AddComponent<HitBox>();
                        }
                        else if (obstacleType == ObstacleEnum.spikes)
                        {
                            SpikesScript spikeScript = obstacle.AddComponent<SpikesScript>();
                            spikeScript.Damage = obstacleDamage;
                            spikeScript.Enemies = new List<string>() { "player" };
                        }
                        obstacle.layer = obstacleLayer;
                        obstacle.tag = tagName;
                        if (obstacleParentName != string.Empty)
                        {
                            if (parentObj == null)
                            {
                                parentObj = new GameObject(obstacleParentName);
                                parentObj.transform.position = mouseWorldPos;
                            }
                            obstacle.transform.parent = parentObj.transform;
                        }
                    }
                    GameObject newObstacle = Instantiate(currentObstacle, mouseWorldPos+obstacleOffset*Vector3.up, Quaternion.identity) as GameObject;
                    newObstacle.tag = tagName;
                    newObstacle.layer = obstacleLayer;
                    newObstacle.GetComponent<SpriteRenderer>().sortingLayerName = sortingLayer;

                    if (obstacle != null)
                    {
                        newObstacle.transform.parent = obstacle.transform;
                        CorrectObstacle(obstacle);
                    }
                    if (Physics2D.Raycast(mouseWorldPos + Vector3.up * (obstacleOffset + damageBoxOffset + (damageBoxSize - gridSize.y) / 2f) + Vector3.right * gridSize.x * .55f,
                                                                Vector2.right, gridSize.x * .15f, LayerMask.GetMask(olName)) &&
                        Physics2D.Raycast(mouseWorldPos + Vector3.up * (obstacleOffset + damageBoxOffset + (damageBoxSize - gridSize.y) / 2f) + Vector3.left * gridSize.x * .55f,
                        Vector2.left, gridSize.x * .15f, LayerMask.GetMask(olName)))
                    {
                        GameObject obstacle1 = Physics2D.Raycast(mouseWorldPos + Vector3.up * (obstacleOffset + damageBoxOffset + (damageBoxSize - gridSize.y) / 2f) + Vector3.right * gridSize.x * .55f,
                                                                Vector2.right, gridSize.x * .15f, LayerMask.GetMask(olName)).collider.gameObject;
                        if (((obstacleType == ObstacleEnum.plants)&&(obstacle1.GetComponent<ObstacleScript>()!=null)||
                            (obstacleType==ObstacleEnum.spikes)&&(obstacle1.GetComponent<SpikesScript>()!=null)))
                            CombineObstacles(obstacle, obstacle1);
                    }
                        
                }
            }
        }

        /// <summary>
        /// Функция, которая учитывает расположение и содержимое препятствия и правильно настраивает его
        /// </summary>
        static void CorrectObstacle(GameObject _obstacle)
        {
            //Сначала вытащим все дочерние объекты из объекта
            List<GameObject> obChildren = new List<GameObject>();
            for (int i=_obstacle.transform.childCount-1;i>=0;i--)
            {
                obChildren.Add(_obstacle.transform.GetChild(i).gameObject);
            }
            _obstacle.transform.DetachChildren();

            //Настроим само препятствие
            obChildren.Sort((x, y) => { return x.transform.position.x.CompareTo(y.transform.position.x); });
            Vector3 pos = _obstacle.transform.position;
            _obstacle.transform.position = new Vector3(obChildren[0].transform.position.x+(obChildren[obChildren.Count - 1].transform.position.x - obChildren[0].transform.position.x) / 2f, 
                                                        pos.y, pos.z);
            BoxCollider2D col = _obstacle.GetComponent<BoxCollider2D>();
            col.size = new Vector2((obChildren[obChildren.Count - 1].transform.position - obChildren[0].transform.position).x + gridSize.x, col.size.y);
            ObstacleScript obstacleScript = _obstacle.GetComponent<ObstacleScript>();
            if (obstacleScript != null)
            {
                obstacleScript.HitData.hitSize = new Vector2(col.size.x, col.size.y);
            }

            //И засунем в объект дочерние объекты обратно
            foreach (GameObject obj in obChildren)
            {
                obj.transform.parent = _obstacle.transform;
            }
        }

        /// <summary>
        /// Разделить препятствие на два
        /// </summary>
        static void SeparateObstacle(GameObject _obstacle, GameObject separator)
        {
            //Сначала вытащим все дочерние объекты из объекта
            List<GameObject> obChildren = new List<GameObject>();
            for (int i = _obstacle.transform.childCount - 1; i >= 0; i--)
            {
                obChildren.Add(_obstacle.transform.GetChild(i).gameObject);
            }
            _obstacle.transform.DetachChildren();
            GameObject obstacle1 = Instantiate(_obstacle,_obstacle.transform.position,_obstacle.transform.rotation) as GameObject;
            obstacle1.transform.parent = _obstacle.transform.parent;

            foreach (GameObject obj in obChildren)
            {
                if (obj.transform.position.x < separator.transform.position.x)
                    obj.transform.parent = _obstacle.transform;
                else if (obj.transform.position.x > separator.transform.position.x)
                    obj.transform.parent = obstacle1.transform;
            }
            CorrectObstacle(_obstacle);
            CorrectObstacle(obstacle1);
            DestroyImmediate(separator);
        }

        /// <summary>
        /// Объединить два препятствия
        /// </summary>
        static void CombineObstacles(GameObject obstacle1, GameObject obstacle2)
        {
            //Сначала вытащим все дочерние объекты из объектов
            List<GameObject> obChildren = new List<GameObject>();
            for (int i = obstacle1.transform.childCount - 1; i >= 0; i--)
            {
                obChildren.Add(obstacle1.transform.GetChild(i).gameObject);
            }
            for (int i = obstacle2.transform.childCount - 1; i >= 0; i--)
            {
                obChildren.Add(obstacle2.transform.GetChild(i).gameObject);
            }
            obstacle1.transform.DetachChildren();
            obstacle2.transform.DetachChildren();
            foreach (GameObject obj in obChildren)
            {
                obj.transform.parent = obstacle1.transform;
            }
            CorrectObstacle(obstacle1);
            DestroyImmediate(obstacle2);
        }

        #endregion //obstacles

        #region usualMod

        /// <summary>
        /// Обычная отрисовка
        /// </summary>
        static void UsualHandler()
        {
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && (currentSprites != null))
            {
                Camera camera = SceneView.currentDrawingSceneView.camera;

                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = camera.pixelHeight - mousePos.y;
                Vector3 mouseWorldPos = camera.ScreenPointToRay(mousePos).origin;
                mouseWorldPos.z = zPosition;
                if (gridSize.x > 0.05f && gridSize.y > 0.05f)
                {
                    mouseWorldPos.x = Mathf.Floor(mouseWorldPos.x / gridSize.x) * gridSize.x + gridSize.x / 2.0f;
                    mouseWorldPos.y = Mathf.Ceil(mouseWorldPos.y / gridSize.y) * gridSize.y - gridSize.y / 2.0f;
                }
                Ray ray = camera.ScreenPointToRay(mouseWorldPos);

                Vector2 pos = gridSize;
                string spName = LayerMask.LayerToName(spriteLayer);

                if (overpaint || !Physics2D.Raycast(mouseWorldPos, Vector2.down, gridSize.y * .45f, LayerMask.GetMask(spName)))
                {
                    if (overpaint && Physics2D.Raycast(mouseWorldPos, Vector2.down, gridSize.y * .45f, LayerMask.GetMask(spName)))
                    {
                        GameObject dObject = Physics2D.Raycast(mouseWorldPos, Vector2.down, gridSize.y * .45f, LayerMask.GetMask(spName)).collider.gameObject;
                        DestroyImmediate(dObject);
                    }
                    Sprite loadedSprite = currentSprites[Random.Range(0, currentSprites.Count)];
                    if (spriteParentObjName != string.Empty ? parentObj == null : false)
                    {
                        parentObj = new GameObject(spriteParentObjName);
                        parentObj.transform.position = mouseWorldPos;
                    }
                    string spriteName = (parentObj != null) ? parentObj.name + "0" : "sprite";
                    GameObject newSprite = new GameObject(spriteName, typeof(SpriteRenderer));
                    newSprite.transform.position = mouseWorldPos;
                    newSprite.GetComponent<SpriteRenderer>().sprite = loadedSprite;
                    newSprite.GetComponent<SpriteRenderer>().sortingLayerName = sortingLayer;
                    newSprite.tag = tagName;
                    newSprite.layer = spriteLayer;
                    if (parentObj != null)
                        newSprite.transform.parent = parentObj.transform;
                    BoxCollider2D col = newSprite.AddComponent<BoxCollider2D>();
                    if (!hasCollider)
                        newSprite.AddComponent<EditorCollider>();
                    col.isTrigger = isTrigger;
                }
            }
        }

        #endregion //usualMod

        #region lightObstacles

        /// <summary>
        /// Определение коллайдеров препятствий света по нажатию кнопкой мыши на коллайдер земли
        /// </summary>
        static void LightPointHandler()
        {
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (e.type == EventType.keyDown)
            {
                if (e.keyCode == KeyCode.R)//Удалить последние созданные препятствия света
                {
                    foreach (GameObject obj in lightObstacles)
                        DestroyImmediate(obj);
                    lightObstacles.Clear();
                }
            }
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Camera camera = SceneView.currentDrawingSceneView.camera;

                //Шаг 1: укажем на твёрдый объект (находящийся на слое ground) 

                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = camera.pixelHeight - mousePos.y;
                Vector3 mouseWorldPos = camera.ScreenPointToRay(mousePos).origin;
                mouseWorldPos.z = zPosition;
                if (gridSize.x > 0.05f && gridSize.y > 0.05f)
                {
                    mouseWorldPos.x = Mathf.Floor(mouseWorldPos.x / lightPointerPrecision) * lightPointerPrecision;
                    mouseWorldPos.y = Mathf.Ceil(mouseWorldPos.y / lightPointerPrecision) * lightPointerPrecision;
                }
                Ray ray = camera.ScreenPointToRay(mouseWorldPos);

                string lName = LayerMask.LayerToName(groundLayer), olName = LayerMask.LayerToName(lightObstacleLayer);

                if (Physics2D.Raycast(mouseWorldPos, Vector2.right, lightPointerPrecision, LayerMask.GetMask(lName)) &&
                    !Physics2D.Raycast(mouseWorldPos, Vector2.right, lightPointerPrecision, LayerMask.GetMask(olName)))
                {

                    lightObstacles.Clear();
                    //Шаг 2: Поймём откуда и в какую сторону начать обход

                    Vector3 currentPoint = mouseWorldPos;
                    while (Physics2D.Raycast(currentPoint, Vector2.right, lightPointerPrecision, LayerMask.GetMask(lName)))
                        currentPoint += Vector3.right * lightPointerPrecision;
                    Collider2D col = Physics2D.Raycast(currentPoint + Vector3.left * lightPointerPrecision / 2f,
                        Vector2.left, lightPointerPrecision, LayerMask.GetMask(lName)).collider;
                    Vector2[] colPoints = GetColliderPoints(col);
                    if (colPoints == null)
                        return;
                    float colDistance = Mathf.Infinity;
                    int index = 0;
                    Vector3 nextPoint = Vector3.zero;
                    for (int i = 0; i < colPoints.Length; i++)
                    {
                        Vector2 colPoint = colPoints[i];
                        if (Vector2.Distance(colPoint, currentPoint) < colDistance && 
                            !Physics2D.Raycast(currentPoint, (colPoint-(Vector2)currentPoint).normalized,
                            Mathf.Clamp(Vector2.Distance(colPoint,currentPoint)-lightPointerPrecision,0f, Mathf.Infinity), LayerMask.GetMask(lName)))
                        {
                            colDistance = Vector2.Distance(colPoint, currentPoint);
                            nextPoint = colPoint;
                            index = i;
                        }
                    }
                    currentPoint = new Vector3(nextPoint.x, nextPoint.y, currentPoint.z);
                    Vector2 endPoint = colPoints[index < colPoints.Length - 1 ? index + 1 : 0];
                    Vector2 movingDirection = (endPoint - (Vector2)currentPoint).normalized;
                    Vector2 colDirection = new Vector2(1, 0);
                    if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(colDirection, movingDirection)), 1f))
                        colDirection = new Vector2(0, 1);
                    else
                        colDirection = (colDirection - Vector2.Dot(colDirection, movingDirection) * movingDirection).normalized;
                    if (colDirection.x * movingDirection.y - colDirection.y * movingDirection.x > 0f)
                        colDirection *= -1;
                    if (Vector2.Dot(colDirection, Vector2.left) <= 0)
                    {
                        colDirection *= -1;
                        movingDirection *= -1;
                    }
                    if (Physics2D.Raycast(currentPoint - (Vector3)colDirection * lightPointerPrecision / 2f,
                        movingDirection, lightPointerPrecision, LayerMask.GetMask(lName)) ||
                        !Physics2D.Raycast(currentPoint - (Vector3)colDirection * lightPointerPrecision / 2f,
                        colDirection, lightPointerPrecision, LayerMask.GetMask(lName)) ||
                        Vector2.Dot(colDirection, Vector2.left) <= 0)
                    {
                        endPoint = colPoints[index > 0 ? index - 1 : colPoints.Length - 1];
                        movingDirection = (endPoint - (Vector2)currentPoint).normalized;
                        colDirection = new Vector2(1, 0);
                        if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(colDirection, movingDirection)), 1f))
                            colDirection = new Vector2(0, 1);
                        else
                            colDirection = (colDirection - Vector2.Dot(colDirection, movingDirection) * movingDirection).normalized;
                        if (colDirection.x * movingDirection.y - colDirection.y * movingDirection.x > 0f)
                            colDirection *= -1;
                        if (Vector2.Dot(colDirection, Vector2.left) <= 0)
                        {
                            movingDirection *= -1;
                        }
                    }

                    //Шаг 3: Получим контур, что обрамляет указанный твёрдый объект
                    List<Vector3> allPoints = new List<Vector3>();
                    allPoints.Add(GetNextLightObstaclePoint(currentPoint,ref movingDirection));
                    while (allPoints.Count == 1 || Mathf.Abs(Vector3.Distance(allPoints[allPoints.Count - 1],allPoints[0]))>lightPointerPrecision)
                        allPoints.Add(GetNextLightObstaclePoint(allPoints[allPoints.Count - 1], ref movingDirection));
                    allPoints.RemoveAt(allPoints.Count - 1);//Удалим последнюю точку, замыкающую контур

                    //Шаг 4: Обработаем этот контур для некоторых световых эффектов
                    if (createMargin)
                        allPoints = GetContourWithMargin(allPoints);//Добавить контуру некоторый отступ от края

                    //Шаг 5: рарежем контур на более мелкие части и создадим из мелких контуров коллайдеры
                    if (sliceObstacle)
                        SliceAndCreateLightObstacles(allPoints, mouseWorldPos);
                    else
                    {
                        CreateLightObstacle(allPoints, mouseWorldPos);
                    }
                }
            }
        }

        /// <summary>
        /// Функция, что возвращает следующую точку контура, обрамляющую твёрдый объект 
        /// </summary>
        /// <returns>Точка, составляющая контур твёрдого объекта</returns>
        static Vector3 GetNextLightObstaclePoint(Vector3 currentPoint , ref Vector2 movingDirection)
        {

            string lName = LayerMask.LayerToName(groundLayer);

            //Определить вектор, что направлен в сторону препятствий (считаем, что обход происходит в положительном направлении)
            Vector2 colDirection = new Vector2(1, 0);
            if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(colDirection, movingDirection)), 1f))
                colDirection = new Vector2(0, 1);
            else
                colDirection = (colDirection - Vector2.Dot(colDirection, movingDirection) * movingDirection).normalized;
            if (colDirection.x * movingDirection.y - colDirection.y * movingDirection.x > 0f)
                colDirection *= -1;

            currentPoint += (Vector3)movingDirection * lightPointerPrecision;
            while (!Physics2D.Raycast(currentPoint - (Vector3)colDirection * lightPointerPrecision / 2f, movingDirection, lightPointerPrecision, LayerMask.GetMask(lName)) &&
                Physics2D.Raycast(currentPoint - (Vector3)colDirection * lightPointerPrecision / 2f, colDirection, lightPointerPrecision, LayerMask.GetMask(lName)))
            {
                currentPoint += (Vector3)movingDirection * lightPointerPrecision;
            }
            //Если во время обхода дошли до препятствия или возможности обхода, то рассматриваем все коллайдеры, расположенные рядом и щем в них точку, 
            //направляясь к которой можно продолжать обход
            Collider2D[] cols = Physics2D.OverlapAreaAll((Vector2)currentPoint + new Vector2(-1, 1) * lightPointerPrecision,
                                                        (Vector2)currentPoint + new Vector2(1, -1) * lightPointerPrecision, LayerMask.GetMask(lName));
            Vector2 newDirection = Vector2.zero;
            int colIndex = 0;
            Vector2 prevDirection = movingDirection;
            while ((colIndex < cols.Length) && (newDirection == Vector2.zero))
            { 
                Vector2[] colPoints = GetColliderPoints(cols[colIndex]);
                if (colPoints != null)
                {
                    Vector2 nextPoint = Vector2.zero;
                    float distance = Mathf.Infinity;
                    int index = 0;
                    for (int i = 0; i < colPoints.Length; i++)
                    {
                        Vector2 colPoint = colPoints[i];
                        if (Vector2.Distance(colPoint, currentPoint) < distance)
                        {
                            distance = Vector2.Distance(colPoint, currentPoint);
                            nextPoint = colPoint;
                            index = i;
                        }
                    }
                    //Проверим следующую точку коллайдера и возможность перемещения в её сторону
                    if (Mathf.Approximately(Mathf.Abs(Vector2.Dot((nextPoint - (Vector2)currentPoint).normalized, movingDirection)), 1f))
                        currentPoint = nextPoint;
                    Vector2 endPoint = colPoints[index < colPoints.Length - 1 ? index + 1 : 0];
                    newDirection = (endPoint - (Vector2)currentPoint).normalized;
                    colDirection = new Vector2(1, 0);
                    if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(colDirection, newDirection)), 1f))
                        colDirection = new Vector2(0, 1);
                    else
                        colDirection = (colDirection - Vector2.Dot(colDirection, newDirection) * newDirection).normalized;
                    if (colDirection.x * newDirection.y - colDirection.y * newDirection.x > 0f)
                        colDirection *= -1;
                    if (Physics2D.Raycast(currentPoint - (Vector3)colDirection * lightPointerPrecision / 2f+(Vector3)newDirection*lightPointerPrecision/5f,
                        newDirection, lightPointerPrecision, LayerMask.GetMask(lName)) ||
                        !Physics2D.Raycast(currentPoint - (Vector3)colDirection * lightPointerPrecision / 2f + (Vector3)newDirection * lightPointerPrecision / 5f,
                        colDirection, lightPointerPrecision, LayerMask.GetMask(lName)) ||
                        (Mathf.Approximately(Mathf.Abs(Vector2.Dot(prevDirection, newDirection)), 1f)))
                    {
                        //Проверим, возможно ли перемещение к другой точке коллайдера
                        //Если невозможно, то рассматривам другой коллайдер
                        endPoint = colPoints[index > 0 ? index - 1 : colPoints.Length - 1];
                        newDirection = (endPoint - (Vector2)currentPoint).normalized;
                        colDirection = new Vector2(1, 0);
                        if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(colDirection, newDirection)), 1f))
                            colDirection = new Vector2(0, 1);
                        else
                            colDirection = (colDirection - Vector2.Dot(colDirection, newDirection) * newDirection).normalized;
                        if (colDirection.x * newDirection.y - colDirection.y * newDirection.x > 0f)
                            colDirection *= -1;
                        if (Physics2D.Raycast(currentPoint - (Vector3)colDirection * lightPointerPrecision / 2f + (Vector3)newDirection * lightPointerPrecision / 5f,
                            newDirection, lightPointerPrecision, LayerMask.GetMask(lName)) ||
                            !Physics2D.Raycast(currentPoint - (Vector3)colDirection * lightPointerPrecision / 2f + (Vector3)newDirection * lightPointerPrecision / 5f,
                            colDirection, lightPointerPrecision, LayerMask.GetMask(lName)) ||
                            (Mathf.Approximately(Mathf.Abs(Vector2.Dot(prevDirection, newDirection)), 1f)))
                        {
                            newDirection = Vector2.zero;
                        }
                    }
                }
                colIndex++;
            }
            if (newDirection != Vector2.zero)
            {
                movingDirection = newDirection;
                return currentPoint;
            }
            movingDirection = Vector2.zero;
            return Vector3.zero;
        }

        /// <summary>
        /// Функция, возвращающая граничный точки простого коллайдера
        /// </summary>
        /// <param name="коллайдер"></param>
        /// <returns></returns>
        static Vector2[] GetColliderPoints(Collider2D col)
        {
            if (col is PolygonCollider2D)
            {
                Vector2[] points = ((PolygonCollider2D)col).points;
                for (int i = 0; i < points.Length; i++)
                    points[i] = (Vector2)col.transform.TransformPoint((Vector3)points[i]);
                return points;
            }
            else if (col is BoxCollider2D)
            {
                BoxCollider2D bCol = (BoxCollider2D)col;
                float angle = Mathf.Repeat(bCol.transform.eulerAngles.z, 90f) * Mathf.PI / 180f;

                Vector2 e = bCol.bounds.extents;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                float cos2 = Mathf.Cos(2 * angle);
                float b = 2 * (e.x * sin - e.y * cos) / -cos2;

                Vector3 b1 = new Vector3(e.x - b * sin, e.y),
                        b2 = new Vector3(e.x, e.y - b * cos);

                Transform bTrans = bCol.transform;
                Vector3 vect = bCol.transform.position;
                Vector2[] points = new Vector2[] {vect+b1, vect+b2,vect-b1,vect-b2};
                return points;
            }
            return null;
        }

        /// <summary>
        /// Вернуть уменьшенный контур
        /// </summary>
        /// <param name="Контур, который нужно изменить"></param>
        /// <returns></returns>
        static List<Vector3> GetContourWithMargin(List<Vector3> contour)
        {
            List<Vector3> contourWithMargin = new List<Vector3>();
            string lName = LayerMask.LayerToName(groundLayer);

            for (int i = 0; i < contour.Count; i++)
            {
                bool vertical = false;//Булева переменная на случай вертикальной прямой
                Vector2 beginPoint = Vector2.zero;
                float verticalX = 0f;

                //Сдвинем одну прямую
                int prevIndex = i >0 ? i - 1 : contour.Count-1;
                Vector2 direction = ((Vector2)(contour[i] - contour[prevIndex])).normalized;
                Vector2 normalDirection = new Vector2(1, 0);
                if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(normalDirection, direction)), 1f))
                    normalDirection = new Vector2(0, 1);
                else
                    normalDirection = (normalDirection - Vector2.Dot(normalDirection, direction) * direction).normalized;
                if (normalDirection.x * direction.y - normalDirection.y * direction.x > 0f)
                    normalDirection *= -1;
                Vector2 a0 = (Vector2)contour[prevIndex] + normalDirection * lightMarginOffset;
                Vector2 a1 = (Vector2)contour[i] + normalDirection * lightMarginOffset;
                if (Mathf.Approximately(a1.x, a0.x))
                {
                    vertical = true;
                    verticalX = a1.x;
                }

                //А потом вторую
                int nextIndex = i < contour.Count-1 ? i + 1 : 0;
                direction = ((Vector2)(contour[nextIndex] - contour[i])).normalized;
                normalDirection = new Vector2(1, 0);
                if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(normalDirection, direction)), 1f))
                    normalDirection = new Vector2(0, 1);
                else
                    normalDirection = (normalDirection - Vector2.Dot(normalDirection, direction) * direction).normalized;
                if (normalDirection.x * direction.y - normalDirection.y * direction.x > 0f)
                    normalDirection *= -1;
                Vector2 a2 = (Vector2)contour[i] + normalDirection * lightMarginOffset;
                Vector2 a3 = (Vector2)contour[nextIndex] + normalDirection * lightMarginOffset;
                if (vertical)//Первая прямая вертикальна (тогда 2-я нет (из=за устройства программы линии не могут быть параллельны))
                {
                    contourWithMargin.Add(new Vector3(verticalX, a3.y + (a2.y - a3.y) / (a2.x - a3.x) * (verticalX - a3.x), zPosition));
                    continue;
                }
                else if (Mathf.Approximately(a2.x, a3.x))//Если же вторая прямая вертикальна
                {
                    contourWithMargin.Add(new Vector3(a2.x, a0.y + (a1.y - a0.y) / (a1.x - a0.x) * (a2.x - a0.x), zPosition));
                    continue;
                }

                else //Найдём пересечение этих прямых и добавим в новый контур
                {
                    float intersectionX = (a2.y - a0.y - (a3.y - a2.y) / (a3.x - a2.x) * a2.x + (a1.y - a0.y) /
                                                            (a1.x - a0.x) * a0.x) / ((a1.y - a0.y) / (a1.x - a0.x) - (a3.y - a2.y) / (a3.x - a2.x));
                    float intersectionY = a0.y + (a1.y - a0.y) / (a1.x - a0.x) * (intersectionX - a0.x);
                    contourWithMargin.Add(new Vector3(intersectionX, intersectionY, zPosition));
                }
            }

            return contourWithMargin;
        }

        /// <summary>
        /// Разрезать прямоугольниками весь контур и создать новые объекты с коллайдерами
        /// </summary>
        static void SliceAndCreateLightObstacles(List<Vector3> contour, Vector3 lPosition)
        {
            //Найдём прямоугольник, что вмещает в себя данный контур
            float minX = Mathf.Infinity, minY = Mathf.Infinity, maxX = Mathf.NegativeInfinity, maxY = Mathf.NegativeInfinity;
            foreach (Vector3 cPoint in contour)
            {
                if (cPoint.x < minX) minX = cPoint.x;
                if (cPoint.x > maxX) maxX = cPoint.x;
                if (cPoint.y < minY) minY = cPoint.y;
                if (cPoint.y > maxY) maxY = cPoint.y;
            }

            int hLinesCount = Mathf.CeilToInt((maxY - minY) / maxLightObstacleSize.x);
            int vLinesCount = Mathf.CeilToInt((maxX - minX) / maxLightObstacleSize.y);

            //Найдём пересечения с горизонтальными линиями
            for (int i = 1; i <= hLinesCount; i++)
            {
                float offsetY = minY + i * maxLightObstacleSize.y;
                for (int j = 0; j < contour.Count; j++)
                {
                    int nextIndex = j < contour.Count - 1 ? j + 1 : 0;
                    Vector3 beginPoint = contour[j], endPoint = contour[nextIndex];
                    if ((beginPoint.y - offsetY) * (endPoint.y - offsetY) < 0f)
                    {
                        contour.Insert(j, new Vector3(beginPoint.x +
                                                (endPoint.x - beginPoint.x) / (endPoint.y - beginPoint.y) * (endPoint.y - offsetY),
                                                offsetY, beginPoint.z));
                        j++;
                    }
                }
            }

            //Затем найдём пересечения с вертикальными линиями
            for (int i = 1; i <= vLinesCount; i++)
            {
                float offsetX = minX + i * maxLightObstacleSize.x;
                for (int j = 0; j < contour.Count; j++)
                {
                    int nextIndex = j < contour.Count - 1 ? j + 1 : 0;
                    Vector3 beginPoint = contour[j], endPoint = contour[nextIndex];
                    if ((beginPoint.x - offsetX) * (endPoint.x - offsetX) < 0f)
                    {
                        contour.Insert(j, new Vector3(offsetX,
                                                beginPoint.y + (endPoint.y - beginPoint.y) / (endPoint.x - beginPoint.x) * (endPoint.x - offsetX),
                                                beginPoint.z));
                        j++;
                    }
                }
            }

            //Список всех разрезанных контуров
            List<List<List<LightObstacleSlicePoint>>> allSlicedContours = new List<List<List<LightObstacleSlicePoint>>>();
            for (int i = 0; i <= vLinesCount; i++)
            {
                List<List<LightObstacleSlicePoint>> columnSlicedContours = new List<List<LightObstacleSlicePoint>>();
                for (int j = 0; j <= hLinesCount; j++)
                    columnSlicedContours.Add(new List<LightObstacleSlicePoint>());
                allSlicedContours.Add(columnSlicedContours);
            }
            for (int i = 0; i < contour.Count; i++)
            {
                float deltaX = (contour[i].x - minX);
                float deltaY = (contour[i].y - minY);
                //Находится ли точка на вертикальной линии разреза
                bool onBorderX = Mathf.Approximately(deltaX - Mathf.RoundToInt(deltaX / maxLightObstacleSize.x) * maxLightObstacleSize.x, 0);
                //Находится ли точка на горизонтальной линии разреза
                bool onBorderY = Mathf.Approximately(deltaY - Mathf.RoundToInt(deltaY / maxLightObstacleSize.y) * maxLightObstacleSize.y, 0);

                // Случай, когда точка находится на одной из границ
                bool onBorder = onBorderX || onBorderY;

                if (!onBorder)
                    allSlicedContours[Mathf.CeilToInt(deltaX / maxLightObstacleSize.x)][Mathf.CeilToInt(deltaY / maxLightObstacleSize.y)].Add(
                        new LightObstacleSlicePoint(contour[i], i));
                else
                {
                    int prevIndexX = -1;
                    int prevIndexY = -1;

                    #region downLeft

                    int indexX = Mathf.CeilToInt(deltaX - maxLightObstacleSize.x / 10f);
                    int indexY = Mathf.CeilToInt(deltaY - maxLightObstacleSize.y / 10f);
                    if (indexX != -1 && indexX != vLinesCount && indexY != -1 && indexY != hLinesCount)
                    {
                        if (indexX != prevIndexX || indexY != prevIndexY)
                        {
                            allSlicedContours[indexX][indexY].Add(new LightObstacleSlicePoint(contour[i], i));
                            prevIndexX = indexX;
                            prevIndexY = indexY;
                        }
                    }

                    #endregion //downLeft

                    #region downRight

                    indexX = Mathf.CeilToInt(deltaX + maxLightObstacleSize.x / 10f);
                    if (indexX != -1 && indexX != vLinesCount && indexY != -1 && indexY != hLinesCount)
                    {
                        if (indexX != prevIndexX || indexY != prevIndexY)
                        {
                            allSlicedContours[indexX][indexY].Add(new LightObstacleSlicePoint(contour[i], i));
                            prevIndexX = indexX;
                            prevIndexY = indexY;
                        }
                    }

                    #endregion //downRight

                    #region upRight

                    indexY = Mathf.CeilToInt(deltaY + maxLightObstacleSize.y / 10f);
                    if (indexX != -1 && indexX != vLinesCount && indexY != -1 && indexY != hLinesCount)
                    {
                        if (indexX != prevIndexX || indexY != prevIndexY)
                        {
                            allSlicedContours[indexX][indexY].Add(new LightObstacleSlicePoint(contour[i], i));
                            prevIndexX = indexX;
                            prevIndexY = indexY;
                        }
                    }

                    #endregion //upRight

                    #region upLeft

                    indexX = Mathf.CeilToInt(deltaX + maxLightObstacleSize.x / 10f);
                    if (indexX != -1 && indexX != vLinesCount && indexY != -1 && indexY != hLinesCount)
                    {
                        if (indexX != prevIndexX || indexY != prevIndexY)
                        {
                            allSlicedContours[indexX][indexY].Add(new LightObstacleSlicePoint(contour[i], i));
                            prevIndexX = indexX;
                            prevIndexY = indexY;
                        }
                    }

                    #endregion //upLeft

                }
            }

            //Постобработка разрезанных контуров и создание коллайдеров
            for (int i = 0; i <= vLinesCount; i++)
                for (int j = 0; j <= hLinesCount; j++)
                {
                    List<LightObstacleSlicePoint> slicedContour = allSlicedContours[i][j];
                    for (int k = 0; k < slicedContour.Count; k++)
                    {
                        LightObstacleSlicePoint beginPoint = slicedContour[k];
                        LightObstacleSlicePoint endPoint = slicedContour[k < slicedContour.Count - 1 ? k + 1 : 0];
                        int index = beginPoint.index;
                        int nextIndex = index < contour.Count - 1 ? index + 1 : 0;
                        if (!(nextIndex == endPoint.index || beginPoint.position.x == endPoint.position.x || beginPoint.position.y == endPoint.position.y))
                        {
                            slicedContour.Insert(k+1, new LightObstacleSlicePoint(new Vector3(Mathf.Max(beginPoint.position.x, endPoint.position.x),
                                                                          Mathf.Max(beginPoint.position.y, endPoint.position.y),
                                                                          zPosition)));
                            k++;
                        }
                        if (slicedContour.Count>0)
                            CreateLightObstacle(slicedContour.ConvertAll<Vector3>(x => x.position), lPosition);
                    }
                }

        }

        /// <summary>
        /// Создать препятствие света по контуру
        /// </summary>
        static void CreateLightObstacle(List<Vector3> contour, Vector3 lPosition)
        {
            GameObject newLObstacle = new GameObject(lightObstacleName);
            newLObstacle.transform.position = lPosition;
            newLObstacle.tag = tagName;
            newLObstacle.layer = lightObstacleLayer;

            List<Vector3> newContour = new List<Vector3>();
            for (int i = 0; i < contour.Count; i++)
            {
                newContour.Add(newLObstacle.transform.InverseTransformPoint(contour[i]));
            }
            PolygonCollider2D col = newLObstacle.AddComponent<PolygonCollider2D>();
            col.isTrigger = true;
            col.points = newContour.ConvertAll(x=>(Vector2)x).ToArray();

            if (parentObj == null && lightObstacleParentObjName != string.Empty)
            {
                parentObj = new GameObject(lightObstacleParentObjName);
                parentObj.transform.position = lPosition;
            }
            if (parentObj != null)
                newLObstacle.transform.parent = parentObj.transform;
            lightObstacles.Add(newLObstacle);
        }

        #endregion //lightObstacles

        #endregion //draw

        /// <summary>
        /// Что можно сделать на сцене в режиме перетаскивания
        /// </summary>
        static void DragHandler()
        {
        }

        #region erase

        /// <summary>
        /// Что можно сделать на сцене в режиме стирания
        /// </summary>
        static void EraseHandler()
        {
            Event hotkey_e = Event.current;

            if (isEnable)
            {

                switch (drawMod)
                {
                    case DrawModEnum.ground:
                        {
                            GroundErase();
                            break;
                        }
                    case DrawModEnum.plant:
                        {
                            UsualErase();
                            break;
                        }
                    case DrawModEnum.water:
                        {
                            WaterErase();
                            break;
                        }
                    case DrawModEnum.ladder:
                        {
                            UsualErase();
                            break;
                        }
                    case DrawModEnum.spikes:
                        {
                            ObstacleErase();
                            break;
                        }
                    case DrawModEnum.usual:
                        {
                            UsualErase();
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Функция стирания земных поверхностей
        /// </summary>
        static void GroundErase()
        {
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0)
            {
                Camera camera = SceneView.currentDrawingSceneView.camera;

                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = camera.pixelHeight - mousePos.y;
                Vector3 mouseWorldPos = camera.ScreenPointToRay(mousePos).origin;
                mouseWorldPos.z = zPosition;
                if (gridSize.x > 0.05f && gridSize.y > 0.05f)
                {
                    mouseWorldPos.x = Mathf.Floor(mouseWorldPos.x / gridSize.x) * gridSize.x + gridSize.x / 2.0f;
                    mouseWorldPos.y = Mathf.Ceil(mouseWorldPos.y / gridSize.y) * gridSize.y - gridSize.y / 2.0f;
                }
                Ray ray = camera.ScreenPointToRay(mouseWorldPos);

                Vector2 pos = gridSize;

                Collider2D col = null;
                if ((col = Physics2D.Raycast(mouseWorldPos, Vector2.down, Mathf.Min(gridSize.x, gridSize.y) / 4f, LayerMask.GetMask(LayerMask.LayerToName (eraseLayer))).collider)!=null)
                {
                    GameObject eraseGround = col.gameObject;
                    DestroyImmediate(eraseGround);
                    GameObject[] groundBlocks = new GameObject[8];
                    RaycastHit2D hit = new RaycastHit2D();
                    groundBlocks[0] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(-gridSize.x, gridSize.y * 1.1f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(LayerMask.LayerToName(eraseLayer)))) ? hit.collider.gameObject : null;
                    groundBlocks[1] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(0f, gridSize.y * 1.1f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(LayerMask.LayerToName(eraseLayer)))) ? hit.collider.gameObject : null;
                    groundBlocks[2] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(gridSize.x, gridSize.y * 1.1f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(LayerMask.LayerToName(eraseLayer)))) ? hit.collider.gameObject : null;
                    groundBlocks[3] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(-gridSize.x, gridSize.y * 0.1f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(LayerMask.LayerToName(eraseLayer)))) ? hit.collider.gameObject : null;
                    groundBlocks[4] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(gridSize.x, gridSize.y * 0.1f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(LayerMask.LayerToName(eraseLayer)))) ? hit.collider.gameObject : null;
                    groundBlocks[5] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(-gridSize.x, gridSize.y * -0.9f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(LayerMask.LayerToName(eraseLayer)))) ? hit.collider.gameObject : null;
                    groundBlocks[6] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(0f, gridSize.y * -0.9f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(LayerMask.LayerToName(eraseLayer)))) ? hit.collider.gameObject : null;
                    groundBlocks[7] = (hit = Physics2D.Raycast(mouseWorldPos + new Vector3(gridSize.x, gridSize.y * -0.9f, 0f), Vector2.down, gridSize.x / 4f, LayerMask.GetMask(LayerMask.LayerToName(eraseLayer)))) ? hit.collider.gameObject : null;

                    for (int i = 0; i < 8; i++)
                        if (groundBlocks[i] != null)
                            CorrectGround(groundBlocks[i]);
                }
            }
        }

        /// <summary>
        /// Функция, стирания уровня воды
        /// </summary>
        static void WaterErase()
        {
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && (wBrush != null ? !wBrush.Incomplete : false))
            {
                Camera camera = SceneView.currentDrawingSceneView.camera;

                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = camera.pixelHeight - mousePos.y;
                Vector3 mouseWorldPos = camera.ScreenPointToRay(mousePos).origin;
                mouseWorldPos.z = zPosition;
                if (gridSize.x > 0.05f && gridSize.y > 0.05f)
                {
                    mouseWorldPos.x = Mathf.Floor(mouseWorldPos.x / gridSize.x) * gridSize.x + gridSize.x / 2.0f;
                    mouseWorldPos.y = Mathf.Ceil(mouseWorldPos.y / gridSize.y) * gridSize.y - gridSize.y / 2.0f;
                }
                Ray ray = camera.ScreenPointToRay(mouseWorldPos);

                Vector2 pos = gridSize;
                int wLayer = waterLayer;
                string wlName = LayerMask.LayerToName(eraseLayer);

                bool b1 = (!Physics2D.Raycast(mouseWorldPos + Vector3.up * gridSize.y / 10f, Vector2.up, gridSize.y / 4f, LayerMask.GetMask(wlName)));
                bool b2 = (!Physics2D.Raycast(mouseWorldPos + Vector3.right * gridSize.x / 10f, Vector2.right, gridSize.x / 4f, LayerMask.GetMask(wlName)));
                bool b3 = (!Physics2D.Raycast(mouseWorldPos + Vector3.down * gridSize.y / 10f, Vector2.down, gridSize.y / 4f, LayerMask.GetMask(wlName)));
                bool b4 = (!Physics2D.Raycast(mouseWorldPos + Vector3.left * gridSize.x / 10f, Vector2.left, gridSize.x / 4f, LayerMask.GetMask(wlName)));

                if (!b1 || !b2 || !b3 || !b4)
                {
                    while (Physics2D.Raycast(mouseWorldPos + Vector3.up * gridSize.y * 0.1f, Vector2.down, gridSize.y / 2f, LayerMask.GetMask(wlName)))
                    {
                        List<Vector2> waterBorder = GetWaterAreaByPoint(mouseWorldPos);
                        if (waterBorder != null ? waterBorder.Count > 0 : false)
                            ReduceWaterLevel(waterBorder);
                        else
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Функция стирания препятствия
        /// </summary>
        static void ObstacleErase()
        {
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0)
            {
                Camera camera = SceneView.currentDrawingSceneView.camera;

                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = camera.pixelHeight - mousePos.y;
                Vector3 mouseWorldPos = camera.ScreenPointToRay(mousePos).origin;
                mouseWorldPos.z = zPosition;
                if (gridSize.x > 0.05f && gridSize.y > 0.05f)
                {
                    mouseWorldPos.x = Mathf.Floor(mouseWorldPos.x / gridSize.x) * gridSize.x + gridSize.x / 2.0f;
                    mouseWorldPos.y = Mathf.Ceil(mouseWorldPos.y / gridSize.y) * gridSize.y - gridSize.y / 2.0f;
                }
                Ray ray = camera.ScreenPointToRay(mouseWorldPos);

                Vector2 pos = gridSize;

                GameObject obstacle = null;

                Collider2D col = null;
                string elName = LayerMask.LayerToName(eraseLayer);
                if ((col = Physics2D.Raycast(mouseWorldPos, Vector2.down, gridSize.y*.45f, LayerMask.GetMask(elName)).collider) != null)
                {
                    obstacle = col.gameObject;
                }
                else if ((col = Physics2D.Raycast(mouseWorldPos, Vector2.up, gridSize.y*.45f, LayerMask.GetMask(elName)).collider) != null)
                {
                    obstacle = col.gameObject;
                }
                if (obstacle!= null)
                {
                    List<GameObject> obChildren=new List<GameObject>();
                    for (int i = 0; i < obstacle.transform.childCount; i++)
                    {
                        obChildren.Add(obstacle.transform.GetChild(i).gameObject);
                    }
                    for (int i = 0; i < obChildren.Count; i++)
                    {
                        if (Mathf.Approximately(mouseWorldPos.x, obChildren[i].transform.position.x))
                        {
                            if ((i != 0) && (i != obChildren.Count - 1))
                            {
                                SeparateObstacle(obstacle, obChildren[i]);
                            }
                            else
                                DestroyImmediate(obChildren[i]);
                            CorrectObstacle(obstacle);
                            obChildren.RemoveAt(i);
                            break;
                        }
                    }
                    if (obChildren.Count == 0)
                    {
                        DestroyImmediate(obstacle);
                    }
                }
            }
        }

        /// <summary>
        /// Функция стирания спрайтов
        /// </summary>
        static void UsualErase()
        {
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0)
            {
                Camera camera = SceneView.currentDrawingSceneView.camera;

                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = camera.pixelHeight - mousePos.y;
                Vector3 mouseWorldPos = camera.ScreenPointToRay(mousePos).origin;
                mouseWorldPos.z = zPosition;
                if (gridSize.x > 0.05f && gridSize.y > 0.05f)
                {
                    mouseWorldPos.x = Mathf.Floor(mouseWorldPos.x / gridSize.x) * gridSize.x + gridSize.x / 2.0f;
                    mouseWorldPos.y = Mathf.Ceil(mouseWorldPos.y / gridSize.y) * gridSize.y - gridSize.y / 2.0f;
                }
                Ray ray = camera.ScreenPointToRay(mouseWorldPos);

                Vector2 pos = gridSize;

                Collider2D col = null;
                if ((col = Physics2D.Raycast(mouseWorldPos, Vector2.down, Mathf.Min(gridSize.x, gridSize.y) / 4f, LayerMask.GetMask(LayerMask.LayerToName(eraseLayer))).collider) != null)
                {
                    GameObject eraseGround = col.gameObject;
                    DestroyImmediate(eraseGround);
                }
            }
        }

        #endregion //erase

        [DrawGizmo(GizmoType.NotInSelectionHierarchy)]
        static void RenderCustomGrid(Transform objectTransform, GizmoType gizmoType)
        {
            if (isGrid && isEnable)
            {
                Gizmos.color = Color.white;
                Vector3 minGrid = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(new Vector2(0f, 0f)).origin;
                Vector3 maxGrid = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(new Vector2(SceneView.currentDrawingSceneView.camera.pixelWidth, SceneView.currentDrawingSceneView.camera.pixelHeight)).origin;
                for (float i = Mathf.Round(minGrid.x / gridSize.x) * gridSize.x; i < Mathf.Round(maxGrid.x / gridSize.x) * gridSize.x && gridSize.x > 0.05f; i += gridSize.x)
                    Gizmos.DrawLine(new Vector3(i, minGrid.y, 0.0f), new Vector3(i, maxGrid.y, 0.0f));
                for (float j = Mathf.Round(minGrid.y / gridSize.y) * gridSize.y; j < Mathf.Round(maxGrid.y / gridSize.y) * gridSize.y && gridSize.y > 0.05f; j += gridSize.y)
                    Gizmos.DrawLine(new Vector3(minGrid.x, j, 0.0f), new Vector3(maxGrid.x, j, 0.0f));
                SceneView.RepaintAll();
            }
        }
        
    }

    /// <summary>
    /// Отрисовка окна редактора уровней
    /// </summary>
    void OnGUI()
    {
        textureStyle = new GUIStyle(GUI.skin.button);
        textureStyle.margin = new RectOffset(2, 2, 2, 2);
        textureStyle.normal.background = null;
        textureStyleAct = new GUIStyle(textureStyle);
        textureStyleAct.margin = new RectOffset(0, 0, 0, 0);
        textureStyleAct.normal.background = textureStyle.active.background;

        EditorGUILayout.BeginHorizontal(textureStyleAct);
        {

            for (int i=0;i<4;i++)
            {
                Sprite[] iconSprites = { selectIcon, drawIcon, dragIcon, eraseIcon };
                Sprite currentSprite = iconSprites[i];
                if (editorMod == (EditorModEnum)i)
                {
                    GUILayout.Button("", textureStyleAct, GUILayout.Width(currentSprite.textureRect.width + 6), GUILayout.Height(currentSprite.textureRect.height + 4));
                    GUI.DrawTextureWithTexCoords(new Rect(GUILayoutUtility.GetLastRect().x + 3f,
                                                          GUILayoutUtility.GetLastRect().y + 2f,
                                                          GUILayoutUtility.GetLastRect().width - 6f,
                                                          GUILayoutUtility.GetLastRect().height - 4f),
                                                 currentSprite.texture,
                                                 new Rect(currentSprite.textureRect.x / (float)currentSprite.texture.width,
                             currentSprite.textureRect.y / (float)currentSprite.texture.height,
                             currentSprite.textureRect.width / (float)currentSprite.texture.width,
                             currentSprite.textureRect.height / (float)currentSprite.texture.height));
                }
                else
                {
                    if (GUILayout.Button("", textureStyle, GUILayout.Width(currentSprite.textureRect.width + 2), GUILayout.Height(currentSprite.textureRect.height + 2)))
                        editorMod= (EditorModEnum)i;
                    GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), currentSprite.texture,
                                                 new Rect(currentSprite.textureRect.x / (float)currentSprite.texture.width,
                                                             currentSprite.textureRect.y / (float)currentSprite.texture.height,
                                                             currentSprite.textureRect.width / (float)currentSprite.texture.width,
                                                             currentSprite.textureRect.height / (float)currentSprite.texture.height));
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        
        GUILayout.BeginHorizontal();
        {
            isGrid = EditorGUILayout.Toggle(isGrid, GUILayout.Width(16));
            gridSize = EditorGUILayout.Vector2Field("Grid Size (0.05 minimum)", gridSize, GUILayout.Width(236));
        }
       GUILayout.EndHorizontal();

        switch (editorMod)
        {
            case EditorModEnum.select:
                {
                    OnSelectGUI();
                    break;
                }
            case EditorModEnum.draw:
                {
                    OnDrawGUI();
                    break;
                }
            case EditorModEnum.drag:
                {
                    OnDragGUI();
                    break;
                }
            case EditorModEnum.erase:
                {
                    OnEraseGUI();
                    break;
                }
        }

    }

    /// <summary>
    /// Что выводится на окне редактора при режиме выбора
    /// </summary>
    void OnSelectGUI()
    {
    }

    #region drawGUI

    /// <summary>
    /// Что выводится на окне редактора при режиме рисования
    /// </summary>
    void OnDrawGUI()
    {
        EditorGUILayout.BeginHorizontal(textureStyleAct);
        {
            Sprite[] drawIconSprites = { groundIcon, plantIcon, waterIcon, ladderIcon, spikesIcon,usualDrawIcon, lightPointIcon };
            for (int i = 0; i < 7; i++)
            {
                Sprite currentDrawSprite = drawIconSprites[i];
                if (drawMod == (DrawModEnum)i)
                {
                    GUILayout.Button("", textureStyleAct, GUILayout.Width(currentDrawSprite.textureRect.width + 6), GUILayout.Height(currentDrawSprite.textureRect.height + 4));
                    GUI.DrawTextureWithTexCoords(new Rect(GUILayoutUtility.GetLastRect().x + 3f,
                                                          GUILayoutUtility.GetLastRect().y + 2f,
                                                          GUILayoutUtility.GetLastRect().width - 6f,
                                                          GUILayoutUtility.GetLastRect().height - 4f),
                                                 currentDrawSprite.texture,
                                                 new Rect(currentDrawSprite.textureRect.x / (float)currentDrawSprite.texture.width,
                             currentDrawSprite.textureRect.y / (float)currentDrawSprite.texture.height,
                             currentDrawSprite.textureRect.width / (float)currentDrawSprite.texture.width,
                             currentDrawSprite.textureRect.height / (float)currentDrawSprite.texture.height));
                }
                else
                {
                    if (GUILayout.Button("", textureStyle, GUILayout.Width(currentDrawSprite.textureRect.width + 2), GUILayout.Height(currentDrawSprite.textureRect.height + 2)))
                        drawMod = (DrawModEnum)i;
                    GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), currentDrawSprite.texture,
                                                 new Rect(currentDrawSprite.textureRect.x / (float)currentDrawSprite.texture.width,
                                                             currentDrawSprite.textureRect.y / (float)currentDrawSprite.texture.height,
                                                             currentDrawSprite.textureRect.width / (float)currentDrawSprite.texture.width,
                                                             currentDrawSprite.textureRect.height / (float)currentDrawSprite.texture.height));
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        switch (drawMod)
        {
            case DrawModEnum.ground:
                {
                    GroundDrawGUI();
                    break;
                }
            case DrawModEnum.plant:
                {
                    PlantDrawGUI();
                    break;
                }
            case DrawModEnum.water:
                {
                    WaterDrawGUI();
                    break;
                }
            case DrawModEnum.ladder:
                {
                    LadderDrawGUI();
                    break;
                }
            case DrawModEnum.spikes:
                {
                    ObstaclesDrawGUI();
                    break;
                }
            case DrawModEnum.usual:
                {
                    UsualDrawGUI();
                    break;
                }
            case DrawModEnum.lightObstacle:
                {
                    LightPointGUI();
                    break;
                }
        }

    }

    #region groundBrushGUI

    void GroundDrawGUI()
    {
        tagName = EditorGUILayout.TagField("tag", tagName);
        groundLayer = EditorGUILayout.LayerField("ground layer", groundLayer);
        groundCollider = EditorGUILayout.Toggle("ground has collider?", groundCollider);

        EditorGUILayout.BeginHorizontal();
        if (sLayerIndex >= sortingLayers.Length)
        {
            sLayerIndex = 0;
        }
        EditorGUILayout.LabelField("sorting layer");
        sLayerIndex = EditorGUILayout.Popup(sLayerIndex, sortingLayers);
        sortingLayer = sortingLayers[sLayerIndex];
        EditorGUILayout.EndHorizontal();

        zPosition = EditorGUILayout.FloatField("z-position", zPosition);
        grParentObjName = EditorGUILayout.TextField("parent name", grParentObjName);
        if (grParentObjName != string.Empty && (parentObj != null? parentObj.name!=grParentObjName: true))
        {
            parentObj = GameObject.Find(grParentObjName);
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        {
            groundBrushName=EditorGUILayout.TextField(groundBrushName);
            if (GUILayout.Button("Create new ground brush"))
            {
                if (groundBrushName != string.Empty)
                {
                    grBrush = new GroundBrush();
                    grBrush.brushName = groundBrushName;
                    grBrush.Incomplete = true;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        groundBrushScrollPos = EditorGUILayout.BeginScrollView(groundBrushScrollPos);

        if (grBrush != null ? grBrush.Incomplete : false)
        {
            CreateNewGroundBrushWindow();

        }
        else
        {
            drawScrollPos = EditorGUILayout.BeginScrollView(drawScrollPos);
            EditorGUILayout.BeginHorizontal();
            float ctr = maxCtr;
            foreach (GroundBrush gBrush in groundBrushes)
            {
                Sprite gBrushImage = gBrush.outGround;
                if (ctr < gBrushImage.textureRect.x)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    ctr = maxCtr;
                }
                ctr -= gBrushImage.textureRect.x;
                if (grBrush == gBrush)
                {
                    GUILayout.Button("", textureStyleAct, GUILayout.Width(gBrushImage.textureRect.width + 6), GUILayout.Height(gBrushImage.texture.height + 4));
                    GUI.DrawTextureWithTexCoords(new Rect(GUILayoutUtility.GetLastRect().x + 3f,
                                                          GUILayoutUtility.GetLastRect().y + 2f,
                                                          GUILayoutUtility.GetLastRect().width - 6f,
                                                          GUILayoutUtility.GetLastRect().height - 4f),
                                                 gBrushImage.texture,
                                                 new Rect(gBrushImage.textureRect.x / (float)gBrushImage.texture.width,
                                                            gBrushImage.textureRect.y / (float)gBrushImage.texture.height,
                                                            gBrushImage.textureRect.width / (float)gBrushImage.texture.width,
                                                            gBrushImage.textureRect.height / (float)gBrushImage.texture.height));
                }
                else
                {
                    if (GUILayout.Button("", textureStyle, GUILayout.Width(gBrushImage.textureRect.width + 2), GUILayout.Height(gBrushImage.textureRect.height + 2)))
                        grBrush = gBrush;
                    GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), gBrushImage.texture,
                                                 new Rect(gBrushImage.textureRect.x / (float)gBrushImage.texture.width,
                                                             gBrushImage.textureRect.y / (float)gBrushImage.texture.height,
                                                             gBrushImage.textureRect.width / (float)gBrushImage.texture.width,
                                                             gBrushImage.textureRect.height / (float)gBrushImage.texture.height));
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndScrollView();
        SceneView.RepaintAll();
    }

    void CreateNewGroundBrushWindow()
    {

        grBrush.defGround = (Sprite)EditorGUILayout.ObjectField("default ground",grBrush.defGround, typeof(Sprite));
        grBrush.outGround = (Sprite)EditorGUILayout.ObjectField("out ground",grBrush.outGround, typeof(Sprite));
        grBrush.inAngleGround = (Sprite)EditorGUILayout.ObjectField("inner angle ground",grBrush.inAngleGround, typeof(Sprite));
        grBrush.outAngleGround = (Sprite)EditorGUILayout.ObjectField("outer angle ground",grBrush.outAngleGround, typeof(Sprite));
        grBrush.edgeGround = (Sprite)EditorGUILayout.ObjectField("edge ground",grBrush.edgeGround, typeof(Sprite));
        grBrush.marginGround = (Sprite)EditorGUILayout.ObjectField("margin ground",grBrush.marginGround, typeof(Sprite));
        grBrush.inGround = (Sprite)EditorGUILayout.ObjectField("inner ground",grBrush.inGround, typeof(Sprite));
        grBrush.angleGround = (Sprite)EditorGUILayout.ObjectField("45 angle ground",grBrush.angleGround, typeof(Sprite));

        if (GUILayout.Button("CreateBrush"))
        {
            if (grBrush.defGround && grBrush.outGround && grBrush.inAngleGround && grBrush.outAngleGround && grBrush.edgeGround && grBrush.marginGround && grBrush.inGround && grBrush.angleGround)
            grBrush.Incomplete = false;
            AssetDatabase.CreateAsset(grBrush, groundBrushesPath + grBrush.brushName + ".asset");
            AssetDatabase.SaveAssets();
            OnEnable();
        }

    }

    #endregion //groundDrawGUI

    /// <summary>
    /// Меню отрисовки растений
    /// </summary>
    void PlantDrawGUI()
    {

        if (currentPlants == null)
        {
            currentPlants = new List<Sprite>();
        }

        tagName = EditorGUILayout.TagField("tag", tagName);
        plantLayer = EditorGUILayout.LayerField("plant layer", plantLayer);
        groundLayer = EditorGUILayout.LayerField("ground layer", groundLayer);

        EditorGUILayout.BeginHorizontal();
        if (sLayerIndex >= sortingLayers.Length)
        {
            sLayerIndex = 0;
        }
        EditorGUILayout.LabelField("sorting layer");
        sLayerIndex = EditorGUILayout.Popup(sLayerIndex, sortingLayers);
        sortingLayer = sortingLayers[sLayerIndex];
        EditorGUILayout.EndHorizontal();

        zPosition = EditorGUILayout.FloatField("z-position", zPosition);
        plantParentObjName = EditorGUILayout.TextField("parent name", plantParentObjName);
        if (plantParentObjName != string.Empty && (parentObj != null ? parentObj.name != plantParentObjName : true))
        {
            parentObj = GameObject.Find(plantParentObjName);
        }

        plantOffset = EditorGUILayout.FloatField("plant offset", plantOffset);

        drawScrollPos = EditorGUILayout.BeginScrollView(drawScrollPos);
        EditorGUILayout.BeginHorizontal();
        float ctr = maxCtr;
        if (plantBase.plants == null)
        {
            plantBase.plants = new List<Sprite>();
        }
        foreach (Sprite plant in plantBase.plants)
        {
            if (ctr < plant.textureRect.x)
            {
                GUILayout.EndHorizontal();
                EditorGUILayout.Space(); 
                GUILayout.BeginHorizontal();
                ctr = maxCtr;
            }
            ctr -= plant.textureRect.x;
            if (currentPlants.Contains(plant))
            {
                if (GUILayout.Button("", textureStyleAct, GUILayout.Width(plant.textureRect.width + 6), GUILayout.Height(plant.texture.height + 4)))
                    currentPlants.Remove(plant);
                GUI.DrawTextureWithTexCoords(new Rect(GUILayoutUtility.GetLastRect().x + 3f,
                                                      GUILayoutUtility.GetLastRect().y + 2f,
                                                      GUILayoutUtility.GetLastRect().width - 6f,
                                                      GUILayoutUtility.GetLastRect().height - 4f),
                                             plant.texture,
                                             new Rect(plant.textureRect.x / (float)plant.texture.width,
                                                        plant.textureRect.y / (float)plant.texture.height,
                                                        plant.textureRect.width / (float)plant.texture.width,
                                                        plant.textureRect.height / (float)plant.texture.height));
            }
            else
            {
                if (GUILayout.Button("", textureStyle, GUILayout.Width(plant.textureRect.width + 2), GUILayout.Height(plant.textureRect.height + 2)))
                    currentPlants.Add(plant);
                GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), plant.texture,
                                             new Rect(plant.textureRect.x / (float)plant.texture.width,
                                                         plant.textureRect.y / (float)plant.texture.height,
                                                         plant.textureRect.width / (float)plant.texture.width,
                                                         plant.textureRect.height / (float)plant.texture.height));
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        {
            nextPlant = (Sprite)EditorGUILayout.ObjectField("new object", nextPlant, typeof(Sprite));

            EditorGUILayout.BeginVertical();
            {
                if (GUILayout.Button("Add"))
                {
                    if (nextPlant != null)
                        if (!plantBase.plants.Contains(nextPlant))
                        {
                            plantBase.plants.Add(nextPlant);
                            currentPlants.Add(nextPlant);
                            nextPlant = null;
                        }
                }
                if (GUILayout.Button("Delete"))
                {
                    for (int i = currentPlants.Count - 1; i >= 0; i--)
                    {
                        plantBase.plants.Remove(currentPlants[i]);
                        currentPlants.RemoveAt(i);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
        }
        EditorGUILayout.EndHorizontal();
        plantBase.SetDirty();
    }

    #region waterGUI

    /// <summary>
    /// Меню отрисовки воды
    /// </summary>
    void WaterDrawGUI()
    {
        tagName = EditorGUILayout.TagField("tag", tagName);
        groundLayer = EditorGUILayout.LayerField("ground layer", groundLayer);
        waterLayer = EditorGUILayout.LayerField("water layer", waterLayer);

        EditorGUILayout.BeginHorizontal();
        if (sLayerIndex >= sortingLayers.Length)
        {
            sLayerIndex = 0;
        }
        EditorGUILayout.LabelField("sorting layer");
        sLayerIndex = EditorGUILayout.Popup(sLayerIndex, sortingLayers);
        sortingLayer = sortingLayers[sLayerIndex];
        EditorGUILayout.EndHorizontal();

        zPosition = EditorGUILayout.FloatField("z-position", zPosition);
        maxWaterHeight = EditorGUILayout.FloatField("max water height",maxWaterHeight);
        maxWaterWidth = EditorGUILayout.FloatField("max water width", maxWaterWidth);

        waterParentObjName = EditorGUILayout.TextField("parent name", waterParentObjName);
        if (waterParentObjName != string.Empty && (parentObj != null ? parentObj.name != waterParentObjName : true))
        {
            parentObj = GameObject.Find(waterParentObjName);
        }

        waterMaterial = (Material)EditorGUILayout.ObjectField(waterMaterial, typeof(Material));

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        {
            waterBrushName = EditorGUILayout.TextField(waterBrushName);
            if (GUILayout.Button("Create new water brush"))
            {
                if (waterBrushName != string.Empty)
                {
                    wBrush = new WaterBrush();
                    wBrush.brushName = waterBrushName;
                    wBrush.Incomplete = true;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        groundBrushScrollPos = EditorGUILayout.BeginScrollView(groundBrushScrollPos);

        if (wBrush != null ? wBrush.Incomplete : false)
        {
            CreateNewWaterBrushWindow();
        }
        else
        {
            drawScrollPos = EditorGUILayout.BeginScrollView(drawScrollPos);
            EditorGUILayout.BeginHorizontal();
            float ctr = maxCtr;
            foreach (WaterBrush waterBrush in waterBrushes)
            {
                Sprite wBrushImage = waterBrush.waterSprite;
                if (ctr < wBrushImage.textureRect.x)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    ctr = maxCtr;
                }
                ctr -= wBrushImage.textureRect.x;
                if (wBrush == waterBrush)
                {
                    GUILayout.Button("", textureStyleAct, GUILayout.Width(wBrushImage.textureRect.width + 6), GUILayout.Height(wBrushImage.texture.height + 4));
                    GUI.DrawTextureWithTexCoords(new Rect(GUILayoutUtility.GetLastRect().x + 3f,
                                                          GUILayoutUtility.GetLastRect().y + 2f,
                                                          GUILayoutUtility.GetLastRect().width - 6f,
                                                          GUILayoutUtility.GetLastRect().height - 4f),
                                                 wBrushImage.texture,
                                                 new Rect(wBrushImage.textureRect.x / (float)wBrushImage.texture.width,
                                                            wBrushImage.textureRect.y / (float)wBrushImage.texture.height,
                                                            wBrushImage.textureRect.width / (float)wBrushImage.texture.width,
                                                            wBrushImage.textureRect.height / (float)wBrushImage.texture.height));
                }
                else
                {
                    if (GUILayout.Button("", textureStyle, GUILayout.Width(wBrushImage.textureRect.width + 2), GUILayout.Height(wBrushImage.textureRect.height + 2)))
                        wBrush = waterBrush;
                    GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), wBrushImage.texture,
                                                 new Rect(wBrushImage.textureRect.x / (float)wBrushImage.texture.width,
                                                             wBrushImage.textureRect.y / (float)wBrushImage.texture.height,
                                                             wBrushImage.textureRect.width / (float)wBrushImage.texture.width,
                                                             wBrushImage.textureRect.height / (float)wBrushImage.texture.height));
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndScrollView();
        SceneView.RepaintAll();
    }

    /// <summary>
    /// Создать новую водную кисть
    /// </summary>
    void CreateNewWaterBrushWindow()
    {
        GameObject newWaterObject = null;
        wBrush.waterSprite = (Sprite)EditorGUILayout.ObjectField("Water sprite", wBrush.waterSprite, typeof(Sprite));
        wBrush.waterAngleSprite = (Sprite)EditorGUILayout.ObjectField("Water angle sprite", wBrush.waterAngleSprite, typeof(Sprite));

        if (wBrush.waterObjects == null)
            wBrush.waterObjects = new List<GameObject>();
        foreach (GameObject waterObject in wBrush.waterObjects)
            EditorGUILayout.ObjectField(waterObject, typeof(GameObject));
        newWaterObject = (GameObject)EditorGUILayout.ObjectField(newWaterObject, typeof(GameObject));
        if (newWaterObject)
            wBrush.waterObjects.Add(newWaterObject);
        if (GUILayout.Button("CreateBrush"))
        {
            if (wBrush.waterSprite && wBrush.waterAngleSprite)
                wBrush.Incomplete = false;
            AssetDatabase.CreateAsset(wBrush, waterBrushesPath + wBrush.brushName + ".asset");
            AssetDatabase.SaveAssets();
            OnEnable();
        }

    }

    #endregion //waterGUI

    #region lightPoint

    /// <summary>
    /// Меню cоздания коллайдеров препятствий для света
    /// </summary>
    void LightPointGUI()
    {
        tagName = EditorGUILayout.TagField("tag", tagName);
        groundLayer = EditorGUILayout.LayerField("ground layer", groundLayer);
        lightObstacleLayer = EditorGUILayout.LayerField("light obstacle layer", lightObstacleLayer);

        zPosition = EditorGUILayout.FloatField("z-position", zPosition);

        EditorGUILayout.BeginHorizontal();
        createMargin = EditorGUILayout.Toggle("Create margin", createMargin);
        lightMarginOffset = EditorGUILayout.FloatField("light obstacle margin", lightMarginOffset);
        EditorGUILayout.EndHorizontal();

        lightPointerPrecision = EditorGUILayout.FloatField("pointer precision", lightPointerPrecision);
        if (lightPointerPrecision > lightMarginOffset)
            lightPointerPrecision = lightMarginOffset;

        sliceObstacle = EditorGUILayout.Toggle("Slice Obstacle?", sliceObstacle);
        maxLightObstacleSize = EditorGUILayout.Vector2Field("light obstacle max size", maxLightObstacleSize);

        lightObstacleName = EditorGUILayout.TextField("light obstacle name", lightObstacleName);
        lightObstacleParentObjName = EditorGUILayout.TextField("parent name", lightObstacleParentObjName);
        if (lightObstacleParentObjName != string.Empty && (parentObj != null ? parentObj.name != lightObstacleParentObjName : true))
        {
            parentObj = GameObject.Find(lightObstacleParentObjName);
        }       
        SceneView.RepaintAll();

    }

    #endregion //lightPoint

    #region ladderGUI

    /// <summary>
    /// Меню отрисовки лестниц
    /// </summary>
    void LadderDrawGUI()
    {

        ladderTag = EditorGUILayout.TagField("tag", ladderTag);
        ladderLayer = EditorGUILayout.LayerField("ladder layer",  ladderLayer);
        groundLayer = EditorGUILayout.LayerField("ground layer", groundLayer);

        EditorGUILayout.BeginHorizontal();
        if (sLayerIndex >= sortingLayers.Length)
        {
            sLayerIndex = 0;
        }
        EditorGUILayout.LabelField("sorting layer");
        sLayerIndex = EditorGUILayout.Popup(sLayerIndex, sortingLayers);
        sortingLayer = sortingLayers[sLayerIndex];
        EditorGUILayout.EndHorizontal();

        zPosition = EditorGUILayout.FloatField("z-position", zPosition);
        ladderParentObjName = EditorGUILayout.TextField("parent name", ladderParentObjName);
        if (ladderParentObjName != string.Empty && (parentObj != null ? parentObj.name != ladderParentObjName : true))
        {
            parentObj = GameObject.Find(ladderParentObjName);
        }

        isLiana = EditorGUILayout.Toggle("is liana?", isLiana);

        drawScrollPos = EditorGUILayout.BeginScrollView(drawScrollPos);
        EditorGUILayout.BeginHorizontal();
        float ctr = maxCtr;
        if (ladderBase.ladders == null)
        {
            ladderBase.ladders = new List<GameObject>();
        }
        foreach (GameObject ladder in ladderBase.ladders)
        {
            if (ladder.GetComponent<SpriteRenderer>() == null)
                continue;
            Sprite ladderSprite = ladder.GetComponent<SpriteRenderer>().sprite;
            Rect textRect = ladderSprite.textureRect;
            Texture2D texture = ladderSprite.texture;
            if (ctr < ladderSprite.textureRect.x)
            {
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                ctr = maxCtr;
            }
            ctr -= ladderSprite.textureRect.x;
            if (currentLadder==ladder)
            {
                if (GUILayout.Button("", textureStyleAct, GUILayout.Width(textRect.width + 6), GUILayout.Height(textRect.height + 4)))
                { }
                GUI.DrawTextureWithTexCoords(new Rect(GUILayoutUtility.GetLastRect().x + 3f,
                                                      GUILayoutUtility.GetLastRect().y + 2f,
                                                      GUILayoutUtility.GetLastRect().width - 6f,
                                                      GUILayoutUtility.GetLastRect().height - 4f),
                                             texture,
                                             new Rect(textRect.x / (float)texture.width,
                                                        textRect.y / (float)texture.height,
                                                        textRect.width / texture.width,
                                                        textRect.height / texture.height));
            }
            else
            {
                if (GUILayout.Button("", textureStyle, GUILayout.Width(textRect.width + 2), GUILayout.Height(textRect.height + 2)))
                    currentLadder=ladder;
                GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), texture,
                                             new Rect(textRect.x / (float)texture.width,
                                                         textRect.y / (float)texture.height,
                                                         textRect.width / (float)texture.width,
                                                         textRect.height / (float)texture.height));
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("new ladder", GUILayout.Width(75f));
            nextLadder = (GameObject)EditorGUILayout.ObjectField(nextLadder,typeof(GameObject), GUILayout.Width(150f));

            EditorGUILayout.BeginVertical();
            {
                if (GUILayout.Button("Add"))
                {
                    if (nextLadder != null? nextLadder.GetComponent<Collider2D>()!=null && nextLadder.GetComponent<SpriteRenderer>() != null : false)
                        if (!ladderBase.ladders.Contains(nextLadder))
                        {
                            ladderBase.ladders.Add(nextLadder);
                            currentLadder=nextLadder;
                            nextLadder = null;
                        }
                }
                if (GUILayout.Button("Delete"))
                {
                    if (currentLadder != null)
                        ladderBase.ladders.Remove(currentLadder);
                }
            }
            EditorGUILayout.EndHorizontal();

        }
        EditorGUILayout.EndHorizontal();
        ladderBase.SetDirty();
    }

    /// <summary>
    /// Меню отрисовки препятствий
    /// </summary>
    void ObstaclesDrawGUI()
    {

        tagName = EditorGUILayout.TagField("tag", tagName);
        obstacleLayer = EditorGUILayout.LayerField("ladder layer", obstacleLayer);
        groundLayer = EditorGUILayout.LayerField("ground layer", groundLayer);

        EditorGUILayout.BeginHorizontal();
        if (sLayerIndex >= sortingLayers.Length)
        {
            sLayerIndex = 0;
        }
        EditorGUILayout.LabelField("sorting layer");
        sLayerIndex = EditorGUILayout.Popup(sLayerIndex, sortingLayers);
        sortingLayer = sortingLayers[sLayerIndex];
        EditorGUILayout.EndHorizontal();

        zPosition = EditorGUILayout.FloatField("z-position", zPosition);

        obstacleName = EditorGUILayout.TextField("obstacle name", obstacleName);

        obstacleParentName = EditorGUILayout.TextField("obstacle parent name", obstacleParentName);
        if (obstacleParentName != string.Empty && (parentObj != null ? parentObj.name != obstacleParentName : true))
        {
            parentObj = GameObject.Find(obstacleParentName);
        }

        obstacleOffset = EditorGUILayout.FloatField("obstacle offset", obstacleOffset);

        obstacleDamage = EditorGUILayout.FloatField("obstacle damage", obstacleDamage);
        damageBoxSize = EditorGUILayout.FloatField("damage size", damageBoxSize);
        damageBoxOffset= EditorGUILayout.FloatField("damage offset", damageBoxOffset);

        obstacleType = (ObstacleEnum)EditorGUILayout.EnumPopup("obstacle type", obstacleType);

        drawScrollPos = EditorGUILayout.BeginScrollView(drawScrollPos);
        EditorGUILayout.BeginHorizontal();

        float ctr = maxCtr;
        if (obstacleBase.obstacles == null)
        {
            obstacleBase.obstacles = new List<GameObject>();
        }
        foreach (GameObject obstacle in obstacleBase.obstacles)
        {
            if (obstacle.GetComponent<SpriteRenderer>() == null)
                continue;
            Sprite obstacleSprite = obstacle.GetComponent<SpriteRenderer>().sprite;
            Rect textRect = obstacleSprite.textureRect;
            Texture2D texture = obstacleSprite.texture;
            if (ctr < obstacleSprite.textureRect.x)
            {
               GUILayout.EndHorizontal();
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                ctr = maxCtr;
            }
            ctr -= obstacleSprite.textureRect.x;
            if (currentObstacle == obstacle)
            {
                if (GUILayout.Button("", textureStyleAct, GUILayout.Width(textRect.width + 6), GUILayout.Height(textRect.height + 4)))
                { }
                GUI.DrawTextureWithTexCoords(new Rect(GUILayoutUtility.GetLastRect().x + 3f,
                                                      GUILayoutUtility.GetLastRect().y + 2f,
                                                      GUILayoutUtility.GetLastRect().width - 6f,
                                                      GUILayoutUtility.GetLastRect().height - 4f),
                                             texture,
                                             new Rect(textRect.x / (float)texture.width,
                                                        textRect.y / (float)texture.height,
                                                        textRect.width / texture.width,
                                                        textRect.height / texture.height));
            }
            else
            {
                if (GUILayout.Button("", textureStyle, GUILayout.Width(textRect.width + 2), GUILayout.Height(textRect.height + 2)))
                    currentObstacle = obstacle;
                GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), texture,
                                             new Rect(textRect.x / (float)texture.width,
                                                         textRect.y / (float)texture.height,
                                                         textRect.width / (float)texture.width,
                                                         textRect.height / (float)texture.height));
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("new obstacle", GUILayout.Width(75f));
            nextObstacle = (GameObject)EditorGUILayout.ObjectField(nextObstacle, typeof(GameObject), GUILayout.Width(150f));

            EditorGUILayout.BeginVertical();
            {
                if (GUILayout.Button("Add"))
                {
                    if (nextObstacle != null ? nextObstacle.GetComponent<SpriteRenderer>() != null : false)
                        if (!obstacleBase.obstacles.Contains(nextObstacle))
                        {
                            obstacleBase.obstacles.Add(nextObstacle);
                            currentObstacle = nextObstacle;
                            nextObstacle = null;
                        }
                }
                if (GUILayout.Button("Delete"))
                {
                    if (currentObstacle != null)
                        obstacleBase.obstacles.Remove(currentObstacle);
                }
            }
            EditorGUILayout.EndHorizontal();

        }
        EditorGUILayout.EndHorizontal();
        obstacleBase.SetDirty();
    }

    /// <summary>
    /// Меню обычной отрисовки
    /// </summary>
    void UsualDrawGUI()
    {
        if (currentSprites == null)
        {
            currentSprites = new List<Sprite>();
        }

        tagName = EditorGUILayout.TagField("tag", tagName);
        spriteLayer = EditorGUILayout.LayerField("sprite layer", spriteLayer);

        EditorGUILayout.BeginHorizontal();
        if (sLayerIndex >= sortingLayers.Length)
        {
            sLayerIndex = 0;
        }
        EditorGUILayout.LabelField("sorting layer");
        sLayerIndex = EditorGUILayout.Popup(sLayerIndex, sortingLayers);
        sortingLayer = sortingLayers[sLayerIndex];
        EditorGUILayout.EndHorizontal();

        zPosition = EditorGUILayout.FloatField("z-position", zPosition);
        spriteParentObjName = EditorGUILayout.TextField("parent name", spriteParentObjName);
        if (spriteParentObjName != string.Empty && (parentObj != null ? parentObj.name != spriteParentObjName : true))
        {
            parentObj = GameObject.Find(spriteParentObjName);
        }

        hasCollider = EditorGUILayout.Toggle("has collider?", hasCollider);
        isTrigger = EditorGUILayout.Toggle("is trigger?", isTrigger);
        overpaint = EditorGUILayout.Toggle("overpaint", overpaint);

        drawScrollPos = EditorGUILayout.BeginScrollView(drawScrollPos);
        EditorGUILayout.BeginHorizontal();
        float ctr = maxCtr;
        if (spriteBase.sprites == null)
        {
            spriteBase.sprites = new List<Sprite>();
        }
        foreach (Sprite sprite1 in spriteBase.sprites)
        {
            if (ctr < sprite1.textureRect.x)
            {
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                ctr = maxCtr;
            }
            ctr -= sprite1.textureRect.x;
            if (currentSprites.Contains(sprite1))
            {
                if (GUILayout.Button("", textureStyleAct, GUILayout.Width(sprite1.textureRect.width + 6), GUILayout.Height(sprite1.texture.height + 4)))
                    currentSprites.Remove(sprite1);
                GUI.DrawTextureWithTexCoords(new Rect(GUILayoutUtility.GetLastRect().x + 3f,
                                                      GUILayoutUtility.GetLastRect().y + 2f,
                                                      GUILayoutUtility.GetLastRect().width - 6f,
                                                      GUILayoutUtility.GetLastRect().height - 4f),
                                             sprite1.texture,
                                             new Rect(sprite1.textureRect.x / (float)sprite1.texture.width,
                                                        sprite1.textureRect.y / (float)sprite1.texture.height,
                                                        sprite1.textureRect.width / (float)sprite1.texture.width,
                                                        sprite1.textureRect.height / (float)sprite1.texture.height));
            }
            else
            {
                if (GUILayout.Button("", textureStyle, GUILayout.Width(sprite1.textureRect.width + 2), GUILayout.Height(sprite1.textureRect.height + 2)))
                    currentSprites.Add(sprite1);
                GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), sprite1.texture,
                                             new Rect(sprite1.textureRect.x / (float)sprite1.texture.width,
                                                         sprite1.textureRect.y / (float)sprite1.texture.height,
                                                         sprite1.textureRect.width / (float)sprite1.texture.width,
                                                         sprite1.textureRect.height / (float)sprite1.texture.height));
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        {
            nextSprite = (Sprite)EditorGUILayout.ObjectField("new sprite", nextSprite, typeof(Sprite));

            EditorGUILayout.BeginVertical();
            {
                if (GUILayout.Button("Add"))
                {
                    if (nextSprite != null)
                        if (!spriteBase.sprites.Contains(nextSprite))
                        {
                            spriteBase.sprites.Add(nextSprite);
                            currentSprites.Add(nextSprite);
                            nextSprite = null;
                        }
                }
                if (GUILayout.Button("Delete"))
                {
                    for (int i = currentSprites.Count - 1; i >= 0; i--)
                    {
                        spriteBase.sprites.Remove(currentSprites[i]);
                        currentSprites.RemoveAt(i);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

        }
        EditorGUILayout.EndHorizontal();
        spriteBase.SetDirty();
    }

    #endregion //ladderGUI

    #endregion //drawGUI

    /// <summary>
    /// Что выводится на окне редактора при режиме перетаскивания
    /// </summary>
    void OnDragGUI()
    {
    }

    /// <summary>
    /// Что выводится на окне редактора при режиме стирания
    /// </summary>
    void OnEraseGUI()
    {
        EditorGUILayout.BeginHorizontal(textureStyleAct);
        {
            Sprite[] drawIconSprites = { groundIcon, plantIcon, waterIcon, ladderIcon,spikesIcon,usualDrawIcon };
            for (int i = 0; i < 6; i++)
            {
                Sprite currentDrawSprite = drawIconSprites[i];
                if (drawMod == (DrawModEnum)i)
                {
                    GUILayout.Button("", textureStyleAct, GUILayout.Width(currentDrawSprite.textureRect.width + 6), GUILayout.Height(currentDrawSprite.textureRect.height + 4));
                    GUI.DrawTextureWithTexCoords(new Rect(GUILayoutUtility.GetLastRect().x + 3f,
                                                          GUILayoutUtility.GetLastRect().y + 2f,
                                                          GUILayoutUtility.GetLastRect().width - 6f,
                                                          GUILayoutUtility.GetLastRect().height - 4f),
                                                 currentDrawSprite.texture,
                                                 new Rect(currentDrawSprite.textureRect.x / (float)currentDrawSprite.texture.width,
                             currentDrawSprite.textureRect.y / (float)currentDrawSprite.texture.height,
                             currentDrawSprite.textureRect.width / (float)currentDrawSprite.texture.width,
                             currentDrawSprite.textureRect.height / (float)currentDrawSprite.texture.height));
                }
                else
                {
                    if (GUILayout.Button("", textureStyle, GUILayout.Width(currentDrawSprite.textureRect.width + 2), GUILayout.Height(currentDrawSprite.textureRect.height + 2)))
                        drawMod = (DrawModEnum)i;
                    GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), currentDrawSprite.texture,
                                                 new Rect(currentDrawSprite.textureRect.x / (float)currentDrawSprite.texture.width,
                                                             currentDrawSprite.textureRect.y / (float)currentDrawSprite.texture.height,
                                                             currentDrawSprite.textureRect.width / (float)currentDrawSprite.texture.width,
                                                             currentDrawSprite.textureRect.height / (float)currentDrawSprite.texture.height));
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        switch (drawMod)
        {
            case DrawModEnum.ground:
                {
                    eraseLayer = EditorGUILayout.LayerField("erase Mask", eraseLayer);
                    break;
                }
            case DrawModEnum.plant:
                {
                    eraseLayer = EditorGUILayout.LayerField("erase Mask", eraseLayer);
                    break;
                }
            case DrawModEnum.water:
                {
                    eraseLayer = EditorGUILayout.LayerField("erase Mask", eraseLayer);
                    break;
                }
            case DrawModEnum.ladder:
                {
                    eraseLayer = EditorGUILayout.LayerField("erase mask", eraseLayer);
                    break;
                }
            case DrawModEnum.spikes:
                {
                    eraseLayer = EditorGUILayout.LayerField("erase mask", eraseLayer);
                    break;
                }
            case DrawModEnum.usual:
                {
                    eraseLayer = EditorGUILayout.LayerField("erase mask", eraseLayer);
                    break;
                }
        }
    }

}

/// <summary>
/// точка линии разреза для создания более мелкого коллайдера препятствия света
/// </summary>
public class LightObstacleSlicePoint
{
    public Vector3 position;//Где находится точка
    public int index;//Какой точке основного контура соответствует данная точка

    public LightObstacleSlicePoint(Vector3 _position)
    {
        position = _position;
        index = -1;
    }

    public LightObstacleSlicePoint(Vector3 _position, int _index)
    {
        position = _position;
        index = _index;
    }

}