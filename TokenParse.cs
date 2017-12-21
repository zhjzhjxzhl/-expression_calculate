using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

/// <summary>
/// <para>Author: zhaojun zhjzhjxzhl@163.com</para>
/// <para>Date: $time$</para>
/// <para>$Id: TokenParse.cs 6294 2014-09-19 07:33:18Z zhaojun $</para>
/// </summary>
public class TokenParse{
    /// <summary>
    /// 接受字符串并计算一个返回的值 3+0.5*{level}+floor(5*{a.b})*ceil(23*{e})
    /// 支持属性获取.{属性名},data的属性及字段
    /// 支持符号：+-*/^（次方)
    /// 支持数学函数:floor,ceil
    /// </summary>
    /// <param name="raw"></param>
    /// <param name="data"></param>
    /// <param name="datatype"></param>
    /// <returns></returns>
    public static float parseT(string raw,object data)
    {
        int i = 0;
        Stack<Operator> ops = new Stack<Operator>();
        ops.Push(null);
        StringBuilder sb = new StringBuilder();
        int m = 0;
        List<object> result = new List<object>();
        while(i<raw.Length)
        {
            m++;

            if(m>100)
            {
                break;
            }
            bool isOp = false;
            Operator o=null;
            switch(raw[i])
            {
                case '{':
                    //属性获取
                    float p = readPre(raw, data, ref i);
                    sb.Append(p);
                    result.Add(p);
                    break;
                case '+':
                case '-':
                    if (i == 0 || charIsOp(raw[i - 1]))
                    {
                        //数字
                        float n1 = readNum(raw, data, ref i);
                        sb.Append(n1);
                        result.Add(n1);
                    }
                    else
                    {
                        //运算符
                        string op1 = raw[i].ToString();
                        i++;
                        o = new Operator(op1);
                        isOp = true;
                    }
                    break;
                case '*':
                case '/':
                case '^':
                case '(':
                case ')':
                    //运算符
                    string op = raw[i].ToString();
                    i++;
                    o = new Operator(op);
                    isOp = true;
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    //数字
                    float n = readNum(raw, data,  ref i);
                    sb.Append(n);
                    result.Add(n);
                    break;
                case 'f':
                    o = new Operator("floor");
                    i += 5;
                    isOp = true;
                    break;
                case 'c':
                    //floor,ceil
                    o = new Operator("ceil");
                    i += 4;
                    isOp = true;
                    break;
                default:
                    break;
            }
            if(isOp)
            {
                Operator curr = ops.Peek();
                if(curr != null)
                {
                    if (curr.ops == "(" && o.ops != ")")
                    {
                        ops.Push(o);
                    }
                    else
                    {
                        if (o.ops != ")")
                        {
                            while (curr > o)
                            {
                                Operator oo = ops.Pop();
                                sb.Append(oo.ops);
                                result.Add(oo);
                                curr = ops.Peek();
                                if(curr == null)
                                {
                                    break;
                                }
                                if(curr.ops == "(")
                                {
                                    break;
                                }
                            }
                            if (curr == null || curr.ops=="(" || curr < o)
                            {
                                ops.Push(o);
                            }
                            else if (curr.priority == o.priority)
                            {
                                if (curr.ops == "(" && o.ops == ")")
                                {
                                    ops.Pop();
                                }
                                else
                                {
                                    Operator oo = ops.Pop();
                                    sb.Append(oo.ops);
                                    result.Add(oo);
                                    ops.Push(o);
                                }
                            }
                        }
                        else
                        {
                            //向前一直找到左括号为止
                            Operator l = ops.Pop();
                            while(l.ops != "(")
                            {
                                sb.Append(l.ops);
                                result.Add(l);
                                l = ops.Pop();
                            }
                        }
                    }
                }
                else
                {
                    ops.Push(o);
                }
            }
            sb.Append(" ");
        }
        Operator ooo = ops.Pop();
        while(ooo != null)
        {
            sb.Append(ooo.ops);
            result.Add(ooo);
            ooo = ops.Pop();
        }
        //Debug.Log(sb.ToString());
        string ss = "";
        Stack<float> ss1 = new Stack<float>();
        foreach (object o in result)
        {
            if(o is float)
            {
                ss1.Push((float)o);
            }
            else
            {
                Operator op = (Operator)o;
                switch(op.ops)
                {
                    case "+":
                    case "-":
                    case "*":
                    case "/":
                    case "^":
                        float f2 = ss1.Pop();
                        float f1 = ss1.Pop();
                        ss1.Push(op.doOp(f1, f2));
                        break;
                    case "floor":
                    case "ceil":
                        float f3 = ss1.Pop();
                        ss1.Push(op.doOp(f3));
                        break;
                    default:
                        break;
                }
            }
        }
        Debug.Log(ss);
        return ss1.Peek();
    }
    
