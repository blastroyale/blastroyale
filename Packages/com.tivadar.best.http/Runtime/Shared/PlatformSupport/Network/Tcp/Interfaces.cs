using System;

using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Shared.PlatformSupport.Network.Tcp
{
    /// <summary>
    /// The IPeekableContentProvider interface defines an abstraction for providing content to an <see cref="IContentConsumer"/> with the ability to peek at the content without consuming it.
    /// It is an essential part of content streaming over a TCP connection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Key Functions of IPeekableContentProvider:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Content Provision</term><description>It provides content to an associated <see cref="IContentConsumer"/> without immediately consuming the content. This allows the consumer to examine the data before processing.
    /// </description></item>
    /// <item>
    /// <term>Two-Way Binding</term><description>Supports establishing a two-way binding between the <see cref="IPeekableContentProvider"/> and an <see cref="IContentConsumer"/>, enabling bidirectional communication between the provider and consumer.
    /// </description></item>
    /// <item>
    /// <term>Unbinding</term><description>Provides methods for unbinding a content consumer, terminating the association between the provider and consumer.
    /// </description></item>
    /// </list>
    /// </remarks>
    public interface IPeekableContentProvider
    {
        /// <summary>
        /// Gets the <see cref="PeekableContentProviderStream"/> associated with this content provider, which allows peeking at the content without consuming it.
        /// </summary>
        PeekableContentProviderStream Peekable { get; }

        /// <summary>
        /// Gets the <see cref="IContentConsumer"/> implementor that will be notified through <see cref="IContentConsumer.OnContent"/> calls when new data is available in the TCPStreamer.
        /// </summary>
        IContentConsumer Consumer { get; }

        /// <summary>
        /// Sets up a two-way binding between this content provider and an <see cref="IContentConsumer"/>. This enables bidirectional communication between the provider and consumer.
        /// </summary>
        /// <param name="consumer">The <see cref="IContentConsumer"/> to bind to.</param>
        void SetTwoWayBinding(IContentConsumer consumer);

        /// <summary>
        /// Unbinds the content provider from its associated content consumer. This terminates the association between the provider and consumer.
        /// </summary>
        void Unbind();

        /// <summary>
        /// Unbinds the content provider from a specific content consumer if it is currently bound to that consumer.
        /// </summary>
        /// <param name="consumer">The <see cref="IContentConsumer"/> to unbind from.</param>
        void UnbindIf(IContentConsumer consumer);
    }

    /// <summary>
    /// The IContentConsumer interface represents a consumer of content provided by an <see cref="IPeekableContentProvider"/>. It defines methods for handling received content and connection-related events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Key Functions of IContentConsumer:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Content Handling</term><description>Defines methods for handling incoming content, allowing consumers to process data as it becomes available.
    /// </description></item>
    /// <item>
    /// <term>Connection Management</term><description>Provides event methods to notify consumers of connection closure and error conditions, facilitating graceful handling of connection-related issues.
    /// </description></item>
    /// </list>
    /// </remarks>
    public interface IContentConsumer
    {
        /// <summary>
        /// Gets the <see cref="PeekableContentProviderStream"/> associated with this content consumer, which allows access to incoming content.
        /// </summary>
        PeekableContentProviderStream ContentProvider { get; }

        /// <summary>
        /// This method should not be called directly. It is used internally to set the binding between the content consumer and its associated content provider.
        /// </summary>
        /// <param name="contentProvider">The <see cref="PeekableContentProviderStream"/> to bind to.</param>
        void SetBinding(PeekableContentProviderStream contentProvider);

        /// <summary>
        /// This method should not be called directly. It is used internally to unset the binding between the content consumer and its associated content provider.
        /// </summary>
        void UnsetBinding();

        /// <summary>
        /// Called when new content is available from the associated content provider.
        /// </summary>
        void OnContent();

        /// <summary>
        /// Called when the connection is closed by the remote peer. It notifies the content consumer about the connection closure.
        /// </summary>
        void OnConnectionClosed();

        /// <summary>
        /// Called when an error occurs during content processing or connection handling. It provides the exception that caused the error.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> that represents the error condition.</param>
        void OnError(Exception ex);
    }
}
