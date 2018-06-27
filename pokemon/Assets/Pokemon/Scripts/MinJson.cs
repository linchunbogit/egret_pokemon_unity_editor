using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


/// <summary>
/// This class encodes and decodes JSON strings.
/// Spec. details, see http://www.json.org/
///
/// JSON uses Arrays and Objects. These correspond here to the datatypes IList and IDictionary.
/// All numbers are parsed to doubles.
/// </summary>
public static class MinJson
{
    /// <summary>
    /// Parses the string json into a value
    /// </summary>
    /// <param name="json">A JSON string.</param>
    /// <returns>An List<object>, a Dictionary<string, object>, a double, an integer,a string, null, true, or false</returns>
    //反序列化
    public static object Deserialize(string json)
    {
        // save the string for debug information
        if (json == null)
        {
            return null;
        }
        return Parser.Parse(json);
    }

    //阻止其他类从该类继承
    sealed class Parser : IDisposable
    {
        const string WORD_BREAK = "{}[],:\"";
        public static bool IsWordBreak(char c)
        {
            //     如果 c 是空白，则为 true；否则，为 false;报告指定 Unicode 字符在此字符串中的第一个匹配项的索引。
            return Char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
        }
        enum TOKEN
        {
            NONE,
            CURLY_OPEN,
            CURLY_CLOSE,
            SQUARED_OPEN,
            SQUARED_CLOSE,
            COLON,
            COMMA,
            STRING,
            NUMBER,
            TRUE,
            FALSE,
            NULL
        };
        //     实现从字符串进行读取的 System.IO.TextReader。
        StringReader json;
        Parser(string jsonString)
        {
            json = new StringReader(jsonString);
        }
        public static object Parse(string jsonString)
        {
            using (var instance = new Parser(jsonString))
            {
                return instance.ParseValue();
            }
        }
        //释放
        public void Dispose()
        {
            json.Dispose();
            json = null;
        }
        Dictionary<string, object> ParseObject()
        {
            Dictionary<string, object> table = new Dictionary<string, object>();
            // ditch opening brace
            json.Read();
            // {
            while (true)
            {
                switch (NextToken)
                {
                    case TOKEN.NONE:
                        return null;
                    case TOKEN.COMMA:
                        continue;
                    case TOKEN.CURLY_CLOSE:
                        return table;
                    default:
                        // name
                        string name = ParseString();
                        if (name == null)
                        {
                            return null;
                        }
                        // :
                        if (NextToken != TOKEN.COLON)
                        {
                            return null;
                        }
                        // ditch the colon
                        json.Read();
                        // value
                        table[name] = ParseValue();
                        break;
                }
            }
        }
        List<object> ParseArray()
        {
            List<object> array = new List<object>();
            // ditch opening bracket
            json.Read();
            // [
            bool parsing = true;
            while (parsing)
            {
                TOKEN nextToken = NextToken;
                switch (nextToken)
                {
                    case TOKEN.NONE:
                        return null;
                    case TOKEN.COMMA:
                        continue;
                    case TOKEN.SQUARED_CLOSE:
                        parsing = false;
                        break;
                    default:
                        object value = ParseByToken(nextToken);
                        array.Add(value);
                        break;
                }
            }
            return array;
        }
        object ParseValue()
        {
            TOKEN nextToken = NextToken;
            return ParseByToken(nextToken);
        }
        object ParseByToken(TOKEN token)
        {
            switch (token)
            {
                case TOKEN.STRING:
                    return ParseString();
                case TOKEN.NUMBER:
                    return ParseNumber();
                case TOKEN.CURLY_OPEN:
                    return ParseObject();
                case TOKEN.SQUARED_OPEN:
                    return ParseArray();
                case TOKEN.TRUE:
                    return true;
                case TOKEN.FALSE:
                    return false;
                case TOKEN.NULL:
                    return null;
                default:
                    return null;
            }
        }
        string ParseString()
        {
            StringBuilder s = new StringBuilder();
            char c;
            // ditch opening quote
            json.Read();
            bool parsing = true;
            while (parsing)
            {
                if (json.Peek() == -1)
                {
                    parsing = false;
                    break;
                }
                c = NextChar;
                switch (c)
                {
                    case '"':
                        parsing = false;
                        break;
                    case '\\':
                        if (json.Peek() == -1)
                        {
                            parsing = false;
                            break;
                        }
                        c = NextChar;
                        switch (c)
                        {
                            case '"':
                            case '\\':
                            case '/':
                                s.Append(c);
                                break;
                            case 'b':
                                s.Append('\b');
                                break;
                            case 'f':
                                s.Append('\f');
                                break;
                            case 'n':
                                s.Append('\n');
                                break;
                            case 'r':
                                s.Append('\r');
                                break;
                            case 't':
                                s.Append('\t');
                                break;
                            case 'u':
                                var hex = new char[4];
                                for (int i = 0; i < 4; i++)
                                {
                                    hex[i] = NextChar;
                                }
                                s.Append((char)Convert.ToInt32(new string(hex), 16));
                                break;
                        }
                        break;
                    default:
                        s.Append(c);
                        break;
                }
            }
            return s.ToString();
        }
        object ParseNumber()
        {
            string number = NextWord;
            // 摘要:
            //     报告指定 Unicode 字符在此字符串中的第一个匹配项的索引。
            //
            // 参数:
            //   value:
            //     要查找的 Unicode 字符。
            //
            // 返回结果:
            //     如果找到该字符，则为 value 的从零开始的索引位置；如果未找到，则为 -1。
            if (number.IndexOf('.') == -1)
            {
                long parsedInt;
                //     将数字的字符串表示形式转换为它的等效 64 位有符号整数。一个指示转换是否成功的返回值。
                Int64.TryParse(number, out parsedInt);
                return parsedInt;
            }
            double parsedDouble;
            Double.TryParse(number, out parsedDouble);
            return parsedDouble;
        }
        //
        void EatWhitespace()
        {
            //指示指定字符串中位于指定位置处的字符是否属于空白类别。
            while (Char.IsWhiteSpace(PeekChar))
            {
                json.Read();
                //摘要:
                //     返回下一个可用的字符，但不使用它。
                //
                // 返回结果:
                //     表示下一个要读取的字符的整数，或者，如果没有更多的可用字符或该流不支持查找，则为 -1。
                if (json.Peek() == -1)
                {
                    break;
                }
            }
        }
        char PeekChar
        {
            get
            {
                //     读取输入字符串中的下一个字符并将该字符的位置提升一个字符。
                //
                // 返回结果:
                //     基础字符串中的下一个字符，或者如果没有更多的可用字符，则为 -1。
                return Convert.ToChar(json.Peek());
            }
        }
        char NextChar
        {
            get
            {
                return Convert.ToChar(json.Read());
            }
        }
        string NextWord
        {
            get
            {
                //     表示可变字符字符串。无法继承此类。
                StringBuilder word = new StringBuilder();
                while (!IsWordBreak(PeekChar))
                {
                    // 摘要:
                    //     在此实例的结尾追加指定 Unicode 字符的字符串表示形式。
                    //
                    // 参数:
                    //   value:
                    //     要追加的 Unicode 字符。
                    //
                    // 返回结果:
                    //     完成追加操作后对此实例的引用。
                    word.Append(NextChar);
                    //下一个字符为空
                    if (json.Peek() == -1)
                    {
                        break;
                    }
                }
                //
                return word.ToString();
            }
        }
        TOKEN NextToken
        {
            get
            {
                EatWhitespace();
                if (json.Peek() == -1)
                {
                    return TOKEN.NONE;
                }
                switch (PeekChar)
                {
                    case '{':
                        return TOKEN.CURLY_OPEN;
                    case '}':
                        json.Read();
                        return TOKEN.CURLY_CLOSE;
                    case '[':
                        return TOKEN.SQUARED_OPEN;
                    case ']':
                        json.Read();
                        return TOKEN.SQUARED_CLOSE;
                    case ',':
                        json.Read();
                        return TOKEN.COMMA;
                    case '"':
                        return TOKEN.STRING;
                    case ':':
                        return TOKEN.COLON;
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
                    case '-':
                        return TOKEN.NUMBER;
                }
                switch (NextWord)
                {
                    case "false":
                        return TOKEN.FALSE;
                    case "true":
                        return TOKEN.TRUE;
                    case "null":
                        return TOKEN.NULL;
                }
                return TOKEN.NONE;
            }
        }
    }
    /// <summary>
    /// Converts a IDictionary / IList object or a simple type (string, int, etc.) into a JSON string
    /// </summary>
    /// <param name="json">A Dictionary<string, object> / List<object></param>
    /// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
    public static string Serialize(object obj)
    {
        return Serializer.Serialize(obj);
    }
    sealed class Serializer
    {
        StringBuilder builder;
        Serializer()
        {
            //创建生成器
            builder = new StringBuilder();
        }
        //序列化
        public static string Serialize(object obj)
        {
            var instance = new Serializer();
            instance.SerializeValue(obj);
            return instance.builder.ToString();
        }
        //类型
        void SerializeValue(object value)
        {
            IList asList;
            IDictionary asDict;
            string asStr;
            if (value == null)
            {
                builder.Append("null");
            }
            else if ((asStr = value as string) != null)
            {
                SerializeString(asStr);
            }
            else if (value is bool)
            {
                builder.Append((bool)value ? "true" : "false");
            }
            else if ((asList = value as IList) != null)
            {
                SerializeArray(asList);
            }
            else if ((asDict = value as IDictionary) != null)
            {
                SerializeObject(asDict);
            }
            else if (value is char)
            {
                SerializeString(new string((char)value, 1));
            }
            else
            {
                SerializeOther(value);
            }
        }
        //序列化对象
        void SerializeObject(IDictionary obj)
        {
            bool first = true;
            builder.Append('{');
            foreach (object e in obj.Keys)
            {
                if (!first)
                {
                    builder.Append(',');
                }
                SerializeString(e.ToString());
                builder.Append(':');
                SerializeValue(obj[e]);
                first = false;
            }
            builder.Append('}');
        }
        // 序列化数组
        void SerializeArray(IList anArray)
        {
            builder.Append('[');
            bool first = true;
            foreach (object obj in anArray)
            {
                if (!first)
                {
                    builder.Append(',');
                }
                SerializeValue(obj);
                first = false;
            }
            builder.Append(']');
        }
        //string
        void SerializeString(string str)
        {
            builder.Append('\"');
            char[] charArray = str.ToCharArray();
            foreach (var c in charArray)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        int codepoint = Convert.ToInt32(c);
                        if ((codepoint >= 32) && (codepoint <= 126))
                        {
                            builder.Append(c);
                        }
                        else
                        {
                            builder.Append("\\u");
                            builder.Append(codepoint.ToString("x4"));
                        }
                        break;
                }
            }
            builder.Append('\"');
        }
        //其他
        void SerializeOther(object value)
        {
            // NOTE: decimals lose precision during serialization.
            // They always have, I'm just letting you know.
            // Previously floats and doubles lost precision too.
            //注意：小数在序列化过程中丢失精度。
            //他们总是有，我只是让你知道。
            //以前失去精度和双精度浮点数。
            if (value is float)
            {
                builder.Append(((float)value).ToString("R"));
            }
            else if (value is int
              || value is uint
              || value is long
              || value is sbyte
              || value is byte
              || value is short
              || value is ushort
              || value is ulong)
            {
                builder.Append(value);
            }
            else if (value is double
              || value is decimal)
            {
                builder.Append(Convert.ToDouble(value).ToString("R"));
            }
            else
            {
                SerializeString(value.ToString());
            }
        }
    }
}

