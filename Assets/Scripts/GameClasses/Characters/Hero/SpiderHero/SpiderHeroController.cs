using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Контроллер главного героя в обличии паука
/// </summary>
public class SpiderHeroController : HeroController
{

    #region consts

    protected const float spiderOffset = .026f;//Насколько должен быть смещён паук относительно поверхности земли
    protected const float spiderOffsetEps = .01f;//Какая может быть ошибка смещения относительно поверхности?
    protected const float precipiceEps = .01f;//Насколько близко паук должен быть у края поверхности, чтобы перейти на следующую поверхность
    protected const float catchWallTime = .2f;//Как долго паук будет пытаться закрепиться за стену?
    protected const float minAngle = 1f;

    protected const float maxObservationDistance = 10f;
    protected const float maxCeilTime = 10f;//Максимальное время, при котором паук может быть прицеплённым к потолке
    protected const int maxWebUsageTimes = 3;//Максимальное число использований паутины, стоя на потолке

    #endregion //consts  

    #region fields

    protected HeroController originalController;

    #endregion //fields

    #region parametres

    protected int id = 0;
    protected Vector2 spiderOrientation = Vector2.up;//нормаль поверхности, на которой стоит паук
    protected virtual Vector2 SpiderOrientation
    {
        get
        {
            return spiderOrientation;
        }
        set
        {
            spiderOrientation = value;
            rightMovementDirection = GetNormal(spiderOrientation);
            //На какой угол надо повернуть паука
            float angle = Vector2.Angle(Vector2.up, spiderOrientation) * Mathf.Sign(-spiderOrientation.x);

            transform.eulerAngles = new Vector3(0f, 0f, angle);//Повернём паука
            if (spiderOrientation.y < 0 || Mathf.Abs(spiderOrientation.x) > Mathf.Abs(spiderOrientation.y)-.3f)
                rigid.gravityScale = 0f;
            else
                rigid.gravityScale = 1f;
            OnCeil = spiderOrientation.y < -.8f;
            wallCheck.SetPosition(angle / 180f * Mathf.PI, (int)orientation);
            groundCheck.SetPosition(angle / 180f * Mathf.PI, (int)orientation);
        }
    }
    protected Vector2 rightMovementDirection = Vector2.right;//В какую сторону движется паук, если он повёрнут вправо
    protected SurfaceLineClass currentSurface = SurfaceLineClass.zero;
    protected SurfaceLineClass CurrentSurface
    {
        set
        {
            currentSurface = value;
            moveDirection = currentSurface.exists ? currentSurface.GetMoveDirection() : Vector2.right; StopCatchingWall();
            if (value.exists)
                nextSurface = SurfaceLineClass.zero;
        }
    }
    protected SurfaceLineClass nextSurface = SurfaceLineClass.zero;
    protected Vector2 moveDirection;

    protected bool tryingCatchWall = false;//Пытается ли паук закрепиться за стену (например, в полёте)
    protected int webUsageTimes = maxWebUsageTimes;//Сколько раз паук может использовать паутину
    protected bool onWeb = false;//Находится ли паук на паутине в данный момент?
    [SerializeField]
    protected float webSpeed = .4f;//Скорость перемещения по паутине
    public float WebSpeed { get { return webSpeed; } set { webSpeed = value; } }
    protected Vector2 webConnectionPoint = Vector2.zero;

    protected bool onCeil = false;//Находится ли паук на потолке?
    protected bool OnCeil { get { return onCeil; }  set { if (!onCeil && value) StartCoroutine("CeilProcess"); if (!value) StopCeilProcess(); onCeil = value; } }

    #endregion //parametres

