﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gen_mapa;
using UnityEngine;

public class Supergenerador : MonoBehaviourEx
{
    #region variables
    private bool _shouldReset = false;
    private Map _map;
    private PoolMaterial _poolMaterial;
    //posicio actual laberint 
    // Nombre màxim de sales a generar
    private int _maxSales;
    // sala actual        
    //elements map   
    private GameObject _contenidorInst;

    //References that will be used by the instantiator
    public Vector2 ProtaPosition;
    public Vector2 MinoPosition;
    //Empty Quadrants
    private List<int> _availableQuadrantsList = new List<int>();
    private int _quadrantProta;
    public Vector2 PosZero;
    //other resources
    public GameObject ConstructMapa;
    #endregion

    #region Funcions Inici

    public Map Generar_mapa(int x, int y, int niv, int maxSa)
    {
        SetStarting();
        _map = new Map(x, y);
        ProtaPosition = ToRealWorldPosition(_map.Pointer.X, _map.Pointer.Y);
        _quadrantProta = Quin_Quadrant(_map.Pointer);
        _availableQuadrantsList.RemoveAt(_quadrantProta);
        _poolMaterial = new PoolMaterial(niv);
        _maxSales = maxSa;
        Crear_dungeon();
        StartCoroutine(Colocar_al_mon());
        return _map;
    }

    private void SetStarting()
    {
        if (_contenidorInst != null) Destroy(_contenidorInst);
        _contenidorInst = new GameObject { name = "Map" };
        _availableQuadrantsList.Add(0);
        _availableQuadrantsList.Add(1);
        _availableQuadrantsList.Add(2);
        _availableQuadrantsList.Add(3);
    }

    #endregion

    #region Crear laberint
    void Crear_dungeon()
    {

        var primeraSala = true;
        var countOnPath = 0;
        while (_maxSales > _map.RoomCount)
        {

            if (_map.RoomCount == 0 && primeraSala)
            {
                //primera sala
                primeraSala = false;
                PopulateWith(Constants.RandomGeneration.InitalRoomId, new Punt2d());
            }
            else
            {
                //calculs map
                List<Punt2d> emptyDirections;
                var tempDis = false;
                var repetir = countOnPath > 3;
                do
                {
                    emptyDirections = TestDirections();
                    if (repetir)
                    {
                        countOnPath = 0;
                        if (Utils.ChanceTrue(Constants.RandomGeneration.ChanceConnectStuck))
                        {
                            emptyDirections = new List<Punt2d>(IsNotEmpty(emptyDirections));

                            if (emptyDirections.Count != 0)
                            {
                                PopulateWith(Constants.RandomGeneration.CorridorId, emptyDirections[Random.Range(0, emptyDirections.Count)]);
                            }
                        }
                        if (FindNextRoom())
                        {
                            emptyDirections = new List<Punt2d>(TestDirections());
                            tempDis = true;
                            repetir = false;
                        }
                        else
                        {
                            Error_fatal("Tot ple!! o fallo gordo");
                            break;
                        }
                    }
                    else
                    {
                        emptyDirections = new List<Punt2d>(testDirections(emptyDirections));
                        if (emptyDirections.Count == 0) repetir = true;
                        else
                        {
                            countOnPath++;
                            tempDis = true;
                        }
                    }

                } while (!tempDis);

                var tempDir = emptyDirections[Random.Range(0, emptyDirections.Count)];
                PopulateWith(Constants.RandomGeneration.CorridorId, tempDir);
                if (testDirections(tempDir))
                {
                    PopulateWith(Constants.RandomGeneration.EmptyRoomId, tempDir);
                }
                else
                {
                    if (FindNextRoom())
                    {
                        TestDirections();
                    }
                    else
                    {
                        Error_fatal("Tot ple!! o fallo gordo");
                        break;
                    }

                }
            }

        }
        // Segona fase map    
        //Localització del minotaure 
        LocateMinotaur();
        //Tercera fase map
        //Localització dels fars
        LocateLighthouses();
        //Quarta fase
        //Localització de Mercat Negre
        LocateInAvailableQuadrant(Constants.RandomGeneration.BlackMarketRoomId);
        //Quinta fase
        //Localització de Sortida
        LocateInAvailableQuadrant(Constants.RandomGeneration.ExitRoomId);
        //Sexta fase
        //Sales enemics (Sempre va l'últim!)
        LocateGeneralRooms();

    }

