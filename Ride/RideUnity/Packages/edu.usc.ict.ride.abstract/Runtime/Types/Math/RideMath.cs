using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ride.Entities;

namespace Ride
{
    /// <summary>
    /// A comprehensive math helper library for simulation and geometry.
    /// Includes constants, trigonometric operations, angle conversions, rounding,
    /// easing functions, statistical operations, and raycasting wrappers tailored for RIDE.
    /// This class mirrors Unity's Mathf API but adds simulation-specific behavior.
    /// </summary>
    public static class RideMath
    {
        #region Constants

        public const float Deg2Rad = Mathf.Deg2Rad;
        public const float Rad2Deg = Mathf.Rad2Deg;
        public const float Infinity = Mathf.Infinity;
        public const float NegativeInfinity = Mathf.NegativeInfinity;
        public const float PI = Mathf.PI;
        public const float TwoPI = Mathf.PI * 2f;

        #endregion

        #region Core Math Functions

        /// <summary>
        /// Returns the larger of the 2 values
        /// </summary>
        /// <param name="a">Value a</param>
        /// <param name="b">Value b</param>
        /// <returns>The larger value</returns>
        public static int Max(int a, int b) => Mathf.Max(a, b);

        /// <summary>
        /// Returns the larger of the 2 values
        /// </summary>
        /// <param name="a">Value a</param>
        /// <param name="b">Value b</param>
        /// <returns>The larger value</returns>
        public static float Max(float a, float b) => Mathf.Max(a, b);

        /// <summary>
        /// Returns the largest value in the set
        /// </summary>
        /// <param name="values"></param>
        /// <returns>The largest value in the set</returns>
        public static float Max(params int[] values) => Mathf.Max(values);

        /// <summary>
        /// Returns the largest value in the set
        /// </summary>
        /// <param name="values"></param>
        /// <returns>The largest value in the set</returns>
        public static float Max(params float[] values) => Mathf.Max(values);

        /// <summary>
        /// Returns the smaller of the 2 values
        /// </summary>
        /// <param name="a">Value a</param>
        /// <param name="b">Value b</param>
        /// <returns>The smaller value</returns>
        public static int Min(int a, int b) => Mathf.Min(a, b);

        /// <summary>
        /// Returns the smaller of the 2 values
        /// </summary>
        /// <param name="a">Value a</param>
        /// <param name="b">Value b</param>
        /// <returns>The smaller value</returns>
        public static float Min(float a, float b) => Mathf.Min(a, b);

        /// <summary>
        /// Returns the smallest value in the set
        /// </summary>
        /// <param name="values"></param>
        /// <returns>The smallest value in the set</returns>
        public static float Min(params int[] values) => Mathf.Min(values);

        /// <summary>
        /// Returns the smallest value in the set
        /// </summary>
        /// <param name="values"></param>
        /// <returns>The smallest value in the set</returns>
        public static float Min(params float[] values) => Mathf.Min(values);

        /// <summary>
        /// Returns the sum of all the values in the set
        /// </summary>
        /// <param name="values"></param>
        /// <returns>The smallest value in the set</returns>
        public static float Sum(params float[] values) => values == null ? 0f : values.Sum();

        /// <summary>
        /// Clamps the given value between the min and the max range.  If the value is between min and max, value is returned
        /// </summary>
        /// <param name="value">The value to clamp</param>
        /// <param name="min">The minimum value returned</param>
        /// <param name="max">The maximum value returned</param>
        /// <returns>If the value is between min and max, value is returned.
        /// If value is larger than max, max is returned.
        /// If value is smaller than min, min is returned. </returns>
        public static int Clamp(int value, int min, int max) => Mathf.Clamp(value, min, max);

