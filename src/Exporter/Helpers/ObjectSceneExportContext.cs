using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using CodeCave.Threejs.Entities;
using CodeCave.Threejs.Revit.Exporter.Helpers;

namespace CodeCave.Threejs.Revit.Exporter
{
    public sealed class ObjectSceneExportContext : IExportContext, IDisposable
    {
        private readonly Document document;
        private readonly View3D view3D;
        private readonly int levelOfDetail;
        private bool isDisposed; // To detect redundant calls

        private CurrentSet current;
        private bool isCanceled;
        private Stack<Transform> transformations;
        private ObjectScene outputScene;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ObjectSceneExportContext" /> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <exception cref="System.ArgumentException">document.</exception>
        /// <exception cref="System.ArgumentNullException">
        ///     document
        ///     or
        ///     outputFile.
        /// </exception>
        public ObjectSceneExportContext(Document document, View3D view3D, int levelOfDetail = 15)
        {
            if (levelOfDetail < 1 && levelOfDetail > 15)
                throw new ArgumentException("Please use a valid level of detail for exporter (between 1 and 15, both included).", nameof(levelOfDetail));

            if (document?.IsFamilyDocument ?? false)
                throw new ArgumentException("Please make sure you wrap families into a project-type document.", nameof(document));

            this.document = document ?? throw new ArgumentNullException(nameof(document));
            this.view3D = view3D;
            this.levelOfDetail = levelOfDetail;

            Reset();
        }

        public bool Optimize { get; set; }

        public ObjectSceneExportContext Reset()
        {
            transformations = new Stack<Transform>();
            current = new CurrentSet(null);

            return this;
        }

        /// <summary>
        ///     This method is called at the very start of the export process, still before the first entity of the model was send
        ///     out.
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            transformations.Push(Transform.Identity);
            outputScene = new ObjectScene(
                generator: $"{nameof(Threejs)}.{nameof(FamilyExporter)} v{typeof(ObjectSceneExportContext).Assembly.GetName().Version}",
                uuid: (view3D ?? document.ActiveView).UniqueId)
            {
                Object =
                {
                    Name = $"Revit {document.Title}",
                },
            };

            return true;
        }

        /// <summary>
        ///     This method marks the beginning of a 3D view to be exported.
        /// </summary>
        /// <param name="node">The view node.</param>
        /// <returns></returns>
        public RenderNodeAction OnViewBegin(ViewNode node)
        {
            node.LevelOfDetail = levelOfDetail;

            // just go on with export
            return RenderNodeAction.Proceed;
        }