    #endregion

    #region quadrant
    Punt2d Quadrant_cant_ex(int quad)
    {
        switch (quad)
        {
            case 0:
                return new Punt2d();
            case 1:
                return new Punt2d(_map.Width - 1, 0);
            case 2:
                return new Punt2d(0, _map.Height - 1);
            case 3:
                return new Punt2d(_map.Width - 1, _map.Height - 1);
            default:
                Debug.Log("Error esquina");
                return new Punt2d(6666, 6666);
        }
    }

    int Quadrant_Oposat(int quad)
    {
        switch (quad)
        {
            case 0:
                return 3;
            case 1:
                return 2;
            case 2:
                return 1;
            case 3:
                return 0;
            default:
                Debug.Log("Error quadrant oposat");
                return 6000;
        }
    }

    int Quin_Quadrant(Punt2d puntQua)
    {
        if (puntQua.X > (_map.Width / 2))
        {
            return puntQua.Y > (_map.Height / 2) ? 3 : 1;
        }
        return puntQua.Y < (_map.Height / 2) ? 0 : 2;
    }

    Quadrant AreaQuadrant(int quad, int quadSize)
    {

        //quadSize must be 2 or 3
        Quadrant tempQuadrant;
        switch (quad)
        {
            case 0:
                tempQuadrant = new Quadrant(0, 0, (_map.Width / quadSize) - 1, (_map.Height / quadSize) - 1);
                break;
            case 1:
                tempQuadrant = new Quadrant(_map.Width - _map.Width / quadSize, 0, _map.Width - 1, (_map.Height / quadSize) - 1);
                break;
            case 2:
                tempQuadrant = new Quadrant(0, _map.Height - _map.Height / quadSize, (_map.Width / quadSize) - 1, _map.Height - 1);
                break;
            case 3:
                tempQuadrant = new Quadrant(_map.Width - _map.Width / quadSize, _map.Height - _map.Height / quadSize, _map.Width - 1, _map.Height - 1);
                break;
            default:
                Debug.Log("Funció map error");
                tempQuadrant = new Quadrant(0, 0, 5, 5);
                break;
        }
        return tempQuadrant;
    }

    List<Punt2d> LocationsInQuadrant(Quadrant quadrant)
    {
        var locations = new List<Punt2d>();
        for (var x = quadrant.TopLeftCorner.X; x < quadrant.BottomRightCorner.X; x++)
        {
            for (var y = quadrant.TopLeftCorner.Y; y < quadrant.BottomRightCorner.Y; y++)
            {
                if (_map[x, y].Equals(1))
                {
                    locations.Add(new Punt2d(x, y));
                }
            }

        }
        return locations;
    }
    #endregion

    #region Funcions ajuda laberint
    //test if all 4 directions from the pointer are empty
    private List<Punt2d> TestDirections()
    {
        var direccionsDis = new List<Punt2d>();

        for (var dir = 0; dir < 4; dir++)
        {
            if (_map[_map.Pointer.X + Constants.RandomGeneration.CloseDirections[dir].X, _map.Pointer.Y + Constants.RandomGeneration.CloseDirections[dir].Y] == 0) direccionsDis.Add(Constants.RandomGeneration.CloseDirections[dir]);
        }
        return direccionsDis;

    }

    //test if all 4 directions from the point provided are empty
    private List<Punt2d> TestDirectionsFrom(Punt2d point)
    {
        var direccionsDis = new List<Punt2d>();

        for (var dir = 0; dir < 4; dir++)
        {
            if (_map[point.X + Constants.RandomGeneration.CloseDirections[dir].X, point.Y + Constants.RandomGeneration.CloseDirections[dir].Y] == 0) direccionsDis.Add(Constants.RandomGeneration.CloseDirections[dir]);
        }
        return direccionsDis;
    }