        /// <summary>
        /// Clamps the given value between the min and the max range.  If the value is between min and max, value is returned
        /// </summary>
        /// <param name="value">The value to clamp</param>
        /// <param name="min">The minimum value returned</param>
        /// <param name="max">The maximum value returned</param>
        /// <returns>If the value is between min and max, value is returned.
        /// If value is larger than max, max is returned.
        /// If value is smaller than min, min is returned. </returns>
        public static float Clamp(float value, float min, float max) => Mathf.Clamp(value, min, max);

        /// <summary>
        /// Clamps the given value between the 0 and 1.  If the value is between 0 and 1, value is returned
        /// </summary>
        /// <param name="value">The value to clamp</param>
        /// <returns>If the value is between 0 and 1, value is returned</returns>
        public static float Clamp01(float value) => Mathf.Clamp01(value);

        public static int Abs(int v) => Mathf.Abs(v);

        public static float Abs(float v) => Mathf.Abs(v);

        /// <summary>
        /// C#'s % operator uses a truncated definition. This is an alternative floored definition
        /// </summary>
        /// <param name="a">Left side.</param>
        /// <param name="b">Right side.</param>
        /// <returns>a mod b</returns>
        public static int Mod(int a, int b) => (a % b + b) % b;

        /// <summary>
        /// Returns the square root of f
        /// </summary>
        /// <param name="f">The value to be squared</param>
        /// <returns>The square root of f</returns>
        public static float Sqrt(float f) => Mathf.Sqrt(f);

        /// <summary>
        /// Returns f raised to power of p.
        /// </summary>
        /// <param name="f">The value to be raised</param>
        /// <param name="p">The power</param>
        /// <returns>f raised to power of p</returns>
        public static float Pow(float f, float p) => Mathf.Pow(f, p);

        /// <summary>
        /// Increments the value by 1 and wraps to 0 if it reaches max.
        /// </summary>
        /// <param name="value">Current value</param>
        /// <param name="max">Max bound (exclusive)</param>
        /// <returns>Incremented value with wraparound</returns>
        public static int IncrementWrap(int value, int max)
        {
            if (max == 0)
                return 0;

            return (value + 1) % max;
        }

        /// <summary>
        /// Decrements the value by 1 and wraps to max - 1 if it goes below 0.
        /// </summary>
        /// <param name="value">Current value</param>
        /// <param name="max">Max bound (exclusive)</param>
        /// <returns>Decremented value with wraparound</returns>
        public static int DecrementWrap(int value, int max)
        {
            if (max == 0)
                return 0;

            return (value == 0) ? max - 1 : value - 1;
        }

        #endregion

        #region Trig and Angle Math

        /// <summary>
        /// Calculates the sin of the angle
        /// </summary>
        /// <param name="angle">The angle, in radians</param>
        /// <returns>the sin of the angle</returns>
        public static float Sin(float angle) => Mathf.Sin(angle);

        /// <summary>
        /// Calculates the cosine of the angle
        /// </summary>
        /// <param name="angle">The angle, in radians</param>
        /// <returns>the cosine of the angle</returns>
        public static float Cos(float angle) => Mathf.Cos(angle);

        /// <summary>
        /// Calculates the arcosine of the angle.
        /// </summary>
        /// <param name="angle">The angle, in radians</param>
        /// <returns>the arcosine of the angle</returns>
        public static float Acos(float angle) => Mathf.Acos(angle);

        /// <summary>
        /// Returns the angle between the x-axis and a 2D vector starting at zero and terminating at (x,y)
        /// </summary>
        /// <param name="y">The value to be squared</param>
        /// <param name="x">The value to be squared</param>
        /// <returns>The angle in radians whose Tan is y/x</returns>
        public static float Atan2(float y, float x) => Mathf.Atan2(y, x);

        public static float DeltaAngle(float current, float target) => Mathf.DeltaAngle(current, target);

        #endregion

        #region Range Mapping & Conversion

