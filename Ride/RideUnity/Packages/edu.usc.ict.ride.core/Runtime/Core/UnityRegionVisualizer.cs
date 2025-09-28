using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Utility class that provides functionality for visualizing a region on terrain.
    /// Uses the IRegionSystem interface
    /// </summary>
    public class UnityRegionVisualizer
    {
        /// <summary>
        /// Draw a region on terrain using the default color
        /// </summary>
        /// <param name="region"></param>
        /// <param name="regionSystem"></param>
        public LineRenderer DrawRegion(RideID region, RideID regionSystem)
        {
            RideColor defaultColor = new RideColor(1,0,0,0.5f); // semi-transparent red
            return DrawRegion(region, regionSystem, defaultColor);
        }

        /// <summary>
        /// Draw a region on terrain using the specified color
        /// </summary>
        /// <param name="region">The RideID of the region</param>
        /// <param name="regionSystem">The RideID of the region system</param>
        /// <param name="color">The RideColor to use for the material</param>
        public LineRenderer DrawRegion(RideID region, RideID regionSystem, RideColor color)
        {
            GameObject go = new GameObject($"Region{region.id}");
            LineRenderer lr = go.AddComponent<LineRenderer>();
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            lr.material = mat;
            lr.startWidth = 0.3f;
            lr.loop = true;
            RideVector2[] polygon = Globals.api.GetSystem<IRegionSystem>(regionSystem).GetRegionPoints(region);

            lr.positionCount = polygon.Length;
            for (int i = 0; i < polygon.Length; i++)
            {
                lr.SetPosition(i, new Vector3(
                    polygon[i].x,
                    Globals.api.terrainSystem.GetTerrainHeight(new RideVector3(polygon[i].x, 0, polygon[i].y)) + 0.01f,
                    polygon[i].y
                ));
            }
            return lr;
        }
    }
}


