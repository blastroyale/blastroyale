using System;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.JSON.LitJson;

namespace Best.SignalR.Encoders
{
    public sealed class LitJsonEncoder : Best.SignalR.IEncoder
    {
        public LitJsonEncoder()
        {
            JsonMapper.RegisterImporter<int, long>((input) => input);
            JsonMapper.RegisterImporter<long, int>((input) => (int)input);
            JsonMapper.RegisterImporter<double, int>((input) => (int)(input + 0.5));
            JsonMapper.RegisterImporter<string, DateTime>((input) => Convert.ToDateTime((string)input).ToUniversalTime());
            JsonMapper.RegisterImporter<double, float>((input) => (float)input);
            JsonMapper.RegisterImporter<string, byte[]>((input) => Convert.FromBase64String(input));
            JsonMapper.RegisterExporter<float>((f, writer) => writer.Write((double)f));
        }

        public T DecodeAs<T>(BufferSegment buffer)
        {
            using (var reader = new System.IO.StreamReader(new System.IO.MemoryStream(buffer.Data, buffer.Offset, buffer.Count)))
            {
                return JsonMapper.ToObject<T>(reader);
            }
        }

        public BufferSegment Encode<T>(T value)
        {
            var json = JsonMapper.ToJson(value);
            int len = System.Text.Encoding.UTF8.GetByteCount(json);
            byte[] buffer = BufferPool.Get(len + 1, true);
            System.Text.Encoding.UTF8.GetBytes(json, 0, json.Length, buffer, 0);
            buffer[len] = (byte)JsonProtocol.Separator;
            return new BufferSegment(buffer, 0, len + 1);
        }

        public object ConvertTo(Type toType, object obj)
        {
            string json = JsonMapper.ToJson(obj);
            return JsonMapper.ToObject(toType, json);
        }
    }
}