    protected override void Update()
    {
        Vector2 pos = transform.position;

        if (immobile)
            goto analyseRegion;

        if (!onWeb)
            goto usualMovement;
        else
            goto onWeb;

    #region usualMovement

    usualMovement:

        if (employment <= 6)
            goto analyseRegion;

        if (Input.GetButton("Horizontal"))
        {
            if (moveDirection.x >= moveDirection.y -.1f)
                Move(Input.GetAxis("Horizontal"));
            if (nextSurface.exists ? nextSurface.normal.y > .1f || nextSurface.normal.y < -.8f : false)
                if (Input.GetAxis("Horizontal")*(nextSurface.GetFarPoint(pos)-nextSurface.GetNearPoint(pos)).x>0f) 
                    ChangeSurface(nextSurface);
        }
        else
        {
            StopCatchingWall();//Если игрок не зажимает клавишу движения, то он не сможет закрепиться за отвесную стену
            if (Input.GetButton("Vertical"))
            {
                if (moveDirection.y > moveDirection.x-.1f)
                    Move(Input.GetAxis("Vertical"));
                if (nextSurface.exists ? nextSurface.normal.y < .1f && nextSurface.normal.y > -.8f : false)
                    if (Input.GetAxis("Vertical") * (nextSurface.GetFarPoint(pos) - nextSurface.GetNearPoint(pos)).y > 0f)
                        ChangeSurface(nextSurface);
            }
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpInput = 0;
            if (groundState == GroundStateEnum.grounded && !jumping)
            {
                Jump();
            }
        }

        if (Input.GetButtonUp("Jump"))
        {
            jumpInput = 0;
        }

        if (Input.GetButtonDown("Attack"))
        {
            if (spiderOrientation.y < -Mathf.Abs(spiderOrientation.x) - .1f && currentSurface.exists)
            {
                if (webUsageTimes>0)
                    WebOn();
            }
            else if (interactor.ReadyForInteraction())
                interactor.Interact();
        }

        goto analyseRegion;

    #endregion //usualMovement

    #region onWeb

    onWeb:
        if (Input.GetButton("Vertical"))
            WebMove(Input.GetAxis("Vertical"));
        else StopWebMove();

        if (Input.GetButtonDown("Jump"))
        {
            jumpInput = 0;
            Jump();
        }

        if (Input.GetButtonDown("Attack"))
            if (Vector2.SqrMagnitude((Vector2)transform.position - webConnectionPoint) < 1.5 * spiderOffset * spiderOffset)
                WebOff();

    #endregion //onWeb

    analyseRegion:
        Analyse();

        if (onWeb)
            Animate(new AnimationEventArgs("webMove"));
        else if (groundState == GroundStateEnum.inAir)
            Animate(new AnimationEventArgs("airMove"));
        else
            Animate(new AnimationEventArgs("groundMove"));
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();

        SpiderOrientation = Vector2.up;
        CurrentSurface = SurfaceLineClass.zero;
        onWeb = false;
        onCeil = false;
        tryingCatchWall = false;
        webUsageTimes = maxWebUsageTimes;
    }

    #region movement

    /// <summary>
    /// Перемещение
    /// </summary>
    protected virtual void Move(float value)
    {
        Vector2 velocity = value * moveDirection * speed * speedCoof;
        Vector2 _moveDirection = moveDirection * Mathf.Sign(value);
        if (rigid.gravityScale == 1f)
        {
            if (wallCheck.WallInFront)
            {
                if (!currentSurface.exists)
                {
                    rigid.velocity = new Vector2(0f, rigid.velocity.y);
                }
            }
            else
                rigid.velocity = new Vector2(velocity.x, rigid.velocity.y);
        }
        else
            rigid.velocity = velocity;

        Turn((OrientationEnum)Mathf.RoundToInt(Vector2.Dot(rightMovementDirection, _moveDirection)));
    }

    /// <summary>
    /// Остановить перемещение
    /// </summary>
    protected override void StopMoving()
    {
        Vector2 projection = Vector2.Dot(rigid.velocity,rightMovementDirection) * rightMovementDirection;
        rigid.velocity -= projection;
    }

    /// <summary>
    /// Начать закрепление за стену
    /// </summary>
    protected void TryCatchWall()
    {
        StartCoroutine("TryCatchWallProcess");
    }

    /// <summary>
    /// Прекратить закрепление за стену
    /// </summary>
    protected void StopCatchingWall()
    {
        tryingCatchWall = false;
        StopCoroutine("TryCatchWallProcess");
    }

