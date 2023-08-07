#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public interface IIntPatternExpressionVisitor
    {
        public void visitIntLiteralSequence (IntPatternIntLiteralSequenceExpression intLiteralSeqExpr);
        public void visitIntLiteral         (IntPatternIntLiteralExpression intLiteralExpr);
        public void visitRepeat             (IntPatternRepeatExpression repeatExpr);
        public void visitIsoTriangle        (IntPatternIsoTriangleEpression isoTriangleExpr);
        public void visitSteps              (IntPatternStepsExpression stepsExpr);
        public void visitUnary              (IntPatternUnaryExpression unaryExpr);
        public void visitBinary             (IntPatternBinaryExpression binaryExpr);
        public void visitIntSeq             (IntPatternIntSeqExpression intSeqExpr);
    }

    public abstract class IntPatternExpression
    {
        public abstract void accept(IIntPatternExpressionVisitor visitor);
    }

    public class IntPatternIntLiteralSequenceExpression : IntPatternExpression
    {
        private List<int>   _values     = new List<int>();

        public int          numValues   { get { return _values.Count; } }
       
        public void addValue(int val)
        {
            _values.Add(val);
        }

        public List<int> getValues()
        {
            return new List<int>(_values);
        }

        public override void accept(IIntPatternExpressionVisitor visitor)
        {
            visitor.visitIntLiteralSequence(this);
        }
    }

    public class IntPatternIntLiteralExpression : IntPatternExpression
    {
        private int _value;

        public int  value   { get { return _value; } }

        public IntPatternIntLiteralExpression(int val)
        {
            _value = val;
        }

        public override void accept(IIntPatternExpressionVisitor visitor)
        {
            visitor.visitIntLiteral(this);
        }
    }

    public class IntPatternRepeatExpression : IntPatternExpression
    {
        private int                     _repeatCount;
        private IntPatternExpression    _expr;

        public int                      repeatCount { get { return _repeatCount; } }
        public IntPatternExpression     expr        { get { return _expr; } }

        public IntPatternRepeatExpression(int repeatCount, IntPatternExpression expr)
        {
            _repeatCount = repeatCount;
            _expr = expr;
        }

        public override void accept(IIntPatternExpressionVisitor visitor)
        {
            visitor.visitRepeat(this);
        }
    }

    public class IntPatternIsoTriangleEpression : IntPatternExpression
    {
        private List<int>   _values     = new List<int>();
        private int         _height;

        public IntPatternIsoTriangleEpression(int height)
        {
            _height = height;
        }

        public override void accept(IIntPatternExpressionVisitor visitor)
        {
            visitor.visitIsoTriangle(this);
        }

        public List<int> getValues()
        {
            if (_values.Count == 0)
            {
                int incr = _height > 0 ? 1 : -1;
                for (int val = incr; val != _height; val += incr)
                    _values.Add(val);

                _values.Add(_height);

                int end = incr - incr;
                for (int val = _height - incr; val != end; val -= incr)
                    _values.Add(val);
            }

            return new List<int>(_values);
        }
    }

    public class IntPatternStepsExpression : IntPatternExpression
    {
        private List<int>   _values         = new List<int>();
        private int         _numSteps;
        private int         _stepLength;
        private int         _heightSign;

        public IntPatternStepsExpression(int numSteps, int stepLength, int heightSign)
        {
            _numSteps = numSteps;
            _stepLength = stepLength;
            _heightSign = heightSign;
        }

        public List<int> getValues()
        {
            if (_values.Count == 0)
            {
                int startHeight = 1 * _heightSign;
                for (int stepIndex = 0; stepIndex < _numSteps; ++stepIndex)
                {
                    int heightVal = startHeight + stepIndex * _heightSign;
                    for (int i = 0; i < _stepLength; ++i)
                        _values.Add(heightVal);
                }
            }

            return new List<int>(_values);
        }

        public override void accept(IIntPatternExpressionVisitor visitor)
        {
            visitor.visitSteps(this);
        }
    }

    public class IntPatternUnaryExpression : IntPatternExpression
    {
        private int                     _tokenId;
        private IntPatternExpression    _expr;

        public int                      tokenId { get { return _tokenId; } }
        public IntPatternExpression     expr    { get { return _expr; } }

        public IntPatternUnaryExpression(int tokenId, IntPatternExpression expr)
        {
            _tokenId = tokenId;
            _expr = expr;
        }

        public override void accept(IIntPatternExpressionVisitor visitor)
        {
            visitor.visitUnary(this);
        }
    }

    public class IntPatternBinaryExpression : IntPatternExpression
    {
        private IntPatternExpression    _left;
        private int                     _tokenId;
        private IntPatternExpression    _right;

        public IntPatternExpression     left    { get { return _left; } }
        public int                      tokenId { get { return _tokenId; } }
        public IntPatternExpression     right   { get { return _right; } }

        public IntPatternBinaryExpression(IntPatternExpression left, int tokenId, IntPatternExpression right)
        {
            _left = left;
            _tokenId = tokenId;
            _right = right;
        }

        public override void accept(IIntPatternExpressionVisitor visitor)
        {
            visitor.visitBinary(this);
        }
    }

    public class IntPatternIntSeqExpression : IntPatternExpression
    {
        private string _seqName = string.Empty;

        public string   seqName { get { return _seqName; } }

        public IntPatternIntSeqExpression(string seqName)
        {
            _seqName = seqName;
        }

        public override void accept(IIntPatternExpressionVisitor visitor)
        {
            visitor.visitIntSeq(this);
        }
    }
}
#endif