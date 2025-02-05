using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabPainter : EditorWindow
{
    private bool isPainting = false;
    [SerializeField] private GameObject[] prefabsToPaint;
    private Transform parentTransform;
    private Vector2 xRotationRange = new Vector2(-5f, 5f);
    private Vector2 zRotationRange = new Vector2(-5f, 5f);
    private float minYRotation = 0f;
    private float maxYRotation = 360f;
    private Vector2 scaleRange = new Vector2(1f, 2f);
    private float yPositionOffset = -0.25f;

    private SerializedObject serializedObject;
    private SerializedProperty prefabsProperty;

    private Stack<GameObject> placedObjects = new Stack<GameObject>();
    private const int MAX_UNDO_HISTORY = 50;

    [MenuItem("Tools/Prefab Painter")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPainter>("Prefab Painter");
    }

    private void OnEnable()
    {
        serializedObject = new SerializedObject(this);
        prefabsProperty = serializedObject.FindProperty("prefabsToPaint");
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Prefab Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(prefabsProperty, true);
        EditorGUILayout.Space(5);
        parentTransform = (Transform)EditorGUILayout.ObjectField("Parent Transform", parentTransform, typeof(Transform), true);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Rotation Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        xRotationRange = EditorGUILayout.Vector2Field("X Rotation Range (Min, Max)", xRotationRange);
        zRotationRange = EditorGUILayout.Vector2Field("Z Rotation Range (Min, Max)", zRotationRange);
        EditorGUILayout.Space(2);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Y Rotation Range", GUILayout.Width(120));
            minYRotation = EditorGUILayout.FloatField(minYRotation, GUILayout.Width(50));
            EditorGUILayout.LabelField("to", GUILayout.Width(20));
            maxYRotation = EditorGUILayout.FloatField(maxYRotation, GUILayout.Width(50));
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Scale & Position Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        scaleRange = EditorGUILayout.Vector2Field("Scale Range (Min, Max)", scaleRange);
        EditorGUILayout.Space(2);
        yPositionOffset = EditorGUILayout.FloatField("Y Position Offset", yPositionOffset);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);
        
        // Painting Controls
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.color = isPainting ? Color.red : Color.green;
        if (GUILayout.Button(isPainting ? "■ STOP PAINTING" : "▶ START PAINTING", GUILayout.Height(30)))
        {
            isPainting = !isPainting;
            if (isPainting)
            {
                SceneView.duringSceneGui += OnSceneGUI;
            }
            else
            {
                SceneView.duringSceneGui -= OnSceneGUI;
            }
        }
        GUI.color = Color.white;

        if (isPainting)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Left click in scene view to paint prefabs.", MessageType.Info);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.enabled = placedObjects.Count > 0;
        if (GUILayout.Button("Undo Last Placement", GUILayout.Height(25)))
        {
            UndoLastPlacement();
        }
        GUI.enabled = true;
        EditorGUILayout.LabelField($"Undo History: {placedObjects.Count}/{MAX_UNDO_HISTORY}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (isPainting && e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (prefabsToPaint != null && prefabsToPaint.Length > 0)
                {
                    GameObject selectedPrefab = prefabsToPaint[Random.Range(0, prefabsToPaint.Length)];
                    if (selectedPrefab != null)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                        Vector3 spawnPosition = hit.point + new Vector3(0f, yPositionOffset, 0f);
                        instance.transform.position = spawnPosition;

                        float randomXRotation = Random.Range(xRotationRange.x, xRotationRange.y);
                        float randomYRotation = Random.Range(minYRotation, maxYRotation);
                        float randomZRotation = Random.Range(zRotationRange.x, zRotationRange.y);
                        instance.transform.rotation = Quaternion.Euler(randomXRotation, randomYRotation, randomZRotation);

                        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
                        instance.transform.localScale = Vector3.one * randomScale;

                        if (parentTransform != null)
                        {
                            instance.transform.SetParent(parentTransform);
                        }

                        Undo.RegisterCreatedObjectUndo(instance, "Paint Prefab");
                        
                        // Add to undo history
                        AddToUndoHistory(instance);
                    }
                }
                e.Use();
            }
        }
    }

    private void AddToUndoHistory(GameObject obj)
    {
        if (placedObjects.Count >= MAX_UNDO_HISTORY)
        {
            placedObjects.Pop();
        }
        placedObjects.Push(obj);
        Repaint(); // Add this line to update the UI immediately
    }

    private void UndoLastPlacement()
    {
        if (placedObjects.Count > 0)
        {
            GameObject objToRemove = placedObjects.Pop();
            if (objToRemove != null)
            {
                Undo.DestroyObjectImmediate(objToRemove);
            }
            Repaint();
        }
    }
}