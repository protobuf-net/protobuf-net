using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace ProtoBufGenerator
{
    /// <summary>
    /// Wraps IVsGeneratorProgress to allow for easy generation-time
    /// reporting of progress to Visual Studio and of errors and 
    /// warnings during code generation.
    /// </summary>
    public class GenerationProgressFacade
    {
        internal GenerationProgressFacade(IVsGeneratorProgress vsGeneratorProgress)
        {
            Debug.Assert(vsGeneratorProgress != null, "progress parameter cannot be null");

            vsProgress = vsGeneratorProgress;
        }

        IVsGeneratorProgress vsProgress;

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
            vsProgress.GeneratorError(0, 0, errorMessage, (uint)sourceFileLineNumber, (uint)sourceFileColumnNumber);
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
            vsProgress.GeneratorError(1, 0, errorMessage, (uint)sourceFileLineNumber, (uint)sourceFileColumnNumber);
        }

        public void ReportProgress(int currentStep, int totalSteps)
        {
            vsProgress.Progress((uint)currentStep, (uint)totalSteps);
        }
    }
}
