using System;
using System.Text;
using EnvDTE;
using System.Collections.Generic;

namespace ProtoBufGenerator
{
    /// <summary>
    /// This is the class passed to the custom tool class that provides
    /// a StringBuilder for code generation output, and a number of 
    /// properties that provide information or other ways to control
    /// output at generation time.
    /// </summary>
    public class GenerationEventArgs : EventArgs
    {
        public GenerationEventArgs() { }

        public GenerationEventArgs(string inputText, string filePath,
            string defaultNamespace,
            ProjectItem projItem)
        {
            InputText = inputText;
            InputFilePath = filePath;
            Namespace = defaultNamespace;
            OutputCode = new StringBuilder();
            ProjectItem = projItem;
            Encoding = Encoding.Default;
            FailOnError = false;
            Warnings = new List<GenerationWarning>();
            Errors = new List<GenerationError>();
            OutputFileExtension = ".cs"; // default
        }

        /// <summary>
        /// Inputcode is the text from the file that the custom tool
        /// is applied to, like the raw text from a template, you must
        /// parse it and build whatever output makes sense for your 
        /// tool
        /// </summary>
        public virtual string InputText { get; private set; }

        /// <summary>
        /// This is the StringBuilder provided to collect the
        /// output text from your template.  Currently you must
        /// provide valid C# code by the end of generation as the 
        /// only implemented output is a .cs class.
        /// </summary>
        public virtual StringBuilder OutputCode { get; private set; }

        /// <summary>
        /// Provides the full path to the Input file.
        /// </summary>
        public virtual string InputFilePath { get; private set; }

        /// <summary>
        /// Defaults to 'cs'.  Allows choosing the
        /// generated file extension.
        /// </summary>
        public virtual string OutputFileExtension { get; set; }

        /// <summary>
        /// The is the (user definable) namespace for the output
        /// C# code.
        /// </summary>
        public virtual string Namespace { get; private set; }

        /// <summary>
        /// Provides the very useful EnvDTE.ProjectItem instance
        /// for the input code file.  This is incredibly useful if
        /// you wish to, say, add references to the project, or additional
        /// code or other tyeps files of files by using the
        /// ProjectItem.ContainingProject property.
        /// </summary>
        public virtual ProjectItem ProjectItem { get; private set; }

        /// <summary>
        /// The encoding used to turn the output from OutputCode to
        /// a byte array.  Encoding.Default is used by default.
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// If set to true, FailOnError causes the tool
        /// to fail and not generate any code.  Having it
        /// set to false will allow the code generation to
        /// complete.
        /// </summary>
        public bool FailOnError { get; set; }

        public List<GenerationWarning> Warnings { get; private set; }
        public List<GenerationError> Errors { get; private set; }

        public void GenerateError(string errorMessage)
        {
            GenerateError(errorMessage, 0, 0);
        }

        public void GenerateError(string errorMessage, int sourceFileLineNumber)
        {
            GenerateError(errorMessage, sourceFileLineNumber, 0);
        }

        public void GenerateError(string errorMessage, int sourceFileLineNumber, int sourceFileColumnNumber)
        {
            Errors.Add(new GenerationError
            {
                Message = errorMessage,
                LineNumber = sourceFileLineNumber,
                ColumnNumber = sourceFileColumnNumber
            });
        }

        public void GenerateWarning(string errorMessage)
        {
            GenerateWarning(errorMessage, 0, 0);
        }

        public void GenerateWarning(string errorMessage, int sourceFileLineNumber)
        {
            GenerateWarning(errorMessage, sourceFileLineNumber, 0);
        }

        public void GenerateWarning(string errorMessage, int sourceFileLineNumber, int sourceFileColumnNumber)
        {
            Warnings.Add(new GenerationWarning
            {
                Message = errorMessage,
                LineNumber = sourceFileLineNumber,
                ColumnNumber = sourceFileColumnNumber
            });
        }

        public byte[] GetOutputCodeBytes()
        {
            return this.Encoding.GetBytes(OutputCode.ToString());
        }
    }
}
