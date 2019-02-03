using System;
using Autodesk.Revit.DB;

namespace CodeCave.Revit.Threejs.Exporter
{
    public class ObjectSceneExportContext : IExportContext
    {
        public bool Start()
        {
            throw new NotImplementedException();
        }

        public void Finish()
        {
            throw new NotImplementedException();
        }

        public bool IsCanceled()
        {
            throw new NotImplementedException();
        }

        public RenderNodeAction OnViewBegin(ViewNode node)
        {
            throw new NotImplementedException();
        }

        public void OnViewEnd(ElementId elementId)
        {
            throw new NotImplementedException();
        }

        public RenderNodeAction OnElementBegin(ElementId elementId)
        {
            throw new NotImplementedException();
        }

        public void OnElementEnd(ElementId elementId)
        {
            throw new NotImplementedException();
        }

        public RenderNodeAction OnInstanceBegin(InstanceNode node)
        {
            throw new NotImplementedException();
        }

        public void OnInstanceEnd(InstanceNode node)
        {
            throw new NotImplementedException();
        }

        public RenderNodeAction OnLinkBegin(LinkNode node)
        {
            throw new NotImplementedException();
        }

        public void OnLinkEnd(LinkNode node)
        {
            throw new NotImplementedException();
        }

        public RenderNodeAction OnFaceBegin(FaceNode node)
        {
            throw new NotImplementedException();
        }

        public void OnFaceEnd(FaceNode node)
        {
            throw new NotImplementedException();
        }

        public void OnRPC(RPCNode node)
        {
            throw new NotImplementedException();
        }

        public void OnLight(LightNode node)
        {
            throw new NotImplementedException();
        }

        public void OnMaterial(MaterialNode node)
        {
            throw new NotImplementedException();
        }

        public void OnPolymesh(PolymeshTopology node)
        {
            throw new NotImplementedException();
        }
    }
}