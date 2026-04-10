using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;

public class BananaShellWindow : EditorWindow
{
    private ScrollView outputScrollView;
    private Label outputLabel;
    
    private TextField inputField;
    private Label fakeInputLabelBefore;
    private Label fakeInputLabelAfter;
    private VisualElement customCaret;
    private Label spinnerLabel;
    
    private BananaShell shell;
    private bool cursorBlink = true;
    private bool isFieldFocused = true;
    private int lastCursorIndex = -1;
    private string lastValue = null;
    private double lastInteractionTime = 0;
    private bool isExecutingCommand = false;

    private VisualElement inputContainer;
    private Label promptLabel;
    private string activeCommand = "";
    private double executionStartTime = 0;
    private string[] spinnerFrames = new string[] { "-", "\\", "|", "/" };
    private int spinnerIndex = 0;

    private const long CARET_BLINK_RATE_MS = 500;
    private const long CARET_UPDATE_POLL_MS = 50;
    private const long TIMER_UPDATE_POLL_MS = 70;

    [SerializeField] private List<string> serializedHistory = new List<string>();
    [SerializeField] private string serializedInputText = "";
    [SerializeField] private int serializedCursorIndex = 0;
    [SerializeField] private Vector2 serializedScrollOffset = Vector2.zero;

    public static void ShowWindow()
    {
        var window = EditorWindow.GetWindow<BananaShellWindow>();
        
        // Set up the tab title and icon
        // You can replace "UnityEditor.ConsoleWindow" with your own texture via AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/path/to/icon.png")
        Texture2D icon = EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow").image as Texture2D;
        window.titleContent = new GUIContent("Banana Shell", icon); 
        
        window.Show();
        window.Focus();
    }