public class Deserializer
{

    /// <summary>
    /// A Class used to direct which class to make when it's not obvious, like if you have a class
    /// with a member that's a base class but the actual class could be one of many derived classes.
    /// </summary>
    public abstract class CustomCreator
    {

        /// <summary>
        /// Creates an new derived class when a base is expected
        /// </summary>
        /// <param name="src">A dictionary of the json fields that belong to the object to be created.</param>
        /// <param name="parentSrc">A dictionary of the json fields that belong to the object that is the parent of the object to be created.</param>
        /// <example>
        /// Example: Assume you have the following classes
        /// <code>
        ///     class Fruit { public int type; }
        ///     class Apple : Fruit { public float height; public float radius; };
        ///     class Raspberry : Fruit { public int numBulbs; }
        /// </code>
        /// You'd register a dervied CustomCreator for type `Fruit`. When the Deserialize needs to create
        /// a `Fruit` it will call your Create function. Using `src` you could look at `type` and
        /// decide whether to make an Apple or a Raspberry.
        /// <code>
        ///     int type = src["type"];
        ///     if (type == 0) { return new Apple; }
        ///     if (type == 1) { return new Raspberry; }
        ///     ..
        /// </code>
        /// If the parent has info on the type you can do this
        /// <code>
        ///     class Fruit { }
        ///     class Apple : Fruit { public float height; public float radius; };
        ///     class Raspberry : Fruit { public int numBulbs; }
        ///     class Basket { public int typeInBasket; Fruit fruit; }
        /// </code>
        /// In this case again, when trying to create a `Fruit` your CustomCreator.Create function
        /// will be called. You can use `'parentSrc`' to look at the fields from 'Basket' as in
        /// <code>
        ///     int typeInBasket = parentSrc['typeInBasket'];
        ///     if (type == 0) { return new Apple; }
        ///     if (type == 1) { return new Raspberry; }
        ///     ..
        /// </code>
        /// </example>
        /// <returns>The created object</returns>
        public abstract object Create(Dictionary<string, object> src, Dictionary<string, object> parentSrc);

