using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class ARTapToPlaceObject : MonoBehaviour
{
    class ObjectToSerialize
    {
        public float x, y, z;
        public int prefabType;
        public GameObject gObject;

        public ObjectToSerialize(Vector3 loc, int prefabType)
        {
            this.x = loc.x;
            this.y = loc.y;
            this.z = loc.z;
            this.prefabType = prefabType;
        }

        public void SetGameObject(GameObject obj)
        {
            this.gObject = obj;
        }

        public string toString()
        {
            return string.Format("{0}:{1}:{2}:{3}", this.x, this.y, this.z, this.prefabType);
        }

        public static ObjectToSerialize fromString(string input)
        {
            var splited = input.Split(':');
            if (splited.Length != 4)
            {
                return null;
            }
            var x = float.Parse(splited[0]);
            var y = float.Parse(splited[1]);
            var z = float.Parse(splited[2]);
            var prefabType = int.Parse(splited[3]);

            return new ObjectToSerialize(new Vector3(x, y, z), prefabType);
        }
    }

    private ARRaycastManager _arRaycastManager;
    private GameObject spawnedObject;
    private List<ObjectToSerialize> placedPrefabList = new List<ObjectToSerialize>();

    [SerializeField]
    private int maxPrefabSpawnCount = 0;
    private int placedPrefabCount;
    private GameObject placablePrefab;
    public int prefabID;
    
    private Vector3 gridSize = new Vector3(0.2F, 0.2F, 0.2F);

    private Vector2 touchPosition;
    private float? baseY = null;
    private bool deleteMode = false;
    //private bool loadMode = false;

    // Prefabs list
    public GameObject cube1;
    public GameObject cube2;
    public GameObject cube3;
    public GameObject cube4;
    public GameObject cube5;
    public GameObject cube6;
    public GameObject cube7;
    public GameObject cube8;
    public GameObject cube9;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Awake()
    {
        _arRaycastManager = GetComponent<ARRaycastManager>();
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if(Input.touches.Length != 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                touchPosition = default;
                return false;
            }
            touchPosition = Input.GetTouch(0).position;
            return true;
        }
        touchPosition = default;
        return false;
    }

    //lista przetrzymująca klocki
    //kliknięcie generuje kolejny klocek
    //kolizje klocków
    //przytrzymanie - edycja wyglądu bądź usunięcie

    // Update is called once per frame
    void Update()
    {
        if (!TryGetTouchPosition(out Vector2 touchPosition))
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        RaycastHit hit;
        var hitCollider = false;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.tag == "cube") {
                hitCollider = true;
            }
        }

        if (placablePrefab != null)
        {
            var rayCasted = true;
            Vector3 hitPosition;
            if (hitCollider) {
                hitPosition = this.GetNewPositionForPrefab(hit.point, hit.collider.gameObject.transform.position);
            }
            else if (_arRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                hitPosition = hits[0].pose.position;
                rayCasted = false;
            } else {
                return;
            }
             
            if (placedPrefabCount < maxPrefabSpawnCount)
            {
                SpawnPrefab(hitPosition, rayCasted);
            }
            return;
        }
        else if (deleteMode)
        {
            if (hitCollider) {
                int i = 0;
                for (i = 0; i < placedPrefabList.Count; i++)
                {
                    if (placedPrefabList[i].gObject == hit.collider.gameObject)
                    {
                        break;
                    }
                }
                placedPrefabList.RemoveAt(i);
                placedPrefabCount--;
                Destroy(hit.collider.gameObject);
            }
            return;
        }
        Debug.Log("Done nothing ;c");
    }

    public void SetPrefabType(int prefabID)
    {
        this.placablePrefab = getPrefabByID(prefabID);
        this.deleteMode = false;
        this.prefabID = prefabID;
    }

    private GameObject getPrefabByID(int id) {
        switch (id)
        {
            case 1:
                return cube1;
            case 2:
                return cube2;
            case 3:
                return cube3;
            case 4:
                return cube4;
            case 5:
                return cube5;
            case 6:
                return cube6;
            case 7:
                return cube7;
            case 8:
                return cube8;
            case 9:
                return cube9;
            default:
                return null;
        }
    }

    public void SetDeleteMode(bool on)
    {
        placablePrefab = null;
        deleteMode = on;
    }

    private Vector3 GetNewPositionForPrefab(Vector3 hitPoint, Vector3 parentPosition)
    {
        
        var newPosition = new Vector3(
            this.getNewPosition(hitPoint.x, parentPosition.x),
            this.getNewPosition(hitPoint.y, parentPosition.y),
            this.getNewPosition(hitPoint.z, parentPosition.z)
        );

        return newPosition;
    }

    public void SaveGame()
    {
        var serialized = new List<string>();

        foreach (var srlObj in placedPrefabList)
        {
            serialized.Add(srlObj.toString());
        }

        PlayerPrefs.SetString("saveData", string.Join("&", serialized));
        PlayerPrefs.Save();
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey("saveData"))
        {
            return;
        }

        foreach (var srlObj in placedPrefabList)
        {
            if (srlObj.gObject != null)
            {
                Destroy(srlObj.gObject);
            } 
        }

        placedPrefabList.Clear();
        foreach (var stringObject in PlayerPrefs.GetString("saveData").Split('&'))
        {
            Debug.Log(stringObject);
            var newSpawnedObject = ObjectToSerialize.fromString(stringObject);
            Debug.Log(string.Format("({0}, {1}, {2}, {3})", newSpawnedObject.x, newSpawnedObject.y, newSpawnedObject.z, newSpawnedObject.prefabType));

            spawnedObject = Instantiate(getPrefabByID(newSpawnedObject.prefabType), new Vector3(newSpawnedObject.x, newSpawnedObject.y, newSpawnedObject.z), Quaternion.identity);
            newSpawnedObject.gObject = spawnedObject;
            placedPrefabList.Add(newSpawnedObject);
        }
    }

    private float getNewPosition(float hitP, float parentP)
    {
        var diff = hitP - parentP;
        // Bad calculation if we encounter any problems with float approximation
        if (Mathf.Abs(diff) > 0.09F) {
            if (diff > 0)
            {
                return parentP + 0.2F;
            }
            else 
            {
                return parentP - 0.2F;
            }
        }

        return parentP;
    }

    private void SpawnPrefab(Vector3 position, bool onRayCast)
    {
        var newPosition = new Vector3(
            Mathf.Round(position.x / this.gridSize.x) * this.gridSize.x,
            position.y,
            Mathf.Round(position.z / this.gridSize.z) * this.gridSize.z
        );

        if (!onRayCast)
        {
            if (this.baseY == null)
            {
                this.baseY = position.y;
            }
            newPosition.y = (float)this.baseY + 0.1F;
        }

        spawnedObject = Instantiate(placablePrefab, newPosition, Quaternion.identity);

        var serializedSpawnedObject = new ObjectToSerialize(newPosition, this.prefabID);
        serializedSpawnedObject.SetGameObject(spawnedObject);

        placedPrefabList.Add(serializedSpawnedObject);
        placedPrefabCount++;
    }
}
