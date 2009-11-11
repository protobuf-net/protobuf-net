using TestWcfDto;

namespace TestWcfServer
{
    // NOTE: If you change the class name "Service1" here, you must also update the reference to "Service1" in Web.config and in the associated .svc file.
    public class Service1 : IService1
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite != null && composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }
    }
}
