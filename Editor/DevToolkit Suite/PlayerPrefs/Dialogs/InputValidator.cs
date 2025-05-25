using System;
using System.Text.RegularExpressions;

namespace DevToolkitSuite.PreferenceEditor.Dialogs
{
    /// <summary>
    /// Validation engine for text input fields with support for regex patterns and custom validation functions.
    /// Provides feedback levels ranging from informational messages to blocking errors.
    /// </summary>
    public class InputValidator
    {
        /// <summary>
        /// Defines the severity level of validation feedback to present to the user.
        /// </summary>
        public enum ErrorType
        {
            /// <summary>Invalid or uninitialized validation state</summary>
            Invalid = -1,
            /// <summary>Informational message that doesn't prevent submission</summary>
            Info = 0,
            /// <summary>Warning that suggests caution but allows submission</summary>
            Warning = 1,
            /// <summary>Error that prevents form submission until resolved</summary>
            Error = 2
        }

        [NonSerialized]
        public ErrorType ValidationLevel = ErrorType.Invalid;

        [NonSerialized]
        private string regexPattern = string.Empty;

        [NonSerialized]
        private Func<string, bool> customValidationFunction;

        [NonSerialized]
        public string ErrorMessage = string.Empty;

        /// <summary>
        /// Creates a new input validator based on regular expression pattern matching.
        /// </summary>
        /// <param name="validationLevel">Severity level of validation failure</param>
        /// <param name="errorMessage">User-friendly message explaining the validation requirement</param>
        /// <param name="pattern">Regular expression pattern that valid input must match</param>
        public InputValidator(ErrorType validationLevel, string errorMessage, string pattern)
        {
            ValidationLevel = validationLevel;
            ErrorMessage = errorMessage;
            regexPattern = pattern;
        }

        /// <summary>
        /// Creates a new input validator using a custom validation function.
        /// </summary>
        /// <param name="validationLevel">Severity level of validation failure</param>
        /// <param name="errorMessage">User-friendly message explaining the validation requirement</param>
        /// <param name="validationFunction">Custom function that returns true for valid input, false otherwise</param>
        public InputValidator(ErrorType validationLevel, string errorMessage, Func<string, bool> validationFunction)
        {
            ValidationLevel = validationLevel;
            ErrorMessage = errorMessage;
            customValidationFunction = validationFunction;
        }

        /// <summary>
        /// Evaluates the provided input string against the configured validation rules.
        /// </summary>
        /// <param name="inputText">Text input to validate</param>
        /// <returns>True if input passes validation, false if validation fails</returns>
        public bool ValidateInput(string inputText)
        {
            // Regex pattern validation takes precedence
            if (!string.IsNullOrEmpty(regexPattern))
                return Regex.IsMatch(inputText, regexPattern);
            
            // Fall back to custom validation function
            if (customValidationFunction != null)
                return customValidationFunction(inputText);

            // No validation method configured - consider invalid
            return false;
        }
    }
}