    /// <summary>
    /// Процесс закрепления за отвесную стену
    /// </summary>
    protected IEnumerator TryCatchWallProcess()
    {
        tryingCatchWall = true;
        yield return new WaitForSeconds(catchWallTime);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, (int)orientation * rightMovementDirection, spiderOffset * 2f, whatIsGround);
        if (hit)
        {
            ChangeOrientation(hit.collider, true);
        }

    }

    /// <summary>
    /// Слезть с паутины, вернуться на прежнюю поверхность
    /// </summary>
    protected virtual void WebOff()
    {
        onWeb = false;
        transform.position = webConnectionPoint + Vector2.down * spiderOffset;
        SpiderOrientation = spiderOrientation;
        Animate(new AnimationEventArgs("setWebMove", "", 0));
    }

    /// <summary>
    /// Взобраться на паутину
    /// </summary>
    protected virtual void WebOn()
    {
        webUsageTimes--;
        onWeb = true;
        webConnectionPoint = (Vector2)transform.position + Vector2.up * spiderOffset;
        transform.eulerAngles = Vector3.zero;
        Turn(OrientationEnum.right);
        Animate(new AnimationEventArgs("setWebMove", "", 1));
    }

    /// <summary>
    /// Передвижение по паутине
    /// </summary>
    protected virtual void WebMove(float direction)
    {
        rigid.velocity = new Vector2(0f, direction * webSpeed * speedCoof);
    }

    /// <summary>
    /// Остановить передвижение по паутине
    /// </summary>
    protected virtual void StopWebMove()
    {
        rigid.velocity = Vector2.zero;
    }

    /// <summary>
    /// Сменить ориентацию паука и "прикрепить" его к  заданной поверхности
    /// </summary>
    /// <param name="targetCollider">коллайдер, к которому прикрепляется паук</param>
    protected void ChangeOrientation(Collider2D targetCollider, bool considerCurrentOrientation)
    {
        Vector2[] colPoints = GetColliderPoints(targetCollider);

        if (colPoints.Length <= 0)
            return;

        Vector2 connectionPoint = Vector2.zero;

        //Найдём ту сторону коллайдера, которая имеет нормаль, отличную от текущей ориентации паука, и расстояние до которой от текущего положения паука является наименьшим
        float mDistance = Mathf.Infinity;
        int pointIndex = -1;
        for (int i = 0; i < colPoints.Length; i++)
        {
            Vector2 point1 = colPoints[i];
            Vector2 point2 = i < colPoints.Length - 1 ? colPoints[i + 1] : colPoints[0];
            Vector2 normal = GetNormal(point1, point2, targetCollider);
            if (considerCurrentOrientation? Mathf.Abs(Vector2.Angle(spiderOrientation, normal)) >= minAngle: true)
            {
                Vector2 _connectionPoint = GetConnectionPoint(point1, point2, transform.position);
                float newDistance = Vector2.SqrMagnitude(_connectionPoint - (Vector2)transform.position);
                if (newDistance < mDistance)
                {
                    connectionPoint = _connectionPoint;
                    mDistance = newDistance;
                    pointIndex = i;
                }
            }
        }

        if (pointIndex < 0)
            return;

        Vector2 surfacePoint1 = colPoints[pointIndex], surfacePoint2 = colPoints[pointIndex < colPoints.Length - 1 ? pointIndex + 1 : 0];
        Vector2 _spiderOrientation = GetNormal(surfacePoint1, surfacePoint2, targetCollider);

        //Определим, на какой поверхности мы находимся
        surfacePoint1 = ObserveSurfacePoint(surfacePoint1, (surfacePoint1 - surfacePoint2).normalized, _spiderOrientation, targetCollider);
        surfacePoint2 = ObserveSurfacePoint(surfacePoint2, (surfacePoint2 - surfacePoint1).normalized, _spiderOrientation, targetCollider);
        CurrentSurface = new SurfaceLineClass(surfacePoint1, surfacePoint2,_spiderOrientation);

        SpiderOrientation = _spiderOrientation;

        transform.position = connectionPoint + spiderOffset * spiderOrientation;//Расположить паука
        StopMoving();
    }

    /// <summary>
    /// Найти поверхность, на которую можно перейти (но не переходить на неё)
    /// </summary>
    /// <param name="surfacePoint">Точка, откуда ищется следующая поверхность</param>
    protected SurfaceLineClass GetNextSurface(Vector2 _surfacePoint, bool considerCurrentOrientation)
    {
        Collider2D targetCollider = Physics2D.OverlapCircle(_surfacePoint, spiderOffset, whatIsGround);
        if (targetCollider == null)
            return SurfaceLineClass.zero;
        Vector2[] colPoints = GetColliderPoints(targetCollider);

        if (colPoints.Length <= 0)
            return SurfaceLineClass.zero;

        //Найдём ту сторону коллайдера, которая имеет нормаль, отличную от текущей ориентации паука, и расстояние до которой от текущего положения паука является наименьшим
        float mDistance = Mathf.Infinity;
        int pointIndex = -1;
        for (int i = 0; i < colPoints.Length; i++)
        {
            Vector2 point1 = colPoints[i];
            Vector2 point2 = i < colPoints.Length - 1 ? colPoints[i + 1] : colPoints[0];
            Vector2 normal = GetNormal(point1, point2, targetCollider);
            if (considerCurrentOrientation? Mathf.Abs(Vector2.Angle(spiderOrientation, normal)) >= minAngle: true)
            {
                Vector2 _connectionPoint = GetConnectionPoint(point1, point2, _surfacePoint);
                float newDistance = Vector2.SqrMagnitude(_connectionPoint - (Vector2)transform.position);
                if (newDistance < mDistance)
                {
                    mDistance = newDistance;
                    pointIndex = i;
                }
            }
        }

        if (pointIndex < 0)
            return SurfaceLineClass.zero;

        Vector2 surfacePoint1 = colPoints[pointIndex], surfacePoint2 = colPoints[pointIndex < colPoints.Length - 1 ? pointIndex + 1 : 0];
        Vector2 _spiderOrientation = GetNormal(surfacePoint1, surfacePoint2, targetCollider);

        //Определим, на какой поверхности мы находимся
        surfacePoint1 = ObserveSurfacePoint(surfacePoint1, (surfacePoint1 - surfacePoint2).normalized, _spiderOrientation, targetCollider);
        surfacePoint2 = ObserveSurfacePoint(surfacePoint2, (surfacePoint2 - surfacePoint1).normalized, _spiderOrientation, targetCollider);
        return new SurfaceLineClass(surfacePoint1, surfacePoint2, _spiderOrientation);
    }

    /// <summary>
    /// Перейти на следующую поверхность
    /// </summary>
    /// <param name="_surface">Поверхность, на которую происходит переход</param>
    protected virtual void ChangeSurface(SurfaceLineClass _surface)
    {
        Vector2 connectionPoint = GetConnectionPoint(_surface.point1, _surface.point2, transform.position);
        SpiderOrientation = _surface.normal;
        CurrentSurface = _surface;
        transform.position = connectionPoint + spiderOffset * spiderOrientation;//Расположить паука
        StopMoving();
    }

    /// <summary>
    /// Совершить прыжок
    /// </summary>
    protected override void Jump()
    {
        jumping = true;
        if (!onWeb)
            rigid.AddForce(spiderOrientation*jumpForce * (underWater ? waterCoof : 1f));
        StartCoroutine("JumpProcess");
        JumpDown();
    }

    /// <summary>
    /// Процесс самого прыжка
    /// </summary>
    protected override IEnumerator JumpProcess()
    {
        employment = Mathf.Clamp(employment - 2, 0, maxEmployment);
        if (!onWeb && Mathf.Abs(spiderOrientation.x)<spiderOrientation.y)
            jumpInput = 1;
        yield return new WaitForSeconds(jumpInputTime);
        jumpInput = 0;
        yield return new WaitForSeconds(jumpTime);
        employment = Mathf.Clamp(employment + 2, 0, maxEmployment);
        jumping = false;
    }

    /// <summary>
    /// Спрыгнуть с поверхности и вернуть свою изначальную ориентацию
    /// </summary>
    protected virtual void JumpDown()
    {
        rigid.gravityScale = 1f;
        if (onWeb)
        {
            onWeb = false;
            Animate(new AnimationEventArgs("setWebMove", "", 0));
        }
        SpiderOrientation = Vector2.up;
        rightMovementDirection = Vector2.right;
        CurrentSurface = SurfaceLineClass.zero;
        transform.eulerAngles = Vector3.zero;
    }

    /// <summary>
    /// Процесс стояния на потолке
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator CeilProcess()
    {
        yield return new WaitForSeconds(maxCeilTime-3f);
        Animate(new AnimationEventArgs("startCeilBlink"));
        yield return new WaitForSeconds(3f);
        JumpDown();
        onCeil = false;
    }

    /// <summary>
    /// Прекратить стояние на потолке
    /// </summary>
    protected virtual void StopCeilProcess()
    {
        StopCoroutine("CeilProcess");
        Animate(new AnimationEventArgs("stopCeilBlink"));
        webUsageTimes = maxWebUsageTimes;
    }

    #endregion //movement

    /// <summary>
    /// Анализ окружающей персонажа обстановки
    /// </summary>
    protected override void Analyse()
    {
        Vector2 pos = transform.position;
        if (groundCheck.CheckWall())
            groundState = GroundStateEnum.grounded;
        else
            groundState = GroundStateEnum.inAir;

        if ((groundState == GroundStateEnum.grounded))
        {
            if (!currentSurface.exists)
            {
                RaycastHit2D hit = Physics2D.Raycast(groundCheck.transform.position, -spiderOrientation, spiderOffset * 2f, whatIsGround);
                if (hit)
                    ChangeOrientation(hit.collider, false);
            }
            else if (currentSurface.IsNearPoint(pos, spiderOffsetEps / 2f))
            {
                Vector2 nearPoint = currentSurface.GetNearPoint(pos);
                string buttonName = currentSurface.normal.y >= 0.1f || currentSurface.normal.y < -.9f ? "Horizontal" : "Vertical";
                if (Input.GetButton(buttonName) && Vector2.Dot((int)orientation * rightMovementDirection, nearPoint - currentSurface.GetFarPoint(pos)) > 0f)
                    nextSurface = GetNextSurface(nearPoint, true);
            }
            if (fallSpeed > minDamageFallSpeed)
                TakeDamage(Mathf.Round((fallSpeed - minDamageFallSpeed) * damagePerFallSpeed), DamageType.Physical, true, 1);
            if (fallSpeed > minDamageFallSpeed / 10f)
                Animate(new AnimationEventArgs("fall"));
            fallSpeed = 0f;

        }
        else
            fallSpeed = -rigid.velocity.y;

        bool _underWater = Physics2D.OverlapCircle(waterCheck.position, groundRadius, LayerMask.GetMask("Water"));
        if (underWater != _underWater)
        {
            Underwater = _underWater;
        }

    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage, DamageType _dType, int attackPower = 0)
    {
        if (!invul)
        {
            bool stunned = GetBuff("StunnedProcess") != null;
            if (attackPower > balance || stunned)
                JumpDown();
        }
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage, DamageType _dType, bool ignoreInvul, int attackPower = 0)
    {
        if (ignoreInvul || !invul)
        {
            bool stunned = GetBuff("StunnedProcess") != null;
            if (attackPower > balance || stunned)
                JumpDown();
        }
    }

    /// <summary>
    /// Функция, из-за которой управление переходит от обычного контроллера к паучьему контроллеру
    /// </summary>
    protected virtual void BecomeSpider()
    {
        originalController = SpecialFunctions.player.GetComponent<HeroController>();
        SpecialFunctions.SwitchPlayer(this);
        originalController.gameObject.SetActive(false);
    }

    /// <summary>
    /// Вернуться к гуманоидному оригинальному контроллеру
    /// </summary>
    protected virtual void BecomeHuman()
    {
        if (!originalController)
            return;
        originalController.gameObject.SetActive(true);
        SpecialFunctions.SwitchPlayer(originalController);
        gameObject.SetActive(false);
    }

    #region IHaveID

    /// <summary>
    /// Вернуть id персонажа
    /// </summary>
    public override int GetID()
    {
        return id;
    }

    /// <summary>
    /// Установить заданное id
    /// </summary>
    public override void SetID(int _id)
    {
        id = _id;
        BecomeSpider();
    }

    /// <summary>
    /// Настроить персонажа в соответствии с сохранёнными данными
    /// </summary>
    public override void SetData(InterObjData _intObjData)
    {
        BecomeSpider();//Считаем, что если этот паук вообще загружается, то он и становится основным персонажем
    }

    /// <summary>
    /// Вернуть сохраняемые данные персонажа
    /// </summary>
    public override InterObjData GetData()
    {
        return new InterObjData(id,gameObject.name,transform.position);
    }

    #endregion //IHaveID

    #region storyActions

    /// <summary>
    /// Уничтожение объекта в результате скриптового события
    /// </summary>
    public void StoryBecomeHuman(StoryAction _action)
    {
        BecomeHuman();
    }

    /// <summary>
    /// Сформировать словари стори-действий
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        storyActionBase.Add("becomeHuman", StoryBecomeHuman);
    }

    #endregion //storyActions

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public override List<string> actionNames()
    {
        List<string> _actionNames = base.actionNames();
        _actionNames.Add("becomeHuman");
        return _actionNames;
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs1()
    {
        Dictionary<string,List<string>> _actionIDs1 = base.actionIDs1();
        _actionIDs1.Add("becomeHuman", new List<string>());
        return _actionIDs1;
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs2()
    {
        Dictionary<string, List<string>> _actionIDs2 = base.actionIDs2();
        _actionIDs2.Add("becomeHuman", new List<string>());
        return _actionIDs2;
    }

    #endregion //IHaveStory

    #region other

    /// <summary>
    /// Возвращает вектор нормали по отношению к заданному вектору. Причём от этой нормали до заданного вектора должен быть кратчайший поворот против часовой стрелки
    /// </summary>
    /// <param name="direction">Заданное направление, по отношению к которому строим нормаль</param>
    /// <returns>Вектор нормали</returns>
    protected Vector2 GetNormal(Vector2 direction)
    {
        Vector2 normal = new Vector2(-direction.y, direction.x);
        if (normal.x * direction.y - normal.y * direction.x < 0)
            normal *= -1;
        return normal.normalized;
    }

    /// <summary>
    /// Возвращает вектор нормали заданной поверхности земли
    /// </summary>
    /// <param name="surfacePoint1">Первая точка, заданной прямой</param>
    /// <param name="surfacePoint2">Вторая точка, заданной прямой</param>
    /// <returns>Вектор нормали</returns>
    protected Vector2 GetNormal(Vector2 surfacePoint1, Vector2 surfacePoint2)
    {
        return GetNormal((surfacePoint1-surfacePoint2).normalized);
    }

    /// <summary>
    /// Возвращает вектор нормали заданной поверхности земли
    /// </summary>
    /// <param name="surfacePoint1">Первая точка, заданной прямой</param>
    /// <param name="surfacePoint2">Вторая точка, заданной прямой</param>
    /// <returns>Вектор нормали</returns>
    protected Vector2 GetNormal(Vector2 surfacePoint1, Vector2 surfacePoint2, Collider2D gCol)
    {
        Vector2 direction = (surfacePoint2 - surfacePoint1).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x);
        Vector2 _point = (surfacePoint1 + surfacePoint2) / 2f + normal * 0.02f;
        if (gCol.OverlapPoint(_point))
            normal *= -1f;

        return normal.normalized;
    }

    /// <summary>
    /// Функция, возвращающая граничные точки простого коллайдера
    /// </summary>
    /// <param name="col">заданный коллайдер</param>
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
            Vector2[] points = new Vector2[] { vect + b1, vect + b2, vect - b1, vect - b2 };
            return points;
        }
        return null;
    }

    /// <summary>
    /// Узнать точку пересечения заданной прямой и ортогонального ей вектора, пущенного из точки
    /// </summary>
    /// <param name="point1">первая точка, принадлежащая заданной прямой</param>
    /// <param name="point2">вторая точка, принадлежащая заданной прямой</param>
    /// <param name="fromPoint">точка, откуда мы ищем точку пересечения</param>
    /// <returns>Точка пересечения</returns>
    protected Vector2 GetConnectionPoint(Vector2 point1, Vector2 point2, Vector2 fromPoint)
    {
        Vector2 connectionPoint = Vector2.zero;//Точка пересечения 2-х прямых
        Vector2 normal = GetNormal(point1, point2);//Нормаль рассматриваемой поверхности
                                                   //if (Vector2.Angle(spiderOrientation, normal) < minAngle)
                                                   //return Vector2.zero;
        if (point1.x - point2.x == 0)
            connectionPoint = new Vector2(point1.x, normal.y / normal.x * (point1.x - fromPoint.x) + fromPoint.y);
        else if (normal.x == 0)
            connectionPoint = new Vector2(fromPoint.x, (point2.y - point1.y) / (point2.x - point1.x) * (fromPoint.x - point1.x) + point1.y);
        else
        {
            float newX = ((normal.y / normal.x) * fromPoint.x -
                                        (point2.y - point1.y) / (point2.x - point1.x) * point1.x +
                                                            (point1.y - fromPoint.y)) /
                            (normal.y / normal.x - (point2.y - point1.y) / (point2.x - point1.x));
            float newY = normal.y / normal.x * (newX - fromPoint.x) + fromPoint.y;
            connectionPoint = new Vector2(newX, newY);
        }
        //Если точка крепления по какой-то причине оказалась не между двумя точками заданной прямой, то установить точкой крепления ближайшую из этой точек
        if ((connectionPoint.x - point1.x) * (connectionPoint.x - point2.x) > 0 || (connectionPoint.y - point1.y) * (connectionPoint.y - point2.y) > 0)
            connectionPoint = (Vector2.SqrMagnitude(connectionPoint - point1) < Vector2.SqrMagnitude(connectionPoint - point2) ? point1 + (point2 - point1).normalized * spiderOffset :
                                                                                                                                 point2 + (point1 - point2).normalized * spiderOffset);

        return connectionPoint;
    }
    
    /// <summary>
    /// Функция, что находит самую дальнюю точку заданной поверхности, до которой может дойти паук, не меняя ориентацию. Долбанная функция для долбанного удобства. 
    /// </summary>
    /// <param name="startPoint">Точка поверхности, откуда ведём поиск</param>
    /// <param name="observeDirection">В какую сторону смотрим</param>
    /// <param name="_spiderOrientation">Какова ориентация паука при перемещении по заданной поверхности</param>
    /// <param name="col">Коллайдер, составляющий поверхность, с которого начинаем исследование</param>
    public Vector2 ObserveSurfacePoint(Vector2 startPoint, Vector2 observeDirection, Vector2 _spiderOrientation, Collider2D col)
    {
        Vector2 nextPoint = startPoint;
        bool findNextPoint = true;
        Collider2D prevCol = col;
        while (findNextPoint)
        {
            findNextPoint = false;
            Collider2D[] cols = Physics2D.OverlapCircleAll(nextPoint, spiderOffset, whatIsGround);
            if (cols.Length == 0)
                break;
            foreach (Collider2D _col in cols)
            {
                if (_col == prevCol)
                    continue;
                Vector2[] _colPoints = GetColliderPoints(_col);
                if (_colPoints == null ? true : _colPoints.Length == 0)
                    continue;
                int pointIndex = -1;
                for (int i = 0; i < _colPoints.Length; i++)
                    if (Vector2.SqrMagnitude(_colPoints[i] - nextPoint) < spiderOffset * spiderOffset)
                    {
                        pointIndex = i;
                        break;
                    }
                if (pointIndex == -1)
                    continue;
                Vector2 _point1 = _colPoints[pointIndex < _colPoints.Length - 1 ? pointIndex + 1 : 0], _point2 = _colPoints[pointIndex > 0 ? pointIndex - 1 : _colPoints.Length - 1];
                if (Mathf.Approximately(Vector2.Dot((_point1 - nextPoint).normalized, _spiderOrientation), 0f) && Vector2.Dot(_point1 - nextPoint, observeDirection) > 0f)
                {
                    nextPoint = _point1;
                    findNextPoint = true;
                    break;
                }
                else if (Mathf.Approximately(Vector2.Dot((_point2 - nextPoint).normalized, _spiderOrientation), 0f) && Vector2.Dot(_point2 - nextPoint, observeDirection) > 0f)
                {
                    nextPoint = _point2;
                    findNextPoint = true;
                    break;
                }
            }
        }
        return nextPoint;
    }

    #endregion //other

}

