using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Linq;
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
    private static Sprite groundIcon, waterIcon, plantIcon, ladderIcon, spikesIcon, usualDrawIcon;//Иконки, используемые в меню рисования

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

    private static int waterLayer = LayerMask.NameToLayer("Water");
    private static float maxWaterHeight = 100f, maxWaterWidth = 100f;//Максимальные размеры водной области
    private static string waterParentObjName;//Имя объекта, который станет родительским по отношению к создаваемым объектам.

    #endregion //waterBrush

    #region ladderBrush

    private static LadderBase ladderBase;//База данных по лестницам
    private static GameObject currentLadder;//Какая лестница используется в данный момент

    private static int ladderLayer = LayerMask.NameToLayer("ladder");
    private static GameObject nextLadder;
    private static string ladderParentObjName;//Имя объекта, который станет родительским по отношению к создаваемым объектам.
    private static bool isLiana = false;//Мы рисуем лиану или лестницу?

    #endregion //ladderBrush

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
                            break;
                        }
                    case DrawModEnum.spikes:
                        {
                            break;
                        }
                    case DrawModEnum.usual:
                        {
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
                            break;
                        }
                    case DrawModEnum.water:
                        {
                            WaterErase();
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
            Sprite[] drawIconSprites = { groundIcon, plantIcon, waterIcon, ladderIcon, spikesIcon,usualDrawIcon };
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
                    break;
                }
            case DrawModEnum.spikes:
                {
                    break;
                }
            case DrawModEnum.usual:
                {
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
        grParentObjName = EditorGUILayout.TextField("parent name", grParentObjName);
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
        GameObject newWaterObject=null;
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

    #region ladderGUI

    /// <summary>
    /// Меню отрисовки лестниц
    /// </summary>
    void LadderDrawGUI()
    {

        tagName = EditorGUILayout.TagField("tag", tagName);
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
            if (ladder.GetComponent<Sprite>() == null)
                continue;
            Sprite ladderSprite = ladder.GetComponent<Sprite>();
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
            nextLadder = (GameObject)EditorGUILayout.ObjectField("new ladder", nextLadder, typeof(GameObject));

            EditorGUILayout.BeginVertical();
            {
                if (GUILayout.Button("Add"))
                {
                    if (nextLadder != null? nextLadder.GetComponent<Collider2D>()!=null:false)
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

    #endregion //ladderGUI

    /// <summary>
    /// Меню отрисовки шипов и прочих препятствий
    /// </summary>
    void SpikesDrawGUI()
    {

    }

    /// <summary>
    /// Меню отрисовки по узлам сетки
    /// </summary>
    void UsualDrawGUI()
    {

    }

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
            Sprite[] drawIconSprites = { groundIcon, plantIcon, waterIcon };
            for (int i = 0; i < 3; i++)
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
        }
    }

}