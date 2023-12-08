namespace Best.HTTP.Hosts.Connections.HTTP1
{
    public static class Constants
    {
        public const byte CR = 13;
        public const byte LF = 10;

        public static readonly byte[] EOL = { Constants.CR, Constants.LF };

        public static readonly byte[] HeaderValueSeparator = { (byte)':' };

        // expect: 100-continue
        //public static readonly byte[] Expect100Continue = { (byte)'e', (byte)'x', (byte)'p', (byte)'e', (byte)'c', (byte)'t', (byte)':', (byte)' ', (byte)'1', (byte)'0', (byte)'0', (byte)'-', (byte)'c', (byte)'o', (byte)'n', (byte)'t', (byte)'i', (byte)'n', (byte)'u', (byte)'e', CR, LF };
    }
}
