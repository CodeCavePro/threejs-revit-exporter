using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using CodeCave.Threejs.Revit.Exporter.Helpers;
using CodeCave.Threejs.Entities;
using Newtonsoft.Json;

namespace CodeCave.Threejs.Revit.Exporter
{
    public class ObjectSceneExportContext : IExportContext
    {
        protected readonly Document document;
        protected readonly FileInfo outputFile;
        protected CurrentSet current;
        protected bool isCanceled;
        protected JsonObjectScene outputScene;
        protected Stack<Transform> transformations;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ObjectSceneExportContext" /> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="metadata">The metadata.</param>
        /// <exception cref="T:System.ArgumentException">document</exception>
        /// <exception cref="T:System.ArgumentNullException">
        ///     document
        ///     or
        ///     outputFile
        /// </exception>
        public ObjectSceneExportContext(Document document, FileInfo outputFile)
        {
            if (document?.IsFamilyDocument ?? false)
                throw new ArgumentException("", nameof(document));

            this.document = document ?? throw new ArgumentNullException(nameof(document));
            this.outputFile = outputFile ?? throw new ArgumentNullException(nameof(outputFile));

            transformations = new Stack<Transform>();
            current = new CurrentSet(null);
        }

        /// <summary>
        ///     This method is called at the very start of the export process, still before the first entity of the model was send
        ///     out.
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            transformations.Push(Transform.Identity);
            outputScene = new JsonObjectScene(
                generator: $"{nameof(Threejs)}.{nameof(Exporter)} v{typeof(ObjectSceneExportContext).Assembly.GetName().Version}",
                uuid: document.ActiveView.UniqueId)
            {
                Object =
                {
                    Name = $"Revit {document.Title}"
                }
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
            // just go on with export
            return RenderNodeAction.Proceed;
        }

        /// <summary>
        ///     This method marks the end of a 3D view being exported.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        /// <exception cref="InvalidDataException"></exception>
        public void OnViewEnd(ElementId elementId)
        {
            try
            {
                var element = document.GetElement(elementId);
                var uid = element?.UniqueId;

                if (element == null || string.IsNullOrWhiteSpace(uid))
                    throw new InvalidDataException();

                if (outputScene.Object.HasChild(uid))
                {
                    Debug.WriteLine("\r\n*** Duplicate element!\r\n");
                    return;
                }

                if (null == element.Category)
                {
                    Debug.WriteLine("\r\n*** Non-category element!\r\n");
                    return;
                }

                foreach (var material in current.VerticesCache.Keys.ToList())
                {
                    var obj = current.ObjectCache[material];
                    var geo = current.GeometryCache[material];

                    foreach (var p in current.VerticesCache[material])
                    {
                        geo.AddVertice(p.X);
                        geo.AddVertice(p.Y);
                        geo.AddVertice(p.Z);
                    }

                    obj.Geometry = geo.Uuid;
                    outputScene.AddGeometry(geo);
                    current.Element.AddChild(obj);
                }

                outputScene.Object.AddChild(current.Element);
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
        }

        /// <summary>
        ///     This method marks the beginning of an element to be exported
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public RenderNodeAction OnElementBegin(ElementId elementId)
        {
            try
            {
                var element = document.GetElement(elementId);
                var uid = element?.UniqueId;

                if (element == null || string.IsNullOrWhiteSpace(uid))
                    throw new InvalidDataException();

                Debug.WriteLine(
                    $"OnElementBegin: id {elementId.IntegerValue} category {element.Category?.Name} name {element.Name}");

                if (outputScene.Object.HasChild(uid))
                {
                    Debug.WriteLine("\r\n*** Duplicate element!\r\n");
                    return RenderNodeAction.Skip;
                }

                if (null == element.Category)
                {
                    Debug.WriteLine("\r\n*** Non-category element!\r\n");
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
                    Material = current.MaterialUid
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
        ///     This method marks the end of an element being exported
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        public void OnElementEnd(ElementId elementId)
        {
            try
            {
                var element = document.GetElement(elementId);
                var uid = element.UniqueId;

                Debug.WriteLine(
                    $"OnElementEnd: id {elementId.IntegerValue} category {element.Category.Name} name {element.Name}");

                if (outputScene.Object.HasChild(uid))
                {
                    Debug.WriteLine("\r\n*** Duplicate element!\r\n");
                    return;
                }

                if (null == element.Category)
                {
                    Debug.WriteLine("\r\n*** Non-category element!\r\n");
                    return;
                }

                foreach (var material in current.VerticesCache.Keys.ToArray())
                {
                    var obj = current.ObjectCache[material];
                    var geo = current.GeometryCache[material];

                    foreach (var p in current.VerticesCache[material])
                    {
                        geo.AddVertice(p.X);
                        geo.AddVertice(p.Y);
                        geo.AddVertice(p.Z);
                    }

                    obj.Geometry = geo.Uuid;
                    outputScene.AddGeometry(geo);
                    current.Element.AddChild(obj);
                }

                outputScene.Object.AddChild(current.Element);
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
        }

        /// <summary>
        ///     This method marks the beginning of a Face to be exported
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
                                                      + " symbol: " + node.GetSymbolId().IntegerValue);

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
                                node.GetSymbolId().IntegerValue);
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
                if (ElementId.InvalidElementId != elementId)
                {
                    SetMaterial(document.GetElement(node.MaterialId).UniqueId);
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
            try
            {
                var points = node
                    .GetPoints()
                    .Select(p => transformations.Peek().OfPoint(p))
                    .ToArray();

                foreach (var facet in node.GetFacets())
                    current.GeometryPerMaterial.AddFaces(new[]
                    {
                        0,
                        current.VerticesPerMaterial.AddVertex(points[facet.V1]),
                        current.VerticesPerMaterial.AddVertex(points[facet.V2]),
                        current.VerticesPerMaterial.AddVertex(points[facet.V3])
                    });
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
        /// <exception cref="InvalidDataException"></exception>
        public void Finish()
        {
            // Finish populating scene

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };

            if (outputScene == null)
                throw new InvalidDataException();

            var outPutJson = JsonConvert.SerializeObject(outputScene, settings);
            File.WriteAllText(outputFile.FullName, outPutJson);
        }

        /// <summary>
        ///     Determines whether export process is canceled.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the export process is canceled; otherwise, <c>false</c>.
        /// </returns>
        public bool IsCanceled()
        {
            return isCanceled;
        }

        /// <summary>
        ///     Sets current material.
        /// </summary>
        /// <param name="materialUuid">The material UUID.</param>
        /// <exception cref="T:System.ArgumentException">Value cannot be null or whitespace. - materialUuid</exception>
        /// <exception cref="T:System.IO.InvalidDataException"></exception>
        public void SetMaterial(string materialUuid)
        {
            if (string.IsNullOrWhiteSpace(materialUuid))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(materialUuid));

            if (!outputScene.HasMaterial(materialUuid))
            {
                var materialElement = document.GetElement(materialUuid) as Autodesk.Revit.DB.Material;
                if (materialElement == null)
                    throw new InvalidDataException();
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
            isCanceled = true;
        }
    }
}