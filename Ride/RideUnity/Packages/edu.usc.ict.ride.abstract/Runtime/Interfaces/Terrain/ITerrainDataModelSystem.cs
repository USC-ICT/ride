using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.Terrain
{
    /// <summary>Collects imported terrain metadata, feature models, materials, and geospatial transformation data.</summary>
    public class DataModel
    {
        /// <summary>The terrain dataset identifier.</summary>
        public string TerrainID;
        /// <summary>The source path of the terrain dataset.</summary>
        public string Path;
        /// <summary>The levels of detail available for the dataset.</summary>
        public List<int> AvailableLODs;
        /// <summary>Building features keyed by source identifier.</summary>
        public Dictionary<int, BuildingData> Buildings;
        /// <summary>Vegetation features keyed by source identifier.</summary>
        public Dictionary<int, VegetationData> Vegetation;
        /// <summary>Ground material codes keyed by source terrain positions.</summary>
        public Hashtable GroundMaterials;
        /// <summary>The latitude of the dataset's geographic origin.</summary>
        public double GeoOriginLat;
        /// <summary>The longitude of the dataset's geographic origin.</summary>
        public double GeoOriginLong;
        /// <summary>The weather-zone identifier associated with the dataset.</summary>
        public int WeatherZone;
        /// <summary>The coordinate and scale transformations applied to the terrain.</summary>
        public TerrainTransformations Transformations;
    }

    /// <summary>Describes the geospatial-to-scene transformation applied to an imported terrain dataset.</summary>
    public class TerrainTransformations
    {
        /// <summary>The source origin latitude.</summary>
        public double OriginLatitude;
        /// <summary>The source origin longitude.</summary>
        public double OriginLongitude;
        /// <summary>The terrain origin in Unity scene coordinates.</summary>
        public Vector3 TerrainOriginPosition;
        /// <summary>The positional offset applied to terrain feature coordinates.</summary>
        public Vector3 TerrainOffset;
        /// <summary>The scale applied to the terrain root.</summary>
        public float TerrainRootScale;
        /// <summary>The terrain root's local position.</summary>
        public Vector3 TerrainRootLocalPosition;
        /// <summary>The terrain root's local Euler rotation.</summary>
        public Vector3 TerrainRootLocalRotation;
        /// <summary>The pivot around which terrain transformations are applied.</summary>
        public Vector3 TerrainPivotPoint;
    }

    /// <summary>Describes an imported building and its available model representations.</summary>
    public class BuildingData : ITerrainDataModel
    {
        /// <summary>The levels of detail available for the building.</summary>
        public List<int> availableLODs;
        /// <summary>Source-defined auxiliary building metadata.</summary>
        public string attribute1;
        /// <summary>The source building identifier.</summary>
        public int ID;
        /// <summary>The available models representing the building.</summary>
        public List<BuildingModel> model_representations;

        /// <inheritdoc/>
        public DataModelTypes type
        {
            get
            {
               return type = DataModelTypes.BuildingExterior;
            }
            set => Debug.Log("Why Setting?");
        }
    }

    /// <summary>Describes one source model representation of a building.</summary>
    public class BuildingModel
    {
        /// <summary>The model source or format.</summary>
        public string source;
        /// <summary>The model asset name.</summary>
        public string model_name;
        /// <summary>The model orientation.</summary>
        public float orientation;
        /// <summary>The source X position.</summary>
        public float pos_x;
        /// <summary>The source Y position.</summary>
        public float pos_y;
        /// <summary>The source Z position.</summary>
        public float pos_z;
        /// <summary>The horizontal model scale.</summary>
        public float scale_xy;
    }

    /// <summary>Describes an imported vegetation instance.</summary>
    public struct VegetationData : ITerrainDataModel
    {
        /// <summary>The source vegetation identifier.</summary>
        public int ID;
        /// <summary>The source X position.</summary>
        public float pos_x;
        /// <summary>The source Y position.</summary>
        public float pos_y;
        /// <summary>The source Z position.</summary>
        public float pos_z;
        /// <summary>The vegetation height.</summary>
        public float treeHeight;
        /// <summary>The red color component.</summary>
        public float color_r;
        /// <summary>The green color component.</summary>
        public float color_g;
        /// <summary>The blue color component.</summary>
        public float color_b;
        /// <summary>The vegetation width.</summary>
        public float treeWidth;

        /// <inheritdoc/>
        public DataModelTypes type
        {
            get
            {
                return type = DataModelTypes.Vegetation;
            }
            set => Debug.Log("Why Setting?");
        }
    }

    /// <summary>Maps an imported numeric ground-material code to its descriptive category.</summary>
    public class GroundMaterialData : ITerrainDataModel
    {
        /// <summary>Gets or sets the imported material code.</summary>
        public int MaterialInt { get; set; }
        /// <summary>Gets the descriptive material category associated with <see cref="MaterialInt"/>.</summary>
        public string MaterialString {
            get
            {
                string material = null;
                switch (MaterialInt)
                {
                    case 1:
                        material = "dirt";
                        break;
                    case 2:
                        material = "road";
                        break;
                    case 3:
                        material =  "grass";
                        break;
                }

                return material;
            }
        }

        /// <inheritdoc/>
        public DataModelTypes type
        {
            get
            {
                return type = DataModelTypes.Ground;
            }
            set => Debug.Log("Why Setting?");
        }
    }

    /// <summary>Provides coordinate conversion and ground-material lookup for the active imported terrain data model.</summary>
    /// <remarks>Used by terrain REST services and implemented by the internal terrain data-model manager adapter.</remarks>
    public interface ITerrainDataModelSystem : IRideSystem
    {
        /// <summary>Gets the ground-material code nearest a scene position.</summary>
        /// <param name="scenePosition">The Unity scene position to query.</param>
        /// <returns>The imported ground-material code.</returns>
        int GroundMaterialAtPoint(RideVector3 scenePosition);

        /// <summary>Converts a Unity-style Y-up vector to the data model's Z-up coordinate order.</summary>
        /// <param name="scenePosition">The vector to convert.</param>
        /// <returns>The vector with its Y and Z components exchanged.</returns>
        RideVector3 Vector3toZup(RideVector3 scenePosition);
    }
}
