#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public class IntPatternLexer : Lexer
    {
        public const int EOF            = 0;
        public const int LEFT_PAREN     = EOF + 1;
        public const int RIGHT_PAREN    = LEFT_PAREN + 1;
        public const int LEFT_CURLY     = RIGHT_PAREN + 1;
        public const int RIGHT_CURLY    = LEFT_CURLY + 1;
        public const int COMMA          = RIGHT_CURLY + 1;
        public const int SEMI_COLON     = COMMA + 1;
        public const int MINUS          = SEMI_COLON + 1;
        public const int PLUS           = MINUS + 1;
        public const int EQUALS         = PLUS + 1;

        public const int INT_LITERAL    = EQUALS + 1;
        public const int TRUE           = INT_LITERAL + 1;
        public const int FALSE          = TRUE + 1;
        public const int IDENTIFIER     = FALSE + 1;

        public const int ADD            = IDENTIFIER + 1;
        public const int REPEAT         = ADD + 1;
        public const int ISO_TRIANGLE   = REPEAT + 1;
        public const int STEPS          = ISO_TRIANGLE + 1;
        public const int INT_SEQ        = STEPS + 1;

        public readonly static Dictionary<string, int> _keywords = new Dictionary<string, int>()
        {
            { "true",           TRUE },
            { "false",          FALSE },

            { "add",            ADD },
            //{ "repeat",         REPEAT },
            { "isoTriangle",    ISO_TRIANGLE },
            { "steps",          STEPS },
            //{ "IntSeq",         INT_SEQ },
        };

        public override int getEofId()
        {
            return EOF;
        }

        protected override void lexImpl()
        {
            while (!reachedEnd)
            {
                char c = advance();
                switch (c)
                {
                    case '(': addToken(LEFT_PAREN, "("); break;
                    case ')': addToken(RIGHT_PAREN, ")"); break;
                    case '{': addToken(LEFT_CURLY, "{"); break;
                    case '}': addToken(RIGHT_CURLY, "}"); break;
                    case ';': addToken(SEMI_COLON, ";"); break;
                    case '-': addToken(MINUS, "-"); break;
                    case ',': addToken(COMMA, ","); break;
                    case '=': addToken(EQUALS, "="); break;
                    case '+': addToken(PLUS, "+"); break;

                    case '\n':

                        addNewLine();
                        break;

                    case ' ':
                    case '\r':
                    case '\t':

                        break;

                    default:

                        // Note: We need to rollback to the previous character before processing
                        //       more complicated character sequences.
                        retreat();

                        if (c == '/')
                        {
                            if (peekNext() == '/') { singleLineCStyleComment(); break; }
                            else if (peekNext() == '*') { multiLineCStyleComment(); break; }
                        }
                        else
                        if (isDigit(c))
                        {
                            integerLiterlal();
                            break;
                        }
                        else if (beginsIdentifier(c))
                        {
                            identifier();
                            break;
                        }

                        invalidCharError(c);
                        advance();
                        break;
                }
            }

            addToken(EOF, eof.ToString());
        }

        protected override int getIdentifierId()
        {
            return IDENTIFIER;
        }

        protected override bool getKeywordId(string lexeme, out int id)
        {
            if (_keywords.TryGetValue(lexeme, out id)) return true;
            return false;
        }

        protected override int getIntegerLiteralId()
        {
            return INT_LITERAL;
        }
    }
}
#endif
