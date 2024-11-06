#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

[Serializable]
public class SlotSymbol
{
    public string symbolName;
    public Sprite sprite;
    public float multiplier;
}

[ExecuteAlways] // Allows Update to run in the Editor
public class SlotMachine : MonoBehaviour
{
    [SerializeField] private SlotSymbol[] slotSymbols;
    [Space(10f)]
    [Header("Edit Mode Needed To Change Scale & SlotCount. Do Not Change These In Run Time")]
    [SerializeField] bool editMode = false;
    [Space(10f)]
    [Range(0.01f, 1f)]
    [SerializeField] float scale = 0.5f;
    [Range(3, 5)]
    [SerializeField] int verticalSlotsCount = 3;

    List<GameObject> slots = new List<GameObject>();
    List<GameObject> visualSlots = new List<GameObject>();
    Transform verticalVisualSlotsContainer;

    bool slotsSpinning = false;
    
    [SerializeField] SlotStyle slotStyle = SlotStyle.custom;
    SlotStyle oldSlotStyle = SlotStyle.custom;
    public enum SlotStyle
    {
        custom,
        original,
        fast,
        dropping,
        droppingOneByOne,
    }

    [Header("Spin Style")]
    [SerializeField] bool visualsSpin = false;
    [SerializeField] float visualsSpinTime = 1f;
    [SerializeField] float visualsSpinSpeed = 30f;
    [SerializeField] SpinInStyle spinInStyle;
    [SerializeField] float spinSpeedIn = 3f;
    [SerializeField] float bounceSpeed = 0.5f;
    [SerializeField] bool spinOut = true;
    [SerializeField] SpinOutStyle spinOutStyle;
    [SerializeField] float spinSpeedOut = 6f;
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

    Transform[] verticalSlots; // 5 is max vertical slot count

    #region References
    HorizontalLayoutGroup verticalSlotsLayoutGroup;
    Transform verticalSlotsContainer;
    RectTransform visualSpinSlotsContainer;
    #endregion

    [Header("SlotValues")]
    float verticalSlotYStart = 1080f; // Position Y Where Slots start spinning down
    float verticalSlotYEnd = 0f; // Goal Y position;

    private bool shouldUpdateSlots = false; // Flag to control updates
    private bool shouldSpinVisualSlots = false;

    private void Awake()
    {
        if (verticalSlotsContainer == null)
            verticalSlotsContainer = transform.GetChild(1);

        if (verticalSlotsLayoutGroup == null)
            verticalSlotsLayoutGroup = verticalSlotsContainer.GetComponent<HorizontalLayoutGroup>();

        verticalSlotsLayoutGroup.enabled = false;

        if (editMode == true) Debug.LogError("In Editor Disable Edit Mode In SlotMachine Script When In Play Mode");

        verticalSlots = new Transform[verticalSlotsCount];

        for (int i = 0; i < verticalSlotsContainer.childCount; i++)
        {
            verticalSlots[i] = verticalSlotsContainer.GetChild(i);
        }

        visualSpinSlotsContainer = transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();

        InitSlotSymbols();
    }

    private void OnValidate()
    {
        if (verticalSlotsContainer == null)
            verticalSlotsContainer = transform.GetChild(1);

        if (verticalSlotsLayoutGroup == null)
            verticalSlotsLayoutGroup = verticalSlotsContainer.GetComponent<HorizontalLayoutGroup>();
    
        verticalSlotsLayoutGroup.enabled = false;

        if (verticalVisualSlotsContainer == null)
            verticalVisualSlotsContainer = transform.GetChild(0).GetChild(0);

        if (visualSpinSlotsContainer == null)
            visualSpinSlotsContainer = transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
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
        UpdateSlotStyle();
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

        // Find the initial slot (assuming it is the first child)
        if (visualSpinSlotsContainer.childCount == 0) return; // Exit if no child slots exist
        Transform initialVisualSlot = visualSpinSlotsContainer.GetChild(0);

        int currentVisualSlotCount = visualSpinSlotsContainer.childCount;

        // Add slots if current count is less than the desired count
        for (int i = currentVisualSlotCount; i < verticalSlotsCount; i++)
        {
            Transform newVisualSlot = Instantiate(initialVisualSlot, visualSpinSlotsContainer);
            newVisualSlot.name = $"VerticalVisualSlot {i + 1}"; // Optional: rename slots for clarity
        }

        // Remove extra slots if current count is more than the desired count
        for (int i = currentVisualSlotCount - 1; i >= verticalSlotsCount; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(visualSpinSlotsContainer.GetChild(i).gameObject);
#endif
        }

        if (!visualsSpin)
        {
            visualSpinSlotsContainer.gameObject.SetActive(false);
        }
        else if (!spinOut && !visualSpinSlotsContainer.gameObject.activeSelf)
        {
            visualSpinSlotsContainer.gameObject.SetActive(true);
        }

    }

    private void InitSlotSymbols()
    {
        foreach (Transform verticalSlot in verticalSlotsContainer)
        {
            // Get all Slot children inside each VerticalSlot
            foreach (Transform slot in verticalSlot)
            {
                // Add to the list if it's a Slot
                if (slot.name.StartsWith("Slot"))
                {
                    slots.Add(slot.gameObject);
                }
            }
        }
        foreach (Transform verticalVisualSlot in verticalVisualSlotsContainer)
        {
            foreach (Transform visualSlot in verticalVisualSlot)
            {
                if (visualSlot.name.StartsWith("Slot"))
                {
                    visualSlots.Add(visualSlot.gameObject);
                }
            }
        }
    }
    
