#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public abstract class Parser
    {
        private Lexer           _lexer;
        private int             _cursorPos;
        private List<string>    _errors         = new List<string>();

        public bool             reachedEnd      { get { return _cursorPos >= _lexer.numTokens - 1; } }

        public bool parse(Lexer lexer)
        {
            if (lexer.numTokens == 0)
            {
                Debug.LogError("Invalid token stream.");
                return false;
            }

            _lexer = lexer;
            parseImpl(lexer);

            if (_errors.Count != 0)
            {
                foreach (var err in _errors)
                    Debug.LogError(err);

                return false;
            }

            return true;
        }

        protected abstract void parseImpl(Lexer lexer);
        protected abstract void syncOnError();

        protected LexerToken peek()
        {
            return _lexer.getToken(_cursorPos);
        }

        protected int peekId()
        {
            return _lexer.getTokenId(_cursorPos);
        }

        protected string peekLexeme()
        {
            return _lexer.getTokenLexeme(_cursorPos);
        }

        protected LexerToken peekNext()
        {
            return _lexer.getToken(_cursorPos + 1);
        }

        protected int peekNextId()
        {
            return _lexer.getTokenId(_cursorPos + 1);
        }

        protected LexerToken peek(int offset)
        {
            return _lexer.getToken(_cursorPos + offset);
        }

        protected int peekId(int offset)
        {
            return _lexer.getTokenId(_cursorPos + offset);
        }

        protected LexerToken peekPrevious()
        {
            return _lexer.getToken(_cursorPos - 1);
        }

        protected int peekPreviousId()
        {
            return _lexer.getTokenId(_cursorPos - 1);
        }

        protected int advance()
        {
            if (reachedEnd) return _lexer.getEofId();
            return _lexer.getTokenId(_cursorPos++);
        }

        protected int advance(int numTokens)
        {
            int id = _lexer.getEofId();
            for (int i = 0; i < numTokens; ++i)
                id = advance();

            return id;
        }

        protected bool match(int id)
        {
            if (peekId() != id) return false;

            advance();
            return true;
        }

        protected bool matchExpect(int id, string errMsg)
        {
            if (peekId() != id)
            {
                reportErrorAndSync(errMsg, peek().line);
                syncOnError();
                return false;
            }

            advance();
            return true;
        }

        protected bool checkExpectAny(string errMsg, params int[] ids)
        {
            foreach (var id in ids)
            {
                if (peekId() == id) return true;
            }

            reportErrorAndSync(errMsg, peek().line);
            syncOnError();
            return false;
        }

        protected bool matchAny(params int[] ids)
        {
            foreach (var id in ids)
            {
                if (peekId() == id)
                {
                    advance();
                    return true;
                }
            }

            return false;
        }

        protected bool matchAll(params int[] ids)
        {
            int numIds = ids.Length;
            for (int i = 0; i < numIds; ++i)
            {
                if (peekId(i) != ids[i]) return false;
            }

            advance(numIds);
            return true;
        }

        protected void reportErrorAndSync(string msg, int line)
        {
            _errors.Add("[" + line + "] Error: " + msg);
            syncOnError();
        }

        protected string expectedIntLiteralMsg()
        {
            return "Expected int literal.";
        }

        protected string expectedPositiveIntLiteralMsg()
        {
            return "Expected positive int literal.";
        }
    }
}
#endif