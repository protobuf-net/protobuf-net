
namespace WcfServer
{
    // NOTE: If you change the class name "BasicService" here, you must also update the reference to "BasicService" in Web.config.
    public class BasicService : IBasicService
    {
        public BasicType BasicOperation()
        {
            return new BasicType { Id = 123, Name = "abc" };
        }
    }
}