/// <summary>
/// Структура, содержащая информацию о поверхности, по которой двигается паук, а также удобные методы, для работы с такой структурой
/// </summary>
public struct SurfaceLineClass
{

    public Vector2 point1;//Две крайние точки, принадлежащие поверхности
    public Vector2 point2;
    public Vector2 normal;//Нормаль к поверхности
    public bool exists;//Есть ли такая поверхность?

    public SurfaceLineClass(Vector2 _point1, Vector2 _point2)
    {
        point1 = _point1;
        point2 = _point2;
        Vector2 direction = (point2 - point1).normalized;
        normal = new Vector2(-direction.y, direction.x);
        if (normal.x * direction.y - normal.y * direction.x < 0)
            normal *= -1;
        exists = true;
    }

    public SurfaceLineClass(Vector2 _point1, Vector2 _point2, Vector2 _normal, bool _exists)
    {
        point1 = _point1;
        point2 = _point2;
        normal = _normal;
        exists = _exists;
    }

    public SurfaceLineClass(Vector2 _point1, Vector2 _point2, Vector2 _normal)
    {
        point1 = _point1;
        point2 = _point2;
        normal = _normal;
        exists = true;
    }

    public static SurfaceLineClass zero { get { return new SurfaceLineClass(Vector2.zero, Vector2.zero, Vector2.zero, false); } }

