using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bhambhoo
{
    public class Pooling : MonoBehaviour
    {


        public static Pooling Instance;
        public PrefabForPooling[] prefabsToPool;
        public List<GameObject>[] pooledObjectsList;
        public GameObject defaultParent;

        private void OnEnable()
        {
            if (Instance = null)
                Instance = this;
            else
                enabled = false;

            if (defaultParent == null)
                defaultParent = new GameObject("PooledObjects");
            DontDestroyOnLoad(defaultParent);

            pooledObjectsList = new List<GameObject>[prefabsToPool.Length];

            int i = 0;
            foreach (PrefabForPooling onePrefab in prefabsToPool)
            {
                // Create a new list at this point in the pooledObjectsList
                pooledObjectsList[i] = new List<GameObject>();

                for (int n = 0; n < onePrefab.amountToBuffer; n++)
                {
                    GameObject newObject = Instantiate(onePrefab.prefab) as GameObject;
                    newObject.name = onePrefab.prefab.name;
                    PoolObject(newObject);
                }
            }
        }

        public GameObject GetObject(string name, bool onlyPooled = false)
        {
            // Loop over all objectPrefabs specified
            for (int i = 0; i < prefabsToPool.Length; i++)
            {
                // If named object is in original list in inspector
                if (prefabsToPool[i].prefab.name == name)
                {
                    // If there exist pooled copies of this object
                    if(pooledObjectsList[i].Count > 0)
                    {
                        GameObject pooledObject = pooledObjectsList[i][0];
                        pooledObjectsList[i].RemoveAt(0);
                        pooledObject.transform.parent = null;
                        pooledObject.SetActive(true);
                        return pooledObject;
                    }else if (!onlyPooled)
                    {
                        return Instantiate(prefabsToPool[i].prefab);
                    }
                    break;
                }
            }

            return null;
        }
        public bool PoolObject(GameObject objectToPool)
        {
            // Remove (Clone) to bring object's name to its default
            objectToPool.name = objectToPool.name.Replace("(Clone)", "");

            // Loop over all objectPrefabs specified
            for (int i = 0; i < prefabsToPool.Length; i++)
            {
                // Compare names, if name matches
                if (prefabsToPool[i].prefab.name == objectToPool.name)
                {
                    // Disable object
                    objectToPool.SetActive(false);

                    // Attach it to container
                    objectToPool.transform.SetParent(defaultParent.transform, false);

                    // If it has rigidody, bring it to rest
                    if (objectToPool.GetComponent<Rigidbody>() != null)
                    {
                        objectToPool.GetComponent<Rigidbody>().velocity = Vector3.zero;
                    }

                    // Add to list of pooled objects
                    pooledObjectsList[i].Add(objectToPool);
                    return true;
                }
            }
            return false;
        }
    }

    [System.Serializable]
    public class PrefabForPooling
    {
        public GameObject prefab;
        public int amountToBuffer = 3;
    }
}