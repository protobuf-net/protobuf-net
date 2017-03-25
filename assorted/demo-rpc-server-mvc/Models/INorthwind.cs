namespace demo_rpc_server_mvc.Models
{
    public interface INorthwind
    {
        Customer[] GetCustomers();
        Order[] GetOrders(string customerKey);
    }
}
