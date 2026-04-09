using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class BananaShellWindow : EditorWindow
{
    private ScrollView outputScrollView;
    private Label outputLabel;
    private TextField inputField;

    private BananaShell shell;

    public void CreateGUI()
    {
        shell ??= new BananaShell();
        shell.OnOutputChanged += UpdateOutput;

        // Terminal background and text styling
        var root = rootVisualElement;
        root.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f));

        outputScrollView = new ScrollView();
        outputScrollView.style.flexGrow = 1;
        outputScrollView.style.paddingLeft = 5;
        outputScrollView.style.paddingRight = 5;
        outputScrollView.style.paddingTop = 5;
        outputScrollView.style.paddingBottom = 5;

        outputLabel = new Label();
        outputLabel.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
        outputLabel.style.whiteSpace = WhiteSpace.Normal;
        
        outputScrollView.Add(outputLabel);
        root.Add(outputScrollView);

        inputField = new TextField();
        inputField.style.backgroundColor = new StyleColor(Color.black);
        inputField.style.color = new StyleColor(new Color(0.2f, 0.8f, 0.2f)); // Terminal green
        inputField.style.marginBottom = 5;
        inputField.style.marginLeft = 5;
        inputField.style.marginRight = 5;
        inputField.style.marginTop = 5;
        
        // Hide standard text field background to make it look flush
        var textInput = inputField.Q(className: "unity-text-field__input");
        if (textInput != null)
        {
            textInput.style.backgroundColor = new StyleColor(Color.black);
            textInput.style.borderBottomWidth = 0;
            textInput.style.borderTopWidth = 0;
            textInput.style.borderLeftWidth = 0;
            textInput.style.borderRightWidth = 0;
        }

        inputField.RegisterCallback<KeyDownEvent>(OnInputKeyDown);
        
        root.Add(inputField);

        inputField.Focus();
        UpdateOutput();
    }

    private void OnInputKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            string cmd = inputField.value;
            inputField.value = "";
            shell.Execute(cmd);
            
            // Scroll to bottom after layout update
            rootVisualElement.schedule.Execute(() =>
            {
                outputScrollView.scrollOffset = new Vector2(0, outputScrollView.contentContainer.layout.height);
            }).StartingIn(10);
            
            evt.StopPropagation(); // prevent default behavior
            
            // Refocus to keep typing
            inputField.Focus();
        }
    }

    private void UpdateOutput()
    {
        if (outputLabel != null && shell != null)
        {
            outputLabel.text = shell.GetOutput();
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
        if (inputField != null)
        {
            inputField.Focus();
        }
    }
}
