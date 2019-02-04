using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CodeCave.Revit.Threejs.Exporter.Objects
{
    /// <summary>
    /// This is the base class for most objects in three.js
    /// and provides a set of properties and methods for manipulating objects in 3D space.
    /// Note that this can be used for grouping objects via the .add( object ) method
    /// which adds the object as a child, however it is better to use Group for this.
    /// </summary>
    [DataContract]
    public class Object3D
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Object3D"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <exception cref="T:System.ArgumentException">Value cannot be null or whitespace. - type</exception>
        [JsonConstructor]
        public Object3D(string type)
        {
            Type = (string.IsNullOrWhiteSpace(type))
                ? "Object3D"
                : type;
            childrenInternal = new Dictionary<string, Object3D>();
            UserData = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the UUID of this object instance.
        /// </summary>
        /// <value>
        /// The UUID. This gets automatically assigned and shouldn't be edited. 
        /// </value>
        [DataMember(Name = "uuid")]
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        /// <summary>
        /// Gets or sets the optional name of the object (doesn't need to be unique).
        /// Default is an empty string.
        /// </summary>
        /// <value>
        /// The optional name of the object.
        /// </value>
        [DataMember(Name = "name")]
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <value>
        /// The type of the object.
        /// </value>
        [DataMember(Name = "type")]
        [JsonProperty("type")]
        public string Type { get; }

        /// <summary>
        /// Gets or sets the local transform matrix.
        /// </summary>
        /// <value>
        /// The local transform matrix.
        /// </value>
        [DataMember(Name = "matrix")]
        [JsonProperty("matrix")]
        public double[] Matrix { get; set; } =
        {
            1D,0D,0D,0D,
            0D,1D,0D,0D,
            0D,0D,1D,0D,
            0D,0D,0D,1D
        };

        /// <summary>
        /// Gets or sets the array of object's children.
        /// </summary>
        /// <value>
        /// The array of object's children.
        /// </value>
        [DataMember(Name = "children")]
        [JsonProperty("children")]
        public IReadOnlyCollection<Object3D> Children => childrenInternal.Values.ToArray();

        /// <summary>
        /// Gets or sets the ID of the geometry.
        /// </summary>
        /// <value>
        /// The ID of the geometry.
        /// </value>
        [DataMember(Name = "geometry")]
        [JsonProperty("geometry")]
        public string Geometry { get; set; }

        /// <summary>
        /// Gets or sets the name of the material.
        /// </summary>
        /// <value>
        /// The name of the material.
        /// </value>
        [DataMember(Name = "material")]
        [JsonProperty("material")]
        public string Material { get; set; }

        /// <summary>
        /// Gets or sets the user data.
        /// An object that can be used to store custom data about the Object3D. It should not hold references to functions as these will not be cloned.
        /// </summary>
        /// <value>
        /// The user data.
        /// </value>
        [DataMember(Name = "userData")]
        [JsonProperty("userData")]
        public IDictionary<string, string> UserData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Geometries.Geometry.GeometryData"/> is visible.
        /// </summary>
        /// <value>
        ///   <c>true</c> if visible; otherwise, <c>false</c>.
        /// </value>
        [DataMember(Name = "visible")]
        [JsonProperty("visible")]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [casts shadow].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [casts shadow]; otherwise, <c>false</c>.
        /// </value>
        [DataMember(Name = "castShadow")]
        [JsonProperty("castShadow")]
        public bool CastShadow { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [receives shadow].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [receives shadow]; otherwise, <c>false</c>.
        /// </value>
        [DataMember(Name = "receiveShadow")]
        [JsonProperty("receiveShadow")]
        // ReSharper disable once RedundantDefaultMemberInitializer
        public bool ReceiveShadow { get; set; } = false;

        // TODO implement: List<double> position { get; set; }
        // TODO implement: List<double> rotation { get; set; }
        // TODO implement: List<double> quaternion { get; set; }
        // TODO implement: List<double> scale { get; set; }

        [JsonIgnore]
        [IgnoreDataMember]
        protected IDictionary<string, Object3D> childrenInternal { get; }

        internal void AddChildren(Object3D object3D)
        {
            if (object3D == null) throw new ArgumentNullException(nameof(object3D));

            if (!childrenInternal.ContainsKey(object3D.Uuid))
                childrenInternal.Add(object3D.Uuid, object3D);
        }

        internal bool HasChildren(string uuid)
        {
            return childrenInternal.ContainsKey(uuid);
        }
    }
}