    //test if and specific direction from the pointer is empty
    private bool testDirections(Punt2d dir)
    {
        return _map[_map.Pointer.X + dir.X, _map.Pointer.Y + dir.Y] == 0;
    }

    //checks if all 4 directions 2 positions away from the pointer are empty, if they aren't, they are removed 
    private List<Punt2d> testDirections(List<Punt2d> directionsCheck)
    {
        var tempFarDirections = Enumerable.ToList(directionsCheck.Select(t => new Punt2d(t.X * 2, t.Y * 2)));

        var tempList = new List<Punt2d>(directionsCheck);

        for (var dir = 0; dir < directionsCheck.Count; dir++)
        {
            var positionValue = _map[_map.Pointer.X + tempFarDirections[dir].X, _map.Pointer.Y + tempFarDirections[dir].Y];
            if (positionValue == 666) tempList.Remove(directionsCheck[dir]);
            else if (positionValue != 0)
            {
                var point = new Punt2d(_map.Pointer.X + tempFarDirections[dir].X, _map.Pointer.Y + tempFarDirections[dir].Y);
                if (TestDirectionsFrom(point).Count >= 3) continue;
                if (!Utils.ChanceTrue(Constants.RandomGeneration.ChanceAcceptBuild))
                {
                    tempList.Remove(directionsCheck[dir]);
                }
            }
        }
        return tempList;
    }

    //checks if all 4 directions 2 positions away from the pointer are empty,and removes the ones that are empty
    private IEnumerable<Punt2d> IsNotEmpty(IList<Punt2d> directionsCheck)
    {
        var tempFarDirections = Enumerable.ToList(directionsCheck.Select(t => new Punt2d(t.X * 2, t.Y * 2)));

        var tempList = new List<Punt2d>(directionsCheck);

        for (var dir = 0; dir < directionsCheck.Count; dir++)
        {
            var positionValue = _map[_map.Pointer.X + tempFarDirections[dir].X, _map.Pointer.Y + tempFarDirections[dir].Y];
            if (positionValue == 666) tempList.Remove(directionsCheck[dir]);
            else if (positionValue == 0) tempList.Remove(directionsCheck[dir]);
        }
        return tempList;
    }

    private bool FindNextRoom()
    {
        var llistaCambres = new List<Punt2d>();
        var mapWidth = _map.Width;
        var mapHeight = _map.Height;
        for (var x = 0; x < mapWidth; ++x)
        {
            for (var y = 0; y < mapHeight; ++y)
            {
                if (_map[x, y].Equals(1))
                {
                    llistaCambres.Add(new Punt2d(x, y));
                }

            }
        }
        var anyCamb = true;
        do
        {
            var tempRandom = Random.Range(0, llistaCambres.Count);
            _map.Pointer = llistaCambres[tempRandom];
            llistaCambres.RemoveAt(tempRandom);
            var tempCub = new List<Punt2d>(TestDirections());
            if (tempCub.Count != 0)
            {
                if (testDirections(tempCub).Count != 0)
                    return true;
            }
            if (llistaCambres.Count == 0) anyCamb = false;
        } while (anyCamb);
        return false;
    }

