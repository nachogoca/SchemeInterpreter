﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchemeInterpreter.Structures;

namespace SchemeInterpreter.SyntacticAnalysis
{
    class LL1
    {
        private readonly int[,] _table;
        private readonly Dictionary<Symbol, int> _terminalLookup;
        private readonly Dictionary<Symbol, int> _nonTerminalLookUp;
        private readonly Symbol _start;
        private readonly Grammar _coreGrammar;

        public LL1(Grammar g)
        {
            //Get core grammar
            _coreGrammar = g; //consider building new grammar object, not only stealing the pointer

            //Get all terminal symbols from Grammar
            var terminals = (g.Symbols.Where(s => s.IsTerminal())).ToArray();
            var nonTerminal = (g.Symbols.Where(s => s.IsNonTerminal())).ToArray();

            _terminalLookup = new Dictionary<Symbol, int>();
            _nonTerminalLookUp = new Dictionary<Symbol, int>();

            for (var i = 0; i < terminals.Length; i++)
                _terminalLookup.Add(terminals[i], i);
            _terminalLookup.Add(new Symbol(Symbol.SymTypes.EOS, "$"),terminals.Length);

            for (var i = 0; i < nonTerminal.Length;i++)
                _nonTerminalLookUp.Add(nonTerminal[i], i);

            _table = new int[terminals.Length,nonTerminal.Length];
            
            //Get start production (first head of production)
            _start = g.ProductionRules[0].Header;

            //Generate table
            //Ensure First  & follow sets
            g.GenerateFirstAndFollow();

            for (var i = 0; i < g.ProductionRules.Count; i++)
            {
                var focusFirst = g.GetFirstSet(i);

                if (focusFirst.Any(s => s.IsEpsilon()))
                {
                    var focusFollow = g.FollowSets[g.ProductionRules[i].Header];
                    foreach (var term in focusFollow)
                        _table[_terminalLookup[term], _nonTerminalLookUp[g.ProductionRules[i].Header]] = i+1;
                }
                
                foreach (var term in focusFirst)
                    _table[_terminalLookup[term], _nonTerminalLookUp[g.ProductionRules[i].Header]] = i+1;
            }
        }

        public bool Accept(string input)
        {
            var symStack = new Stack<Symbol>();
            var inputQueue = new Queue<Symbol>();

            symStack.Push(new Symbol(Symbol.SymTypes.EOS, "$"));
            symStack.Push(_start); //initialize eval stack

            foreach (var c in input)
                inputQueue.Enqueue(new Symbol(Symbol.SymTypes.Terminal, c.ToString())); 
            inputQueue.Enqueue(new Symbol(Symbol.SymTypes.EOS, "$")); //initialize input queue

            while (symStack.Count != 0 && inputQueue.Count != 0)
            {
                var stackSymbol = symStack.Pop();
                var inputSymbol = inputQueue.Peek();

                //check for collapsing conditions
                if (Equals(stackSymbol, inputSymbol))
                    inputQueue.Dequeue(); //remove symbol from input
                else if (stackSymbol.IsTerminal() && inputSymbol.IsTerminal() && !Equals(stackSymbol, inputSymbol))
                    return false; //symbols are terminal, but they do not match, reject string
                else
                {
                    var nextProductionId = _table[_terminalLookup[inputSymbol], _nonTerminalLookUp[stackSymbol]];
                    //check for undefined productions
                    if (nextProductionId == 0)
                        return false; //transition is not defined for given pair
                    var nextProduction = _coreGrammar.ProductionRules[nextProductionId-1];
                    //push body to stack
                    if(nextProduction.Body.First().IsTerminal())
                        continue;
                    foreach (var sym in nextProduction.Body)
                        symStack.Push(sym);
                }
            }
            return symStack.Count == 0 && symStack.Count == inputQueue.Count; //accept if empty stack and queue, reject otherwise
        }
    }
}