        /// <summary>
        /// Converts the value from the old range to the new range while maintaining the same ratio of the value
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="oldMin">The minimum value of the old range</param>
        /// <param name="oldMax">The maximum value of the old range</param>
        /// <param name="newMin">The minimum value of the new range</param>
        /// <param name="newMax">The maximum value of the new range</param>
        /// <returns>The value converted into the new range</returns>
        public static float ConvertRange(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            float oldRange = oldMax - oldMin;
            if (Approximately(oldRange, 0))
                return newMin;

            float newRange = newMax - newMin;
            return (((value - oldMin) * newRange) / oldRange) + newMin;
        }

        public static float Lerp(float a, float b, float t) => Mathf.Lerp(a, b, t);

        #endregion

        #region Rounding / Flooring / Approximation

        /// <summary>
        /// Rounds the float to the nearest integer.
        /// </summary>
        /// <param name="f"></param>
        /// <returns>the float rounded to the nearest integer.</returns>
        public static float Round(float f) => Mathf.Round(f);

        /// <summary>
        /// Returns the nearest rounded integer
        /// </summary>
        /// <param name="f">The value to be rounded to the nearest integer</param>
        /// <returns>Returns f rounded to the nearest integer</returns>
        public static int RoundToInt(float f) => Mathf.RoundToInt(f);

        public static float Floor(float v) => Mathf.Floor(v);

        public static int FloorToInt(float v) => Mathf.FloorToInt(v);

        public static bool Approximately(float a, float b) => Mathf.Approximately(a, b);

        #endregion

        #region Statistical Functions

        /// <summary>
        /// Calculates the mean average of the set
        /// </summary>
        /// <param name="values">Set of values</param>
        /// <returns>The mean average</returns>
        public static float Mean(params float[] values) => (values == null || values.Length == 0) ? 0 : Sum(values) / values.Length;

        /// <summary>
        /// Calculates the weighted mean average of the set
        /// </summary>
        /// <param name="values">Set of values</param>
        /// <param name="weights">Weights corresponding to the values</param>
        /// <returns>The mean average</returns>
        public static float WeightedMean(float[] values, float[] weights)
        {
            if (values == null || weights == null)
                return 0;

            int count = Min(values.Length, weights.Length);
            if (count == 0)
                return 0f;

            float weightedSum = 0;
            float weightTotal = 0;
            for (int i = 0; i < count; i++)
            {
                weightedSum += values[i] * weights[i];
                weightTotal += weights[i];
            }

            if (weightTotal <= 0)
                return 0;

            return weightedSum / weightTotal;
        }

        /// <summary>
        /// Calculates the standard deviation of the sample set
        /// </summary>
        /// <param name="samples">The population</param>
        /// <returns>The standard deviation</returns>
        public static float StandardDeviation(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return 0;

            int count = samples.Length;
            float mean = 0;
            for (int i = 0; i < count; i++)
                mean += samples[i];
            mean /= count;

            // For each sample, compute (x - mean)^2 and accumulate.
            // Final variance is average of squared differences.
            float variance = 0f;
            for (int i = 0; i < count; i++)
            {
                float diff = samples[i] - mean;
                variance += diff * diff;
            }

            variance /= count;
            return Sqrt(variance);
        }

        #endregion

        #region Easing / Smoothing Functions

        /// <summary>
        /// Smoothing function following an ease out cubic function.
        /// </summary>
        /// <param name="x">A value between 0 and 1, inclusive.</param>
        /// <returns>A smoothed value between 0 and 1, inclusive.</returns>
        public static float EaseOutCubic(float x) => 1 - Mathf.Pow(1 - x, 3);

        /// <summary>
        /// Smoothing function following an ease in out sine function.
        /// </summary>
        /// <param name="x">A value between 0 and 1, inclusive.</param>
        /// <returns>A smoothed value between 0 and 1, inclusive.</returns>
        public static float EaseInOutSine(float x) => -(Mathf.Cos(Mathf.PI * x) - 1) / 2;