    private void PopulateWith(int elementType, Punt2d dir)
    {
        if (elementType < 4 || elementType == 1111)
        {
            if (elementType == Constants.RandomGeneration.EmptyRoomId || elementType == Constants.RandomGeneration.InitalRoomId) _map.RoomCount++;
            _map.Pointer.X += dir.X;
            _map.Pointer.Y += dir.Y;
            if (elementType == Constants.RandomGeneration.CorridorId)
            {
                elementType = dir.Y != 0 ? Constants.RandomGeneration.VerticalCorridorId : Constants.RandomGeneration.HorizontalCorridorId;
            }
        }
        else
        {
            _map.Pointer.X = dir.X;
            _map.Pointer.Y = dir.Y;
        }

        _map[_map.Pointer.X, _map.Pointer.Y] = elementType;
    }
    Punt2d Super_mino(Punt2d punt)
    {
        var direccionsDis = new List<int>();

        for (var i = 0; i < 4; i++)
        {
            switch (i)
            {
                //nord
                case 0:
                    if (_map[punt.X, punt.Y + 2] == Constants.RandomGeneration.EmptyRoomId) direccionsDis.Add(0);
                    break;
                //sud
                case 1:
                    if (_map[punt.X, punt.Y - 2] == Constants.RandomGeneration.EmptyRoomId) direccionsDis.Add(1);
                    break;
                //est
                case 2:
                    if (_map[punt.X + 2, punt.Y] == Constants.RandomGeneration.EmptyRoomId) direccionsDis.Add(2);
                    break;
                //oest
                case 3:
                    if (_map[punt.X - 2, punt.Y] == Constants.RandomGeneration.EmptyRoomId) direccionsDis.Add(3);
                    break;
            }
        }

        //        Debug.Log(direccionsDis.Count);
        var dir = direccionsDis[Random.Range(0, direccionsDis.Count)];
        switch (dir)
        {
            //nord
            case 0:
                return new Punt2d(punt.X, punt.Y + 2);
            //sud
            case 1:
                return new Punt2d(punt.X, punt.Y - 2);
            //est
            case 2:
                return new Punt2d(punt.X + 2, punt.Y);
            //oest
            case 3:
                return new Punt2d(punt.X - 2, punt.Y);
        }
        return new Punt2d(55555, 55555);
    }

    List<int> Test_dir_blo(Punt2d punt)
    {
        int tempCont;
        var direccionsDis = new List<int>();

        for (var i = 0; i < 4; i++)
        {
            switch (i)
            {
                //nord
                case 0:
                    tempCont = _map[punt.X, punt.Y + 1];
                    if (tempCont == 0 || tempCont == 666) direccionsDis.Add(0);
                    break;
                //sud
                case 1:
                    tempCont = _map[punt.X, punt.Y - 1];
                    if (tempCont == 0 || tempCont == 666) direccionsDis.Add(1);
                    break;
                //est
                case 2:
                    tempCont = _map[punt.X + 1, punt.Y];
                    if (tempCont == 0 || tempCont == 666) direccionsDis.Add(2);
                    break;
                //oest
                case 3:
                    tempCont = _map[punt.X - 1, punt.Y];
                    if (tempCont == 0 || tempCont == 666) direccionsDis.Add(3);
                    break;
            }
        }
        return direccionsDis;
    }

    Vector2 ToRealWorldPosition(int x, int y)
    {
        return new Vector2(x * 17, y * 13);
    }

    Vector2 ToRealWorldPositionModified(int x, int y)
    {
        return new Vector2((x * 17) - 12, (y * 13) + 8);
    }

    void Error_fatal(string cosa)
    {
        Debug.Log("Error_Catastrofic");
        Debug.Log(cosa);
    }

    void Error_no_fatal(string cosa)
    {
        Debug.Log("Error_no_Catastrofic");
        Debug.Log(cosa);
    }

    void Colocar_blocks(List<int> bloqueigLlocs, GameObject sala)
    {

        foreach (var bloquejat in bloqueigLlocs)
        {
            switch (bloquejat)
            {
                case 0:
                    var tempBlock0 = Instantiate(_poolMaterial.Blocks[0], new Vector2(sala.transform.position.x + 12, sala.transform.position.y - 8), Quaternion.identity) as GameObject;
                    tempBlock0.transform.parent = sala.transform;
                    break;
                case 1:
                    var tempBlock1 = Instantiate(_poolMaterial.Blocks[1], new Vector2(sala.transform.position.x + 12, sala.transform.position.y - 8), Quaternion.identity) as GameObject;
                    tempBlock1.transform.parent = sala.transform;
                    break;
                case 2:
                    var tempBlock2 = Instantiate(_poolMaterial.Blocks[2], new Vector2(sala.transform.position.x + 12, sala.transform.position.y - 8), Quaternion.identity) as GameObject;
                    tempBlock2.transform.parent = sala.transform;
                    break;
                case 3:
                    var tempBlock3 = Instantiate(_poolMaterial.Blocks[3], new Vector2(sala.transform.position.x + 12, sala.transform.position.y - 8), Quaternion.identity) as GameObject;
                    tempBlock3.transform.parent = sala.transform;
                    break;
            }

        }

    }

