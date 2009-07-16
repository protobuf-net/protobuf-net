using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ProtoBufGenerator
{
    /// <summary>
    /// The base class for the Custom Tool.  It implements the correct
    /// Visual Studio Shell interop and OLE interop interfaces to and 
    /// provides a simple event <see cref="OnGenerateCode"/> to handle
    /// to easily create and deploy a new Visual Studio 2008 Custom Tool.
    /// </summary>
    [ComVisible(true)]
    public abstract class CustomToolBase : Package, IVsSingleFileGenerator, IObjectWithSite
    {
        public CustomToolBase()
        {
            FileExtension = ".cs";
        }

        #region IVsSingleFileGenerator Members
        /// <summary>
        /// Provides the extension for the generated project item.
        /// In this case, always .cs
        /// </summary>
        /// <param name="pbstrDefaultExtension"></param>
        /// <returns></returns>
        int IVsSingleFileGenerator.DefaultExtension(out string pbstrDefaultExtension)
        {
            pbstrDefaultExtension = FileExtension;
            return 0;
        }

        /// <summary>
        /// Called by Visual Studio when it needs to generate or 
        /// regenerate the code file from your the custom tool
        /// input file.
        /// </summary>
        /// <param name="wszInputFilePath"></param>
        /// <param name="bstrInputFileContents"></param>
        /// <param name="wszDefaultNamespace"></param>
        /// <param name="rgbOutputFileContents"></param>
        /// <param name="pcbOutput"></param>
        /// <param name="pGenerateProgress"></param>
        /// <returns></returns>
        int IVsSingleFileGenerator.Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace, IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            GenerationEventArgs gea = new GenerationEventArgs(
                bstrInputFileContents,
                wszInputFilePath,
                wszDefaultNamespace,
                new ServiceProvider(Site as Microsoft.VisualStudio.OLE.Interop.IServiceProvider)
                    .GetService(typeof(ProjectItem)) as ProjectItem);

            if (OnGenerateCode != null)
            {
                OnGenerateCode(this, gea);
            }

            if (gea.OutputFileExtension.StartsWith("."))
            {
                this.FileExtension = gea.OutputFileExtension;
            }
            else
            {
                this.FileExtension = "." + gea.OutputFileExtension;
            }

            GenerationProgressFacade progressFacade =
                new GenerationProgressFacade(pGenerateProgress);

            foreach (GenerationWarning warning in gea.Warnings)
            {
                progressFacade.GenerateWarning(
                    warning.Message,
                    warning.LineNumber,
                    warning.ColumnNumber);
            }

            foreach (GenerationError error in gea.Errors)
            {
                progressFacade.GenerateError(
                    error.Message,
                    error.LineNumber,
                    error.ColumnNumber);
            }

            byte[] bytes = gea.GetOutputCodeBytes();

            int outputLength = bytes.Length;
            rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(outputLength);
            Marshal.Copy(bytes, 0, rgbOutputFileContents[0], outputLength);
            pcbOutput = (uint)outputLength;

            if (gea.FailOnError && gea.Errors.Count > 0)
            {
                return VSConstants.E_FAIL;
            }
            else
            {
                return VSConstants.S_OK;
            }
        }
        #endregion

        #region IObjectWithSite Members
        void IObjectWithSite.GetSite(ref Guid riid, out IntPtr ppvSite)
        {
            IntPtr pUnk = Marshal.GetIUnknownForObject(Site);
            IntPtr intPointer = IntPtr.Zero;
            Marshal.QueryInterface(pUnk, ref riid, out intPointer);
            ppvSite = intPointer;
        }

        void IObjectWithSite.SetSite(object pUnkSite)
        {
            Site = pUnkSite;
        }
        #endregion

        #region Public Members
        /// <summary>
        /// Necessary for the IObjectWithSite interface 
        /// implementation
        /// </summary>
        public object Site { get; private set; }

        /// <summary>
        /// The event inside which code is generated.
        /// </summary>
        public event GenerationHandler OnGenerateCode;

        public string FileExtension { get; set; }
        #endregion
    }
}
