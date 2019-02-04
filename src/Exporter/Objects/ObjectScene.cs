using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CodeCave.Revit.Threejs.Exporter.Geometries;
using CodeCave.Revit.Threejs.Exporter.Materials;
using Newtonsoft.Json;

namespace CodeCave.Revit.Threejs.Exporter.Objects
{
    [DataContract]
    public class ObjectScene
    {
        [JsonConstructor]
        internal ObjectScene()
        {
            Object = new Object3D("Scene");
            Metadata = new Metadata();
            geometries = new Dictionary<string, Geometry>();
            materials = new Dictionary<string, Material>();
        }

        public ObjectScene(Metadata metadata) : this()
        {
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        [DataMember(Name = "metadata" )]
        [JsonProperty("metadata")]
        public Metadata Metadata { get; internal set; }

        [DataMember(Name = "geometries" )]
        [JsonProperty("geometries")]
        public IReadOnlyCollection<Geometry> Geometries => geometries.Values.ToArray();

        [DataMember(Name = "materials")]
        [JsonProperty("materials")]
        public IReadOnlyCollection<Material> Materials => materials.Values.ToArray();
       

        [DataMember(Name = "object" )]
        [JsonProperty("object")]
        public Object3D Object { get; internal set; }

        [JsonIgnore]
        [IgnoreDataMember]
        protected IDictionary<string, Geometry> geometries { get; }

        [JsonIgnore]
        [IgnoreDataMember]
        internal IDictionary<string, Material> materials { get; }

        internal void AddGeometry(Geometry geometry)
        {
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));

            if (!geometries.ContainsKey(geometry.Uuid))
                geometries.Add(geometry.Uuid, geometry);
        }

        internal void AddMaterial(Material material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));

            if (!materials.ContainsKey(material.Uuid))
                materials.Add(material.Uuid, material);
        }

        public bool HasMaterial(string uuid)
        {
            return materials.ContainsKey(uuid);
        }
    }
}
