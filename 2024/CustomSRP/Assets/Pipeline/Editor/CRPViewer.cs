using System;
using UnityEditor;

namespace UnityEngine.Rendering.CustomRenderPipeline
{
    public class CRPViewer : EditorWindow
    {


        private static Optional<CRPAsset> optionalViewer;
        private Material layerShower;
        private Material normalShower;
        private Vector2 scrollPos;
        private float scale;
        private int layer;
        enum ColourWriteMask
        {
            Red = 8,
            Green = 4,
            Blue = 2,
            Alpha = 1
        }
        private ColourWriteMask mask = (ColourWriteMask)15;

        [MenuItem("Window/CRP GBuffer")]
        static void ShowWindow()
        {
            GetWindow<CRPViewer>("CRP GBuffer");
        }
        [MenuItem("Tools/Toggle Game View Gizmos")]
        static void ToggleGameViewGizmos()
        {
            CRPAsset viewer = default;
            if (!optionalViewer.TryGet(ref viewer))
            {
                Debug.LogWarning("Warning: CRP Asset not found.");
                return;
            }
            viewer.EditorShowGameViewGizmos ^= true;
        }
        [MenuItem("Tools/Toggle Scene View Gizmos")]
        static void ToggleSceneViewGizmos()
        {
            CRPAsset viewer = default;
            if (!optionalViewer.TryGet(ref viewer))
            {
                Debug.LogWarning("Warning: CRP Asset not found.");
                return;
            }
            viewer.EditorShowSceneViewGizmos ^= true;
        }

        [InitializeOnLoadMethod]
        public static void ReloadCRPAssets()
        {
            optionalViewer = CurrentCRP.GetAsset();
            CRPAsset viewer = default;
            if (optionalViewer.TryGet(ref viewer))
                viewer.Initialise();
        }

        void OnGUI()
        {
            CRPAsset viewer = default;
            if (!optionalViewer.TryGet(ref viewer))
            {
                if (GUILayout.Button("Find CRP Asset"))
                    ReloadCRPAssets();
                EditorGUILayout.HelpBox("CRP Asset not found. It needs to be set as the current render pipeline", MessageType.Info);
                return;
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Using CRP Asset: ", viewer, typeof(CRPAsset), false);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Reinitialise CRP"))
                viewer.Initialise();

            if (!viewer.FullyConfigured())
            {
                EditorGUILayout.HelpBox("CRP has not been properly initialised.", MessageType.Info);
                return;
            }

            Vector2 size = viewer.GBuffer[CRPTarget.COLOUR].referenceSize;
            scale = EditorGUILayout.Slider("Preview Scale:", scale, 250, size.x);


            mask = (ColourWriteMask)EditorGUILayout.EnumFlagsField("Show Colour Channels:", mask);
            ColorWriteMask filter = (ColorWriteMask)mask;


            Space(2);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.LabelField("Colour RT:");
            EditorGUI.DrawPreviewTexture(GetTextureRect(size), viewer.GBuffer[CRPTarget.COLOUR], null, ScaleMode.ScaleToFit, 0, -1, filter);

            Space();

            if (normalShower == null)
                normalShower = new Material(Shader.Find("Hidden/NormalShower"));
            normalShower.SetColor("_Filter", new Color(
                (filter & ColorWriteMask.Red) > 0 ? 1 : 0,
                (filter & ColorWriteMask.Green) > 0 ? 1 : 0,
                (filter & ColorWriteMask.Blue) > 0 ? 1 : 0,
                (filter & ColorWriteMask.Alpha) > 0 ? 1 : 0));
            EditorGUILayout.LabelField("Normal RT:");
            EditorGUI.DrawPreviewTexture(GetTextureRect(size), viewer.GBuffer[CRPTarget.NORMAL], normalShower, ScaleMode.ScaleToFit, 0, -1);

            Space();

            EditorGUILayout.LabelField("Depth RT:");
            EditorGUI.DrawPreviewTexture(GetTextureRect(size), viewer.GBuffer.DepthBuffer, null, ScaleMode.ScaleToFit, 0, -1);

            Space();

            if (layerShower == null)
                layerShower = new Material(Shader.Find("Hidden/LayerShower"));
            if (viewer.EditorLayerSchema.EditorDeferredNames != null && viewer.EditorLayerSchema.EditorDeferredNames.Length > 0)
            {
                layer = EditorGUILayout.Popup("Show Layer: ", layer, viewer.EditorLayerSchema.EditorDeferredNames);
                layerShower.SetFloat("_Layer", BitConverter.ToSingle(BitConverter.GetBytes(viewer.EditorLayerSchema.EditorDeferredLayers[layer])));
                layerShower.SetFloat("_Mask", BitConverter.ToSingle(BitConverter.GetBytes(viewer.EditorLayerSchema.EditorDeferredMasks[layer])));
            }
            EditorGUILayout.LabelField("Layer RT:");
            EditorGUI.DrawPreviewTexture(GetTextureRect(size), viewer.GBuffer[CRPTarget.LAYER], layerShower, ScaleMode.ScaleToFit, 0, -1);

            EditorGUILayout.EndScrollView();
        }

        private Rect GetTextureRect(Vector2 size) => GUILayoutUtility.GetRect(scale, size.y * scale / size.x);
        private void Space(int height = 1) => GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(height) });
    }
}

