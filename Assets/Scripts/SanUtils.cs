using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;

namespace com.bhambhoo.fairludo
{
    public class SanUtils : MonoBehaviour
    {
        private static Dictionary<Transform, AudioSource[]> transformAudioSourceMap;
        private static int ignoreRaycast = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));

        /// <summary>
        /// Returns a random element from the specified enum type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T RandomEnumValue<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(new System.Random().Next(v.Length));
        }

        /// <summary>
        /// This function returns a random string from the provided array of strings.
        /// </summary>
        /// <param name="arrayOfAnything"></param>
        /// <returns></returns>
        public static object GetRandomArrayElement(object[] arrayOfAnything)
        {
            return arrayOfAnything[UnityEngine.Random.Range(0, arrayOfAnything.Length)];
        }

        public static void PlaySound(AudioClip clip)
        {
            PlaySound(clip, MatchManager.Instance.audioSource);
        }

        // Stops the audio source and plays the given clip on it
        public static void PlaySound(AudioClip clip, AudioSource audioSource)
        {
            if (clip == null)
            {
                Debug.LogError("Trying to play audio but no clip to play for audioSource on " + audioSource.gameObject.name);
                return;
            }
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.time = 0;
            audioSource.loop = false;
            audioSource.Play();
        }
        public static void PlaySoundIfNotPlaying(AudioClip clip, AudioSource audioSource)
        {
            if (clip == null || audioSource.isPlaying)
            {
                return;
            }
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.time = 0;
            audioSource.loop = false;
            audioSource.Play();
        }

        // Assign text, onClickListener, image, etc. by item.transform.find("Text").GetComponent<Text>().text = "anything";
        public delegate void OnListItemGenerate(GameObject item, int itemIndex);
        public delegate void OnListItemAdd(GameObject item);
        public static void PopulateList(GameObject listContainer, GameObject listItemPrefab, int numberOfItems, OnListItemGenerate onListItemGenerate)
        {
            // Clear all items from list
            listContainer.SetActive(false);
            foreach (Transform child in listContainer.transform)
            {
                Destroy(child.gameObject);
            }
            listContainer.SetActive(true);

            for (int i = 0; i < numberOfItems; i++)
            {
                // Instantiate listitem and add it to the list container
                GameObject listItem = Instantiate(listItemPrefab) as GameObject;
                listItem.transform.SetParent(listContainer.transform, false);
                listItem.transform.localScale = new Vector3(1, 1, 1);
                if (onListItemGenerate != null)
                    onListItemGenerate(listItem, i);
            }
            listContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(listContainer.GetComponent<RectTransform>().sizeDelta.x, numberOfItems * listItemPrefab.GetComponent<RectTransform>().sizeDelta.y);
        }
        public static void PopulateList(GameObject listContainer, GameObject listItemPrefab, int numberOfItems, float extraOffset, OnListItemGenerate onListItemGenerate)
        {
            // Clear all items from list
            listContainer.SetActive(false);
            foreach (Transform child in listContainer.transform)
            {
                Destroy(child.gameObject);
            }
            listContainer.SetActive(true);

            for (int i = 0; i < numberOfItems; i++)
            {
                // Instantiate listitem and add it to the list container
                GameObject listItem = Instantiate(listItemPrefab) as GameObject;
                listItem.transform.SetParent(listContainer.transform, false);
                listItem.transform.localScale = new Vector3(1, 1, 1);
                if (onListItemGenerate != null)
                    onListItemGenerate(listItem, i);
            }
            listContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(listContainer.GetComponent<RectTransform>().sizeDelta.x, numberOfItems * listItemPrefab.GetComponent<RectTransform>().sizeDelta.y + extraOffset);
        }
        public static void AddListItemToList(GameObject listContainer, GameObject listItemPrefab, OnListItemAdd onListItemAdd)
        {
            GameObject listItem = Instantiate(listItemPrefab) as GameObject;
            listItem.transform.SetParent(listContainer.transform, false);
            if (onListItemAdd != null)
                onListItemAdd(listItem);
        }
        public static void AddListItemToScrollableList(GameObject listContainer, GameObject listItemPrefab, OnListItemAdd onListItemAdd)
        {
            GameObject listItem = Instantiate(listItemPrefab) as GameObject;
            listItem.transform.SetParent(listContainer.transform, false);
            listItem.transform.localScale = new Vector3(1, 1, 1);
            if (onListItemAdd != null)
                onListItemAdd(listItem);
            listContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(listContainer.GetComponent<RectTransform>().sizeDelta.x, listContainer.transform.childCount * listItemPrefab.GetComponent<RectTransform>().sizeDelta.y);
        }
        public static void ClearList(Transform listContainer)
        {
            listContainer.DestroyChildren();
        }

        // Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.
        public static string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        public static Color HexToColor(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

        // Determines if the targetObject is within sight of the transform. It will set the angle regardless of whether or not the object is within sight
        private static Transform WithinSight(Transform transform, Vector3 positionOffset, float fieldOfViewAngle, float viewDistance, Transform targetObject, bool usePhysics2D, out float angle, int layerMask)
        {
            // The target object needs to be within the field of view of the current object
            var direction = targetObject.position - (transform.TransformPoint(positionOffset));
            direction.y = 0;
            if (usePhysics2D)
            {
                angle = Vector3.Angle(direction, transform.up);
            }
            else
            {
                angle = Vector3.Angle(direction, transform.forward);
            }
            if (direction.magnitude < viewDistance && angle < fieldOfViewAngle * 0.5f)
            {
                // The hit agent needs to be within view of the current agent
                if (LineOfSight(transform, positionOffset, targetObject, usePhysics2D, layerMask) != null)
                {
                    return targetObject; // return the target object meaning it is within sight
                }
                else if (targetObject.GetComponent<Collider>() == null)
                {
                    // If the linecast doesn't hit anything then that the target object doesn't have a collider and there is nothing in the way
                    if (targetObject.gameObject.activeSelf)
                        return targetObject;
                }
            }
            // return null if the target object is not within sight
            return null;
        }

        public static Transform LineOfSight(Transform transform, Vector3 positionOffset, Transform targetObject, bool usePhysics2D, int layerMask)
        {
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2)
            if (usePhysics2D)
            {
                RaycastHit2D hit;
                if ((hit = Physics2D.Linecast(transform.TransformPoint(positionOffset), targetObject.position)))
                {
                    if (hit.transform.Equals(targetObject))
                    {
                        return targetObject; // return the target object meaning it is within sight
                    }
                }
            }
            else
            {
#endif
                RaycastHit hit;

                if (Physics.Raycast(transform.position, targetObject.position, out hit, 2000f))
                {
                    if (hit.transform.name == "Soldier")
                        print("RaycastHits " + hit.transform.name);
                    if (ContainsTransform(targetObject, hit.transform))
                    {
                        Debug.DrawLine(transform.position, hit.transform.position);
                        return targetObject;
                    }
                }

                // C2
                //if (Physics.Raycast(new Ray(transform.position, targetObject.position - transform.position), out hit, 2000f, layerMask, QueryTriggerInteraction.Collide))
                //{
                //    print("TargetObject: " + targetObject.name+ " RaycastHit : "+hit.transform.name);
                //    if (ContainsTransform(targetObject, hit.transform))
                //    {
                //        Debug.DrawLine(transform.position, hit.transform.position);
                //        return targetObject; // return the target object meaning it is within sight
                //    }
                //}

                //if (Physics.Linecast(transform.TransformPoint(positionOffset), targetObject.position, out hit, layerMask))
                //{
                //    if (ContainsTransform(targetObject, hit.transform))
                //    {
                //        Debug.DrawLine(transform.position, hit.transform.position);
                //        return targetObject; // return the target object meaning it is within sight
                //    }
                //}
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2)
            }
