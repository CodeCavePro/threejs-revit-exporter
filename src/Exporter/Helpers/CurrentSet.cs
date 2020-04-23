using System;
using System.Collections.Generic;
using System.Diagnostics;
using CodeCave.Threejs.Entities;

namespace CodeCave.Threejs.Revit.Exporter.Helpers
{
    public struct CurrentSet
    {
        internal string MaterialUid;
        internal Object3D Element;
        internal Dictionary<string, Object3D> ObjectCache;
        internal Dictionary<string, Geometry> GeometryCache;
        internal Dictionary<string, Vector3Collection> VerticesCache;

        public CurrentSet(Object3D element)
        {
            Element = element;
            MaterialUid = string.Empty;
            ObjectCache = new Dictionary<string, Object3D>();
            GeometryCache = new Dictionary<string, Geometry>();
            VerticesCache = new Dictionary<string, Vector3Collection>();
        }

        public Geometry GeometryPerMaterial => GeometryCache[MaterialUid];

        public Vector3Collection VerticesPerMaterial => VerticesCache[MaterialUid];

        public void SetMaterial(string materialUuid)
        {
            if (string.IsNullOrWhiteSpace(materialUuid))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(materialUuid));

            MaterialUid = materialUuid;

            var uuidPerMaterial = Element.Uuid + "-" + materialUuid;
            if (!ObjectCache.ContainsKey(materialUuid))
            {
                Debug.Assert(!GeometryCache.ContainsKey(materialUuid), "expected same keys in both");

                ObjectCache.Add(materialUuid, new Object3D("Mesh", uuidPerMaterial)
                {
                    Geometry = uuidPerMaterial,
                    Material = MaterialUid,
                    Name = Element.Name,
                });
            }

            if (!GeometryCache.ContainsKey(materialUuid))
                GeometryCache.Add(materialUuid, new Geometry(uuidPerMaterial));

            if (!VerticesCache.ContainsKey(materialUuid))
                VerticesCache.Add(materialUuid, new Vector3Collection());
        }
    }
}
