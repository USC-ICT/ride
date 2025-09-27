using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.Terrain
{
    public class DataModel
    {
        public string TerrainID;
        public string Path;
        public List<int> AvailableLODs;
        public Dictionary<int, BuildingData> Buildings;
        public Dictionary<int, VegetationData> Vegetation;
        public Hashtable GroundMaterials;
        public double GeoOriginLat;
        public double GeoOriginLong;
        public int WeatherZone;
        public TerrainTransformations Transformations;
    }

    public class TerrainTransformations
    {
        public double OriginLatitude;
        public double OriginLongitude;
        public Vector3 TerrainOriginPosition;
        public Vector3 TerrainOffset;
        public float TerrainRootScale;
        public Vector3 TerrainRootLocalPosition;
        public Vector3 TerrainRootLocalRotation;
        public Vector3 TerrainPivotPoint;
    }

    public class BuildingData : ITerrainDataModel
    {
        public List<int> availableLODs;
        public string attribute1;
        public int ID;
        public List<BuildingModel> model_representations;

        public DataModelTypes type
        {
            get
            {
               return type = DataModelTypes.BuildingExterior;
            }
            set => Debug.Log("Why Setting?");
        }
    }

    public class BuildingModel
    {
        public string source;
        public string model_name;
        public float orientation;
        public float pos_x;
        public float pos_y;
        public float pos_z;
        public float scale_xy;
    }

    public struct VegetationData : ITerrainDataModel
    {
        public int ID;
        public float pos_x;
        public float pos_y;
        public float pos_z;
        public float treeHeight;
        public float color_r;
        public float color_g;
        public float color_b;
        public float treeWidth;

        public DataModelTypes type
        {
            get
            {
                return type = DataModelTypes.Vegetation;
            }
            set => Debug.Log("Why Setting?");
        }
    }

    public class GroundMaterialData : ITerrainDataModel
    {
        public int MaterialInt { get; set; }
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

        public DataModelTypes type
        {
            get
            {
                return type = DataModelTypes.Ground;
            }
            set => Debug.Log("Why Setting?");
        }
    }

    public interface ITerrainDataModelSystem : IRideSystem
    {
        int GroundMaterialAtPoint(RideVector3 scenePosition);
        RideVector3 Vector3toZup(RideVector3 scenePosition);
    }
}
