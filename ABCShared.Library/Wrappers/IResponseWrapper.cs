namespace ABCShared.Library.Wrappers
{
    public interface IResponseWrapper
    {
        List<string> Messages { get; set; }
        bool IsSuccessful { get; set; }
    }

    public interface IResponseWrapper<out T> : IResponseWrapper
    {
        T Data { get; }
    }
}
