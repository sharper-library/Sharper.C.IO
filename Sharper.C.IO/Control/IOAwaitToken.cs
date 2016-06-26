namespace Sharper.C.Control
{
    public sealed class IOAwaitToken
    {
        private IOAwaitToken()
        {
        }

        internal static IOAwaitToken Instance { get; }
        =   new IOAwaitToken();
    }
}
