using Best.HTTP.Shared.PlatformSupport.Network.Tcp;

namespace Best.HTTP.Shared.Streams
{
    /// <summary>
    /// A PeekableStream implementation that also implements the <see cref="IPeekableContentProvider"/> interface too.
    /// </summary>
    public abstract class PeekableContentProviderStream : PeekableStream, IPeekableContentProvider
    {
        public PeekableContentProviderStream Peekable => this;

        public IContentConsumer Consumer { get; private set; }

        public void SetTwoWayBinding(IContentConsumer consumer)
        {
            this.Consumer = consumer;
            this.Consumer?.SetBinding(this);
        }

        /// <summary>
        /// This will set Consumer to null.
        /// </summary>
        public void Unbind()
        {
            this.Consumer?.UnsetBinding();
            this.Consumer = null;
        }

        /// <summary>
        /// Set Consumer to null if the current one is the one passed in the parameter. 
        /// </summary>
        public void UnbindIf(IContentConsumer consumer)
        {
            if (consumer == null || consumer == this.Consumer)
            {
                this.Consumer?.UnsetBinding();
                this.Consumer = null;
            }
        }
    }
}