    public void CreateGUI()
    {
        shell = new BananaShell(serializedHistory);
        shell.OnOutputChanged += UpdateOutput;

        var root = rootVisualElement;
        root.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f));

        // We removed the strict TrickleDown block on root so the TextField can accurately receive actual click coordinates for caret placement!

        outputScrollView = new ScrollView();
        outputScrollView.style.flexGrow = 1;
        outputScrollView.style.paddingLeft = 5;
        outputScrollView.style.paddingRight = 5;
        outputScrollView.style.paddingTop = 5;
        outputScrollView.style.paddingBottom = 6;

        outputLabel = new Label();
        outputLabel.style.color = new StyleColor(Color.white); // ALL text to white
        outputLabel.style.whiteSpace = WhiteSpace.PreWrap;
        outputLabel.enableRichText = true;
        
        outputScrollView.Add(outputLabel);
        
        // Clicking anywhere in the empty history area focuses the input field
        outputScrollView.RegisterCallback<PointerDownEvent>(evt => {
            if (inputField != null) inputField.Focus();
        });

        var inputRow = new VisualElement();
        inputRow.style.flexDirection = FlexDirection.Row;
        inputRow.style.marginTop = 0;

        promptLabel = new Label("> ");
        promptLabel.style.color = new StyleColor(Color.white);
        promptLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        promptLabel.style.marginRight = 0; // Margin removed; space character naturally pads it now 
        promptLabel.enableRichText = true;
        inputRow.Add(promptLabel);

        inputContainer = new VisualElement();
        inputContainer.style.flexGrow = 1;

        // -- ACTUAL HIDDEN INPUT FIELD --
        // Normal layout flow so it perfectly captures text and events!
        inputField = new TextField();
        inputField.style.flexGrow = 1;
        inputField.style.backgroundColor = new StyleColor(Color.clear);
        inputField.style.color = new StyleColor(Color.clear); // Hide real text
        
        inputField.style.borderBottomWidth = 0;
        inputField.style.borderTopWidth = 0;
        inputField.style.borderLeftWidth = 0;
        inputField.style.borderRightWidth = 0;
        inputField.style.marginTop = 0;
        inputField.style.marginBottom = 0;
        inputField.style.marginLeft = 0;
        inputField.style.marginRight = 0;

        // -- VISIBLE OVERLAY (Label + 2px Caret) --
        var visibleInputContainer = new VisualElement();
        visibleInputContainer.style.flexDirection = FlexDirection.Row;
        visibleInputContainer.style.position = Position.Absolute;
        visibleInputContainer.style.left = 0;
        visibleInputContainer.style.top = 0;
        visibleInputContainer.style.bottom = 0;
        visibleInputContainer.style.right = 0;
        visibleInputContainer.style.alignItems = Align.Center; 
        // THIS OPAQUE BACKGROUND PHYSICALLY OVERDRAWS THE NATIVE CARET BENEATH IT
        visibleInputContainer.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f)); 
        visibleInputContainer.pickingMode = PickingMode.Ignore; // Let clicks fall through to textfield

        fakeInputLabelBefore = new Label();
        fakeInputLabelBefore.style.color = new StyleColor(Color.white);
        fakeInputLabelBefore.style.unityTextAlign = TextAnchor.MiddleLeft;
        fakeInputLabelBefore.style.paddingLeft = 0;
        fakeInputLabelBefore.style.paddingRight = 0;
        fakeInputLabelBefore.style.paddingTop = 0;
        fakeInputLabelBefore.style.paddingBottom = 0;
        fakeInputLabelBefore.style.whiteSpace = WhiteSpace.Pre;

        fakeInputLabelAfter = new Label();
        fakeInputLabelAfter.style.color = new StyleColor(Color.white);
        fakeInputLabelAfter.style.unityTextAlign = TextAnchor.MiddleLeft;
        fakeInputLabelAfter.style.paddingLeft = 0;
        fakeInputLabelAfter.style.paddingRight = 0;
        fakeInputLabelAfter.style.paddingTop = 0;
        fakeInputLabelAfter.style.paddingBottom = 0;
        fakeInputLabelAfter.style.whiteSpace = WhiteSpace.Pre;

        customCaret = new VisualElement();
        customCaret.style.width = 2; // Exact 2px wide
        customCaret.style.height = 13; 
        customCaret.style.backgroundColor = new StyleColor(Color.white); 
        customCaret.style.marginLeft = 1;
        customCaret.style.marginRight = 1;

        spinnerLabel = new Label();
        spinnerLabel.style.color = new StyleColor(Color.white);
        spinnerLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        spinnerLabel.style.paddingLeft = 0;
        spinnerLabel.style.paddingRight = 0;
        spinnerLabel.style.paddingTop = 0;
        spinnerLabel.style.paddingBottom = 0;
        spinnerLabel.enableRichText = true;

        visibleInputContainer.Add(fakeInputLabelBefore);
        visibleInputContainer.Add(customCaret);
        visibleInputContainer.Add(fakeInputLabelAfter);
        visibleInputContainer.Add(spinnerLabel);

        // Ensure internal unity text elements have background and padding cleared
        inputField.RegisterCallback<GeometryChangedEvent>(evt => 
        {
            var textInput = inputField.Q(className: "unity-text-field__input");
            if (textInput != null)
            {
                textInput.style.backgroundColor = new StyleColor(Color.clear);
                textInput.style.borderBottomWidth = 0;
                textInput.style.borderTopWidth = 0;
                textInput.style.borderLeftWidth = 0;
                textInput.style.borderRightWidth = 0;
                textInput.style.color = new StyleColor(Color.clear); 
                textInput.style.paddingLeft = 0;
                textInput.style.paddingRight = 0;
                textInput.style.paddingTop = 0;
                textInput.style.paddingBottom = 0;
            }
        });

        inputField.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
        inputField.RegisterCallback<KeyUpEvent>(OnInputKeyUp);
        inputField.RegisterValueChangedCallback(evt => UpdateCustomCaretText());
        
        inputField.RegisterCallback<FocusEvent>(evt => 
        {
            isFieldFocused = true;
            lastInteractionTime = EditorApplication.timeSinceStartup;
            UpdateCustomCaretText();
        });
        inputField.RegisterCallback<BlurEvent>(evt => 
        {
            isFieldFocused = false;
            if (customCaret != null) customCaret.style.opacity = 0f;
        });
        
        // ADD ORDER IS CRITICAL: overlay AFTER field to overwrite its rendering
        inputContainer.Add(inputField);
        inputContainer.Add(visibleInputContainer);

        inputRow.Add(inputContainer);
        outputScrollView.Add(inputRow);
        
        root.Add(outputScrollView);
        
        // Visually anchor the scroll container bounds identically seamlessly to the Window's absolute bottom frame geometrically during manual drag resizes
        outputScrollView.RegisterCallback<GeometryChangedEvent>(evt => {
            if (evt.oldRect.height != evt.newRect.height && evt.oldRect.height > 0f) {
                float heightDelta = evt.oldRect.height - evt.newRect.height;
                outputScrollView.scrollOffset = new Vector2(0, outputScrollView.scrollOffset.y + heightDelta);
            }
        });

        rootVisualElement.schedule.Execute(() => {
            UpdateCustomCaretText(); // Poll to catch mouse clicks or delayed arrow key calculations instantly
            if (outputScrollView != null) {
                serializedScrollOffset = outputScrollView.scrollOffset;
            }
        }).Every(CARET_UPDATE_POLL_MS);

        rootVisualElement.schedule.Execute(() => {
            if (!isFieldFocused || isExecutingCommand || customCaret == null) return;
            
            double diff = EditorApplication.timeSinceStartup - lastInteractionTime;
            long blinkPhase = (long)(diff * 2.0); // 0.5s intervals intrinsically mathematically evaluate down to 2 multiplier 
            customCaret.style.opacity = (blinkPhase % 2 == 0) ? 1f : 0f;
        }).Every(50);
        
        rootVisualElement.schedule.Execute(() => {
            if (isExecutingCommand && spinnerLabel != null) {
                double elapsed = EditorApplication.timeSinceStartup - executionStartTime;
                if (elapsed > 0.23)
                {
                    spinnerIndex = (spinnerIndex + 1) % spinnerFrames.Length;
                    spinnerLabel.text = $"<color=#888888> {BananaShell.FormatExecutionTime(elapsed)}s {spinnerFrames[spinnerIndex]}</color>";
                }
            }
        }).Every(TIMER_UPDATE_POLL_MS);

        // Extremely aggressive permanent focus lock! If this Window sits actively on top of the Unity Editor, 
        // absolutely force the text field to maintain UI Toolkit focus (survives Unity recompilation/Asset imports!)
        rootVisualElement.schedule.Execute(() => {
            if (EditorWindow.focusedWindow == this && inputField != null && inputField.focusController != null)
            {
                if (inputField.focusController.focusedElement != inputField)
                {
                    inputField.Focus();
                }
            }
        }).Every(100);

        // Restore serialized input text and selection from play-mode/compile shifts
        inputField.SetValueWithoutNotify(serializedInputText);
        rootVisualElement.schedule.Execute(() => {
            if (inputField != null) {
                inputField.cursorIndex = serializedCursorIndex;
                UpdateCustomCaretText(); // Force a render sync
            }
        }).StartingIn(20);

        if (outputScrollView != null) {
            EventCallback<GeometryChangedEvent> geoCallback = null;
            geoCallback = (evt) => {
                outputScrollView.scrollOffset = serializedScrollOffset;
                outputScrollView.contentContainer.UnregisterCallback(geoCallback);
            };
            outputScrollView.contentContainer.RegisterCallback(geoCallback);
        }

        inputField.Focus();
        UpdateOutput();
        UpdateCustomCaretText();
    }

    private void UpdateCustomCaretText()
    {
        // Strictly freeze all text updating if unfocused to prevent Unity's native cursor reset from moving our visual caret
        if (!isFieldFocused || isExecutingCommand) return;

        if (fakeInputLabelBefore != null && fakeInputLabelAfter != null && inputField != null)
        {
            int ci = inputField.cursorIndex;
            string val = inputField.value;

            // Skip garbage collection and layout calculations if the cursor hasn't moved
            if (ci == lastCursorIndex && val == lastValue) return;

            lastCursorIndex = ci;
            lastValue = val;
            lastInteractionTime = EditorApplication.timeSinceStartup;
            
            // Constantly serialize the user's active input state into the Unity instance
            serializedInputText = val;
            serializedCursorIndex = ci;

            if (ci < 0) ci = 0;
            if (ci > val.Length) ci = val.Length;
            
            fakeInputLabelBefore.text = val.Substring(0, ci);
            fakeInputLabelAfter.text = val.Substring(ci);
        }
    }

    private void OnInputKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            // Fully silence the native formatting logic on key down so it never inserts new lines
            evt.StopPropagation();
            return;
        }
    }

    private void OnInputKeyUp(KeyUpEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            evt.StopPropagation();

            string cmd = inputField.value;
            if (string.IsNullOrWhiteSpace(cmd)) return;

            // Phase 1: Lock UI perfectly by disabling caret physically while ensuring exact text layout geometry holds identically still
            isExecutingCommand = true;
            activeCommand = cmd;
            executionStartTime = EditorApplication.timeSinceStartup;

            if (customCaret != null) customCaret.style.opacity = 0f;
            if (fakeInputLabelBefore != null) fakeInputLabelBefore.text = cmd;
            if (fakeInputLabelAfter != null) fakeInputLabelAfter.text = "";

            if (outputScrollView != null) {
                // Wait for the UI layout to physically draw the static text string instantly
                outputScrollView.schedule.Execute(() => {
                    outputScrollView.scrollOffset = new Vector2(0, outputScrollView.contentContainer.layout.height);

                    // Phase 2: Execute computationally heavy command natively
                    rootVisualElement.schedule.Execute(() => {
                        // Inflate the history log sequentially just before execution locking!
                        shell.EchoInput(cmd);
                        
                        double trueBackendStart = EditorApplication.timeSinceStartup;
                        shell.ExecuteCommand(cmd);
                        
                        // Phase 4: Immortalize the execution timing footprint into the text history string intrinsically
                        double finalTime = EditorApplication.timeSinceStartup - trueBackendStart;
                        shell.AppendExecutionTime(finalTime);
                        
                        // Phase 5: Resume full terminal interactability
                        isExecutingCommand = false;
                        
                        inputField.value = ""; // ONLY NOW safely wipe natively!
                        if (spinnerLabel != null) spinnerLabel.text = "";
                        
                        UpdateCustomCaretText();
                        if (inputField != null) inputField.Focus();

                        // Push log down again after command output triggers
                        if (outputScrollView != null) {
                            outputScrollView.schedule.Execute(() => {
                                outputScrollView.scrollOffset = new Vector2(0, outputScrollView.contentContainer.layout.height);
                            }).StartingIn(10);
                        }
                    }).StartingIn(10); // Standard layout physics buffer

                }).StartingIn(10);
            }
        }
    }

    private void UpdateOutput()
    {
        if (outputLabel != null && shell != null)
        {
            outputLabel.text = shell.GetOutput();
            // Cache shell history so it survives domain reloads natively
            serializedHistory = new List<string>(shell.GetHistoryList());
        }
    }

    private void OnDisable()
    {
        if (shell != null)
        {
            shell.OnOutputChanged -= UpdateOutput;
        }
    }

    private void OnFocus()
    {
        if (inputField != null && rootVisualElement != null)
        {
            rootVisualElement.schedule.Execute(() =>
            {
                inputField.Focus();
            }).StartingIn(10);
        }
    }
}
