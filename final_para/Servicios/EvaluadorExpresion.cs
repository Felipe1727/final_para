using System.Globalization;
using System.Text;

namespace final_para.Servicios;

// Evaluador de expresiones matemáticas en notación infija.
// Implementa un parser por descenso recursivo sin dependencias externas.
//
// Gramática soportada:
//   Expr   := Termino (('+' | '-') Termino)*
//   Termino := Factor (('*' | '/') Factor)*
//   Factor := Unario ('^' Factor)?              // potencia asociativa por la derecha
//   Unario := ('+' | '-') Unario | Primario
//   Primario := numero | identificador | identificador '(' Expr ')' | '(' Expr ')'
//
// Identificadores soportados:
//   - Funciones: sin, cos, tan, exp, log, sqrt, abs
//   - Constantes: pi, e
//   - Variables: cualquier otro identificador (resuelto contra `vars`)
public static class EvaluadorExpresion
{
    public static double Evaluar(string expr, IReadOnlyDictionary<string, double> vars)
    {
        if (string.IsNullOrWhiteSpace(expr))
            throw new FormatException("Expresión inválida: cadena vacía.");

        var parser = new Parser(expr, vars);
        var resultado = parser.ParsearExpresion();
        parser.AsegurarFin();
        return resultado;
    }

    private sealed class Parser
    {
        private readonly string _expr;
        private readonly IReadOnlyDictionary<string, double> _vars;
        private int _pos;

        public Parser(string expr, IReadOnlyDictionary<string, double> vars)
        {
            _expr = expr;
            _vars = vars;
            _pos = 0;
        }

        public void AsegurarFin()
        {
            SaltarEspacios();
            if (_pos != _expr.Length)
                throw new FormatException($"Expresión inválida: token inesperado en posición {_pos} ('{_expr[_pos]}').");
        }

        public double ParsearExpresion()
        {
            var izq = ParsearTermino();
            while (true)
            {
                SaltarEspacios();
                if (Coincide('+'))
                {
                    _pos++;
                    izq += ParsearTermino();
                }
                else if (Coincide('-'))
                {
                    _pos++;
                    izq -= ParsearTermino();
                }
                else break;
            }
            return izq;
        }

        private double ParsearTermino()
        {
            var izq = ParsearFactor();
            while (true)
            {
                SaltarEspacios();
                if (Coincide('*'))
                {
                    _pos++;
                    izq *= ParsearFactor();
                }
                else if (Coincide('/'))
                {
                    _pos++;
                    izq /= ParsearFactor();
                }
                else break;
            }
            return izq;
        }

        private double ParsearFactor()
        {
            var izq = ParsearUnario();
            SaltarEspacios();
            if (Coincide('^'))
            {
                _pos++;
                var der = ParsearFactor();
                izq = Math.Pow(izq, der);
            }
            return izq;
        }

        private double ParsearUnario()
        {
            SaltarEspacios();
            if (Coincide('+'))
            {
                _pos++;
                return ParsearUnario();
            }
            if (Coincide('-'))
            {
                _pos++;
                return -ParsearUnario();
            }
            return ParsearPrimario();
        }

        private double ParsearPrimario()
        {
            SaltarEspacios();
            if (_pos >= _expr.Length)
                throw new FormatException("Expresión inválida: fin inesperado.");

            var c = _expr[_pos];

            if (c == '(')
            {
                _pos++;
                var valor = ParsearExpresion();
                SaltarEspacios();
                if (!Coincide(')'))
                    throw new FormatException($"Expresión inválida: falta ')' en posición {_pos}.");
                _pos++;
                return valor;
            }

            if (char.IsDigit(c) || c == '.')
                return ParsearNumero();

            if (char.IsLetter(c) || c == '_')
                return ParsearIdentificador();

            throw new FormatException($"Expresión inválida: carácter inesperado '{c}' en posición {_pos}.");
        }

        private double ParsearNumero()
        {
            var inicio = _pos;
            while (_pos < _expr.Length && (char.IsDigit(_expr[_pos]) || _expr[_pos] == '.'))
                _pos++;

            // Soporte para notación científica (1e-3, 2E5)
            if (_pos < _expr.Length && (_expr[_pos] == 'e' || _expr[_pos] == 'E'))
            {
                // Cuidado: 'e' como constante en posición primaria ya fue manejado por ParsearIdentificador.
                // Aquí entramos sólo si el número arrancó con dígito o punto.
                _pos++;
                if (_pos < _expr.Length && (_expr[_pos] == '+' || _expr[_pos] == '-'))
                    _pos++;
                while (_pos < _expr.Length && char.IsDigit(_expr[_pos]))
                    _pos++;
            }

            var token = _expr[inicio.._pos];
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var valor))
                throw new FormatException($"Expresión inválida: número mal formado '{token}'.");
            return valor;
        }

        private double ParsearIdentificador()
        {
            var inicio = _pos;
            while (_pos < _expr.Length && (char.IsLetterOrDigit(_expr[_pos]) || _expr[_pos] == '_'))
                _pos++;

            var nombre = _expr[inicio.._pos];

            SaltarEspacios();
            if (_pos < _expr.Length && _expr[_pos] == '(')
            {
                _pos++;
                var arg = ParsearExpresion();
                SaltarEspacios();
                if (!Coincide(')'))
                    throw new FormatException($"Expresión inválida: falta ')' tras función '{nombre}'.");
                _pos++;
                return AplicarFuncion(nombre, arg);
            }

            return ResolverIdentificador(nombre);
        }

        private double ResolverIdentificador(string nombre)
        {
            switch (nombre)
            {
                case "pi": return Math.PI;
                case "e": return Math.E;
            }

            if (_vars.TryGetValue(nombre, out var valor))
                return valor;

            throw new FormatException($"Expresión inválida: variable desconocida '{nombre}'.");
        }

        private static double AplicarFuncion(string nombre, double arg) => nombre switch
        {
            "sin" => Math.Sin(arg),
            "cos" => Math.Cos(arg),
            "tan" => Math.Tan(arg),
            "exp" => Math.Exp(arg),
            "log" => Math.Log(arg),
            "sqrt" => Math.Sqrt(arg),
            "abs" => Math.Abs(arg),
            _ => throw new FormatException($"Expresión inválida: función desconocida '{nombre}'.")
        };

        private bool Coincide(char c) => _pos < _expr.Length && _expr[_pos] == c;

        private void SaltarEspacios()
        {
            while (_pos < _expr.Length && char.IsWhiteSpace(_expr[_pos]))
                _pos++;
        }
    }
}
