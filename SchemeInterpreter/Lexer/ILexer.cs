﻿using System;
using System.Collections.Generic;

namespace SchemeInterpreter.LexerEngine
{
    //Implementation by Drew Miller
    public interface ILexer
    {
        void AddDefinition(TokenDefinition tokenDefinition);
        IEnumerable<Token> Tokenize(string source);
    }
}