    private void InstantiateTrigger(GameObject trigger, Transform parent, Punt2d position)
    {
        var temptrigger = Instantiate(trigger, ToRealWorldPosition(position.X, position.Y), Quaternion.identity) as GameObject;
        temptrigger.GetComponent<Trigg_ele>().coor = position;
        temptrigger.transform.SetParent(parent, true);
    }

    private void LocateMinotaur()
    {
        var quadrantMinId = Quadrant_Oposat(_quadrantProta);
        var quadrantCant = Quadrant_cant_ex(quadrantMinId);
        if (_map[quadrantCant.X, quadrantCant.Y] == Constants.RandomGeneration.EmptyRoomId) MinoPosition = ToRealWorldPosition(quadrantCant.X, quadrantCant.Y);
        else
        {
            var tempPunt = Super_mino(quadrantCant);
            MinoPosition = ToRealWorldPosition(tempPunt.X, tempPunt.Y);
        }
    }

    private void LocateLighthouses()
    {
        for (var i = 0; i <= 3; i++)
        {
            var possibleLighthouseLocations = LocationsInQuadrant(AreaQuadrant(i, 3));
            var lighthouseLocation = possibleLighthouseLocations[Random.Range(0, possibleLighthouseLocations.Count)];
            PopulateWith(Constants.RandomGeneration.LighthouseRoomId, lighthouseLocation);
        }
    }

    private void LocateInAvailableQuadrant(int typeroom)
    {
        var tempRandom = Random.Range(0, _availableQuadrantsList.Count);
        var availableQuadrant = _availableQuadrantsList[tempRandom];
        var locations = LocationsInQuadrant(AreaQuadrant(availableQuadrant, 2));
        var location = locations[Random.Range(0, locations.Count)];
        PopulateWith(typeroom, location);
        _availableQuadrantsList.RemoveAt(tempRandom);
    }

