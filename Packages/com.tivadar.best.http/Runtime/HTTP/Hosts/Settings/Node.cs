using System.Collections.Generic;

namespace Best.HTTP.Hosts.Settings
{
    internal sealed class Node
    {
        public string key;
        public SortedList<string, Node> childNodes;
        public HostSettings hostSettings;

        public Node(string key) => this.key = key;
        public Node(string key, HostSettings settings) : this(key) => this.hostSettings = settings;

        public void Add(string subKey, Node subNode)
        {
            if (childNodes == null)
                childNodes = new SortedList<string, Node>(AsteriskStringComparer.Instance);

            childNodes.Add(subKey, subNode);
        }

        public void AddSetting(HostSettings settings)
        {
            this.hostSettings = settings;
        }

        public void Add(List<string> segments, HostSettings settings)
        {
            if (segments.Count == 0)
            {
                this.hostSettings = settings;
                return;
            }

            string subKey = segments[0];
            segments.RemoveAt(0);

            if (this.childNodes == null)
                this.childNodes = new SortedList<string, Node>(AsteriskStringComparer.Instance);

            if (!this.childNodes.TryGetValue(subKey, out var node))
                this.childNodes.Add(subKey, node = new Node(subKey, null));

            node.Add(segments, settings);
        }

        public HostSettings Find(List<string> segments)
        {
            if (segments.Count == 0)
                return this.hostSettings;

            if (this.childNodes == null || this.childNodes.Count == 0)
                return null;

            string subKey = segments[0];
            segments.RemoveAt(0);

            if (this.childNodes.TryGetValue(subKey, out var node))
                return node.Find(segments);

            return null;
        }
    }
}