        /// <summary>
        ///     This method marks the end of a 3D view being exported.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        public void OnViewEnd(ElementId elementId)
        {
            try
            {
                var element = document.GetElement(elementId);
                var uuid = element?.UniqueId;

                if (element is null || string.IsNullOrWhiteSpace(uuid))
                    throw new InvalidDataException();

                if (outputScene.Object.HasChild(uuid))
                {
                    Debug.WriteLine("Duplicate element!");
                    return;
                }

                if (element.Category is null)
                {
                    Debug.WriteLine("Non-category element!");
                    return;
                }

                foreach (var materialId in current.VerticesCache.Keys.ToList())
                {
                    var obj = current.ObjectCache[materialId];
                    var geo = current.GeometryCache[materialId];

                    foreach (var p in current.VerticesCache[materialId])
                    {
                        geo.AddPoint(p);
                    }

                    obj.GeometryUuid = geo.Uuid;
                    outputScene.AddGeometry(geo);
                    current.Element.AddChild(obj);
                }

                if (current.Element is not null)
                    outputScene.Object.AddChild(current.Element);
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
        }

        /// <summary>
        ///     This method marks the beginning of an element to be exported.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        /// <returns></returns>
        public RenderNodeAction OnElementBegin(ElementId elementId)
        {
            if (elementId is null)
                throw new ArgumentNullException(nameof(elementId));

            try
            {
                var element = document.GetElement(elementId);
                var uid = element?.UniqueId;
                if (element is null || string.IsNullOrWhiteSpace(uid))
                    throw new InvalidDataException();

                Debug.WriteLine($"OnElementBegin: id {elementId.IntegerValue} category {element.Category?.Name} name {element.Name}");

                if (outputScene.Object.HasChild(uid))
                {
                    Debug.WriteLine("Duplicate element!");
                    return RenderNodeAction.Skip;
                }

                if (element.Category is null)
                {
                    Debug.WriteLine("Non-category element!");
                    return RenderNodeAction.Skip;
                }

                var idsMaterialGeometry = element.GetMaterialIds(false);
                if (idsMaterialGeometry.Count > 1)
                {
                    var materials = string.Join(", ", idsMaterialGeometry.Select(id => document.GetElement(id).Name));
                    Debug.Print($"{element.GetDescription()} has {idsMaterialGeometry.Count} materials: {materials}");
                }

                current = new CurrentSet(new Object3D("RevitElement", uid)
                {
                    Name = element.GetDescription(),
                    MaterialUuid = current.MaterialUid,
                });

                var materialUuid = element.Category?.Material?.UniqueId;
                if (!string.IsNullOrWhiteSpace(materialUuid))
                    SetMaterial(materialUuid);
            }
            catch (Exception ex)
            {
                ProcessException(ex);
                return RenderNodeAction.Skip;
            }

            return RenderNodeAction.Proceed;
        }

        /// <summary>
        ///     This method marks the end of an element being exported.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        public void OnElementEnd(ElementId elementId)
        {
            if (elementId is null)
                throw new ArgumentNullException(nameof(elementId));

            try
            {
                var element = document.GetElement(elementId);
                var uid = element?.UniqueId;
                if (element is null || string.IsNullOrWhiteSpace(uid))
                    throw new InvalidDataException();

                if (element is Level) // Skip levels
                    return;

                Debug.WriteLine($"OnElementEnd: id {elementId.IntegerValue} category {element.Category.Name} name {element.Name}");

                if (outputScene.Object.HasChild(uid))
                {
                    Debug.WriteLine("Duplicate element!");
                    return;
                }

                if (element.Category is null)
                {
                    Debug.WriteLine("Non-category element!");
                    return;
                }

                foreach (var materialId in current.VerticesCache.Keys.ToArray())
                {
                    var obj = current.ObjectCache[materialId];
                    var geo = current.GeometryCache[materialId];

                    foreach (var p in current.VerticesCache[materialId])
                    {
                        geo.AddPoint(p);
                    }

                    obj.GeometryUuid = geo.Uuid;
                    outputScene.AddGeometry(geo);
                    current.Element.AddChild(obj);
                }

                if (element is FamilyInstance familyInstance &&
                    (familyInstance.Symbol.FamilyName.IndexOf("QF_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    familyInstance.Symbol.FamilyName.IndexOf("VR_", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    var builtInParams = new List<BuiltInParameter>
                        {
                            BuiltInParameter.ALL_MODEL_MANUFACTURER,
                            BuiltInParameter.ALL_MODEL_MODEL,
                            BuiltInParameter.ALL_MODEL_DESCRIPTION,
                            BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
                            BuiltInParameter.ALL_MODEL_TYPE_COMMENTS,
                        }
                        .Select(p => familyInstance.Symbol.get_Parameter(p) ?? familyInstance.get_Parameter(p))
                        .Select(p => new { p?.Definition?.Name, Value = p?.StorageType == StorageType.String ? p?.AsString() : p?.AsValueString() })
                        .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                        .ToDictionary(p => p.Name, p => p.Value);

                    var sharedParams = element.Parameters
                        .OfType<Parameter>()
                        .Where(p => p.IsShared && !p.Definition.Name.StartsWith("Specifi_", StringComparison.OrdinalIgnoreCase))
                        .Select(p => new { p?.Definition?.Name, Value = p?.StorageType == StorageType.String ? p?.AsString() : p?.AsValueString() })
                        .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                        .OrderBy(p => p.Name)
                        .ToDictionary(p => p.Name, p => p.Value);

                    current.Element.UserData = current.Element.UserData
                        .Concat(builtInParams)
                        .Concat(sharedParams)
                        .ToDictionary(p => p.Key, p => p.Value);
                }

                outputScene.Object.AddChild(current.Element);
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
        }

        /// <summary>
        ///     This method marks the beginning of a Face to be exported.
        /// </summary>
        /// <param name="node">The face node.</param>
        /// <returns></returns>
        public RenderNodeAction OnFaceBegin(FaceNode node)
        {
            // just go on with export
            return RenderNodeAction.Proceed;
        }

        /// <summary>
        ///     his method marks the end of the current face being exported.
        /// </summary>
        /// <param name="node">The face node.</param>
        public void OnFaceEnd(FaceNode node)
        {
            // do nothing
        }

        /// <summary>
        ///     This method marks the beginning of a family instance to be exported.
        /// </summary>
        /// <param name="node">The instance node.</param>
        /// <returns></returns>
        public RenderNodeAction OnInstanceBegin(InstanceNode node)
        {
            try
            {
                Debug.WriteLine("  OnInstanceBegin: " + node.NodeName
                                                      + " symbol: " + node.GetSymbolGeometryId());

                // This method marks the start of processing a family instance
                transformations.Push(transformations.Peek().Multiply(
                    node.GetTransform()));
            }
            catch (Exception ex)
            {
                ProcessException(ex);
                return RenderNodeAction.Skip;
            }

            return RenderNodeAction.Proceed;
        }

        /// <summary>
        ///     This method marks the end of a family instance being exported.
        /// </summary>
        /// <param name="node">The family instance node.</param>
        public void OnInstanceEnd(InstanceNode node)
        {
            transformations.Pop();
        }

        /// <summary>
        ///     This method marks the beginning of a link instance to be exported.
        /// </summary>
        /// <param name="node">The link node.</param>
        /// <returns></returns>
        public RenderNodeAction OnLinkBegin(LinkNode node)
        {
            try
            {
                Debug.WriteLine("  OnLinkBegin: " + node.NodeName + " Document: " + node.GetDocument().Title +
                                ": Id: " +
                                node.SymbolId.Value);
                transformations.Push(transformations.Peek().Multiply(node.GetTransform()));
            }
            catch (Exception ex)
            {
                ProcessException(ex);
                return RenderNodeAction.Skip;
            }

            return RenderNodeAction.Proceed;
        }

        /// <summary>
        ///     This method marks the end of a link instance being exported.
        /// </summary>
        /// <param name="node">The link node.</param>
        public void OnLinkEnd(LinkNode node)
        {
            transformations.Pop();
        }

        /// <summary>
        ///     This method marks the beginning of export of an RPC object.
        /// </summary>
        /// <param name="node">The RPC object node.</param>
        public void OnRPC(RPCNode node)
        {
            // do nothing, not supported
        }

        /// <summary>
        ///     This method marks the beginning of export of a light object.
        /// </summary>
        /// <param name="node">The light node.</param>
        public void OnLight(LightNode node)
        {
            node.ToString();
            // do nothing, light export is not supported
        }

        /// <summary>
        ///     This method marks a change of the material.
        /// </summary>
        /// <param name="node">The material node.</param>
        public void OnMaterial(MaterialNode node)
        {
            try
            {
                var elementId = node.MaterialId;
                var materialElement = document.GetElement(node.MaterialId);
                if (materialElement != null && ElementId.InvalidElementId != elementId)
                {
                    SetMaterial(materialElement.UniqueId);
                    return;
                }

                var material = node.ToMeshPhong();
                if (!outputScene.HasMaterial(material.Uuid))
                    outputScene.AddMaterial(material);

                SetMaterial(material.Uuid);
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
        }

        /// <summary>
        ///     This method is called when a tessellated polymesh of a 3d face is being output.
        /// </summary>
        /// <param name="node">The polymesh node.</param>
        public void OnPolymesh(PolymeshTopology node)
        {
            if (node is null)
                throw new ArgumentNullException(nameof(node));

            try
            {
                var points = node
                    .GetPoints()
                    .Select(p => transformations.Peek().OfPoint(p))
                    .ToArray();

                foreach (var facet in node.GetFacets())
                {
                    current.GeometryPerMaterial.AddFace(new[]
                    {
                        0,
                        current.VerticesPerMaterial.AddVertex(points[facet.V1].ToVector3()),
                        current.VerticesPerMaterial.AddVertex(points[facet.V2].ToVector3()),
                        current.VerticesPerMaterial.AddVertex(points[facet.V3].ToVector3()),
                    });
                }

                foreach (var uv in node.GetUVs())
                {
                    current.GeometryPerMaterial.AddUVs(new[] {
                        uv.U,
                        uv.V,
                    });
                }
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
        }

        /// <summary>
        ///     This method is called at the very end of the export process, after all entities were processed (or after the
        ///     process was canceled).
        /// </summary>
        public void Finish()
        {
            // Finish populating scene
            if (outputScene is null)
                throw new InvalidOperationException("The export cannot be finalized if the scene is null");

            if (Optimize)
                outputScene.Optimize(true);
        }

        /// <summary>
        ///     Determines whether export process is canceled.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the export process is canceled; otherwise, <c>false</c>.
        /// </returns>
        public bool IsCanceled() => isCanceled;

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Gets the result of the export.</summary>
        /// <returns></returns>
        internal ObjectScene GetResult() => outputScene;

        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    outputScene = null;
                    transformations = null;
                }

                isDisposed = true;
            }
        }

        /// <summary>
        ///     Sets current material.
        /// </summary>
        /// <param name="materialUuid">The material UUID.</param>
        /// <exception cref="System.ArgumentException">Value cannot be null or whitespace. - materialUuid.</exception>
        /// <exception cref="InvalidDataException">Material must not be null.</exception>
        private void SetMaterial(string materialUuid)
        {
            if (string.IsNullOrWhiteSpace(materialUuid))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(materialUuid));

            if (!outputScene.HasMaterial(materialUuid))
            {
                var materialElement = document.GetElement(materialUuid) as Autodesk.Revit.DB.Material;
                if (materialElement is null)
                    throw new InvalidDataException("Material must not be null.");
                outputScene.AddMaterial(materialElement.ToMeshPhong());
            }

            current.SetMaterial(materialUuid);
        }

        /// <summary>
        ///     Processes the exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        private void ProcessException(Exception ex)
        {
            // TODO add logging
            Debug.WriteLine(ex.Dump());
            isCanceled = true;
        }


#if RVT2016
        public void OnDaylightPortal(DaylightPortalNode node)
        {
            // do nothing, daylight export is not supported
        }
#endif

    }
}