    private void LocateGeneralRooms()
    {
        var allEmptyRooms = new List<Punt2d>[4];
        allEmptyRooms[0] = new List<Punt2d>();
        allEmptyRooms[1] = new List<Punt2d>();
        allEmptyRooms[2] = new List<Punt2d>();
        allEmptyRooms[3] = new List<Punt2d>();
        int roomCount = 0;
        for (var x = 0; x < _map.Width; ++x)
        {
            for (var y = 0; y < _map.Height; ++y)
            {
                if (_map[x, y] == Constants.RandomGeneration.EmptyRoomId)
                {
                    int inQuadrant = Quin_Quadrant(new Punt2d(x, y));
                    allEmptyRooms[inQuadrant].Add(new Punt2d(x, y));
                    roomCount++;
                }
            }
        }

        List<int> quadrantsList = new List<int> { 0, 1, 2, 3 };
        quadrantsList.RemoveAt(_quadrantProta);
        int[] possibleRooms = new[]
        {
            Constants.RandomGeneration.StandardEnemyRoomId,Constants.RandomGeneration.BlueEnemyRoomId, 
           Constants.RandomGeneration.AllEnemyRoomId,Constants.RandomGeneration.BossEnemyRoomId,
           Constants.RandomGeneration.EmptyRoomId
        };
        SetRoomsInQuadrant(allEmptyRooms[_quadrantProta], possibleRooms, Constants.RandomGeneration.RedEnemyRoomId, 60);

        possibleRooms = new[]
        {
            Constants.RandomGeneration.StandardEnemyRoomId, Constants.RandomGeneration.RedEnemyRoomId,
            Constants.RandomGeneration.YellowEnemyRoomId,Constants.RandomGeneration.AllEnemyRoomId, 
            Constants.RandomGeneration.BossEnemyRoomId, Constants.RandomGeneration.EmptyRoomId
        };
        int numberChosen = Random.Range(0, quadrantsList.Count);
        SetRoomsInQuadrant(allEmptyRooms[quadrantsList[numberChosen]], possibleRooms, Constants.RandomGeneration.BlueEnemyRoomId, 60);
        quadrantsList.RemoveAt(numberChosen);

        possibleRooms = new[]
        {
            Constants.RandomGeneration.StandardEnemyRoomId, Constants.RandomGeneration.RedEnemyRoomId,
            Constants.RandomGeneration.BlueEnemyRoomId,Constants.RandomGeneration.AllEnemyRoomId,
            Constants.RandomGeneration.BossEnemyRoomId, Constants.RandomGeneration.EmptyRoomId
        };
        numberChosen = Random.Range(0, quadrantsList.Count);
        SetRoomsInQuadrant(allEmptyRooms[quadrantsList[numberChosen]], possibleRooms, Constants.RandomGeneration.YellowEnemyRoomId, 60);
        quadrantsList.RemoveAt(numberChosen);

        possibleRooms = new[]
        {
            Constants.RandomGeneration.StandardEnemyRoomId, Constants.RandomGeneration.RedEnemyRoomId,
            Constants.RandomGeneration.BlueEnemyRoomId, Constants.RandomGeneration.YellowEnemyRoomId,
            Constants.RandomGeneration.AllEnemyRoomId, Constants.RandomGeneration.BossEnemyRoomId, Constants.RandomGeneration.EmptyRoomId
        };
        numberChosen = Random.Range(0, quadrantsList.Count);
        SetRoomsInQuadrant(allEmptyRooms[quadrantsList[numberChosen]], possibleRooms);
        quadrantsList.RemoveAt(numberChosen);
    }

    //danger can crash in some cases if values are wrong
    private void SetRoomsInQuadrant(List<Punt2d> quadrantRooms, int[] roomsId, int mainRoomId = 404, int mainRoomChance = 404)
    {
        int roomTypeCount = roomsId.Length;
        while (quadrantRooms.Count > 0)
        {
            int targetRoomIndex = Random.Range(0, quadrantRooms.Count);
            if (mainRoomId == 404 || !Utils.ChanceTrue(mainRoomChance))
            {
                _map[quadrantRooms[targetRoomIndex].X, quadrantRooms[targetRoomIndex].Y] = roomsId[Random.Range(0, roomTypeCount)];
            }
            else
            {
                _map[quadrantRooms[targetRoomIndex].X, quadrantRooms[targetRoomIndex].Y] = mainRoomId;
            }
            quadrantRooms.RemoveAt(targetRoomIndex);
        }
    }

    //private void SetEnemyRooms(int roomtype, int roomNumber, List<List<Punt2d>> emptyroomsleft)
    //{
    //    for (var i = 0; i < roomNumber; i++)
    //    {
    //        int targetQuadrantNumber = Random.Range(0, emptyroomsleft.Count);
    //        if (emptyroomsleft[targetQuadrantNumber].Count != 0)
    //        {
    //            var targetRoomPosition = Random.Range(0, emptyroomsleft[targetQuadrantNumber].Count);
    //            _map[emptyroomsleft[targetQuadrantNumber][targetRoomPosition].X, emptyroomsleft[targetRoomPosition][targetRoomPosition].Y] = roomtype;
    //            emptyroomsleft.RemoveAt(targetRoomPosition);
    //        }
    //        else
    //        {
    //            emptyroomsleft.RemoveAt(targetQuadrantNumber);
    //        }
    //    }
    //}

    //private void SetEnemyRooms(int roomtype, int roomNumber, List<Punt2d> emptyroomsleft, int targetQuadrant, int chanceBuildQuadrant)
    //{
    //    for (var i = 0; i < emptyroomsleft.Count; i++)
    //    {

