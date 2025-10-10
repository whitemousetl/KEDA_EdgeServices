using DynamicExpresso;

namespace KEDA_EdgeServices.Helpers;

public static class ExpressionHelper
{
    // 计算表达式，x为变量
    public static double Eval(string expr, double x)
    {
        var interpreter = new Interpreter();
        return Convert.ToDouble(interpreter.SetVariable("x", x).Eval(expr));
    }

    // 反解一元一次表达式，支持 x*a+b 或 x/a+b
    public static double InverseEval(string expr, double y)
    {
        // 只支持 x*a+b 或 x/a+b 形式
        // 例如: x*0.02+1  => x = (y-1)/0.02
        //      x/0.02+1  => x = (y-1)*0.02
        //      x+2       => x = y-2
        //      x*0.01    => x = y/0.01
        //      x/0.01    => x = y*0.01
        try
        {
            expr = expr.Replace(" ", "");
            if (expr.StartsWith("x*"))
            {
                var parts = expr.Substring(2).Split('+');
                double a = double.Parse(parts[0]);
                double b = parts.Length > 1 ? double.Parse(parts[1]) : 0;
                return (y - b) / a;
            }
            else if (expr.StartsWith("x/"))
            {
                var parts = expr.Substring(2).Split('+');
                double a = double.Parse(parts[0]);
                double b = parts.Length > 1 ? double.Parse(parts[1]) : 0;
                return (y - b) * a;
            }
            else if (expr.StartsWith("x+"))
            {
                double b = double.Parse(expr.Substring(2));
                return y - b;
            }
            else if (expr.StartsWith("x-"))
            {
                double b = double.Parse(expr.Substring(2));
                return y + b;
            }
            else if (expr == "x")
            {
                return y;
            }
        }
        catch (FormatException)
        {
            throw new NotSupportedException("只支持简单一元一次表达式反解");
        }
        throw new NotSupportedException("只支持简单一元一次表达式反解");
    }

    public static bool IsNumericType(object value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }
}

