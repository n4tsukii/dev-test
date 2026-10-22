using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Editor-only authoring. The finished boards use ordinary meshes and BoxColliders.
public static class AdvertisingBoardBuilder
{
    public const string AssetRoot = "Assets/StadiumAdvertising";
    public const string PerimeterName = "Stadium Advertising - Orange Border";
    private const float BoardWidth = 3.6f;
    private static readonly string[] Brands = { "APEX", "VOLT", "NOVA", "PULSE", "Corner" };
    private static readonly string[] Taglines = { "BUILT FOR THE GAME", "BRING THE ENERGY", "NEXT LEVEL FOOTBALL", "PLAY WITH HEART", "PLAY ON" };
    private static readonly Color[] Accents = {
        new Color(1f, .29f, .12f), new Color(.73f, .96f, .12f),
        new Color(.10f, .70f, 1f), new Color(.76f, .38f, 1f), new Color(1f, .65f, .10f)
    };

    [MenuItem("Tools/Stadium Advertising/Build Prefabs")]
    public static void BuildAssets()
    {
        foreach (string folder in new[] { "Materials", "Meshes", "Textures", "Prefabs" })
            Directory.CreateDirectory(AssetRoot + "/" + folder);
        AssetDatabase.Refresh();
        Material frame = MaterialAsset("Graphite Frame", new Color(.075f, .10f, .14f), .35f, .35f);
        Material metal = MaterialAsset("Support Metal", new Color(.30f, .36f, .42f), .6f, .45f);
        Mesh structure = StructureMesh();
        Mesh face = FaceMesh();

        for (int i = 0; i < Brands.Length; i++)
        {
            Texture2D artwork = BakeArtwork(i);
            Material print = MaterialAsset(Brands[i] + " Print", Color.white, 0f, .16f, "Unlit/Texture");
            print.mainTexture = artwork;
            EditorUtility.SetDirty(print);

            GameObject board = new GameObject("Advertising Board - " + Brands[i]);
            try
            {
                var body = new GameObject("Frame and Supports", typeof(MeshFilter), typeof(MeshRenderer));
                body.transform.SetParent(board.transform, false);
                body.GetComponent<MeshFilter>().sharedMesh = structure;
                body.GetComponent<MeshRenderer>().sharedMaterials = new[] { frame, metal };
                var panel = new GameObject("Printed Face", typeof(MeshFilter), typeof(MeshRenderer));
                panel.transform.SetParent(board.transform, false);
                panel.GetComponent<MeshFilter>().sharedMesh = face;
                panel.GetComponent<MeshRenderer>().sharedMaterial = print;
                panel.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                var collider = board.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, .68f, .05f);
                collider.size = new Vector3(BoardWidth + .04f, 1.36f, .86f);
                if (i == 4)
                {
                    body.transform.localScale = new Vector3(1.5f / BoardWidth, 1f, 1f);
                    panel.transform.localScale = body.transform.localScale;
                    collider.size = new Vector3(1.54f, 1.36f, .86f);
                }
                PrefabUtility.SaveAsPrefabAsset(board, AssetRoot + "/Prefabs/AdvertisingBoard_" + Brands[i] + ".prefab");
            }
            finally { Object.DestroyImmediate(board); }
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Stadium Advertising/Place Around Orange Border")]
    public static void PlacePerimeter()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode before placing boards.");
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(AssetRoot + "/Prefabs/AdvertisingBoard_APEX.prefab")) BuildAssets();
        Scene scene = SceneManager.GetActiveScene();
        GameObject previous = GameObject.Find(PerimeterName);
        if (previous) Undo.DestroyObjectImmediate(previous);
        var root = new GameObject(PerimeterName);
        Undo.RegisterCreatedObjectUndo(root, "Place stadium advertising");
        SceneManager.MoveGameObjectToScene(root, scene);

