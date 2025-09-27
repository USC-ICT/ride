using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public static class VHMath
{
    public enum Axis
    {
        X,
        Y,
        Z,
    }

    public static uint TurnFlagOn(ref uint data, uint flag)
    {
        return data |= flag;
    }

    public static uint TurnFlagOff(ref uint data, uint flag)
    {
        return data &= ~flag;
    }

    public static uint ToggleFlag(ref uint data, uint flag)
    {
        return data ^= flag;
    }

    public static bool IsFlagOn(uint data, uint flag)
    {
        return (data & flag) == flag;
    }


    public static Vector3 ConvertStringsToVector(string x, string y, string z)
    {
        Vector3 retVal = new Vector3();
        if (!float.TryParse(x, out retVal.x))
        {
            Debug.LogError("ConvertStringsToVector failed on x string: " + x);
        }

        if (!float.TryParse(y, out retVal.y))
        {
            Debug.LogError("ConvertStringsToVector failed on y string: " + y);
        }

        if (!float.TryParse(z, out retVal.z))
        {
            Debug.LogError("ConvertStringsToVector failed on z string: " + z);
        }
        return retVal;
    }


    /// <summary>
    /// Rotate an object towards the target position
    /// </summary>
    /// <param name="turner"></param>
    /// <param name="targetPosition"></param>
    /// <param name="turnRateInDegrees"></param>
    public static Coroutine TurnTowardsTarget(MonoBehaviour turnController, GameObject turner, Vector3 targetPosition, float turnRateInDegrees)
    {
        return turnController.StartCoroutine(Internal_TurnTowardsTarget(turner, targetPosition, turnRateInDegrees));
    }

    static IEnumerator Internal_TurnTowardsTarget(GameObject turner, Vector3 targetPosition, float turnRateInDegrees)
    {
        Vector3 initialOrientation = turner.transform.forward;
        Vector3 targetRotation = (targetPosition - turner.transform.position).normalized;
        float t = 0;
        float secondsToComplete = Vector3.Angle(turner.transform.forward, targetRotation) / turnRateInDegrees;

        while (t < secondsToComplete)
        {
            turner.transform.forward = Vector3.Slerp(initialOrientation, targetRotation, t / secondsToComplete);
            yield return new WaitForEndOfFrame();
            t += Time.deltaTime;
        }
    }

    public delegate void ColorDelegate(Color c);

    public static void LerpColorOverTime(MonoBehaviour coroutineOject, Color startColor, Color endColor, float time, ColorDelegate callback)
    {
        // usage:
        // VHUtils.LerpColorOverTime(this, Color.blue, Color.red, 1, (c) => { GameObject.Find("Plane").GetComponent<Renderer>().material.color = c; } );

        coroutineOject.StartCoroutine(LerpColorOverTime_Internal(startColor, endColor, time, callback));
    }

    static IEnumerator LerpColorOverTime_Internal(Color startColor, Color endColor, float time, ColorDelegate callback)
    {
        callback(startColor);

        float startTime = Time.time;
        float curTime = Time.time - startTime;
        while (curTime < time)
        {
            curTime = Time.time - startTime;

            Color c = Color.Lerp(startColor, endColor, curTime / time);
            callback(c);

            yield return new WaitForEndOfFrame();
        }

        callback(endColor);
    }

    public delegate void Vector3Delegate(Vector3 v);

    public static void LerpVector3OverTime(MonoBehaviour coroutineOject, Vector3 startPosition, Vector3 endPosition, float time, Vector3Delegate callback)
    {
        // usage:
        // VHUtils.LerpVector3OverTime(this, new Vector3(0, 10, 0), new Vector3(10, 0, 0), 1, (v) => { GameObject.Find("Plane").transform.position = v; } );

        coroutineOject.StartCoroutine(LerpVector3OverTime_Internal(startPosition, endPosition, time, callback));
    }

    static IEnumerator LerpVector3OverTime_Internal(Vector3 startPosition, Vector3 endPosition, float time, Vector3Delegate callback)
    {
        callback(startPosition);

        float startTime = Time.time;
        float curTime = Time.time - startTime;
        while (curTime < time)
        {
            curTime = Time.time - startTime;

            Vector3 v = Vector3.Lerp(startPosition, endPosition, curTime / time);
            callback(v);

            yield return new WaitForEndOfFrame();
        }

        callback(endPosition);
    }

    public delegate void QuaternionDelegate(Quaternion v);

    public static void LerpQuaternionOverTime(MonoBehaviour coroutineOject, Quaternion startRotation, Quaternion endRotation, float time, QuaternionDelegate callback)
    {
        // usage:
        // VHUtils.LerpQuaternionOverTime(this, Quaternion.Euler(new Vector3(0, 0, 0)), Quaternion.Euler(new Vector3(0, 90, 0)), 3, (q) => { m_camera.transform.rotation = q; } );

        coroutineOject.StartCoroutine(LerpQuaternionOverTime_Internal(startRotation, endRotation, time, callback));
    }

    static IEnumerator LerpQuaternionOverTime_Internal(Quaternion startRotation, Quaternion endRotation, float time, QuaternionDelegate callback)
    {
        callback(startRotation);

        float startTime = Time.time;
        float curTime = Time.time - startTime;
        while (curTime < time)
        {
            curTime = Time.time - startTime;

            Quaternion q = Quaternion.Lerp(startRotation, endRotation, curTime / time);
            callback(q);

            yield return new WaitForEndOfFrame();
        }

        callback(endRotation);
    }

    public delegate void TransformDelegate(Vector3 position, Quaternion rotation);

    public static void LerpTransformOverTime(MonoBehaviour coroutineOject, Transform startTransform, Transform endTransform, float time, TransformDelegate callback)
    {
        // usage:
        // VHUtils.LerpTransformOverTime(this, m_camera.transform, GameObject.Find("Plane").transform, 3, (v, q) => { m_camera.transform.position = v; m_camera.transform.rotation = q; } );

        coroutineOject.StartCoroutine(LerpTransformOverTime_Internal(startTransform, endTransform, time, callback));
    }

    static IEnumerator LerpTransformOverTime_Internal(Transform startTransform, Transform endTransform, float time, TransformDelegate callback)
    {
        startTransform.GetPositionAndRotation(out Vector3 startPosition, out Quaternion startRotation);
        endTransform.GetPositionAndRotation(out Vector3 endPosition, out Quaternion endRotation);
        callback(startPosition, startRotation);

        float startTime = Time.time;
        float curTime = Time.time - startTime;
        while (curTime < time)
        {
            curTime = Time.time - startTime;

            Vector3 v = Vector3.Lerp(startPosition, endPosition, curTime / time);
            Quaternion q = Quaternion.Lerp(startRotation, endRotation, curTime / time);
            callback(v, q);

            yield return new WaitForEndOfFrame();
        }

        callback(endPosition, endRotation);
    }

    public static void MoveTowardsOverTime(MonoBehaviour coroutineOject, Transform transformToModify, Vector3 endPosition, float unitsPerSecond)
    {
        // targetTransform is a reference, so it adjusts if outside influences change the position

        // this can potentially run forever if never reaches the destination.  Can somewhat easily happen if this is called more than once before finishing.

        coroutineOject.StartCoroutine(MoveTowardsOverTime_Internal(transformToModify, endPosition, unitsPerSecond));
    }

    static IEnumerator MoveTowardsOverTime_Internal(Transform transformToModify, Vector3 endPosition, float unitsPerSecond)
    {
        // move towards the destination.  exit out when we get there
        while (transformToModify.position != endPosition)
        {
            float step = unitsPerSecond * Time.deltaTime;
            transformToModify.position = Vector3.MoveTowards(transformToModify.position, endPosition, step);

            yield return new WaitForEndOfFrame();
        }

        //Debug.Log("MoveTowardsOverTime_Internal() - " + transformToModify.gameObject.name);
    }

    public static void RotateTowardsOverTime(MonoBehaviour coroutineOject, Transform transformToModify, Quaternion endRotation, float degreesPerSecond)
    {
        // targetTransform is a reference, so it adjusts if outside influences change the rotation

        // this can potentially run forever if never reaches the destination.  Can somewhat easily happen if this is called more than once before finishing.

        coroutineOject.StartCoroutine(RotateTowardsOverTime_Internal(transformToModify, endRotation, degreesPerSecond));
    }

    static IEnumerator RotateTowardsOverTime_Internal(Transform transformToModify, Quaternion endRotation, float degreesPerSecond)
    {
        // rotate towards the destination.  exit out when we get there
        while (transformToModify.rotation.eulerAngles != endRotation.eulerAngles)
        {
            float rotStep = degreesPerSecond * Time.deltaTime;
            transformToModify.rotation = Quaternion.RotateTowards(transformToModify.rotation, endRotation, rotStep);

            yield return new WaitForEndOfFrame();
        }

        //Debug.Log("RotateTowardsOverTime_Internal() - " + transformToModify.gameObject.name);
    }

    public static void MoveAndRotateTowardsOverTime(MonoBehaviour coroutineOject, Transform transformToModify, Vector3 endPosition, float unitsPerSecond, Quaternion endRotation, float degreesPerSecond)
    {
        // targetTransform is a reference, so it adjusts if outside influences change the position

        // this can potentially run forever if never reaches the destination.  Can somewhat easily happen if this is called more than once before finishing.

        coroutineOject.StartCoroutine(MoveAndRotateTowardsOverTime_Internal(transformToModify, endPosition, unitsPerSecond, endRotation, degreesPerSecond));
    }

    static IEnumerator MoveAndRotateTowardsOverTime_Internal(Transform transformToModify, Vector3 endPosition, float unitsPerSecond, Quaternion endRotation, float degreesPerSecond)
    {
        // move and rotate towards the destination.  exit out when we get there
        while (transformToModify.position != endPosition ||
               transformToModify.rotation.eulerAngles != endRotation.eulerAngles)
        {
            float step = unitsPerSecond * Time.deltaTime;
            float rotStep = degreesPerSecond * Time.deltaTime;
            transformToModify.SetPositionAndRotation(
                Vector3.MoveTowards(transformToModify.position, endPosition, step), 
                Quaternion.RotateTowards(transformToModify.rotation, endRotation, rotStep));
            yield return new WaitForEndOfFrame();
        }

        //Debug.Log("MoveAndRotateTowardsOverTime_Internal() - " + transformToModify.gameObject.name);
    }

    /// <summary>
    /// Returns the time in seconds that it will take to get from startPos to endPos
    /// given a constant speed
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <param name="speed"></param>
    /// <returns></returns>
    public static float GetTimeToReachPosition(Vector3 startPos, Vector3 endPos, float speed)
    {
        if (speed == 0)
        {
            Debug.LogError("You'll never reach your position because you aren't moving");
            return 0;
        }
        return (endPos - startPos).magnitude / Math.Abs(speed);
    }


    public static T GetMax<T>(T[] data) where T : IComparable<T>
    {
        // Array.Max() is available in newer .net

        T retVal = data[0];

        for (int i = 1; i < data.Length; i++)
        {
            if (retVal.CompareTo(data[i]) < 0)
                retVal = data[i];
        }

        return retVal;
    }


    public static T GetMin<T>(T[] data) where T : IComparable<T>
    {
        // Array.Min() is available in newer .net

        T retVal = data[0];

        for (int i = 1; i < data.Length; i++)
        {
            if (retVal.CompareTo(data[i]) > 0)
                retVal = data[i];
        }

        return retVal;
    }


    public static Quaternion CreateQuat(float x, float y, float z, float degrees)
    {
        Quaternion q = new Quaternion();
        float radians = Mathf.PI * degrees / 180.0f;
        //float radians = degrees * DEG_TO_RAD;

        // normalize axis:
        q.x = x;
        q.y = y;
        q.z = z;

        float f = x * x + y * y + z * z;

        if (f > 0)
        {
            f = Mathf.Sqrt(f);
            q.x /= f;
            q.y /= f;
            q.z /= f;
        }

        // set the quaternion:
        radians /= 2;
        f = Mathf.Sin(radians);
        q.x *= f;
        q.y *= f;
        q.z *= f;
        q.w = Mathf.Cos(radians);
        return q;
    }


    public static void QuatToAxisAngle(Quaternion q, out Vector4 aa, bool normalizeAxis)
    {
        aa = new Vector4();
        float ang = 2.0f * Mathf.Acos(q.w);
        //q.y *= -1;
        //q.z *= -1;
        float norm = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z);
        if (norm == 0 || ang == 0)
        {
            aa[0] = 0;
            aa[1] = 0;
            aa[2] = 0;
        }
        else
        {
            aa[0] = (q[0] / norm);
            aa[1] = (q[1] / norm);
            aa[2] = (q[2] / norm);

            if (!normalizeAxis)
            {
                aa[0] *= ang;
                aa[1] *= ang;
                aa[2] *= ang;
            }
        }

        aa[3] = ang * 180.0f / Mathf.PI;
    }


    public static Quaternion AxisAngleToQuat(Vector3 axisAngle)
    {
        // normalize it and extract the angles
        float angle = axisAngle.sqrMagnitude;
        if (angle > 0)
        {
            angle = axisAngle.magnitude;
            axisAngle.Normalize();
        }

        angle *= 0.5f;
        float w = Mathf.Cos(angle);
        angle = Mathf.Sin(angle);
        return new Quaternion(axisAngle.x * angle, axisAngle.y * angle, axisAngle.z * angle, w);
    }


    /// <summary>
    /// Returns true if the rects are overlapping
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    static public bool IsRectOverlapping(Rect a, Rect b)
    {
        return a.x < b.xMax && a.y < b.yMax && a.xMax > b.x && a.yMax > b.y;
    }

    static public bool RectContainsRect(Rect a, Rect b)
    {
        return a.Contains(new Vector2(b.x, b.y)) && a.Contains(new Vector2(b.xMax, b.y))
            && a.Contains(new Vector2(b.x, b.yMax)) && a.Contains(new Vector2(b.xMax, b.yMax));
    }

    /// <summary>
    /// returns the amount of overlap between the 2 rectangles on the x axis
    /// 0 if they aren't overlapping
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    static public float GetRectangleOverlapX(Rect a, Rect b)
    {
        if (!VHMath.IsRectOverlapping(a, b))
        {
            return 0;
        }

        return a.x > b.x ? b.xMax - a.x : b.x - a.xMax;
    }


    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        if (val.CompareTo(min) < 0) return min;
        else if(val.CompareTo(max) > 0) return max;
        else return val;
    }


    public static int IncrementWithRollover(int value, int max)
    {
        if (max == 0)
            return 0;

        return (value + 1) % max;
    }

    public static int DecrementWithRollover(int value, int max)
    {
        if (max == 0)
            return 0;

        return (value == 0) ? max - 1 : value - 1;
    }

    public static bool IsFacing(Vector3 lookDir, Vector3 startPos, Vector3 targetPos)
    {
        return Vector3.Dot(lookDir, targetPos - startPos) > 0;
    }

    /// <summary>
    /// Returns y-axis rotation
    /// </summary>
    /// <param name="start"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static float GetRotationFromToEuler(Vector3 start, Vector3 target)
    {
        return GetRotationFromTo(start, target).eulerAngles.y;
    }

    public static Quaternion GetRotationFromTo(Vector3 start, Vector3 target)
    {
        Vector3 facingDir = target - start;
        Quaternion rotation = Quaternion.LookRotation(facingDir);
        return rotation;
    }

    /// <summary>
    /// Clamp rotational values between -180 and 180
    /// </summary>
    /// <param name="rotValue"></param>
    /// <returns></returns>
    public static float Clamp180(float rotValue)
    {
        float value = rotValue;
        if (Mathf.Abs(rotValue) > 180)
        {
            value = rotValue + ((Mathf.Abs(rotValue) / -rotValue) * 360f);
        }
        return value;
    }
}
}
