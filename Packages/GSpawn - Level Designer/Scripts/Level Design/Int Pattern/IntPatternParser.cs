#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public class IntPatternParser : Parser
    {
        private IntPatternRootStatement     _rootStatement;
        private IntPatternLexer             _patternLexer;

        private HashSet<string>             _intSeqences    = new HashSet<string>();

        public IntPatternRootStatement      rootStatement   { get { return _rootStatement; } }

        protected override void parseImpl(Lexer lexer)
        {
            _patternLexer = lexer as IntPatternLexer;
            if (_patternLexer == null) return;

            _intSeqences.Clear();
            _rootStatement = new IntPatternRootStatement();
            while (!reachedEnd)
            {
                var stmt = statement();
                if (stmt != null) _rootStatement.statements.Add(stmt);
            }
        }

        protected override void syncOnError()
        {
            // Skip the token that generated the error.
            advance();

            // Now keep skipping until we find a good place to sit
            while (!reachedEnd)
            {
                if (peekPreviousId() == IntPatternLexer.SEMI_COLON) return;

                switch (peekId())
                {
                    case IntPatternLexer.ADD:

                        return;
                }

                advance();
            }
        }

        private IntPatternStatement statement()
        {
            if (peekId() == IntPatternLexer.ADD) return addStatement();
            if (peekId() == IntPatternLexer.INT_SEQ) return intSeqDeclaration();
            else
            {
                reportErrorAndSync("Invalid statement.", peek().line);
                return null;
            }
        }

        private IntPatternAddStatement addStatement()
        {
            if (!matchExpect(IntPatternLexer.ADD, "Expected 'add'.")) return null;
            if (!matchExpect(IntPatternLexer.LEFT_PAREN, "Expected '(' after 'add'.")) return null;

            var addStmt = new IntPatternAddStatement();
            while (peekId() != IntPatternLexer.RIGHT_PAREN && !reachedEnd)
            {
                var expr = term();
                if (expr == null) return null;

                if (peekId() != IntPatternLexer.RIGHT_PAREN)
                {
                    if (!matchExpect(IntPatternLexer.COMMA, "Expected ','.")) return null;
                }

                addStmt.addExpr(expr);
            }

            if (!matchExpect(IntPatternLexer.RIGHT_PAREN, "'add' statement must close with ')'.")) return null;
            if (!matchExpect(IntPatternLexer.SEMI_COLON, "Expected ';' after 'add' statement.")) return null;

            return addStmt;
        }

        private IntPatternIntSeqDeclStatement intSeqDeclaration()
        {
            if (!matchExpect(IntPatternLexer.INT_SEQ, "Expected 'IntSeq'.")) return null;
            if (!matchExpect(IntPatternLexer.IDENTIFIER, "Expected identifier name (i.e. name of int sequence being declared).")) return null;

            string seqName = peekPrevious().lexeme;
            if (_intSeqences.Contains(seqName))
            {
                reportErrorAndSync("Int sequence '" + seqName + "' already declared.", peekPrevious().line);
                return null;
            }

            if (!matchExpect(IntPatternLexer.EQUALS, "Expected '=' after int sequence name.")) return null;

            var initExpr = term();
            if (initExpr == null) return null;

            if (!matchExpect(IntPatternLexer.SEMI_COLON, "Expected ';' after int sequence declaration statement.")) return null;

            _intSeqences.Add(seqName);
            return new IntPatternIntSeqDeclStatement(seqName, initExpr);
        }

        private IntPatternExpression primary()
        {
            switch (peekId())
            {
                case IntPatternLexer.REPEAT:

                    return repeat();

                case IntPatternLexer.ISO_TRIANGLE:

                    return isoTriangle();

                case IntPatternLexer.STEPS:

                    return steps();

                case IntPatternLexer.IDENTIFIER:

                    string seqName = peek().lexeme;
                    if (!_intSeqences.Contains(seqName))
                    {
                        reportErrorAndSync("Undeclared int sequence '" + seqName + "'.", peek().line);
                        return null;
                    }

                    advance();
                    return new IntPatternIntSeqExpression(seqName);

                case IntPatternLexer.LEFT_CURLY:

                    return intLiteralSequence();

                default:

                    return intLiteral();
            }
        }

        private IntPatternRepeatExpression repeat()
        {
            if (!matchExpect(IntPatternLexer.REPEAT, "Expected 'repeat'.")) return null;
            if (!matchExpect(IntPatternLexer.LEFT_PAREN, "Expected '(' after 'repeat'.")) return null;

            var expr = term();
            if (expr == null) return null;

            if (!matchExpect(IntPatternLexer.COMMA, "Expected ',' after first repeat argument.")) return null;

            if (!matchExpect(IntPatternLexer.INT_LITERAL, expectedPositiveIntLiteralMsg())) return null;
            int repCount = int.Parse(peekPrevious().lexeme);
            if (repCount == 0)
            {
                reportErrorAndSync("Second argument to 'repeat' (i.e. repeat count) must be > 0.", peekPrevious().line);
                return null;
            }
       
            if (!matchExpect(IntPatternLexer.RIGHT_PAREN, "'repeat' expression must close with ')'.")) return null;
            return new IntPatternRepeatExpression(repCount, expr);
        }

        private IntPatternIsoTriangleEpression isoTriangle()
        {
            if (!matchExpect(IntPatternLexer.ISO_TRIANGLE, "Expected 'isoTriangle'.")) return null;
            if (!matchExpect(IntPatternLexer.LEFT_PAREN, "Expected '(' after 'isoTriangle'.")) return null;

            var triHeightExpr = signedIntLiteral();
            if (triHeightExpr == null) return null;

            if (triHeightExpr.value > -2 && triHeightExpr.value < 2)
            {
                reportErrorAndSync("Triangle height must be >= 2 OR <= -2.", peekPrevious().line);
                return null;
            }

            if (!matchExpect(IntPatternLexer.RIGHT_PAREN, "'isoTriangle' expression must close with ')'.")) return null;
            return new IntPatternIsoTriangleEpression(triHeightExpr.value);
        }

        private IntPatternStepsExpression steps()
        {
            if (!matchExpect(IntPatternLexer.STEPS, "Expected 'steps'.")) return null;
            if (!matchExpect(IntPatternLexer.LEFT_PAREN, "Expected '(' after 'steps'.")) return null;

            var numStepsExpr = signedIntLiteral();
            if (numStepsExpr == null) return null;
            if (numStepsExpr.value < 1)
            {
                reportErrorAndSync("The number of steps must be >= 1.", peekPrevious().line);
                return null;
            }
            if (!matchExpect(IntPatternLexer.COMMA, "Expected ',' after first step argument.")) return null;

            var stepLengthExpr = signedIntLiteral();
            if (stepLengthExpr == null) return null;
            if (stepLengthExpr.value < 1)
            {
                reportErrorAndSync("The step length must be >= 1.", peekPrevious().line);
                return null;
            }
            if (!matchExpect(IntPatternLexer.COMMA, "Expected ',' after second step argument.")) return null;

            if (!checkExpectAny("Expected true/false for last argument in 'steps'.", IntPatternLexer.TRUE, IntPatternLexer.FALSE)) return null;
            int heightSign = peekId() == IntPatternLexer.TRUE ? -1 : 1;
            advance();

            if (!matchExpect(IntPatternLexer.RIGHT_PAREN, "'steps' expression must close with ')'.")) return null;
            return new IntPatternStepsExpression(numStepsExpr.value, stepLengthExpr.value, heightSign);
        }

        private IntPatternExpression term()
        {
            var expr = unary();
            if (expr == null) return null;

            while (matchAny(IntPatternLexer.PLUS, IntPatternLexer.MINUS))
            {
                int opId = peekPreviousId();

                var right = unary();
                if (right == null) return null;

                expr = new IntPatternBinaryExpression(expr, opId, right);
            }

            return expr;
        }

        private IntPatternExpression unary()
        {
            if (match(IntPatternLexer.MINUS))
            {
                var prevToken = peekPrevious();
                var expr = primary();
                if (expr == null) return null;
                return new IntPatternUnaryExpression(prevToken.id, expr);
            }

            return primary();
        }

        private IntPatternIntLiteralSequenceExpression intLiteralSequence()
        {
            int seqLine = peek().line;
            if (!matchExpect(IntPatternLexer.LEFT_CURLY, "Expected '{' at the beginning of the integer sequence.")) return null;

            var seqExpr = new IntPatternIntLiteralSequenceExpression();
            while (peekId() != IntPatternLexer.RIGHT_CURLY && !reachedEnd)
            {
                var intExpr = signedIntLiteral();
                if (intExpr == null) return null;
                seqExpr.addValue(intExpr.value);

                if (peekId() != IntPatternLexer.RIGHT_CURLY)
                {
                    if (!matchExpect(IntPatternLexer.COMMA, "Expected ','.")) return null;
                }
            }

            if (seqExpr.numValues == 0)
            {
                reportErrorAndSync("Empty integer sequences are not allowed. An integer sequence must contain at least one value.", seqLine);
                return null;
            }

            if (!matchExpect(IntPatternLexer.RIGHT_CURLY, "Expected '}' at the end of the integer sequence.")) return null;
            return seqExpr;
        }

        private IntPatternIntLiteralExpression intLiteral()
        {
            if (!matchExpect(IntPatternLexer.INT_LITERAL, expectedIntLiteralMsg())) return null;
            return new IntPatternIntLiteralExpression(int.Parse(peekPrevious().lexeme));
        }

        private IntPatternIntLiteralExpression signedIntLiteral()
        {
            int sign = 1;
            if (peekId() == IntPatternLexer.MINUS)
            {
                sign = -1;
                advance();
            }

            if (!matchExpect(IntPatternLexer.INT_LITERAL, expectedIntLiteralMsg())) return null;
            return new IntPatternIntLiteralExpression(sign * int.Parse(peekPrevious().lexeme));
        }
    }
}
#endif