        /// <summary>
        /// Smoothing function following an ease in out cubic function.
        /// </summary>
        /// <param name="x">A value between 0 and 1, inclusive.</param>
        /// <returns>A smoothed value between 0 and 1, inclusive.</returns>
        public static float EaseInOutCubic(float x) => x < 0.5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;

        public static float EaseInCirc(float x) => 1 - Mathf.Sqrt(1 - Mathf.Pow(x, 2));

        #endregion

        #region Raycasting and Physics Helpers

        /// <summary>Casts a ray from the main camera when the raycastButton is pressed.</summary>
        /// <param name="raycastButton">The mouse button that is required to press in order to cast a ray</param>
        /// <returns></returns>
        public static RideRaycastHit GetRaycastHitFromCamera(int raycastButton)
        {
            RideRaycastHit hit = RideRaycastHit.Null;
            var inputSystem = Globals.api?.inputSystem;
            var mouseButtonDown = inputSystem?.GetMouseButtonDown(raycastButton) ?? Input.GetMouseButtonDown(raycastButton);
            if (mouseButtonDown)
            {
                var cameraSystem = Globals.api?.cameraSystem;
                var mousePosition = inputSystem?.mousePosition.ToVector3() ?? Input.mousePosition;
                Ray r = cameraSystem?.ScreenPointToRay(mousePosition) ?? Camera.main.ScreenPointToRay(mousePosition);
                return GetRaycastHit(r.origin, r.direction, RideLayerMask.AllLayers);
            }

            return hit;
        }

        /// <summary>Casts a ray and returns the RaycastHit info.</summary>
        /// <param name="origin">origin of the ray</param>
        /// <param name="direction">direction of the ray</param>
        /// <param name="maxDistance">maximum distance of the ray</param>
        /// <param name="mask">layer mask</param>
        /// <param name="spreadFactor">the amount to modify the the axis of direction</param>
        /// <returns>The raycast hit info</returns>
        public static RideRaycastHit GetRaycastHit(RideVector3 origin, RideVector3 direction, float maxDistance, RideLayerMask mask, float spreadFactor = 0)
        {
            direction = Spread(direction, spreadFactor);
            Ray r = new Ray(origin, direction);
            bool isHit = Physics.Raycast(r, out RaycastHit hit, maxDistance, mask.value);
            RideID hitEntity = RideID.Null;
            if (isHit)
            {
                // check if the thing we hit is an entity
                IEntity e = hit.collider.GetComponent<IEntity>();
                if (e != null)
                    hitEntity = e.id;
            }

            return new RideRaycastHit(isHit, hitEntity, hit);
        }

        /// <summary>Casts a ray and returns the RaycastHit info.</summary>
        /// <param name="origin">origin of the ray</param>
        /// <param name="direction">direction of the ray</param>
        /// <param name="mask">layer mask</param>
        /// <param name="spreadFactor">the amount to modify the the axis of direction</param>
        /// <returns>The raycast hit info</returns>
        public static RideRaycastHit GetRaycastHit(RideVector3 origin, RideVector3 direction, RideLayerMask mask, float spreadFactor = 0) 
            => GetRaycastHit(origin, direction, Mathf.Infinity, mask, spreadFactor);

        public static RideRaycastHit[] GetRaycastHits(RideVector3 origin, RideVector3 direction, RideLayerMask mask, float spreadFactor = 0)
        {
            direction = Spread(direction, spreadFactor);
            var r = new Ray(origin, direction);
            var hits = Physics.RaycastAll(r, Mathf.Infinity, mask.value);
            var rideHits = new List<RideRaycastHit>();
            foreach (var hit in hits)
            {
                // check if the thing we hit is an entity
                IEntity e = hit.collider.GetComponent<IEntity>();

                RideID hitEntity = RideID.Null;
                if (e != null)
                    hitEntity = e.id;

                rideHits.Add(new RideRaycastHit(true, hitEntity, hit));
            }

            return rideHits.ToArray();
        }