        // Measured centerline of the existing orange track, including its beveled corners.
        Vector2[] path = {
            new Vector2(-13.1f, 10.92f), new Vector2(13.1f, 10.92f),
            new Vector2(14.49f, 10.33f), new Vector2(15.08f, 9.06f),
            new Vector2(15.08f, -9.06f), new Vector2(14.49f, -10.33f),
            new Vector2(13.1f, -10.92f), new Vector2(-13.1f, -10.92f),
            new Vector2(-14.49f, -10.33f), new Vector2(-15.08f, -9.06f),
            new Vector2(-15.08f, 9.06f), new Vector2(-14.49f, 10.33f)
        };
        int serial = 0;
        for (int edge = 0; edge < path.Length; edge++)
        {
            Vector3 a = new Vector3(path[edge].x, 4.40f, path[edge].y);
            Vector2 next = path[(edge + 1) % path.Length];
            Vector3 b = new Vector3(next.x, 4.40f, next.y);
            float distance = Vector3.Distance(a, b);
            bool corner = distance < 2f;
            int count = Mathf.CeilToInt(distance / BoardWidth);
            float width = distance / count;
            var side = new GameObject("Border Section " + (edge + 1).ToString("00"));
            side.transform.SetParent(root.transform, false);
            for (int n = 0; n < count; n++)
            {
                int variant = corner ? 4 : serial % 4;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetRoot + "/Prefabs/AdvertisingBoard_" + Brands[variant] + ".prefab");
                var board = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                board.name = (++serial).ToString("00") + " - " + Brands[variant];
                board.transform.SetParent(side.transform, false);
                board.transform.position = Vector3.Lerp(a, b, (n + .5f) / count);
                board.transform.rotation = Quaternion.LookRotation(Vector3.Cross((b - a).normalized, Vector3.up));
                board.transform.localScale = new Vector3(width / (corner ? 1.5f : BoardWidth), 1f, 1f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(board.transform);
            }
        }

        // Let the CharacterController reach the physical boards; retain a backup world bound.
        var player = Object.FindFirstObjectByType<PlayerMovements>();
        if (player)
        {
            Undo.RecordObject(player, "Align field bounds with advertising barrier");
            var serialized = new SerializedObject(player);
            if (serialized.FindProperty("fieldMin") != null)
            {
                serialized.FindProperty("fieldMin").vector2Value = new Vector2(-14.5f, -10.5f);
                serialized.FindProperty("fieldMax").vector2Value = new Vector2(14.5f, 10.5f);
            }
            else if (serialized.FindProperty("minX") != null)
            {
                serialized.FindProperty("minX").floatValue = -14.5f;
                serialized.FindProperty("maxX").floatValue = 14.5f;
                serialized.FindProperty("minZ").floatValue = -10.5f;
                serialized.FindProperty("maxZ").floatValue = 10.5f;
            }
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Debug.Log("Placed " + serial + " advertising boards on the orange border.");
    }

