using System;
using System.IO;

using Best.HTTP.Shared.Databases.Utils;

namespace Best.HTTP.Shared.Databases
{
    public abstract class Metadata
    {
        public int Index;
        public int FilePosition;
        public int Length;

        public bool IsDeleted => this.FilePosition == -1 && this.Length == -1;

        public void MarkForDelete()
        {
            this.FilePosition = -1;
            this.Length = -1;
        }

        public virtual void SaveTo(Stream stream)
        {
            if (this.IsDeleted)
                throw new Exception($"Trying to save a deleted metadata({this.ToString()})!");

            stream.EncodeUnsignedVariableByteInteger((uint)this.FilePosition);
            stream.EncodeUnsignedVariableByteInteger((uint)this.Length);
        }

        public virtual void LoadFrom(Stream stream)
        {
            this.FilePosition = (int)stream.DecodeUnsignedVariableByteInteger();
            this.Length = (int)stream.DecodeUnsignedVariableByteInteger();
        }

        public override string ToString()
        {
            return $"[Metadata Idx: {Index}, Pos: {FilePosition}, Length: {Length}, IsDeleted: {IsDeleted}]";
        }
    }
}