        /// <summary>
        /// Physics Raycast
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="hit"></param>
        /// <returns>True if there was a hit on the given layer</returns>
        public static bool Raycast(RideRay ray, out RideRaycastHit hit)
        {
            hit = GetRaycastHit(ray.origin, ray.direction, RideLayerMask.AllLayers);
            return hit.isHit;
        }

        /// <summary>
        /// Physics Raycast
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="hit"></param>
        /// <param name="maxDistance"></param>
        /// <returns>True if there was a hit on the given layer</returns>
        public static bool Raycast(RideRay ray, out RideRaycastHit hit, float maxDistance)
        {
            hit = GetRaycastHit(ray.origin, ray.direction, maxDistance, RideLayerMask.AllLayers);
            return hit.isHit;
        }

        /// <summary>
        /// Physics Raycast
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="hit"></param>
        /// <param name="maxDistance"></param>
        /// <param name="layerMask"></param>
        /// <returns>True if there was a hit on the given layer</returns>
        public static bool Raycast(RideRay ray, out RideRaycastHit hit, float maxDistance, int layerMask)
        {
            bool isHit = Physics.Raycast(ray, out RaycastHit engineHit, maxDistance, layerMask);
            hit = new RideRaycastHit(isHit, RideID.Null, engineHit);
            return isHit;
        }

        /// <summary>
        /// Computes and stores colliders touching or inside the sphere.
        /// </summary>
        /// <param name="position">Center of the sphere.</param>
        /// <param name="radius">Radius of the sphere.</param>
        /// <param name="layerMask">A Layer mask defines which layers of colliders to include in the query.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        /// <returns>Returns an array with all RideID entities with colliders touching or inside the sphere.</returns>
        public static RideID[] OverlapSphere(RideVector3 position, float radius, RideLayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            var hits = Physics.OverlapSphere(position, radius, layerMask.value, queryTriggerInteraction);
            var results = new RideID[hits.Length];
            for (int i = 0; i < hits.Length; i++)
            {
                results[i] = Globals.api.gameObjectSystem.GetObject(hits[i].gameObject.GetInstanceID());
                if (results[i] == RideID.Null)
                    RideLog.LogError($"OverlapSphere(): A collider's gameobject '{hits[i].name}' was not found in the game object system.");
            }

            return results;
        }

        #endregion

        #region Geometry Calculations

        /// <summary>Determines the world positions around an object.</summary>
        /// <param name="coverObj">The object to surround</param>
        /// <param name="coverObjExtents">The boudning volume extents of the object to surround</param>
        /// <param name="extentsPadding">The additional space to add to the extent length so that you aren't on the edge of the cover object</param>
        /// <param name="fidelity">The number of surrounding points in a circle around the object to surround</param>
        /// <returns>The world positions surrounding the coverObj. This array's length is equal to fidelity</returns>
        public static RideVector3[] CalculateSurroundingPositions(Transform coverObj, RideBounds coverObjExtents, float extentsPadding = 1f, int fidelity = 4) =>
            CalculateSurroundingPositions(coverObj.position, coverObj.rotation, coverObjExtents.Extents, fidelity);

        /// <summary>Determines the world positions around an object.</summary>
        /// <param name="spatialObject">The object to surround</param>
        /// <param name="extentsPadding">The additional space to add to the extent length so that you aren't on the edge of the cover object</param>
        /// <param name="fidelity">>The number of surrounding points in a circle around the object to surround</param>
        /// <returns>The world positions surrounding the coverObj. This array's length is equal to fidelity</returns>
        public static RideVector3[] CalculateSurroundingPositions(ISpatialObject spatialObject, float extentsPadding = 1f, int fidelity = 4) =>
            CalculateSurroundingPositions(spatialObject.position, spatialObject.rotation, spatialObject.extents, extentsPadding, fidelity);

