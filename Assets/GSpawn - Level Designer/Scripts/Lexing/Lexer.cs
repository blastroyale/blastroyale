#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public struct LexerToken
    {
        public LexerToken(int tokenId, string tokenLexeme, int tokenLine)
        {
            id = tokenId;
            lexeme = tokenLexeme;
            line = tokenLine;
        }

        public int      id;
        public string   lexeme;
        public int      line;
    }

    public abstract class Lexer
    {
        private string              _stream;
        private int                 _streamLength;
        private int                 _cursorPos;
        private int                 _line;
        private List<string>        _errors         = new List<string>();
        private List<LexerToken>    _tokens         = new List<LexerToken>();

        public char                 eof             { get { return '\0'; } }
        public char                 newLine         { get { return '\n'; } }
        public int                  line            { get { return _line; } }
        public int                  numTokens       { get { return _tokens.Count; } }
        public int                  cursorPos       { get { return _cursorPos; } }
        public int                  streamLength    { get { return _streamLength; } }
        public bool                 reachedEnd      { get { return _cursorPos >= _streamLength; } }

        public bool lex(string stream)
        {
            if (string.IsNullOrEmpty(stream))
            {
                Debug.LogError("Invalid character stream.");
                return false;
            }

            _stream         = stream;
            _streamLength   = _stream.Length;
            _cursorPos      = 0;
            _line           = 0;
            _errors.Clear();
            _tokens.Clear();

            lexImpl();

            if (_errors.Count != 0)
            {
                foreach (var err in _errors)
                    Debug.LogError(err);

                return false;
            }

            return true;
        }
        
        public LexerToken getToken(int index)
        {
            if (index >= 0 && index < _tokens.Count) return _tokens[index];
            return new LexerToken(getEofId(), eof.ToString(), -1);
        }

        public int getTokenId(int index)
        {
            if (index >= 0 && index < _tokens.Count) return _tokens[index].id;
            return getEofId();
        }

        public string getTokenLexeme(int index)
        {
            if (index >= 0 && index < _tokens.Count) return _tokens[index].lexeme;
            return eof.ToString();
        }

        public abstract int getEofId();

        protected abstract void lexImpl();
        protected abstract bool getKeywordId(string lexeme, out int id);
        protected abstract int getIdentifierId();
        protected abstract int getIntegerLiteralId();

        protected void addToken(int id, string lexeme)
        {
            _tokens.Add(new LexerToken(id, lexeme, _line));
        }

        protected void addNewLine()
        {
            ++_line;
        }

        protected char peek()
        {
            if (reachedEnd) return eof;
            return _stream[_cursorPos];
        }

        protected char peekNext()
        {
            if ((_cursorPos + 1) >= _streamLength) return eof;
            return _stream[_cursorPos + 1];
        }

        protected char peek(int offset)
        {
            int index = _cursorPos + offset;
            if (index < 0 || index >= _streamLength) return eof;

            return _stream[index];
        }

        protected char advance()
        {
            if (reachedEnd) return eof;
            return _stream[_cursorPos++];
        }

        protected char retreat()
        {
            char c = peek();
            if (_cursorPos != 0) --_cursorPos;
            return c;
        }

        protected char advance(int numChars)
        {
            char c = eof;
            for (int i = 0; i < numChars; ++i)
                c = advance();

            return c;
        }

        protected bool match(char c)
        {
            if (peek() != c) return false;

            advance();
            return true;
        }

        protected bool matchAny(params char[] chars)
        {
            foreach(var c in chars)
            {
                if (peek() == c)
                {
                    advance();
                    return true;
                }
            }

            return false;
        }

        protected bool matchAll(params char[] chars)
        {
            int numChars = chars.Length;
            for (int i = 0; i < numChars; ++i)
            {
                if (peek(i) != chars[i]) return false;
            }

            advance(numChars);
            return true;
        }

        protected bool isDigit(char c)
        {
            return char.IsDigit(c);
        }

        protected bool isLetter(char c)
        {
            return char.IsLetter(c);
        }

        protected bool beginsIdentifier(char c)
        {
            return c == '_' || isLetter(c);
        }

        protected bool isIdentifierChar(char c)
        {
            return isLetter(c) || c == '_' || isDigit(c);
        }

        protected bool singleLineCStyleComment()
        {
            if (!matchAll('/', '/'))
            {
                error("Invalid single line comment.");
                return false;
            }

            while (peek() != newLine && peek() != eof)
                advance();

            return true;
        }

        protected bool multiLineCStyleComment()
        {
            if (!matchAll('/', '*'))
            {
                error("Invalid multi-line comment.");
                return false;
            }

            while (peek() != '*' && peekNext() != '/' && peek() != eof)
                advance();

            if (!matchAll('*', '/'))
            {
                error("Invalid multi-line comment");
                return false;
            }

            return true;
        }

        protected bool integerLiterlal()
        {
            string intLiteral = string.Empty;

            while (isDigit(peek()))
            {
                intLiteral += peek();
                advance();
            }

            try { int.Parse(intLiteral); }
            catch
            {
                error("Invalid integer literal.");
                return false;
            }

            addToken(getIntegerLiteralId(), intLiteral);
            return true;
        }

        protected bool identifier()
        {
            if (!beginsIdentifier(peek()))
            {
                error("Invalid identifier.");
                return false;
            }

            string ident = string.Empty;
            while (isIdentifierChar(peek()))
            {
                ident += peek();
                advance();
            }

            int id;
            if (getKeywordId(ident, out id)) addToken(id, ident);
            else addToken(getIdentifierId(), ident);

            return true;
        }

        protected void invalidCharError(char c)
        {
            error("Invalid char '" + c + "'.");
        }

        protected void error(string msg)
        {
            _errors.Add("[" + line + "] Error: " + msg);
        }
    }
}
#endif