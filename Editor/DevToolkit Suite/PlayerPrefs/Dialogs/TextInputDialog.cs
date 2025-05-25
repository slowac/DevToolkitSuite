using DevToolkitSuite.PreferenceEditor.UI.Extensions;
using DevToolkitSuite.PreferenceEditor.UI;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DevToolkitSuite.PreferenceEditor.Dialogs
{
    /// <summary>
    /// Modal dialog window for text input with validation support.
    /// Provides customizable validation rules and real-time feedback for user input.
    /// </summary>
    public class TextInputDialog : EditorWindow
    {
        [NonSerialized]
        private string userInput = string.Empty;

        [NonSerialized]
        private Action<string> onConfirmCallback;

        [NonSerialized]
        private string dialogDescription;

        [NonSerialized]
        private List<InputValidator> validationRules = new List<InputValidator>();

        [NonSerialized]
        private InputValidator currentValidationError = null;

        /// <summary>
        /// Creates and displays a new text input dialog with validation support.
        /// </summary>
        /// <param name="title">Window title displayed in the dialog header</param>
        /// <param name="description">Instructional text explaining what input is expected</param>
        /// <param name="validators">List of validation rules to apply to user input</param>
        /// <param name="confirmCallback">Callback function executed when user confirms valid input</param>
        /// <param name="parentWindow">Optional parent window for positioning reference</param>
        public static void ShowInputDialog(string title, string description, List<InputValidator> validators, 
            Action<string> confirmCallback, EditorWindow parentWindow = null)
        {
            TextInputDialog dialogWindow = ScriptableObject.CreateInstance<TextInputDialog>();

            dialogWindow.name = "TextInputDialog '" + title + "'";
            dialogWindow.titleContent = new GUIContent(title);
            dialogWindow.dialogDescription = description;
            dialogWindow.onConfirmCallback = confirmCallback;
            dialogWindow.validationRules = validators;
            dialogWindow.position = new Rect(0, 0, 350, 140);

            dialogWindow.ShowUtility();
            dialogWindow.CenterRelativeToWindow(parentWindow);
            dialogWindow.Focus();
            EditorWindow.FocusWindowIfItsOpen<TextInputDialog>();
        }

        /// <summary>
        /// Handles the dialog's GUI rendering and user interaction.
        /// </summary>
        void OnGUI()
        {
            currentValidationError = null;
            Color originalContentColor = GUI.contentColor;

            GUILayout.Space(20);
            EditorGUILayout.LabelField(dialogDescription);
            GUILayout.Space(20);

            // Setup input field with focus management
            GUI.SetNextControlName(name + "_textInput");
            userInput = EditorGUILayout.TextField(userInput, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            // Validate current input against all rules
            ValidateCurrentInput();
            bool isInputValid = !(currentValidationError != null && currentValidationError.ValidationLevel == InputValidator.ErrorType.Error);

            RenderValidationFeedback(originalContentColor);
            RenderActionButtons(isInputValid);

            GUILayout.Space(20);

            // Manage input focus
            HandleInputFocus();
            ProcessKeyboardInput(isInputValid);
        }

        /// <summary>
        /// Validates the current user input against all defined validation rules.
        /// </summary>
        private void ValidateCurrentInput()
        {
            foreach (InputValidator validator in validationRules)
            {
                if (!validator.ValidateInput(userInput))
                {
                    currentValidationError = validator;
                    break;
                }
            }
        }

        /// <summary>
        /// Renders validation feedback icons and messages to the user.
        /// </summary>
        /// <param name="defaultColor">Original GUI content color to restore after rendering</param>
        private void RenderValidationFeedback(Color defaultColor)
        {
            GUILayout.BeginHorizontal();

            if (currentValidationError != null)
            {
                switch (currentValidationError.ValidationLevel)
                {
                    case InputValidator.ErrorType.Info:
                        GUI.contentColor = UIStyleManager.ColorPalette.InfoBlue;
                        GUILayout.Box(new GUIContent(ResourceManager.InfoIcon, currentValidationError.ErrorMessage), 
                            UIStyleManager.IconDisplayStyle);
                        break;
                        
                    case InputValidator.ErrorType.Warning:
                        GUI.contentColor = UIStyleManager.ColorPalette.WarningYellow;
                        GUILayout.Box(new GUIContent(ResourceManager.WarningIcon, currentValidationError.ErrorMessage), 
                            UIStyleManager.IconDisplayStyle);
                        break;
                        
                    case InputValidator.ErrorType.Error:
                        GUI.contentColor = UIStyleManager.ColorPalette.ErrorRed;
                        GUILayout.Box(new GUIContent(ResourceManager.WarningIcon, currentValidationError.ErrorMessage), 
                            UIStyleManager.IconDisplayStyle);
                        break;
                }
                GUI.contentColor = defaultColor;
            }

            GUILayout.FlexibleSpace();
        }

        /// <summary>
        /// Renders the cancel and confirm action buttons.
        /// </summary>
        /// <param name="enableConfirm">Whether the confirm button should be enabled</param>
        private void RenderActionButtons(bool enableConfirm)
        {
            if (GUILayout.Button("Cancel", GUILayout.Width(75.0f)))
                this.Close();

            GUI.enabled = enableConfirm;

            if (GUILayout.Button("OK", GUILayout.Width(75.0f)))
            {
                onConfirmCallback(userInput);
                Close();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Manages input field focus to ensure proper user experience.
        /// </summary>
        private void HandleInputFocus()
        {
            try
            {
                EditorGUI.FocusTextInControl(name + "_textInput");
            }
            catch (MissingReferenceException)
            {
                // Input field not yet available, safely ignore
            }
        }

        /// <summary>
        /// Processes keyboard shortcuts for dialog interaction.
        /// </summary>
        /// <param name="isInputValid">Whether current input passes validation</param>
        private void ProcessKeyboardInput(bool isInputValid)
        {
            if (Event.current != null && Event.current.isKey)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                        if (isInputValid)
                        {
                            onConfirmCallback(userInput);
                            Close();
                        }
                        break;
                        
                    case KeyCode.Escape:
                        Close();
                        break;
                }
            }
        }
    }
}