        /// <summary>
        /// The base type this CustomCreator makes.
        /// </summary>
        /// <returns>The type this CustomCreator makes.</returns>
        public abstract System.Type TypeToCreate();
    }

    /// <summary>
    /// Deserializer for Json to your classes.
    /// </summary>
    public Deserializer()
    {
        m_creators = new Dictionary<System.Type, CustomCreator>();
    }

    /// <summary>
    /// Deserializes a json string into classes.
    /// </summary>
    /// <param name="json">String containing JSON</param>
    /// <param name="includeTypeInfoForDerviedTypes">Default false</param>
    /// <returns>An instance of class T.</returns>
    /// <example>
    /// <code>
    ///     public class Foo {
    ///         public int num;
    ///         public string name;
    ///         public float weight;
    ///     };
    ///
    ///     public class Bar {
    ///         public int hp;
    ///         public Foo someFoo;
    ///     };
    /// ...
    ///     Deserializer deserializer = new Deserializer();
    ///
    ///     string json = "{\"hp\":123,\"someFoo\":{\"num\":456,\"name\":\"gman\",\"weight\":156.4}}";
    ///
    ///     Bar bar = deserializer.Deserialize<Bar>(json);
    ///
    ///     print("bar.hp: " + bar.hp);
    ///     print("bar.someFoo.num: " + bar.someFoo.num);
    ///     print("bar.someFoo.name: " + bar.someFoo.name);
    ///     print("bar.someFoo.weight: " + bar.someFoo.weight);
    ///
    /// </code>
    /// </example>
    public static T Deserialize<T>(string json)
    {
        Deserializer dSer = new Deserializer();
        object o = MinJson.Deserialize(json);

        return dSer.Deserialize<T>(o);
    }