    private void UpdateSlotStyle()
    {
        if (slotStyle == SlotStyle.custom) return;
        else if (oldSlotStyle != slotStyle)
        {
            oldSlotStyle = slotStyle;
            SetSlotStyle(slotStyle);
        }
    }

    private void SetSlotStyle(SlotStyle slotStyle)
    {
        switch (slotStyle)
        {
            case SlotStyle.custom:
                return;
            case SlotStyle.original:
                SetSpinStyle(true, 1, 11, SpinInStyle.bouncy, 6, 1.5f, false, SpinOutStyle.all, 6);
                break;
            case SlotStyle.fast:
                SetSpinStyle(false, 0, 0, SpinInStyle.bouncy, 30, 2, true, SpinOutStyle.all, 5);
                break;
            case SlotStyle.dropping:
                SetSpinStyle(false, 0.1f, 11, SpinInStyle.bouncy, 10, 1, true, SpinOutStyle.all, 6);
                break;
            case SlotStyle.droppingOneByOne:
                SetSpinStyle(false, 0.1f, 11, SpinInStyle.bouncy, 10, 1, true, SpinOutStyle.oneByOne, 10);
                break;

        }
    }
    private void SetSpinStyle(bool visualsSpin, float visualsSpinTime, float visualsSpinSpeed, SpinInStyle spinInStyle, float spinSpeedIn, float bounceSpeed, bool spinOut, SpinOutStyle spinOutStyle, float spinSpeedOut)
    {
        this.visualsSpin = visualsSpin;
        this.visualsSpinTime = visualsSpinTime;
        this.visualsSpinSpeed = visualsSpinSpeed;
        this.spinInStyle = spinInStyle;
        this.spinSpeedIn = spinSpeedIn;
        this.bounceSpeed = bounceSpeed;
        this.spinOut = spinOut;
        this.spinOutStyle = spinOutStyle;
        this.spinSpeedOut = spinSpeedOut;
    }
    private void UpdateScale()
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }
    
    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.S)) Spin();
        
        if (!visualsSpin)
        {
            visualSpinSlotsContainer.gameObject.SetActive(false);
        }
        else if (!spinOut && !visualSpinSlotsContainer.gameObject.activeSelf)
        {
            visualSpinSlotsContainer.gameObject.SetActive(true);
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
    
    private void CheckSlotMatches()
    {
        Debug.Log("Checking...");
    }
    private void Spin()
    {
        if (slotsSpinning) return;

        RunSlotAnimation();

    }

    private void SetSlotSymbols()
    {
        foreach (var slot in slots)
        {
            int rndIndex = UnityEngine.Random.Range(0, slotSymbols.Length);
            SlotSymbol slotSymbol = slotSymbols[rndIndex];
            slot.name = slotSymbol.symbolName;
            slot.GetComponent<Image>().sprite = slotSymbols[rndIndex].sprite;
        }
    }

    private void RunSlotAnimation()
    {
        StartCoroutine(RunSlotSpinAnimation());
    }
    
    private void SetVisualSlotSymbols()
    {
        foreach (var slot in visualSlots)
        {
            int rndIndex = UnityEngine.Random.Range(0, slotSymbols.Length);
            SlotSymbol slotSymbol = slotSymbols[rndIndex];
            slot.name = slotSymbol.symbolName;
            slot.GetComponent<Image>().sprite = slotSymbols[rndIndex].sprite;
        }
    }
    IEnumerator RunSlotSpinAnimation()
    {
        if (spinOut && visualsSpin)
        {
            visualSpinSlotsContainer.gameObject.SetActive(false);
            RunSlotOutAnimation();
        }
        else if (spinOut)
            RunSlotOutAnimation();

        SetVisualSlotSymbols();

        while (slotsSpinning)
        {
            yield return null;
        }

        if (spinOut && visualsSpin) visualSpinSlotsContainer.gameObject.SetActive(true);
        RunSlotInAnimation();

    }
    
    private void ResetSlotPositions()
    {
        foreach (Transform verticalSlot in verticalSlots)
        {
            verticalSlot.localPosition = new Vector3(verticalSlot.localPosition.x, verticalSlotYStart, verticalSlot.localPosition.z);
        }
        SetSlotSymbols();
    }
    
    private void RunSlotInAnimation()
    {
        ResetSlotPositions();
        StartCoroutine(RunSlotInAnimationsCoroutine());
    }

    IEnumerator RunSlotInAnimationsCoroutine()
    {
        slotsSpinning = true;
        if (visualsSpin)
        {
            float time = 0;
            shouldSpinVisualSlots = true;
            StartCoroutine(StartVisualSlotsSpinning());
            while (time < visualsSpinTime)
            {
                time += Time.deltaTime;
                yield return null;
            }
        }

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
        shouldSpinVisualSlots = false;
        SetSlotPositionsFinish();
        slotsSpinning = false;
        CheckSlotMatches();
    }

    IEnumerator StartVisualSlotsSpinning()
    {
        Vector3 targetPos = new Vector3(visualSpinSlotsContainer.localPosition.x, -540f, visualSpinSlotsContainer.localPosition.z);
        if (spinOut) visualSpinSlotsContainer.localPosition = new Vector3(targetPos.x, 1600, targetPos.z);
        while (shouldSpinVisualSlots)
        {
            if (visualSpinSlotsContainer.localPosition.y > targetPos.y)
            {
                visualSpinSlotsContainer.localPosition = Vector3.MoveTowards(visualSpinSlotsContainer.localPosition, targetPos, visualsSpinSpeed);
            }
            else
            {
                visualSpinSlotsContainer.localPosition = new Vector3(targetPos.x, 540, targetPos.z);
            }
            yield return null;
        }
        visualSpinSlotsContainer.localPosition = new Vector3(targetPos.x, 0, targetPos.z);
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
