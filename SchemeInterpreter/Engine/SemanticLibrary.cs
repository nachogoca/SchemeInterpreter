﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SchemeInterpreter.SyntacticAnalysis;

namespace SchemeInterpreter.Engine
{
    public static class SemanticLibrary
    {
        //Semantic Action library ::

        //AcRepeater -> Repeater action (retransmits state result_
        public static object AcRepeater(List<State> args)
        {
            return args[0].Result;
        }

        //AppendList -> * List
        public static object AcBuildRightList(List<State> args)
        {
            var node = args[0].Result as Tuple<Stdlib.SchemeTypes, object>; //catch new datum to add
            var list = args[1].Result as Tuple<Stdlib.SchemeTypes, object>;
            var listPointer = list.Item2 as List<Tuple<Stdlib.SchemeTypes, object>>;

            listPointer.Insert(0, node);

            return list; //return the list
        }
        //AppendList -> List *
        public static object AcBuildLeftList(List<State> args)
        {
            var node = args[1].Result as Tuple<Stdlib.SchemeTypes, object>; //catch new datum to add
            var list = args[0].Result as Tuple<Stdlib.SchemeTypes, List<Tuple<Stdlib.SchemeTypes, object>>>; //Catch the list variable
            list.Item2.Add(node); //Append the node to the list

            return list; //return the list
        }
        //Create Datum list
        public static object AcCreateList(List<State> args)
        {
            var newList = new Tuple<Stdlib.SchemeTypes, object>(Stdlib.SchemeTypes.List, new List<Tuple<Stdlib.SchemeTypes, object>>());
            return newList; //return the list
        }

        //AcReduceBoolean -> Reduces boolean to const
        public static object AcReduceBoolean(List<State> args)
        {
            var state = args[0];
            var primary = state.Primary as LR1Table.ExtendedSymbol;
            if(primary.Value == "true")
                return new Tuple<Stdlib.SchemeTypes, object>(Stdlib.SchemeTypes.Boolean, true);
            return new Tuple<Stdlib.SchemeTypes, object>(Stdlib.SchemeTypes.Boolean, false);
        }

        //AcReduceNumber -> Reduces Number to const
        public static object AcReduceNumber(List<State> args)
        {
            var state = args[0];
            var primary = state.Primary as LR1Table.ExtendedSymbol;
            var val = Convert.ToDouble(primary.Value);
            var result = new Tuple<Stdlib.SchemeTypes, object>(Stdlib.SchemeTypes.Number, val);
            return result;
        }

        //AcReduceString -> Reduces string to const
        public static object AcReduceString(List<State> args)
        {
            var state = args[0];
            var primary = state.Primary as LR1Table.ExtendedSymbol;
            var val = primary.Value;
            var result = new Tuple<Stdlib.SchemeTypes, object>(Stdlib.SchemeTypes.String, val);
            return result;
        }

        //Ac 'Datum to Expression
        public static object AcRepeaterIgnoreFirst(List<State> args)
        {
            return args[1].Result;
        }
        //Application
        public static object AcBuildApplication(List<State> args)
        {
            var function = args[1].Result as Tuple<Stdlib.SchemeTypes, object>;
            var stateArgs = args[2].Result as Tuple<Stdlib.SchemeTypes, object>;
            var funcArgs = stateArgs.Item2 as List<Tuple<Stdlib.SchemeTypes, object>>;

            Func<object, object> funcPtr;
            try
            {
                funcPtr = Enviroment.functions[(string) function.Item2]; //get function to execute
            }
            catch (Exception)
            {
                throw new Exception("Function: "+ function.Item2 + "is not defined");
            }

            //return (funcPtr(funcArgs) as Tuple<Stdlib.SchemeTypes, object>);
            //build new Application

            var app = new State(args[0].StateId);
            app.Exec = funcPtr;
            app.Args = funcArgs;

            return new Tuple<Stdlib.SchemeTypes, object>(Stdlib.SchemeTypes.Application, app);
        }

        //Build variable from identifier
        public static object AcBuildVariableFromId(List<State> args)
        {
            var primary = args[0].Primary as LR1Table.ExtendedSymbol;
            return new Tuple<Stdlib.SchemeTypes, object>(Stdlib.SchemeTypes.Variable, primary.Value);
        }

        //Lookup variable in Enviroment
        public static object AcLookupSym(List<State> args)
        {
            var id = args[0].Result as Tuple<Stdlib.SchemeTypes, object>;
            Tuple<Stdlib.SchemeTypes, object> variable;

            try
            {
                variable = Enviroment.variables[(string) id.Item2];
            }
            catch (Exception)
            {
                throw new Exception("Symbol not found exception: "+ id.Item2);
            }
            //build result
            return variable;
        }

        //Define variable in Enviroment
        public static object AcDefineVar(List<State> args)
        {
            
            var id = args[2].Result as Tuple<Stdlib.SchemeTypes, object>;
            var value = args[3].Result as Tuple<Stdlib.SchemeTypes, object>;

            //Simple definition, bind value to id
            if (value.Item1 != Stdlib.SchemeTypes.Function)
            {
                if (Enviroment.variables.Keys.Contains((string)id.Item2))
                    Enviroment.variables[(string)id.Item2] = new Tuple<Stdlib.SchemeTypes, object>(value.Item1, value.Item2);
                else
                    Enviroment.variables.Add((string)id.Item2, new Tuple<Stdlib.SchemeTypes, object>(value.Item1, value.Item2));
            }

            //Complex definition, bind function to id

            return value; //return assigned value
        }
        //Handle if statement
        public static object AcHandleIf(List<State> args)
        {
            var cond = args[2].Result as Tuple<Stdlib.SchemeTypes, object>;
            if (cond.Item1 == Stdlib.SchemeTypes.Application)
                cond = _collapseApp(cond);

            var exp1 = args[3].Result as Tuple<Stdlib.SchemeTypes, object>;
            var exp2 = args[4].Result as Tuple<Stdlib.SchemeTypes, object>;

            if (cond.Item1 != Stdlib.SchemeTypes.Boolean)
                return exp1;
            if ((bool)cond.Item2)
                return exp1;
            else
                return exp2;
        }

        //Collapse Expression
        public static object AcCollapseExp(List<State> args)
        {
            //check if its application
            var headerApp = args[0].Result as Tuple<Stdlib.SchemeTypes, object>;
            if (headerApp.Item1 != Stdlib.SchemeTypes.Application)
                return headerApp; //Repeater

            var result = _collapseApp(headerApp);
            return result;
        }

        private static Tuple<Stdlib.SchemeTypes, object> _collapseApp(Tuple<Stdlib.SchemeTypes, object> tupleApp)
        {
            var app = tupleApp.Item2 as State; 
            var args = app.Args as List<Tuple<Stdlib.SchemeTypes, object>>;

            for (var i = 0; i < args.Count; i++)
            {
                if (args[i].Item1 == Stdlib.SchemeTypes.Application)
                    args[i] = _collapseApp(args[i]);
            }
            return app.Exec(app.Args) as Tuple<Stdlib.SchemeTypes, object>;
        }

        //Build lambda function [Warning, here there be dragons]
        public static object AcBuildLambda(List<State> args)
        {
            //Build functions
            return null;
        }
    }
}