    /// <summary>
    /// Возвращает положительное направление движения по данной поверхности
    /// </summary>
    /// <returns></returns>
    public Vector2 GetMoveDirection()
    {
        Vector2 vect = (point1 - point2).normalized;
        if (normal.y>=0.1f || normal.y<-.8f? vect.x < 0f: vect.y < 0f)
            vect = -vect;
        return vect.normalized;
    }

    /// <summary>
    /// Находится ли заданная точка рядом с одним из краёв поверхности
    /// </summary>
    /// <param name="position">Рассматриваемая точка</param>
    /// <param name="eps">при каком минимальном квадратичном отклонении возвращается false</param>
    public bool IsNearPoint(Vector2 position, float eps)
    {
        return Vector2.SqrMagnitude(position - point1) < eps || Vector2.SqrMagnitude(position - point2) < eps;
    }

    /// <summary>
    /// Возвращает ближайший к заданной точке край поверхности
    /// </summary>
    /// <param name="position">Заданная точка</param
    public Vector2 GetNearPoint(Vector2 position)
    {
        float distance1 = Vector2.SqrMagnitude(position-point1);
        float distance2 = Vector2.SqrMagnitude(position - point2);
        if (distance1 < distance2)
            return point1;
        else
            return point2;
    }

    /// <summary>
    /// Возвращает дальнейший к заданной точке край поверхности
    /// </summary>
    /// <param name="position">Заданная точка</param
    public Vector2 GetFarPoint(Vector2 position)
    {
        float distance1 = Vector2.SqrMagnitude(position - point1);
        float distance2 = Vector2.SqrMagnitude(position - point2);
        if (distance1 < distance2)
            return point2;
        else
            return point1;
    }

