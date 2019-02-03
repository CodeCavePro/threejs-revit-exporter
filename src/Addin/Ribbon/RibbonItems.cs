using System.Collections.Generic;

namespace CodeCave.Revit.Threejs.Exporter.Ribbon
{
    public static partial class RibbonHelper
    {
        /// <summary>
        /// Initializes the <see cref="RibbonHelper"/> class.
        /// </summary>
        static RibbonHelper()
        {
            // TODO declare your ribbon items here
            RibbonTitle = StringLocalizer.CallingAssembly["Three.js Exporter"];     // The title of the ribbon
            RibbonItems = new List<RibbonButton>
            {
                new RibbonButton<RibbonCommand>                                     // One can reference commands defined in other assemblies
                {
                    Text = StringLocalizer.CallingAssembly["Export-2-Three.js"],    // Text displayed on the command, can be stored in the resources
                    IconName = "Resources.threejs-exporter.png",                    // Path to the image, it's relative to the assembly where the command above is defined
                    Tooltip = StringLocalizer.CallingAssembly["Export current model to Three.js object JSON"],
                }
            };
        }
    }
}
