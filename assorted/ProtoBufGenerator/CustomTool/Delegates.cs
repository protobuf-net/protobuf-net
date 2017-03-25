namespace ProtoBufGenerator
{
    /// <summary>
    /// The delegate for the code generation
    /// </summary>
    /// <param name="sender">will always be the custom tool implementation
    /// derived from CustomToolBase</param>
    /// <param name="args">The GenerationEventArgs used to write the generated 
    /// code to, or perform other generation-time tasks</param>
    public delegate void GenerationHandler(object sender, GenerationEventArgs args);
}
