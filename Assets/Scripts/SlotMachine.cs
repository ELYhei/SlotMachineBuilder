#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways] // Allows Update to run in the Editor
public class SlotManager : MonoBehaviour
{
    [Space(10f)]
    [SerializeField] bool editMode = false;
    [Space(10f)]
    [Range(0.01f, 1f)]
    [SerializeField] float scale = 0.5f;
    [Range(3, 5)]
    [SerializeField] int slots = 3;

    Transform[] verticalSlots; // 5 is max vertical slot count

    #region References
    HorizontalLayoutGroup verticalSlotsLayoutGroup;
    Transform verticalSlotsContainer;
    #endregion

    [Header("SlotValues")]
    float verticalSlotYStart = 1080f; // Position Y Where Slots start spinning down
    float verticalSlotYEnd = 0f; // Goal Y position;

    private bool shouldUpdateSlots = false; // Flag to control updates

    private void Awake()
    {
        if (verticalSlotsContainer == null)
            verticalSlotsContainer = transform.GetChild(0);

        if (verticalSlotsLayoutGroup == null)
            verticalSlotsLayoutGroup = verticalSlotsContainer.GetComponent<HorizontalLayoutGroup>();

        verticalSlotsLayoutGroup.enabled = false;

        if (editMode == true) Debug.LogError("In Editor Disable Edit Mode In SlotMachine Script When In Play Mode");

        verticalSlots = new Transform[slots];

        for (int i = 0; i < verticalSlotsContainer.childCount; i++)
        {
            verticalSlots[i] = verticalSlotsContainer.GetChild(i);
        }
    }

    private void OnValidate()
    {
        if (verticalSlotsContainer == null)
            verticalSlotsContainer = transform.GetChild(0);

        if (verticalSlotsLayoutGroup == null)
            verticalSlotsLayoutGroup = verticalSlotsContainer.GetComponent<HorizontalLayoutGroup>();

        verticalSlotsLayoutGroup.enabled = false;

        if (!editMode) return;

        verticalSlotsLayoutGroup.enabled = true;

        // Set the flag to update slots in the next frame
        shouldUpdateSlots = true;

#if UNITY_EDITOR
        EditorApplication.update += EditorUpdate; // Register Editor update
#endif
    }

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        if (!shouldUpdateSlots) return;
        shouldUpdateSlots = false;

        // Remove the update callback after use
        EditorApplication.update -= EditorUpdate;

        // Call the UpdateSlots logic
        UpdateSlots();
        UpdateScale();
    }
#endif

    private void UpdateSlots()
    {
        // Find the initial slot (assuming it is the first child)
        if (verticalSlotsContainer.childCount == 0) return; // Exit if no child slots exist
        Transform initialSlot = verticalSlotsContainer.GetChild(0);

        int currentSlotCount = verticalSlotsContainer.childCount;

        // Add slots if current count is less than the desired count
        for (int i = currentSlotCount; i < slots; i++)
        {
            Transform newSlot = Instantiate(initialSlot, verticalSlotsContainer);
            newSlot.name = $"VerticalSlot {i + 1}"; // Optional: rename slots for clarity
        }

        // Remove extra slots if current count is more than the desired count
        for (int i = currentSlotCount - 1; i >= slots; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(verticalSlotsContainer.GetChild(i).gameObject);
#endif
        }

    }

    private void UpdateScale()
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            ResetSlotPosition();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            SetSlotPositionsFinish();
        }

        
        // EDITOR CODE HERE

        // This ensures Update runs even when not in play mode (for Editor use)
        if (Application.isPlaying) return;

        if (shouldUpdateSlots)
        {
            UpdateSlots();
            shouldUpdateSlots = false;
        }
    }

    private void ResetSlotPosition()
    {
        foreach (Transform verticalSlot in verticalSlots)
        {
            verticalSlot.localPosition = new Vector3(verticalSlot.localPosition.x, verticalSlotYStart, verticalSlot.localPosition.z);
        }
    }

    private void SetSlotPositionsFinish()
    {
        foreach (Transform verticalSlot in verticalSlots)
        {
            verticalSlot.localPosition = new Vector3(verticalSlot.localPosition.x, verticalSlotYEnd, verticalSlot.localPosition.z);
        }
    }
}