    public T Deserialize<T>(object o)
    {
        return (T)ConvertToType(o, typeof(T), null);
    }

    /// <summary>
    /// Registers a CustomCreator.
    /// </summary>
    /// <param name="creator">The creator to register</param>
    public void RegisterCreator(CustomCreator creator)
    {
        System.Type t = creator.TypeToCreate();
        m_creators[t] = creator;
    }

    private object DeserializeO(Type destType, Dictionary<string, object> src, Dictionary<string, object> parentSrc)
    {
        object dest = null;

        // This seems like a hack but for now maybe it's the right thing?
        // Basically if the thing you want is a Dictionary<stirng, object>
        // Then just give it do you since that's the source. No need
        // to try to copy it.
        if (destType == typeof(Dictionary<string, object>))
        {
            return src;
        }

        // First see if there is a CustomCreator for this type.
        CustomCreator creator;
        if (m_creators.TryGetValue(destType, out creator))
        {
            dest = creator.Create(src, parentSrc);
        }

        if (dest == null)
        {
            // Check if there is a type serialized for this
            object typeNameObject;
            if (src.TryGetValue("$dotNetType", out typeNameObject))
            {
                destType = System.Type.GetType((string)typeNameObject);
            }
            dest = Activator.CreateInstance(destType);
        }

