#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[ExecuteAlways] // Allows Update to run in the Editor
public class SlotManager : MonoBehaviour
{
    [Space(10f)]
    [SerializeField] bool editMode = false;
    [Space(10f)]
    [Range(0.01f, 1f)]
    [SerializeField] float scale = 0.5f;
    [Range(3, 5)]
    [SerializeField] int verticalSlotsCount = 3;

    bool slotsSpinning = false;

    [Header("Spin Style")]
    [SerializeField] SpinInStyle spinInStyle;
    [SerializeField] float bounceSpeed = 0.5f;
    [SerializeField] SpinOutStyle spinOutStyle;
    public enum SpinOutStyle
    {
        all,
        oneByOne,
    }
    public enum SpinInStyle
    {
        robust,
        bouncy,
    }

    [Header("Spin Settings")]
    [SerializeField] float spinSpeedIn = 3f;
    [SerializeField] float spinSpeedOut = 6f;


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

        verticalSlots = new Transform[verticalSlotsCount];

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
        for (int i = currentSlotCount; i < verticalSlotsCount; i++)
        {
            Transform newSlot = Instantiate(initialSlot, verticalSlotsContainer);
            newSlot.name = $"VerticalSlot {i + 1}"; // Optional: rename slots for clarity
        }

        // Remove extra slots if current count is more than the desired count
        for (int i = currentSlotCount - 1; i >= verticalSlotsCount; i--)
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

        if (Input.GetKeyDown(KeyCode.S)) Spin();
        if (Input.GetKeyDown(KeyCode.R)) SetSlotPositionsFinish();


        // EDITOR CODE HERE

        // This ensures Update runs even when not in play mode (for Editor use)
        if (Application.isPlaying) return;

        if (shouldUpdateSlots)
        {
            UpdateSlots();
            shouldUpdateSlots = false;
        }
    }

    private void Spin()
    {
        if (slotsSpinning) return;

        RunSlotAnimation();
    }

    private void RunSlotAnimation()
    {
        StartCoroutine(RunSlotSpinAnimation());
    }

    IEnumerator RunSlotSpinAnimation()
    {
        RunSlotOutAnimation();

        while (slotsSpinning)
        {
            yield return null;
        }

        RunSlotInAnimation();

    }
    
    private void ResetSlotPositions()
    {
        foreach (Transform verticalSlot in verticalSlots)
        {
            verticalSlot.localPosition = new Vector3(verticalSlot.localPosition.x, verticalSlotYStart, verticalSlot.localPosition.z);
        }
    }
    
    private void RunSlotInAnimation()
    {
        ResetSlotPositions();
        StartCoroutine(RunSlotInAnimationsCoroutine());
    }

    IEnumerator RunSlotInAnimationsCoroutine()
    {
        slotsSpinning = true;

        if (spinInStyle == SpinInStyle.robust)
        {
            for (int i = 0; i < verticalSlotsCount; i++)
            {
                Transform vSlot = verticalSlots[i];
                Vector3 targetPos = new Vector3(vSlot.localPosition.x, verticalSlotYEnd, vSlot.localPosition.z);

                // Start the slot movement coroutine
                yield return StartCoroutine(MoveObjectLocalSmooth(vSlot, targetPos, spinSpeedIn));

                // Wait until the slot has reached its position before moving to the next slot
            }
        }
        else if (spinInStyle == SpinInStyle.bouncy)
        {
            for (int i = 0; i < verticalSlotsCount; i++)
            {
                Transform vSlot = verticalSlots[i];
                Vector3 targetPos = new Vector3(vSlot.localPosition.x, verticalSlotYEnd * 1.1f, vSlot.localPosition.z);

                // Start the slot movement coroutine
                yield return StartCoroutine(MoveObjectLocalSmooth(vSlot, targetPos - (Vector3.up * 200f), spinSpeedIn));
                yield return StartCoroutine(MoveObjectLocalSmooth(vSlot, targetPos, bounceSpeed));
                // Wait until the slot has reached its position before moving to the next slot
            }
        }

        SetSlotPositionsFinish();
        slotsSpinning = false;
    }

    private void RunSlotOutAnimation()
    {
        StartCoroutine(RunSlotOutAnimationsCoroutine());
    }

    IEnumerator RunSlotOutAnimationsCoroutine()
    {
        slotsSpinning = true;
        SetSlotPositionsFinish();
        if (spinOutStyle == SpinOutStyle.oneByOne)
        {
            for (int i = 0; i < verticalSlotsCount; i++)
            {
                Transform vSlot = verticalSlots[i];
                Vector3 targetPos = new Vector3(vSlot.localPosition.x, -verticalSlotYStart, vSlot.localPosition.z);

                // Start the slot movement coroutine and wait until it's finished before moving to the next slot
                yield return StartCoroutine(MoveObjectLocalSmooth(vSlot, targetPos, spinSpeedOut));

                // small delay before the next slot starts moving
                yield return new WaitForSeconds(0.1f);
            }
        }
        else if (spinOutStyle == SpinOutStyle.all)
        {
            List<Coroutine> coroutines = new List<Coroutine>();

            for (int i = 0; i < verticalSlotsCount; i++)
            {
                Transform vSlot = verticalSlots[i];
                Vector3 targetPos = new Vector3(vSlot.localPosition.x, -verticalSlotYStart, vSlot.localPosition.z);

                // Start the slot movement coroutine and add it to the list
                coroutines.Add(StartCoroutine(MoveObjectLocalSmooth(vSlot, targetPos, spinSpeedOut + UnityEngine.Random.Range(-0.1f, 0.1f))));
            }

            // Wait for all slot movement coroutines to finish
            yield return CombineCoroutines(coroutines);
        }

        ResetSlotPositions();
        slotsSpinning = false;
    }

    IEnumerator CombineCoroutines(List<Coroutine> coroutines)
    {
        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }
    }

    IEnumerator MoveObjectLocalSmooth(Transform movingObject, Vector3 targetPosition, float speed)
    {
        float threshold = 0.1f; // Define a small threshold for floating-point precision issues

        while (Vector3.Distance(movingObject.localPosition, targetPosition) > threshold)
        {
            movingObject.localPosition = Vector3.MoveTowards(movingObject.localPosition, targetPosition, speed * 1000f * Time.deltaTime);
            yield return null;
        }

        // Snap to the exact position to avoid issues due to floating-point precision
        movingObject.localPosition = targetPosition;
    }

    private void SetSlotPositionsFinish()
    {
        foreach (Transform verticalSlot in verticalSlots)
        {
            verticalSlot.localPosition = new Vector3(verticalSlot.localPosition.x, verticalSlotYEnd, verticalSlot.localPosition.z);
        }
    }
}
