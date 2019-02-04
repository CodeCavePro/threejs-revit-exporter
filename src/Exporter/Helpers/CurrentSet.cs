using System;
using System.Collections.Generic;
using System.Diagnostics;
using CodeCave.Revit.Threejs.Exporter.Geometries;
using CodeCave.Revit.Threejs.Exporter.Objects;

namespace CodeCave.Revit.Threejs.Exporter.Helpers
{
    public struct CurrentSet
    {
        public Object3D Element;
        public string MaterialUid;
        public Dictionary<string, Object3D> ObjectCache;
        public Dictionary<string, Geometry> GeometryCache;
        public Dictionary<string, VerticesOfPoint3D> VerticesCache;

        public CurrentSet(Object3D element)
        {
            Element = element;
            MaterialUid = string.Empty;
            ObjectCache = new Dictionary<string, Object3D>();
            GeometryCache = new Dictionary<string, Geometry>();
            VerticesCache = new Dictionary<string, VerticesOfPoint3D>();
        }

        public Geometry GeometryPerMaterial => GeometryCache[MaterialUid];

        public VerticesOfPoint3D VerticesPerMaterial => VerticesCache[MaterialUid];

        public void SetMaterial(string materialUuid)
        {
            if (string.IsNullOrWhiteSpace(materialUuid))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(materialUuid));

            MaterialUid = materialUuid;

            var uuidPerMaterial = Element.Uuid + "-" + materialUuid;
            if (!ObjectCache.ContainsKey(materialUuid))
            {
                Debug.Assert(!GeometryCache.ContainsKey(materialUuid), "expected same keys in both");

                ObjectCache.Add(materialUuid, new Object3D("Mesh")
                {
                    Geometry = uuidPerMaterial,
                    Uuid = uuidPerMaterial,
                    Material = MaterialUid,
                    Name = Element.Name,
                });
            }

            if (!GeometryCache.ContainsKey(materialUuid))
                GeometryCache.Add(materialUuid, new Geometry {Uuid = uuidPerMaterial});

            if (!VerticesCache.ContainsKey(materialUuid))
                VerticesCache.Add(materialUuid, new VerticesOfPoint3D());
        }
    }
}