    /// <summary>
    /// Возвращает вектор нормали по отношению к заданному вектору. Причём от этой нормали до заданного вектора должен быть кратчайший поворот против часовой стрелки
    /// </summary>
    /// <param name="direction">Заданное направление, по отношению к которому строим нормаль</param>
    /// <returns>Вектор нормали</returns>
    public Vector2 GetNormal()
    {
        return normal;
    }

    /// <summary>
    /// Возвращает расстояние от точки до прямой
    /// </summary>
    /// <param name="position">От какой точки мерим расстояние</param>
    /// <returns></returns>
    public float GetDistance(Vector2 position)
    {
        Vector2 connectionPoint = GetConnectionPoint(position);
        return (connectionPoint - position).magnitude;
    }

    /// <summary>
    /// Узнать точку пересечения заданной прямой и ортогонального ей вектора, пущенного из точки
    /// </summary>
    /// <param name="point1">первая точка, принадлежащая заданной прямой</param>
    /// <param name="point2">вторая точка, принадлежащая заданной прямой</param>
    /// <param name="fromPoint">точка, откуда мы ищем точку пересечения</param>
    /// <returns>Точка пересечения</returns>
    public Vector2 GetConnectionPoint(Vector2 fromPoint)
    {
        Vector2 connectionPoint = Vector2.zero;//Точка пересечения 2-х прямых
        Vector2 normal = GetNormal();//Нормаль рассматриваемой поверхности
                                                   //if (Vector2.Angle(spiderOrientation, normal) < minAngle)
                                                   //return Vector2.zero;
        if (point1.x - point2.x == 0)
            connectionPoint = new Vector2(point1.x, normal.y / normal.x * (point1.x - fromPoint.x) + fromPoint.y);
        else if (normal.x == 0)
            connectionPoint = new Vector2(fromPoint.x, (point2.y - point1.y) / (point2.x - point1.x) * (fromPoint.x - point1.x) + point1.y);
        else
        {
            float newX = ((normal.y / normal.x) * fromPoint.x -
                                        (point2.y - point1.y) / (point2.x - point1.x) * point1.x +
                                                            (point1.y - fromPoint.y)) /
                            (normal.y / normal.x - (point2.y - point1.y) / (point2.x - point1.x));
            float newY = normal.y / normal.x * (newX - fromPoint.x) + fromPoint.y;
            connectionPoint = new Vector2(newX, newY);
        }
        //Если точка крепления по какой-то причине оказалась не между двумя точками заданной прямой, то установить точкой крепления ближайшую из этой точек
        if ((connectionPoint.x - point1.x) * (connectionPoint.x - point2.x) > 0 || (connectionPoint.y - point1.y) * (connectionPoint.y - point2.y) > 0)
            connectionPoint = (Vector2.SqrMagnitude(connectionPoint - point1) < Vector2.SqrMagnitude(connectionPoint - point2) ? point1 :point2 );

        return connectionPoint;
    }