        DeserializeIt(dest, src);
        return dest;
    }

    private void DeserializeIt(object dest, Dictionary<string, object> src)
    {
        System.Type type = dest.GetType();
        System.Reflection.FieldInfo[] fields = type.GetFields();

        DeserializeClassFields(dest, fields, src);
    }

    private void DeserializeClassFields(object dest, System.Reflection.FieldInfo[] fields, Dictionary<string, object> src)
    {
        foreach (System.Reflection.FieldInfo info in fields)
        {
            object value;

            if (src.TryGetValue(info.Name, out value))
            {
                DeserializeField(dest, info, value, src);
            }
        }
    }

    private void DeserializeField(object dest, System.Reflection.FieldInfo info, object value, Dictionary<string, object> src)
    {
        Type fieldType = info.FieldType;
        object o = ConvertToType(value, fieldType, src);
        info.SetValue(dest, o);
    }

    private object ConvertToType(object value, System.Type type, Dictionary<string, object> src)
    {
        if (type.IsArray)
        {
            return ConvertToArray(value, type, src);
        }
        else if (type == typeof(string))
        {
            return Convert.ToString(value);
        }
        else if (type == typeof(int))
        {
            return Convert.ToInt32(value);
        }
        else if (type == typeof(float))
        {
            return Convert.ToSingle(value);
        }
        else if (type == typeof(double))
        {
            return Convert.ToDouble(value);
        }
        else if (type == typeof(bool))
        {
            return Convert.ToBoolean(value);
        }
        else if (type == typeof(ulong))
        {
            return Convert.ToUInt64(value);
        }
        else if (type == typeof(long))
        {
            return Convert.ToInt64(value);
        }
        else if (type == typeof(ushort))
        {
            return Convert.ToUInt16(value);
        }
        else if (type == typeof(short))
        {
            return Convert.ToInt16(value);
        }
        else if (type.IsClass)
        {
            return DeserializeO(type, (Dictionary<string, object>)value, src);
        }
        else
        {
            // Should we throw here?
        }
        return value;
    }

    private object ConvertToArray(object value, System.Type type, Dictionary<string, object> src)
    {
        List<object> elements = (List<object>)value;
        int numElements = elements.Count;
        Type elementType = type.GetElementType();
        Array array = Array.CreateInstance(elementType, numElements);
        int index = 0;
        foreach (object elementValue in elements)
        {
            object o = ConvertToType(elementValue, elementType, src);
            array.SetValue(o, index);
            ++index;
        }
        return array;
    }

    private Dictionary<System.Type, CustomCreator> m_creators;
};

public class Serializer
{

    public static string Serialize(object obj, bool includeTypeInfoForDerivedTypes = false)
    {
        Serializer s = new Serializer(includeTypeInfoForDerivedTypes);
        s.SerializeValue(obj);
        return s.GetJson();
    }

