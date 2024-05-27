#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public interface IIntPatternStatementVisitor
    {
        public void visitRoot           (IntPatternRootStatement rootStmt);
        public void visitAdd            (IntPatternAddStatement addStmt);
        public void visitIntSeqDecl     (IntPatternIntSeqDeclStatement seqDeclStmt);
    }

    public abstract class IntPatternStatement
    {
        public abstract void accept(IIntPatternStatementVisitor visitor);
    }

    public class IntPatternRootStatement : IntPatternStatement
    {
        public List<IntPatternStatement> statements = new List<IntPatternStatement>();

        public override void accept(IIntPatternStatementVisitor visitor)
        {
            visitor.visitRoot(this);
        }
    }

    public class IntPatternIntSeqDeclStatement : IntPatternStatement
    {
        private string                  _seqName = string.Empty;
        private IntPatternExpression    _initExpr;

        public string                   seqName     { get { return _seqName; } }
        public IntPatternExpression     initExpr    { get { return _initExpr; } }

        public IntPatternIntSeqDeclStatement(string seqName, IntPatternExpression initExpr)
        {
            _seqName = seqName;
            _initExpr = initExpr;
        }

        public override void accept(IIntPatternStatementVisitor visitor)
        {
            visitor.visitIntSeqDecl(this);
        }
    }

    public class IntPatternAddStatement : IntPatternStatement
    {
        private List<IntPatternExpression>  _expressions    = new List<IntPatternExpression>();

        public int                          numExpressions  { get { return _expressions.Count; } }

        public IntPatternExpression expr(int index)
        {
            return _expressions[index];
        }

        public void addExpr(IntPatternExpression expr)
        {
            _expressions.Add(expr);
        }

        public override void accept(IIntPatternStatementVisitor visitor)
        {
            visitor.visitAdd(this);
        }
    }
}
#endif