        /// <summary>Determines the world positions around an object.</summary>
        /// <param name="center">World position of the object to surround</param>
        /// <param name="orientation">World rotation of the object to surround</param>
        /// <param name="extentsFromCenter">Bounding volume extents of the object to surround</param>
        /// <param name="extentsPadding">The additional space to add to the extent length so that you aren't on the edge of the cover object</param>
        /// <param name="fidelity">The number of surrounding points in a circle around the object to surround</param>
        /// <returns>The world positions surrounding the coverObj. This array's length is equal to fidelity</returns>
        public static RideVector3[] CalculateSurroundingPositions(RideVector3 center, RideQuaternion orientation, RideVector3 extentsFromCenter, float extentsPadding = 1f, int fidelity = 4)
        {
            var points = new RideVector3[fidelity];
            float slice = 360 / fidelity;
            RideQuaternion rot = orientation;

            float displacement = Max(extentsFromCenter.x, extentsFromCenter.z) + extentsPadding;
            var bounds = new RideBounds(center, extentsFromCenter * 2f);

            for (int i = 0; i < fidelity; i++)
            {
                RideVector3 pointDirection = rot * RideVector3.forward;
                RideVector3 target = center + pointDirection;
                RideVector3 clamped = bounds.ClosestPoint(target);

                //points[i] = coverObjPos + pointDirection * displacement;
                points[i] = clamped + pointDirection * displacement;

                // rotate around the world up by the slice amount
                rot *= RideQuaternion.Euler(RideVector3.up * slice);
            }

            return points;
        }

        #endregion

        #region Vector Utilities

        /// <summary>
        /// Generates a vector with x, y, and z components between min and max
        /// </summary>
        /// <param name="min">The minimum x, y, and z values</param>
        /// <param name="max">The maximum x, y, and z values</param>
        /// <returns>Vector with x, y, and z components between min and max</returns>
        public static RideVector3 RandomVector(RideVector3 min, RideVector3 max)
        {
            return new RideVector3(
                RideRandom.Range(min.x, max.x),
                RideRandom.Range(min.y, max.y),
                RideRandom.Range(min.z, max.z));
        }

        /// <summary>Modifies the direction vector on all axis by an amount between -spreadFactor and +spreadFactor.</summary>
        /// <param name="direction"></param>
        /// <param name="spreadFactor"></param>
        /// <returns></returns>
        public static RideVector3 Spread(RideVector3 direction, float spreadFactor)
        {
            return new RideVector3(
                direction.x + RideRandom.Range(-spreadFactor, spreadFactor),
                direction.y + RideRandom.Range(-spreadFactor, spreadFactor),
                direction.z + RideRandom.Range(-spreadFactor, spreadFactor));
        }

        #endregion

        #region Polygon Geometry

        /// <summary>
        /// Determines whether a given x and y point falls within the bounds of a polygon.
        /// </summary>
        /// <param name="pointx">The x dimension of the point</param>
        /// <param name="pointy">The y dimension of the point</param>
        /// <param name="polygon">An array of RideVector2 that represents the ordered verticies of a polygon with no crossing edges</param>
        /// <returns>True if the given point falls within the bounds of the polygon</returns>
        public static bool PointInPolygon(float pointx, float pointy, RideVector2[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
                return false;

            bool result = false;
            var a = polygon[polygon.Length - 1];
            foreach (var b in polygon)
            {
                if (b.x == pointx && b.y == pointy)
                    return true;

                if (b.y == a.y && pointy == a.y && a.x <= pointx && pointx <= b.x)
                    return true;

                if (b.y < pointy && a.y >= pointy || a.y < pointy && b.y >= pointy)
                {
                    if (b.x + (pointy - b.y) / (a.y - b.y) * (a.x - b.x) <= pointx)
                        result = !result;
                }

                a = b;
            }

            return result;
        }

        #endregion
    }
}