#endif
            return null;
        }

        // Returns true if the target transform is a child of the parent transform
        private static bool ContainsTransform(Transform target, Transform parent)
        {
            if (target == null)
            {
                return false;
            }
            if (target.Equals(parent))
            {
                return true;
            }
            return ContainsTransform(target.parent, parent);
        }

        // Public helper function that will automatically create an angle variable that is not used. This function is useful if the calling object doesn't
        // care about the angle between transform and targetObject
        public static Transform WithinSight(Transform transform, Vector3 positionOffset, float fieldOfViewAngle, float viewDistance, Transform targetObject)
        {
            float angle;
            return WithinSight(transform, positionOffset, fieldOfViewAngle, viewDistance, targetObject, false, out angle, ignoreRaycast);
        }

        // Public helper function that will automatically create an audibility variable that is not used. This function is useful if the calling call doesn't
        // care about the audibility value
        public static Transform WithinHearingRange(Transform transform, Vector3 positionOffset, float audibilityThreshold, Transform targetObject)
        {
            float audibility = 0;
            return WithinHearingRange(transform, positionOffset, audibilityThreshold, targetObject, ref audibility);
        }

        // Cast a sphere with the desired radius. Check each object's audio source to see if audio is playing. If audio is playing
        // and its audibility is greater than the audibility threshold then return the object heard
        public static Transform WithinHearingRange(Transform transform, Vector3 positionOffset, float audibilityThreshold, float hearingRadius, LayerMask objectLayerMask)
        {
            Transform objectHeard = null;
            var hitColliders = Physics.OverlapSphere(transform.TransformPoint(positionOffset), hearingRadius, objectLayerMask);
            if (hitColliders != null)
            {
                float maxAudibility = 0;
                for (int i = 0; i < hitColliders.Length; ++i)
                {
                    float audibility = 0;
                    Transform obj;
                    // Call the WithinHearingRange function to determine if this specific object is within hearing range
                    if ((obj = WithinHearingRange(transform, positionOffset, audibilityThreshold, hitColliders[i].transform, ref audibility)) != null)
                    {
                        // This object is within hearing range. Set it to the objectHeard GameObject if the audibility is less than any of the other objects
                        if (audibility > maxAudibility)
                        {
                            maxAudibility = audibility;
                            objectHeard = obj;
                        }
                    }
                }
            }
            return objectHeard;
        }

        private static Transform WithinHearingRange(Transform transform, Vector3 positionOffset, float audibilityThreshold, Transform targetObject, ref float audibility)
        {
            AudioSource[] colliderAudioSource;
            // Check to see if the hit agent has an audio source and that audio source is playing
            if ((colliderAudioSource = GetAudioSources(targetObject)) != null)
            {
                for (int i = 0; i < colliderAudioSource.Length; ++i)
                {
                    if (colliderAudioSource[i].isPlaying)
                    {
                        var distance = Vector3.Distance(transform.position, targetObject.position);
                        if (colliderAudioSource[i].rolloffMode == AudioRolloffMode.Logarithmic)
                        {
                            audibility = colliderAudioSource[i].volume / Mathf.Max(colliderAudioSource[i].minDistance, distance - colliderAudioSource[i].minDistance);
                        }
                        else
                        { // linear
                            audibility = colliderAudioSource[i].volume * Mathf.Clamp01((distance - colliderAudioSource[i].minDistance) / (colliderAudioSource[i].maxDistance - colliderAudioSource[i].minDistance));
                        }
                        if (audibility > audibilityThreshold)
                        {
                            return targetObject;
                        }
                    }
                }
            }
            return null;
        }

        // Caches the AudioSource GetComponents for quick lookup
        private static AudioSource[] GetAudioSources(Transform target)
        {
            if (transformAudioSourceMap == null)
            {
                transformAudioSourceMap = new Dictionary<Transform, AudioSource[]>();
            }

            AudioSource[] audioSources;
            if (transformAudioSourceMap.TryGetValue(target, out audioSources))
            {
                return audioSources;
            }

            audioSources = target.GetComponentsInChildren<AudioSource>();
            transformAudioSourceMap.Add(target, audioSources);
            return audioSources;
        }

        // Cast a sphere with the desired distance. Check each collider hit to see if it is within the field of view. Set objectFound
        // to the object that is most directly in front of the agent
        public static Transform WithinSight(Transform transform, Vector3 positionOffset, float fieldOfViewAngle, float viewDistance, LayerMask objectLayerMask)
        {
            Transform objectFound = null;
            var hitColliders = Physics.OverlapSphere(transform.position, viewDistance, objectLayerMask);
            if (hitColliders != null)
            {
                float minAngle = Mathf.Infinity;
                for (int i = 0; i < hitColliders.Length; ++i)
                {
                    float angle;
                    Transform obj;
                    // Call the WithinSight function to determine if this specific object is within sight
                    if ((obj = WithinSight(transform, positionOffset, fieldOfViewAngle, viewDistance, hitColliders[i].transform, false, out angle, objectLayerMask)) != null)
                    {
                        // This object is within sight. Set it to the objectFound GameObject if the angle is less than any of the other objects
                        if (angle < minAngle)
                        {
                            minAngle = angle;
                            objectFound = obj;
                        }
                    }
                }
            }
            return objectFound;
        }

        public static string MD5(string inputString)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bs = System.Text.Encoding.UTF8.GetBytes(inputString);
            bs = x.ComputeHash(bs);
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            return s.ToString();

        }

        public static void ChangeBit(ref byte aByte, int pos, bool value)
        {
            if (value)
            {
                //left-shift 1, then bitwise OR
                aByte = (byte)(aByte | (1 << pos));
            }
            else
            {
                //left-shift 1, then take complement, then bitwise AND
                aByte = (byte)(aByte & ~(1 << pos));
            }
        }

        public static bool GetBit(byte aByte, int pos)
        {
            //left-shift 1, then bitwise AND, then check for non-zero
            return ((aByte & (1 << pos)) != 0);
        }

        /// <summary>
        /// Returns the suffix of a given number (rank). For example pass in 1, it will return "st" which means 1st. For 2 -> "nd", for 3 -> "rd" and so on.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string GetRankSuffix(int i)
        {
            var j = i % 10;
            var k = i % 100;
            if (j == 1 && k != 11)
            {
                return "st";
            }
            if (j == 2 && k != 12)
            {
                return "nd";
            }
            if (j == 3 && k != 13)
            {
                return "rd";
            }
            return "th";
        }
    }

    public static class Helper
    {
        public static GameObject FindInChildren(this GameObject go, string name)
        {
            return (from x in go.GetComponentsInChildren<Transform>()
                    where x.gameObject.name == name
                    select x.gameObject).First();
        }

        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return "{" + string.Join(",", dictionary.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";
        }

        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars - 3) + "...";
        }

        public static void DestroyChildren(this Transform parent)
        {
            foreach (Transform child in parent)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}