    private static float readPre(string raw,object data,ref int i)
    {
        float ret = 0;
        int j = i+1;
        while(j<raw.Length)
        {
            if (raw[j] == '}')
                break;
            j++;
        }
        string pp = raw.Substring(i + 1, j - i - 1);
        string[] ps = pp.Split('.');
        object o = data;
        foreach(string p in ps)
        {
            System.Type dataType = o.GetType();
            FieldInfo fi = dataType.GetField(p);
            if(fi!=null)
            {
                o = fi.GetValue(o);
            }else{
                PropertyInfo pi = dataType.GetProperty(p);
                o = pi.GetValue(o,null);
            }
        }
        i = j + 1;
        ret = float.Parse(o.ToString());

        return ret;
    }
    private static float readNum(string raw,object data,ref int i)
    {
        float ret = 0;
        int j = i + 1;
        while(j<raw.Length)
        {
            bool con = false;
            switch(raw[j])
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '.':
                    con = true;
                    break;
                default:
                    con = false;
                    break;
            }
            if(con)
            {
                j++;
            }
            else
            {
                break;
            }
        }
        string pp = raw.Substring(i, j - i);
        i = j;
        ret = float.Parse(pp);
        return ret;
    }

    /// <summary>
    /// 检查一个字符是不是op，这个为了支持 正负号开头的数
    /// 包括 +-*/^(,但是不包括右括号
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public static bool charIsOp(char o)
    {
        return (o=='+'||
            o=='-'||
            o=='*'||
            o=='/'||
            o=='^'||
            o=='(');
    }
}
internal class Operator
{
    public string ops;
    /// <summary>
    /// 优先级，+-优先级为1
    /// */优先级为2
    /// ^优先级为3
    /// (),floor,ceil优先级为4
    /// </summary>
    public int priority;
    public Operator(string ops)
    {
        this.ops = ops;
        if(ops == "+"||ops=="-")
        {
            priority = 1;
        }else if(ops == "*" || ops=="/")
        {
            priority = 2;
        }else if(ops=="^")
        {
            priority = 3;
        }else if(ops == "floor" || ops == "ceil")
        {
            priority = 4;
        }
        else if(ops=="(" || ops==")")
        {
            priority = 5;
        }
        else
        {
            priority = 0;
        }

    }

    public override string ToString()
    {
        return ops;
    }

    public float doOp(float f1,float f2)
    {
        float re = 0;
        switch(ops)
        {
            case "+":
                re = f1 + f2;
                break;
            case "-":
                re = f1 - f2;
                break;
            case "*":
                re = f1 * f2;
                break;
            case "/":
                re = f1 / f2;
                break;
            case "^":
                re = Mathf.Pow(f1,f2);
                break;
        }
        return re;
    }

    public float doOp(float f1)
    {
        float re = 0;
        switch(ops)
        {
            case "floor":
                re = Mathf.Floor(f1);
                break;
            case "ceil":
                re = Mathf.Ceil(f1);
                break;
        }
        return re;
    }

    public static bool operator <(Operator o1,Operator o2)
    {
        return o1.priority < o2.priority;
    }

    public static bool operator >(Operator o1, Operator o2)
    {
        return o1.priority > o2.priority;
    }

}
