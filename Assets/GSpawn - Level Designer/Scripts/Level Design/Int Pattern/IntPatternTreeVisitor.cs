#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public class IntPatternTreeVisitor :
        IIntPatternExpressionVisitor, IIntPatternStatementVisitor
    {
        private class IntSeqInfo
        {
            public List<int> values = new List<int>();
        }

        private Dictionary<string, IntSeqInfo>  _intSeqMap  = new Dictionary<string, IntSeqInfo>();
        private List<int>                       _pattern    = new List<int>();

        public void getPatternValues(List<int> patternValues)
        {
            patternValues.Clear();
            patternValues.AddRange(_pattern);
        }

        public void visitRoot(IntPatternRootStatement rootStmt)
        {
            _pattern.Clear();

            foreach (var child in rootStmt.statements)
                child.accept(this);
        }

        public void visitIntSeqDecl(IntPatternIntSeqDeclStatement seqDeclStmt)
        {
            _intSeqMap.Add(seqDeclStmt.seqName, new IntSeqInfo() { values = evalExpression(seqDeclStmt.initExpr) });
        }

        public void visitAdd(IntPatternAddStatement addStmt)
        {
            int numExpr = addStmt.numExpressions;
            for (int i = 0; i < numExpr; ++i)
                addStmt.expr(i).accept(this);
        }

        public void visitIntLiteral(IntPatternIntLiteralExpression intLiteralExpr)
        {
            _pattern.AddRange(evalExpression(intLiteralExpr));
        }

        public void visitIntLiteralSequence(IntPatternIntLiteralSequenceExpression intLiteralSeqExpr)
        {
            _pattern.AddRange(evalExpression(intLiteralSeqExpr));
        }

        public void visitRepeat(IntPatternRepeatExpression repeatExpr)
        {
            _pattern.AddRange(evalExpression(repeatExpr));
        }

        public void visitIsoTriangle(IntPatternIsoTriangleEpression isoTriangleExpr)
        {
            _pattern.AddRange(evalExpression(isoTriangleExpr));
        }

        public void visitSteps(IntPatternStepsExpression stepsExpr)
        {
            _pattern.AddRange(evalExpression(stepsExpr));
        }

        public void visitUnary(IntPatternUnaryExpression unaryExpr)
        {
            _pattern.AddRange(evalExpression(unaryExpr));
        }

        public void visitBinary(IntPatternBinaryExpression binaryExpr)
        {
            _pattern.AddRange(evalExpression(binaryExpr));
        }

        public void visitIntSeq(IntPatternIntSeqExpression intSeqExpr)
        {
            _pattern.AddRange(evalExpression(intSeqExpr));
        }

        private List<int> evalExpression(IntPatternExpression expr)
        {
            if (expr is IntPatternUnaryExpression)
            {
                var e = expr as IntPatternUnaryExpression;
                var values = evalExpression(e.expr);
                if (e.tokenId == IntPatternLexer.MINUS)
                {
                    for (int i = 0; i < values.Count; ++i)
                        values[i] = -values[i];
                }

                return values;
            }

            if (expr is IntPatternBinaryExpression)
            {
                var e               = expr as IntPatternBinaryExpression;
                List<int> leftVals  = evalExpression(e.left);
                List<int> rightVals = evalExpression(e.right);
                List<int> vals      = new List<int>();
                int sign            = e.tokenId == IntPatternLexer.PLUS ? 1 : -1;

                if (leftVals.Count == 1)
                {
                    foreach (var v in rightVals)
                        vals.Add(leftVals[0] + v * sign);
                }
                else
                if (rightVals.Count == 1)
                {
                    foreach (var v in leftVals)
                        vals.Add(v + sign * rightVals[0]);
                }
                else
                {
                    int minCount        = leftVals.Count;
                    int maxCount        = rightVals.Count;
                    List<int> minList   = leftVals;
                    List<int> maxList   = rightVals;

                    if (leftVals.Count > rightVals.Count)
                    {
                        minCount        = rightVals.Count;
                        maxCount        = leftVals.Count;
                        minList         = rightVals;
                        maxList         = leftVals;
                    }

                    for (int i = 0; i < minCount; ++i)
                        vals.Add(minList[i] + maxList[i]);

                    for (int i = minCount; i < maxCount; ++i)
                        vals.Add(maxList[i]);
                }

                return vals;
            }

            if (expr is IntPatternIntLiteralExpression)
            {
                var e = expr as IntPatternIntLiteralExpression;
                return new List<int>() { e.value };
            }

            if (expr is IntPatternIntLiteralSequenceExpression)
            {
                var e = expr as IntPatternIntLiteralSequenceExpression;
                return e.getValues();
            }
            if (expr is IntPatternRepeatExpression)
            {
                var e       = expr as IntPatternRepeatExpression;
                var values  = new List<int>();
                for (int i = 0; i < e.repeatCount; ++i)
                    values.AddRange(evalExpression(e.expr));

                return values;
            }
            if (expr is IntPatternIsoTriangleEpression)
            {
                var e = expr as IntPatternIsoTriangleEpression;
                return e.getValues();
            }

            if (expr is IntPatternStepsExpression)
            {
                var e = expr as IntPatternStepsExpression;
                return e.getValues();
            }

            if (expr is IntPatternIntSeqExpression)
            {
                var e           = expr as IntPatternIntSeqExpression;
                var intSeqInfo  = _intSeqMap[e.seqName];
                return new List<int>(intSeqInfo.values);
            }

            return null;
        }
    }
}
#endif