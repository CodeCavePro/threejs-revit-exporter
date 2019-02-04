using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CodeCave.Revit.Threejs.Exporter
{
    [DataContract]
    public class Metadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Metadata"/> class.
        /// </summary>
        [JsonConstructor]
        internal Metadata()
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Metadata"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="generator">The generator.</param>
        /// <param name="type">The type.</param>
        /// <exception cref="ArgumentNullException">version</exception>
        /// <exception cref="ArgumentException">
        /// Value cannot be null or whitespace. - generator
        /// or
        /// Value cannot be null or whitespace. - type
        /// </exception>
        public Metadata(Version version, string generator = "", string type = "Object")
        {
            if (version == null) throw new ArgumentNullException(nameof(version));
            if (string.IsNullOrWhiteSpace(generator))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(generator));
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(type));

            Version = $"{version.Major}.{version.Minor}";
            Type = type;
            Generator = generator;
        }

        /// <summary>
        /// Gets the version of the file.
        /// </summary>
        /// <value>
        /// The version of the file.
        /// </value>
        [DataMember(Name = "version" )]
        [JsonProperty("version")]
        public string Version { get; }

        /// <summary>
        /// Gets the type of the file.
        /// </summary>
        /// <value>
        /// The type of the file.
        /// </value>
        [DataMember(Name = "type" )]
        [JsonProperty("type")]
        public string Type { get; }

        /// <summary>
        /// Gets the generator, which created the file.
        /// </summary>
        /// <value>
        /// The generator, which created the file.
        /// </value>
        [DataMember(Name = "generator" )]
        [JsonProperty("generator")]
        public string Generator { get; }
    }
}
