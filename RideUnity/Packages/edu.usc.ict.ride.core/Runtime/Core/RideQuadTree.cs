using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Ride
{
    public class RideQuadTree<T>
    {
        RideQuadTree<T>[] m_ChildNodes = null;                           // Stores this node's four children nodes. If it's null, this is a leaf node.
        int m_MaxNodePoints;                                             // A node can have this many points before being subdivided into four nodes.
        List<RideVector2> m_NodePoints = new List<RideVector2>();        // The points contained within this node.
        Dictionary<RideVector2, T> m_NodeObjects = new Dictionary<RideVector2, T>();
        RideVector2 m_BoundsMin;                                         // X Z bounds min.
        RideVector2 m_BoundsMax;                                         // X Z bounds max.

        public RideQuadTree(RideVector2 boundsMin, RideVector2 boundsMax, int maxNodeObjects=16)
        {
            if(boundsMin.x == boundsMax.x || boundsMin.y == boundsMax.y)
            {
                RideLog.LogError("Bounds for quadtree are a line. Please do not use a line.");
            }
            m_BoundsMin = boundsMin;
            m_BoundsMax = boundsMax;
            m_MaxNodePoints = maxNodeObjects;
#if UNITY_EDITOR
            Debug.DrawLine(new Vector3(boundsMin.x, 0, boundsMin.y), new Vector3(boundsMax.x, 0, boundsMin.y),Color.grey, float.PositiveInfinity);
            Debug.DrawLine(new Vector3(boundsMin.x, 0, boundsMin.y), new Vector3(boundsMin.x, 0, boundsMax.y),Color.grey, float.PositiveInfinity);
            Debug.DrawLine(new Vector3(boundsMax.x, 0, boundsMax.y), new Vector3(boundsMax.x, 0, boundsMin.y),Color.grey, float.PositiveInfinity);
            Debug.DrawLine(new Vector3(boundsMax.x, 0, boundsMax.y), new Vector3(boundsMin.x, 0, boundsMax.y),Color.grey, float.PositiveInfinity);
#endif
        }

        public void SubdivideNode()
        {
            float halfx = (m_BoundsMax.x - m_BoundsMin.x) / 2f;
            float halfy = (m_BoundsMax.y - m_BoundsMin.y) / 2f;
            RideVector2 minmin = m_BoundsMin;
            RideVector2 midmin = new RideVector2(m_BoundsMin.x + halfx, m_BoundsMin.y);

            RideVector2 minmid = new RideVector2(m_BoundsMin.x,         m_BoundsMin.y + halfy);
            RideVector2 midmid = new RideVector2(m_BoundsMin.x + halfx, m_BoundsMin.y + halfy);
            RideVector2 maxmid = new RideVector2(m_BoundsMax.x,         m_BoundsMin.y + halfy);

            RideVector2 midmax = new RideVector2(m_BoundsMin.x + halfx, m_BoundsMax.y);
            RideVector2 maxmax = m_BoundsMax;

            RideQuadTree<T> sw = new RideQuadTree<T>(minmin, midmid, m_MaxNodePoints);
            RideQuadTree<T> se = new RideQuadTree<T>(midmin, maxmid, m_MaxNodePoints);
            RideQuadTree<T> nw = new RideQuadTree<T>(minmid, midmax, m_MaxNodePoints);
            RideQuadTree<T> ne = new RideQuadTree<T>(midmid, maxmax, m_MaxNodePoints);

            m_ChildNodes = new RideQuadTree<T>[4];
            m_ChildNodes[0] = sw;
            m_ChildNodes[1] = se;
            m_ChildNodes[2] = nw;
            m_ChildNodes[3] = ne;

            for (int i = 0; i < m_NodePoints.Count; i++)
            {
                Insert(m_NodePoints[i], m_NodeObjects[m_NodePoints[i]]);
            }

            m_NodeObjects.Clear();
            m_NodePoints.Clear();
        }

        public void Insert(RideVector2 point, T thing)
        {
            if (!Fits(point)) return;
            if (m_NodePoints.Count == m_MaxNodePoints && m_ChildNodes == null)
            {
                SubdivideNode();
                Insert(point, thing);
            }
            else if (m_ChildNodes == null)
            {
                m_NodePoints.Add(point);
                m_NodeObjects.Add(point, thing);
            }
            else
            {
                if (m_ChildNodes[0].Fits(point)) m_ChildNodes[0].Insert(point, thing);
                else if (m_ChildNodes[1].Fits(point)) m_ChildNodes[1].Insert(point, thing);
                else if (m_ChildNodes[2].Fits(point)) m_ChildNodes[2].Insert(point, thing);
                else if (m_ChildNodes[3].Fits(point)) m_ChildNodes[3].Insert(point, thing);
                else RideLog.LogError($"A point did not fit in any quadtree child nodes despite fitting in the parent. The bounds of the children are likely bad.");
            }
        }

        bool Fits(RideVector2 point)
        {
            return Fits(point, m_BoundsMin, m_BoundsMax);
        }

        bool Fits(RideVector2 point, RideVector2 bMin, RideVector2 bMax)
        {
            if (point.x >= bMin.x && point.x < bMax.x && point.y >= bMin.y && point.y < bMax.y)
            {
                return true;
            }
            return false;
        }

        bool Intersects(RideVector2 queryBoundsMin, RideVector2 queryBoundsMax)
        {

            // Line check.
            if (queryBoundsMin.x == queryBoundsMax.x || queryBoundsMin.y == queryBoundsMax.y)
            {
                return false;
            }

            // If one rectangle is on left side of other
            if (m_BoundsMin.x >= queryBoundsMax.x || queryBoundsMin.x >= m_BoundsMax.x)
                return false;

            // If one rectangle is above other
            if (m_BoundsMin.y >= queryBoundsMax.y || queryBoundsMin.y >= m_BoundsMax.y)
                return false;

            return true;
        }

        bool ContainedBy(RideVector2 queryBoundsMin, RideVector2 queryBoundsMax)
        {
            if(queryBoundsMin.x < m_BoundsMin.x && queryBoundsMin.y < m_BoundsMin.y && queryBoundsMax.x >= m_BoundsMax.x && queryBoundsMax.y >= m_BoundsMax.y)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the quadtree elements within the nodes intersecting with the given query bounds.
        /// </summary>
        /// <param name="bMin">Lower bounds of query. Inclusive.</param>
        /// <param name="bMax">Upper bounds of query. Exclusive.</param>
        /// <returns>Returns elements found within given bounds.</returns>
        public List<T> Query(RideVector2 bMin, RideVector2 bMax)
        {
            return Query(bMin, bMax, false);
        }

        List<T> Query(RideVector2 bMin, RideVector2 bMax, bool isContained = false)
        {
            List<T> output = new List<T>();

            // Query bounds completely envelopes node.
            if (isContained || ContainedBy(bMin, bMax))
            {
                if (m_ChildNodes != null)
                {
                    for (int i = 0; i < m_ChildNodes.Length; i++)
                    {
                        output.AddRange(m_ChildNodes[i].Query(bMin, bMax, true));
                    }
                }
                else
                {
                    for (int i = 0; i < m_NodePoints.Count; i++)
                    {
                        output.Add(m_NodeObjects[m_NodePoints[i]]);
                    }
                }
            }

            // Query bounds intersect with node.
            if (Intersects(bMin, bMax))
            {
                if (m_ChildNodes != null)
                {
                    for (int i = 0; i < m_ChildNodes.Length; i++)
                    {
                        output.AddRange(m_ChildNodes[i].Query(bMin, bMax, false));
                    }
                }
                else
                {
                    for (int i = 0; i < m_NodePoints.Count; i++)
                    {
                        if (Fits(m_NodePoints[i], bMin, bMax))
                        {
                            output.Add(m_NodeObjects[m_NodePoints[i]]);
                        }
                    }
                }
            }

            // Query bounds do not touch node...
            // ...so do nothing.

            return output;
        }
    }
}