    private Serializer(bool includeTypeInfoForDerivedTypes)
    {
        m_builder = new StringBuilder();
        m_includeTypeInfoForDerivedTypes = includeTypeInfoForDerivedTypes;
    }

    private string GetJson()
    {
        return m_builder.ToString();
    }

    private StringBuilder m_builder;
    private bool m_includeTypeInfoForDerivedTypes;

    private void SerializeValue(object obj)
    {
        if (obj == null)
        {
            m_builder.Append("undefined");
            return;
        }
        System.Type type = obj.GetType();

        if (type.IsArray)
        {
            SerializeArray(obj);
        }
        else if (type == typeof(string))
        {
            SerializeString(obj as string);
        }
        else if (type == typeof(int))
        {
            m_builder.Append(obj);
        }
        else if (type == typeof(float))
        {
            m_builder.Append(((float)obj).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (type == typeof(double))
        {
            m_builder.Append(((double)obj).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (type == typeof(bool))
        {
            m_builder.Append((bool)obj ? "true" : "false");
        }
        else if (type == typeof(ulong))
        {
            m_builder.Append((ulong)obj);
        }
        else if (type == typeof(long))
        {
            m_builder.Append((long)obj);
        }
        else if (type == typeof(ushort))
        {
            m_builder.Append((ushort)obj);
        }
        else if (type == typeof(short))
        {
            m_builder.Append((short)obj);
        }
        else if (type.IsClass)
        {
            SerializeObject(obj);
        }
        else
        {
            throw new System.InvalidOperationException("unsupport type: " + type.Name);
        }
    }

    private void SerializeArray(object obj)
    {
        m_builder.Append("[");
        Array array = obj as Array;
        bool first = true;
        foreach (object element in array)
        {
            if (!first)
            {
                m_builder.Append(",");
            }
            SerializeValue(element);
            first = false;
        }
        m_builder.Append("]");
    }

    private void SerializeObject(object obj)
    {
        m_builder.Append("{");
        bool first = true;
        if (m_includeTypeInfoForDerivedTypes)
        {
            // Only inlcude type info for derived types.
            System.Type type = obj.GetType();
            System.Type baseType = type.BaseType;
            if (baseType != null && baseType != typeof(System.Object))
            {
                SerializeString("$dotNetType");  // assuming this won't clash with user's properties.
                m_builder.Append(":");
                SerializeString(type.FullName);
            }
        }
        System.Reflection.FieldInfo[] fields = obj.GetType().GetFields();
        foreach (System.Reflection.FieldInfo info in fields)
        {
            object fieldValue = info.GetValue(obj);
            if (fieldValue != null)
            {
                if (!first)
                {
                    m_builder.Append(",");
                }
                SerializeString(info.Name);
                m_builder.Append(":");
                SerializeValue(fieldValue);
                first = false;
            }
        }
        m_builder.Append("}");
    }

    private void SerializeString(string str)
    {
        m_builder.Append('\"');

        char[] charArray = str.ToCharArray();
        foreach (var c in charArray)
        {
            switch (c)
            {
                case '"':
                    m_builder.Append("\\\"");
                    break;
                case '\\':
                    m_builder.Append("\\\\");
                    break;
                case '\b':
                    m_builder.Append("\\b");
                    break;
                case '\f':
                    m_builder.Append("\\f");
                    break;
                case '\n':
                    m_builder.Append("\\n");
                    break;
                case '\r':
                    m_builder.Append("\\r");
                    break;
                case '\t':
                    m_builder.Append("\\t");
                    break;
                default:
                    int codepoint = Convert.ToInt32(c);
                    if ((codepoint >= 32) && (codepoint <= 126))
                    {
                        m_builder.Append(c);
                    }
                    else
                    {
                        m_builder.Append("\\u");
                        m_builder.Append(codepoint.ToString("x4"));
                    }
                    break;
            }
        }

        m_builder.Append('\"');
    }
}

