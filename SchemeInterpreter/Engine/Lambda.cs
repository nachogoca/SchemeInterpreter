﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SchemeInterpreter.Engine
{
    [Serializable]
    public class Lambda
    {
        public static Dictionary<string, Tuple<Stdlib.SchemeTypes, object>> Variables; //local scope
        private static List<Tuple<Stdlib.SchemeTypes, object>> _formals;
        private static List<Tuple<Stdlib.SchemeTypes, object>> _body;

        public Lambda(List<Tuple<Stdlib.SchemeTypes, object>> formals, List<Tuple<Stdlib.SchemeTypes, object>> body)
        {
            //build lambda function from application
            Variables = new Dictionary<string, Tuple<Stdlib.SchemeTypes, object>>(); //init local enviroment
            _formals = new List<Tuple<Stdlib.SchemeTypes, object>>(formals);
            _body = new List<Tuple<Stdlib.SchemeTypes, object>>(body);
        }

        public Tuple<Stdlib.SchemeTypes, object> Execute(List<Tuple<Stdlib.SchemeTypes, object>> args)
        {
            if(args.Count != _formals.Count)
                throw new Exception("Lambda expected: " + _formals.Count + " had: "+ args.Count);

            for (var i = 0; i < _formals.Count; i++)
                Variables[(string)_formals[i].Item2] = args[i]; //local binding

            //Clone _body and execute clone
            var clone = DeepClone(_body[0]);
            return _collapseApp(clone);
        }

        private static Tuple<Stdlib.SchemeTypes, object> _collapseApp(Tuple<Stdlib.SchemeTypes, object> tupleApp)
        {
            if (tupleApp.Item1 != Stdlib.SchemeTypes.Application)
                return tupleApp; //no need to collapse
            var app = tupleApp.Item2 as State;

            var args = app.Args as List<Tuple<Stdlib.SchemeTypes, object>>;

            for (var i = 0; i < args.Count; i++)
            {
                if (args[i].Item1 == Stdlib.SchemeTypes.Application)
                    args[i] = _collapseApp(args[i]);
                if (args[i].Item1 == Stdlib.SchemeTypes.Variable)
                    args[i] = _collapseApp(_collapseIdentifier(args[i]));
            }
            //Check if its a function or a Lambda
            if (app.Exec != null)
                return app.Exec(app.Args) as Tuple<Stdlib.SchemeTypes, object>;
            if (app.Lamb != null)
                throw new Exception("Lambdas within lambdas is not supported");
            throw new Exception("Lambda resolves to inexecutable expression: " + app);
        }

        private static Tuple<Stdlib.SchemeTypes, object> _collapseIdentifier(Tuple<Stdlib.SchemeTypes, object> tupleApp)
        {
            //Lookup on lambda enviroment
            if (Variables.ContainsKey((string) tupleApp.Item2))
                return Variables[(string)tupleApp.Item2];
            //
            if (Enviroment.variables.ContainsKey((string) tupleApp.Item2))
                return Enviroment.variables[(string) tupleApp.Item2];
            else
            {
                throw new Exception("Identifier: " + tupleApp.Item2 + " Could not be found");
            }
        }

        private static Tuple<Stdlib.SchemeTypes, object> DeepClone(Tuple<Stdlib.SchemeTypes, object> obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (Tuple<Stdlib.SchemeTypes, object>)formatter.Deserialize(ms);
            }
        }
    }
}
