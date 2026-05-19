using System.Globalization;

namespace final_para.Servicios;

/// <summary>
/// Evaluador mínimo de expresiones matemáticas. Soporta literales numéricos,
/// variables (x, y, t, ...), operadores + - * / ^, paréntesis y las funciones
/// sin, cos, tan, exp, log, sqrt, abs y las constantes pi, e.
///
/// Implementación: parser recursivo descendente sin dependencias externas.
/// Es un stub MÍNIMO añadido como parte de Unit 6 (FEM Dirichlet lifting);
/// el evaluador definitivo lo entrega Unit 1.
/// </summary>
public static class EvaluadorExpresion
{
    /// <summary>
    /// Evalúa <paramref name="expresion"/> sustituyendo variables por sus valores.
    /// Si la expresión es null o vacía, devuelve 0.
    /// </summary>
    public static double Evaluar(string expresion, IReadOnlyDictionary<string, double> variables)
    {
        if (string.IsNullOrWhiteSpace(expresion)) return 0.0;
        var parser = new Parser(expresion, variables);
        double valor = parser.ParsearExpresion();
        parser.AsegurarFin();
        return valor;
    }

    private sealed class Parser
    {
        private readonly string _src;
        private readonly IReadOnlyDictionary<string, double> _vars;
        private int _pos;

        public Parser(string src, IReadOnlyDictionary<string, double> vars)
        {
            _src = src;
            _vars = vars;
            _pos = 0;
        }

        public double ParsearExpresion() => ParsearSuma();

        public void AsegurarFin()
        {
            SaltarEspacios();
            if (_pos != _src.Length)
                throw new FormatException($"Caracter inesperado '{_src[_pos]}' en posición {_pos}");
        }

        private double ParsearSuma()
        {
            double izq = ParsearProducto();
            while (true)
            {
                SaltarEspacios();
                if (Coincide('+')) izq += ParsearProducto();
                else if (Coincide('-')) izq -= ParsearProducto();
                else break;
            }
            return izq;
        }

        private double ParsearProducto()
        {
            double izq = ParsearPotencia();
            while (true)
            {
                SaltarEspacios();
                if (Coincide('*')) izq *= ParsearPotencia();
                else if (Coincide('/')) izq /= ParsearPotencia();
                else break;
            }
            return izq;
        }

        private double ParsearPotencia()
        {
            double izq = ParsearUnario();
            SaltarEspacios();
            if (Coincide('^'))
            {
                double exp = ParsearPotencia();
                return Math.Pow(izq, exp);
            }
            return izq;
        }

        private double ParsearUnario()
        {
            SaltarEspacios();
            if (Coincide('+')) return ParsearUnario();
            if (Coincide('-')) return -ParsearUnario();
            return ParsearAtomo();
        }

        private double ParsearAtomo()
        {
            SaltarEspacios();
            if (_pos >= _src.Length)
                throw new FormatException("Expresión truncada");

            char c = _src[_pos];

            if (c == '(')
            {
                _pos++;
                double v = ParsearSuma();
                SaltarEspacios();
                if (!Coincide(')')) throw new FormatException("Falta ')'");
                return v;
            }

            if (char.IsDigit(c) || c == '.') return ParsearNumero();

            if (char.IsLetter(c) || c == '_') return ParsearIdentificador();

            throw new FormatException($"Caracter inesperado '{c}' en posición {_pos}");
        }

        private double ParsearNumero()
        {
            int inicio = _pos;
            while (_pos < _src.Length && (char.IsDigit(_src[_pos]) || _src[_pos] == '.'))
                _pos++;
            if (_pos < _src.Length && (_src[_pos] == 'e' || _src[_pos] == 'E'))
            {
                _pos++;
                if (_pos < _src.Length && (_src[_pos] == '+' || _src[_pos] == '-')) _pos++;
                while (_pos < _src.Length && char.IsDigit(_src[_pos])) _pos++;
            }
            string lit = _src.Substring(inicio, _pos - inicio);
            return double.Parse(lit, CultureInfo.InvariantCulture);
        }

        private double ParsearIdentificador()
        {
            int inicio = _pos;
            while (_pos < _src.Length && (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_'))
                _pos++;
            string nombre = _src.Substring(inicio, _pos - inicio);

            SaltarEspacios();
            if (_pos < _src.Length && _src[_pos] == '(')
            {
                _pos++;
                double arg = ParsearSuma();
                SaltarEspacios();
                if (!Coincide(')')) throw new FormatException("Falta ')' tras función");
                return AplicarFuncion(nombre, arg);
            }

            return nombre.ToLowerInvariant() switch
            {
                "pi" => Math.PI,
                "e" => Math.E,
                _ => _vars.TryGetValue(nombre, out double v)
                    ? v
                    : throw new FormatException($"Variable desconocida '{nombre}'"),
            };
        }

        private static double AplicarFuncion(string nombre, double arg) => nombre.ToLowerInvariant() switch
        {
            "sin" => Math.Sin(arg),
            "cos" => Math.Cos(arg),
            "tan" => Math.Tan(arg),
            "exp" => Math.Exp(arg),
            "log" => Math.Log(arg),
            "ln" => Math.Log(arg),
            "sqrt" => Math.Sqrt(arg),
            "abs" => Math.Abs(arg),
            _ => throw new FormatException($"Función desconocida '{nombre}'"),
        };

        private bool Coincide(char c)
        {
            SaltarEspacios();
            if (_pos < _src.Length && _src[_pos] == c)
            {
                _pos++;
                return true;
            }
            return false;
        }

        private void SaltarEspacios()
        {
            while (_pos < _src.Length && char.IsWhiteSpace(_src[_pos])) _pos++;
        }
    }
}