    private static Material MaterialAsset(string name, Color color, float metallic, float smoothness, string shader = "Standard")
    {
        string path = AssetRoot + "/Materials/" + name.Replace(" ", "_") + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(Shader.Find(shader)) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = Shader.Find(shader);
        if (material.HasProperty("_Color")) material.color = color;
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh StructureMesh()
    {
        var frame = new List<CombineInstance>();
        var legs = new List<CombineInstance>();
        var temporary = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cube = temporary.GetComponent<MeshFilter>().sharedMesh;
        Quaternion lean = Quaternion.Euler(25f, 0f, 0f);
        Vector3 center = new Vector3(0f, .64f, 0f);
        AddBox(frame, cube, center, new Vector3(3.6f, 1.18f, .16f), lean);
        AddBox(frame, cube, center + lean * new Vector3(0f, .58f, -.02f), new Vector3(3.64f, .065f, .22f), lean);
        AddBox(frame, cube, center + lean * new Vector3(0f, -.58f, -.02f), new Vector3(3.64f, .065f, .22f), lean);
        foreach (float x in new[] { -1.2f, 1.2f })
        {
            AddBox(legs, cube, new Vector3(x, .045f, .17f), new Vector3(.16f, .09f, .95f), Quaternion.identity);
            Vector3 start = new Vector3(x, .10f, .57f);
            Vector3 end = new Vector3(x, 1.05f, .22f);
            AddBox(legs, cube, (start + end) * .5f, new Vector3(.085f, (end - start).magnitude, .085f), Quaternion.FromToRotation(Vector3.up, end - start));
        }
        Object.DestroyImmediate(temporary);
        var frameMesh = new Mesh(); frameMesh.CombineMeshes(frame.ToArray(), true, true);
        var legMesh = new Mesh(); legMesh.CombineMeshes(legs.ToArray(), true, true);
        var combined = new Mesh { name = "AdvertisingBoard_Frame" };
        combined.CombineMeshes(new[] {
            new CombineInstance { mesh = frameMesh, transform = Matrix4x4.identity },
            new CombineInstance { mesh = legMesh, transform = Matrix4x4.identity }
        }, false, true);
        Object.DestroyImmediate(frameMesh); Object.DestroyImmediate(legMesh);
        return SaveMesh(combined, "AdvertisingBoard_Frame");
    }

    private static void AddBox(List<CombineInstance> list, Mesh cube, Vector3 center, Vector3 scale, Quaternion rotation)
    {
        list.Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(center, rotation, scale) });
    }

    private static Mesh FaceMesh()
    {
        Quaternion lean = Quaternion.Euler(25f, 0f, 0f);
        Vector3 center = new Vector3(0, .64f, 0);
        var vertices = new[] { new Vector3(-1.74f, -.52f, -.086f), new Vector3(1.74f, -.52f, -.086f),
            new Vector3(-1.74f, .52f, -.086f), new Vector3(1.74f, .52f, -.086f) };
        for (int i = 0; i < vertices.Length; i++) vertices[i] = center + lean * vertices[i];
        var mesh = new Mesh {
            name = "AdvertisingBoard_Face", vertices = vertices,
            triangles = new[] { 0, 2, 1, 2, 3, 1 },
            uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one }
        };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return SaveMesh(mesh, "AdvertisingBoard_Face");
    }

    private static Mesh SaveMesh(Mesh mesh, string name)
    {
        string path = AssetRoot + "/Meshes/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!existing) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh);
        return existing;
    }

    private static Texture2D BakeArtwork(int index)
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Artwork Bake");
        SceneManager.MoveGameObjectToScene(root, preview);
        var resources = new List<Object>();
        RenderTexture target = null;
        Texture2D image = null;
        RenderTexture previous = RenderTexture.active;
        string path = AssetRoot + "/Textures/Advertising_" + Brands[index] + ".png";
        try
        {
            Color ink = new Color(.035f, .055f, .09f);
            Color accent = Accents[index];
            var cameraObject = new GameObject("Artwork Camera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true; camera.orthographicSize = .5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = index == 1 ? accent : ink;
            camera.allowHDR = false; camera.allowMSAA = true;
            camera.nearClipPlane = .1f; camera.farClipPlane = 20f;
            target = new RenderTexture(2048, 512, 24) { antiAliasing = 4 };
            camera.targetTexture = target;

            Color title = index == 1 ? ink : Color.white;
            Color motif = index == 1 ? ink : accent;
            RectShape(root.transform, resources, new Rect(-2f, -.5f, 4f, .045f), accent, .1f);
            RectShape(root.transform, resources, new Rect(-2f, .455f, 4f, .045f), accent, .1f);
            if (index == 4)
            {
                Text(root.transform, "PLAY ON", 0, .02f, 2.65f, .44f, Color.white, true);
                Text(root.transform, "THE BEAUTIFUL GAME", 0, -.31f, 2.7f, .12f, accent, false);
            }
            else
            {
                // Simple vector marks: mountain, lightning bolt, orbit diamond, pulse.
                if (index == 0) Polygon(root.transform, resources, motif, new[] {
                    new Vector2(-1.8f,-.21f), new Vector2(-1.43f,.30f), new Vector2(-1.07f,-.21f),
                    new Vector2(-1.27f,-.21f), new Vector2(-1.43f,.04f), new Vector2(-1.61f,-.21f) });
                if (index == 1) Polygon(root.transform, resources, motif, new[] {
                    new Vector2(-1.45f,.32f), new Vector2(-1.13f,.32f), new Vector2(-1.42f,.01f),
                    new Vector2(-1.19f,.01f), new Vector2(-1.65f,-.30f), new Vector2(-1.49f,-.06f), new Vector2(-1.73f,-.06f) });
                if (index == 2)
                {
                    Polygon(root.transform, resources, motif, new[] { new Vector2(-1.76f,0),new Vector2(-1.43f,.32f),new Vector2(-1.10f,0),new Vector2(-1.43f,-.32f) });
                    Polygon(root.transform, resources, ink, new[] { new Vector2(-1.58f,0),new Vector2(-1.43f,.15f),new Vector2(-1.28f,0),new Vector2(-1.43f,-.15f) }, -.02f);
                }
                if (index == 3)
                {
                    Vector2[] pulse = { new Vector2(-1.8f,0), new Vector2(-1.61f,0), new Vector2(-1.51f,.22f), new Vector2(-1.36f,-.22f), new Vector2(-1.23f,0), new Vector2(-1.08f,0) };
                    for (int p = 0; p < pulse.Length - 1; p++) Line(root.transform, resources, pulse[p], pulse[p+1], .06f, motif);
                }
                Text(root.transform, Brands[index], .23f, .095f, 2.05f, .48f, title, true);
                Text(root.transform, Taglines[index], .23f, -.285f, 2.18f, .11f, title, false);
                for (int stripe = 0; stripe < 3; stripe++)
                {
                    float x = 1.45f + stripe * .18f;
                    Polygon(root.transform, resources, motif, new[] { new Vector2(x,-.43f),new Vector2(x+.10f,-.43f),new Vector2(x+.42f,.43f),new Vector2(x+.32f,.43f) });
                }
            }
            camera.Render(); RenderTexture.active = target;
            image = new Texture2D(2048, 512, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 2048, 512), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            foreach (Camera camera in root.GetComponentsInChildren<Camera>()) camera.targetTexture = null;
            if (target) { target.Release(); Object.DestroyImmediate(target); }
            if (image) Object.DestroyImmediate(image);
            EditorSceneManager.ClosePreviewScene(preview);
            foreach (Object resource in resources) if (resource) Object.DestroyImmediate(resource);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.anisoLevel = 4;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void Text(Transform parent, string value, float x, float y, float width, float height, Color color, bool bold)
    {
        var go = new GameObject(value, typeof(TextMesh));
        go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(x, y, -.08f);
        var text = go.GetComponent<TextMesh>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 160; text.characterSize = .1f;
        text.text = value; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; text.color = color;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = text.font.material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        Vector3 size = renderer.bounds.size;
        go.transform.localScale = Vector3.one * Mathf.Min(width / Mathf.Max(.001f, size.x), height / Mathf.Max(.001f, size.y));
    }

    private static void RectShape(Transform parent, List<Object> resources, Rect rect, Color color, float z)
    {
        Polygon(parent, resources, color, new[] { new Vector2(rect.xMin,rect.yMin),new Vector2(rect.xMax,rect.yMin),new Vector2(rect.xMax,rect.yMax),new Vector2(rect.xMin,rect.yMax) }, z);
    }

    private static void Line(Transform parent, List<Object> resources, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 n = new Vector2(-(b-a).y, (b-a).x).normalized * width * .5f;
        Polygon(parent, resources, color, new[] { a-n, b-n, b+n, a+n });
    }

    private static void Polygon(Transform parent, List<Object> resources, Color color, Vector2[] points, float z = 0)
    {
        var go = new GameObject("Vector Artwork", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        var verts = new Vector3[points.Length];
        for (int i = 0; i < verts.Length; i++) verts[i] = new Vector3(points[i].x, points[i].y, z);
        var mesh = new Mesh { vertices = verts, triangles = Triangulate(points) }; mesh.RecalculateBounds();
        var material = new Material(Shader.Find("Unlit/Color")) { color = color };
        resources.Add(mesh); resources.Add(material);
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
        go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    private static int[] Triangulate(Vector2[] points)
    {
        float area = 0;
        for (int i = 0; i < points.Length; i++) area += Cross(points[i], points[(i + 1) % points.Length]);
        var remaining = new List<int>();
        for (int i = 0; i < points.Length; i++) remaining.Add(area > 0 ? i : points.Length - 1 - i);
        var triangles = new List<int>();
        int budget = points.Length * points.Length;
        while (remaining.Count > 2 && budget-- > 0)
        {
            bool clipped = false;
            for (int i = 0; i < remaining.Count; i++)
            {
                int a = remaining[(i + remaining.Count - 1) % remaining.Count];
                int b = remaining[i], c = remaining[(i + 1) % remaining.Count];
                if (Cross(points[b] - points[a], points[c] - points[b]) <= .000001f) continue;
                bool contains = false;
                foreach (int p in remaining)
                {
                    if (p == a || p == b || p == c) continue;
                    if (Cross(points[b]-points[a], points[p]-points[a]) >= 0 &&
                        Cross(points[c]-points[b], points[p]-points[b]) >= 0 &&
                        Cross(points[a]-points[c], points[p]-points[c]) >= 0) { contains = true; break; }
                }
                if (contains) continue;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                remaining.RemoveAt(i); clipped = true; break;
            }
            if (!clipped) break;
        }
        return triangles.ToArray();
    }

    private static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }
}