    //    }
    //    for (var i = 0; i < roomNumber; i++)
    //    {
    //        var tempRandom = Random.Range(0, emptyroomsleft.Count);
    //        _map[emptyroomsleft[tempRandom].X, emptyroomsleft[tempRandom].Y] = roomtype;
    //        emptyroomsleft.RemoveAt(tempRandom);
    //    }
    //}

    #endregion


    IEnumerator Colocar_al_mon()
    {
        var lighthousesSpawned = 0;
        var w = _map.Width;
        var h = _map.Height;
        var total = h * w;
        var loadingProgress = 0;
        for (var x = 0; x < w; ++x)
        {
            for (var y = 0; y < h; ++y)
            {
                switch (_map[x, y])
                {
                    case Constants.RandomGeneration.EmptyRoomId:
                        var tempCambra = Instantiate(_poolMaterial.EmptyRooms, ToRealWorldPositionModified(x, y), Quaternion.identity) as GameObject;

                        Colocar_blocks(Test_dir_blo(new Punt2d(x, y)), tempCambra);
                        tempCambra.transform.SetParent(_contenidorInst.transform, true);

                        InstantiateTrigger(_poolMaterial.TriggEle[2], tempCambra.transform, new Punt2d(x, y));
                        break;

                    case Constants.RandomGeneration.StandardEnemyRoomId:
                    case Constants.RandomGeneration.RedEnemyRoomId:
                    case Constants.RandomGeneration.BlueEnemyRoomId:
                    case Constants.RandomGeneration.YellowEnemyRoomId:
                    case Constants.RandomGeneration.AllEnemyRoomId:
                    case Constants.RandomGeneration.BossEnemyRoomId:
                        var enemyroom = Instantiate(_poolMaterial.EnemyRoom(_map[x, y]), ToRealWorldPositionModified(x, y), Quaternion.identity) as GameObject;
                        var tempContenidor = Instantiate(ConstructMapa, ToRealWorldPosition(x, y), Quaternion.identity) as GameObject;

                        Colocar_blocks(Test_dir_blo(new Punt2d(x, y)), enemyroom);
                        enemyroom.transform.SetParent(tempContenidor.transform, true);
                        tempContenidor.transform.SetParent(_contenidorInst.transform, true);

                        InstantiateTrigger(_poolMaterial.TriggEle[2], enemyroom.transform, new Punt2d(x, y));

                        enemyroom.BroadcastMessage("Crear_Besties", SendMessageOptions.DontRequireReceiver);
                        break;

                    case Constants.RandomGeneration.VerticalCorridorId:
                        var passV = Instantiate(_poolMaterial.Corridors("pass_V"), ToRealWorldPosition(x, y), Quaternion.identity) as GameObject;
                        passV.transform.SetParent(_contenidorInst.transform, true);

                        InstantiateTrigger(_poolMaterial.TriggEle[1], passV.transform, new Punt2d(x, y));

                        break;
                    case Constants.RandomGeneration.HorizontalCorridorId:
                        var passH = Instantiate(_poolMaterial.Corridors("pass_H"), ToRealWorldPosition(x, y), Quaternion.identity) as GameObject;
                        passH.transform.SetParent(_contenidorInst.transform, true);

                        InstantiateTrigger(_poolMaterial.TriggEle[0], passH.transform, new Punt2d(x, y));

                        break;
                    case Constants.RandomGeneration.InitalRoomId:
                        var tempCambraIn = Instantiate(_poolMaterial.InitalRooms, ToRealWorldPositionModified(x, y), Quaternion.identity) as GameObject;
                        Colocar_blocks(Test_dir_blo(new Punt2d(x, y)), tempCambraIn);
                        tempCambraIn.transform.SetParent(_contenidorInst.transform, true);

                        InstantiateTrigger(_poolMaterial.TriggEle[2], tempCambraIn.transform, new Punt2d(x, y));

                        break;
                    case Constants.RandomGeneration.LighthouseRoomId:
                        var templighthouseExterior = Instantiate(_poolMaterial.LighthouseExterior, ToRealWorldPositionModified(x, y), Quaternion.identity) as GameObject;
                        Colocar_blocks(Test_dir_blo(new Punt2d(x, y)), templighthouseExterior);
                        templighthouseExterior.transform.SetParent(_contenidorInst.transform, true);
                        templighthouseExterior.GetComponentInChildren<LighthouseStructure>().LighthouseNumber = Quin_Quadrant(new Punt2d(x, y));

                        Quadrant tempQuadrant = AreaQuadrant(Quin_Quadrant(new Punt2d(x, y)), 3);
                        var areaLighthouse = new GameObject(Quin_Quadrant(new Punt2d(x, y)).ToString())
                        {
                            tag = "LighthouseArea"
                        };
                        areaLighthouse.transform.localPosition = new Vector2(tempQuadrant.Center.X * 17, tempQuadrant.Center.Y * 13);
                        areaLighthouse.AddComponent<BoxCollider2D>().isTrigger = true;
                        areaLighthouse.GetComponent<BoxCollider2D>().size = new Vector2(90, 70);
                        areaLighthouse.transform.SetParent(_contenidorInst.transform, true);

                        InstantiateTrigger(_poolMaterial.TriggEle[2], templighthouseExterior.transform, new Punt2d(x, y));

                        var templighthouseInterior = Instantiate(_poolMaterial.LighthouseInterior, ToRealWorldPosition(Constants.RandomGeneration.LighthouseInteriorLocations[lighthousesSpawned].X, Constants.RandomGeneration.LighthouseInteriorLocations[lighthousesSpawned].Y), Quaternion.identity) as GameObject;
                        templighthouseInterior.transform.parent = templighthouseExterior.transform;
                        templighthouseExterior.GetComponentInChildren<LighthouseStructure>().LighthouseInterior = templighthouseInterior;
                        templighthouseInterior.GetComponent<LighthouseInterior>().LighthouseRoom = templighthouseExterior;
                        lighthousesSpawned++;
                        break;
                    case Constants.RandomGeneration.ExitRoomId:
                        var exitRoom = Instantiate(_poolMaterial.ExitRoom, ToRealWorldPositionModified(x, y), Quaternion.identity) as GameObject;

                        Colocar_blocks(Test_dir_blo(new Punt2d(x, y)), exitRoom);
                        exitRoom.transform.SetParent(_contenidorInst.transform, true);

                        InstantiateTrigger(_poolMaterial.TriggEle[2], exitRoom.transform, new Punt2d(x, y));
                        break;
                    case Constants.RandomGeneration.BlackMarketRoomId:
                        var blackMarketExterior = Instantiate(_poolMaterial.BlackMarketExterior, ToRealWorldPositionModified(x, y), Quaternion.identity) as GameObject;

                        Colocar_blocks(Test_dir_blo(new Punt2d(x, y)), blackMarketExterior);
                        blackMarketExterior.transform.SetParent(_contenidorInst.transform, true);

                        blackMarketExterior.GetComponentInChildren<BlackMarketDoor>().marketLocation = ToRealWorldPosition(-10, 10);

                        var blackMarketInterior = Instantiate(_poolMaterial.BlackMarketInterior, ToRealWorldPosition(-10, 10), Quaternion.identity) as GameObject;
                        blackMarketInterior.transform.SetParent(blackMarketExterior.transform, true);

                        blackMarketInterior.GetComponentInChildren<BlackMarketExit>().exitLocation = ToRealWorldPosition(x, y);

                        InstantiateTrigger(_poolMaterial.TriggEle[2], blackMarketExterior.transform, new Punt2d(x, y));
                        break;
                    case 0:
                        //Debug.Log("Emptyness");
                        break;
                    default:
                        Debug.Log("Number not recognized");
                        break;
                }
                loadingProgress += 1;
                Messenger.Publish(new LoadingTick(loadingProgress, total));
                yield return null;

            }

        }
        GetComponent<Dungeon_manager>().Iniciar_nivell();
        _shouldReset = true;
    }



}