    /// <summary>
    /// Находится ли заданная точка далеко от поверхности
    /// </summary>
    /// <param name="position">Заданная точка</param>
    /// <param name="eps">Минимальное расстояние до поверхности, при котором возвращается false</param>
    /// <returns></returns>
    public bool IsNearSurface(Vector2 position,float eps)
    {
        return GetDistance(position) < eps;
    }



}

/// <summary>
/// Редактор героя в паучьем обличии
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(SpiderHeroController))]
public class SpiderHeroControllerEditor : Editor
{

    SerializedObject serSpiderHero;

    public void OnEnable()
    {
        serSpiderHero = new SerializedObject((SpiderHeroController)target);
    }

    public override void OnInspectorGUI()
    {
        SerializedProperty
        whatIsGround = serSpiderHero.FindProperty("whatIsGround"),
        whatIsAim = serSpiderHero.FindProperty("whatIsAim");

        SpiderHeroController spiderHero = (SpiderHeroController)target;
        spiderHero.MaxHealth = EditorGUILayout.FloatField("Max Health", spiderHero.MaxHealth);
        spiderHero.Health = EditorGUILayout.FloatField("Health", spiderHero.Health);
        spiderHero.Balance = EditorGUILayout.IntField("Balance", spiderHero.Balance);
        spiderHero.Speed = EditorGUILayout.FloatField("Speed", spiderHero.Speed);
        spiderHero.WebSpeed = EditorGUILayout.FloatField("Web Speed", spiderHero.WebSpeed);
        spiderHero.JumpForce = EditorGUILayout.FloatField("Jump Force", spiderHero.JumpForce);
        spiderHero.JumpAdd = EditorGUILayout.FloatField("Jump Add", spiderHero.JumpAdd);
        EditorGUILayout.PropertyField(whatIsGround);
        EditorGUILayout.PropertyField(whatIsAim);

        serSpiderHero.ApplyModifiedProperties();
    }

}
#endif //UNITY_EDITOR